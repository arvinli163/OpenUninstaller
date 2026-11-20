using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using OpenUninstaller.Models;

namespace OpenUninstaller.Services;

/// <summary>User-mode registry change notifications. Watches selected high-value trees recursively.</summary>
public sealed class RegistryChangeMonitor : IDisposable
{
 private const int NotifyName=1, NotifyAttributes=2, NotifyLastSet=4, NotifySecurity=8;
 private const int RegNotifyThreadAgnostic=0x10000000;
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode)]
 private static extern int RegNotifyChangeKeyValue(SafeRegistryHandle hKey,bool watchSubtree,int notifyFilter,SafeWaitHandle hEvent,bool asynchronous);
 private readonly CancellationTokenSource cts=new();
 private readonly List<Task> tasks=new();
 public event Action<TimelineEvent>? EventReceived;
 private static readonly (RegistryHive Hive,string Path)[] Targets =
 {
  (RegistryHive.LocalMachine,@"SOFTWARE"),
  (RegistryHive.CurrentUser,@"SOFTWARE"),
  (RegistryHive.LocalMachine,@"SYSTEM\CurrentControlSet\Services"),
  (RegistryHive.CurrentUser,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
  (RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
  (RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
  (RegistryHive.CurrentUser,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
 };
 public void Start()
 {
  foreach(var (hive,path) in Targets)
   foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
   {
    try
    {
     RegistryKey? key;
     using(var baseKey=RegistryKey.OpenBaseKey(hive,view)) key=baseKey.OpenSubKey(path,false);
     if(key==null)continue;
     tasks.Add(Task.Factory.StartNew(()=>Watch(key,$"{hive}\\{view}\\{path}"),cts.Token,TaskCreationOptions.LongRunning,TaskScheduler.Default));
    }catch{}
   }
 }
 private void Watch(RegistryKey key,string label)
 {
  using(key)
  using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset))
  {
   while(!cts.IsCancellationRequested)
   {
    try
    {
     int rc=RegNotifyChangeKeyValue(key.Handle as SafeRegistryHandle ?? throw new InvalidOperationException("Registry handle unavailable"),true,NotifyName|NotifyAttributes|NotifyLastSet|NotifySecurity,signal.SafeWaitHandle,true);
     if(rc!=0)throw new System.ComponentModel.Win32Exception(rc);
     var result=WaitHandle.WaitAny(new WaitHandle[]{signal,cts.Token.WaitHandle});
     if(result==1)break;
     EventReceived?.Invoke(new TimelineEvent{Timestamp=DateTime.Now,Category="注册表",Action="键/值变化",Target=label,Details="监控树发生变化（Windows 通知不提供具体值差异）",Source="RegistryNotifyChangeKeyValue"});
    }catch(ObjectDisposedException){break;}catch(Exception ex){EventReceived?.Invoke(new TimelineEvent{Category="监控",Action="注册表监控错误",Target=label,Details=ex.Message});break;}
   }
  }
 }
 public void Dispose(){cts.Cancel();try{Task.WaitAll(tasks.ToArray(),1500);}catch{}cts.Dispose();}
}
