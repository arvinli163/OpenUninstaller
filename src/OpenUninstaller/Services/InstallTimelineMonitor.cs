using OpenUninstaller.Models;

namespace OpenUninstaller.Services;

/// <summary>Combines user-mode file notifications and registry notifications into one event stream.</summary>
public sealed class InstallTimelineMonitor : IDisposable
{
 private readonly List<FileSystemWatcher> watchers=new();
 private readonly RegistryChangeMonitor registry=new();
 public event Action<TimelineEvent>? EventReceived;
 private static readonly string[] FileRoots =
 {
  Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
  Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
  Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
  Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
  Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
  Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
  Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
 };
 public void Start()
 {
  registry.EventReceived+=Forward; registry.Start();
  foreach(var root in FileRoots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
  {
   try
   {
    var w=new FileSystemWatcher(root){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.Size,InternalBufferSize=65536,EnableRaisingEvents=true};
    w.Created+=(_,e)=>Send("文件系统","创建",e.FullPath);
    w.Changed+=(_,e)=>Send("文件系统","修改",e.FullPath);
    w.Deleted+=(_,e)=>Send("文件系统","删除",e.FullPath);
    w.Renamed+=(_,e)=>Send("文件系统","重命名",e.OldFullPath+" -> "+e.FullPath);
    w.Error+=(_,e)=>Send("监控","文件监控丢失/缓冲区溢出",e.GetException().Message);
    watchers.Add(w);
   }catch(Exception ex){Send("监控","无法启动文件监控",root+": "+ex.Message);}
  }
  // Service installation/configuration is represented by changes below the Services registry tree.
 }
 private void Send(string category,string action,string target)=>Forward(new TimelineEvent{Timestamp=DateTime.Now,Category=category,Action=action,Target=target,Source="FileSystemWatcher"});
 private void Forward(TimelineEvent e)
 {
  if(e.Target.Contains(@"CurrentControlSet\Services",StringComparison.OrdinalIgnoreCase))
   e.Category="服务/注册表";
  EventReceived?.Invoke(e);
 }
 public void Dispose(){foreach(var w in watchers)w.Dispose();watchers.Clear();registry.EventReceived-=Forward;registry.Dispose();}
}
