using System.IO;
using Microsoft.Win32;
using OpenUninstaller.Models;
using System.Diagnostics;
namespace OpenUninstaller.Services;
/// <summary>Reversible cleanup: quarantine files; export exact uninstall key before removing it.</summary>
public sealed class SafeCleanupService
{
 public string BackupRoot {get;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenUninstaller","Quarantine",DateTime.Now.ToString("yyyyMMdd-HHmmss"));
 public List<string> Execute(IEnumerable<ResidualItem> selected)
 {
  Directory.CreateDirectory(BackupRoot);var done=new List<string>();
  foreach(var x in selected.Where(x=>x.Selected)){
   if(x.Kind is "文件" or "文件夹"){
    var path=Path.GetFullPath(x.Target);if(!File.Exists(path)&&!Directory.Exists(path))continue;
    var protectedRoots=new[]{Environment.GetFolderPath(Environment.SpecialFolder.Windows),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}.Where(p=>!string.IsNullOrWhiteSpace(p));
    if(protectedRoots.Any(p=>path.StartsWith(p,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("拒绝处理受保护系统目录："+path);
    var dest=Path.Combine(BackupRoot,"Files",Guid.NewGuid().ToString("N")+"_"+Path.GetFileName(path));Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
    if(Directory.Exists(path))Directory.Move(path,dest);else File.Move(path,dest);done.Add(dest);
   }else if(x.Kind=="卸载注册表项"){ExportAndRemove(x.Target);done.Add("Registry backup");}
  }return done;
 }
 void ExportAndRemove(string path)
 {
  var marker=@"\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";var i=path.IndexOf(marker,StringComparison.OrdinalIgnoreCase);
  if(i<0)throw new InvalidOperationException("只允许处理明确的卸载注册表项。");
  var hiveName=path[..i];var sub=path[(i+1)..];
  var hive=hiveName switch{"LocalMachine"=>RegistryHive.LocalMachine,"CurrentUser"=>RegistryHive.CurrentUser,_=>throw new InvalidOperationException("不支持的注册表根键。")};
  var regDir=Path.Combine(BackupRoot,"Registry");Directory.CreateDirectory(regDir);var file=Path.Combine(regDir,Guid.NewGuid().ToString("N")+".reg");
  var full=(hive==RegistryHive.LocalMachine?"HKEY_LOCAL_MACHINE\\":"HKEY_CURRENT_USER\\")+sub;
  using var proc=Process.Start(new ProcessStartInfo("reg.exe",$"export \"{full}\" \"{file}\" /y"){UseShellExecute=false,CreateNoWindow=true});
  proc?.WaitForExit();if(proc?.ExitCode!=0||!File.Exists(file))throw new InvalidOperationException("注册表备份失败，未执行删除。");
  using var b=RegistryKey.OpenBaseKey(hive,RegistryView.Default);using var parent=b.OpenSubKey(Path.GetDirectoryName(sub)!.Replace('/','\\'),true);
  var leaf=Path.GetFileName(sub);if(parent==null||string.IsNullOrWhiteSpace(leaf))throw new InvalidOperationException("无法打开注册表项。");
  parent.DeleteSubKeyTree(leaf,false);
 }
}
