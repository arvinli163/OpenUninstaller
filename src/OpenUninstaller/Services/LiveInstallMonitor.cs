using System.IO;
namespace OpenUninstaller.Services;
/// <summary>User-mode directory watcher only; not kernel-level and may overflow under heavy activity.</summary>
public sealed class LiveInstallMonitor : IDisposable
{
 readonly List<FileSystemWatcher> watchers=new();
 public event Action<string>? EventReceived;
 public void Start(IEnumerable<string> roots)
 {
  Stop();
  foreach(var root in roots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase)){
   var w=new FileSystemWatcher(root){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.Size,InternalBufferSize=65536,EnableRaisingEvents=true};
   w.Created+=(_,e)=>Send("创建",e.FullPath);w.Changed+=(_,e)=>Send("修改",e.FullPath);w.Deleted+=(_,e)=>Send("删除",e.FullPath);
   w.Renamed+=(_,e)=>Send("重命名",e.OldFullPath+" -> "+e.FullPath);w.Error+=(_,e)=>Send("监控警告",e.GetException().Message);watchers.Add(w);
  }
 }
 void Send(string action,string path)=>EventReceived?.Invoke($"{DateTime.Now:HH:mm:ss.fff} [{action}] {path}");
 public void Stop(){foreach(var w in watchers){w.EnableRaisingEvents=false;w.Dispose();}watchers.Clear();}
 public void Dispose()=>Stop();
}
