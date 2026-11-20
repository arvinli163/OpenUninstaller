using System.IO;
using Microsoft.Win32;
using OpenUninstaller.Models;
namespace OpenUninstaller.Services;
/// <summary>MVP snapshot monitor. Detects persistent changes in selected roots; not a complete kernel-level recorder.</summary>
public sealed class SnapshotService
{
 static readonly string[] Roots={Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)};
 const string Uninstall=@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
 public async Task<Dictionary<string,string>> CaptureAsync(IProgress<string>? progress=null)=>await Task.Run(()=>{
  var d=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  foreach(var root in Roots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase)){
   progress?.Report("扫描目录 "+root);
   try{
    foreach(var f in Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories))try{var x=new FileInfo(f);d["FILE|"+f]=$"{x.Length}|{x.LastWriteTimeUtc.Ticks}";}catch{}
    foreach(var f in Directory.EnumerateDirectories(root,"*",SearchOption.AllDirectories))d["DIR|"+f]="directory";
   }catch{}
  }
  foreach(var h in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var v in new[]{RegistryView.Registry64,RegistryView.Registry32})try{
   using var b=RegistryKey.OpenBaseKey(h,v);WalkRoot(b,Uninstall,$"{h}|{v}|",d);WalkRoot(b,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",$"{h}|{v}|",d);WalkRoot(b,@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",$"{h}|{v}|",d);
  }catch{}
  return d;
 });
 static void WalkRoot(RegistryKey b,string path,string prefix,Dictionary<string,string>d){using var k=b.OpenSubKey(path);if(k!=null)Walk(k,path,prefix,d);}
 static void Walk(RegistryKey k,string path,string prefix,Dictionary<string,string>d){
  foreach(var n in k.GetValueNames())try{var val=k.GetValue(n,null,RegistryValueOptions.DoNotExpandEnvironmentNames);d["REG|"+prefix+path+"|"+n]=$"{k.GetValueKind(n)}|{val}";}catch{}
  foreach(var n in k.GetSubKeyNames())try{using var c=k.OpenSubKey(n);if(c!=null)Walk(c,path+"\\"+n,prefix,d);}catch{}
 }
 public static List<ChangeItem> Compare(Dictionary<string,string>a,Dictionary<string,string>b){
  var r=new List<ChangeItem>();
  foreach(var k in b.Keys){var kind=k.StartsWith("REG|")?"Registry":"FileSystem";var path=k[(k.IndexOf('|')+1)..];
   if(!a.TryGetValue(k,out var old))r.Add(new(){Kind=kind,Path=path,ChangeType="Added",AfterValue=b[k]});
   else if(old!=b[k])r.Add(new(){Kind=kind,Path=path,ChangeType="Modified",BeforeValue=old,AfterValue=b[k]});}
  foreach(var k in a.Keys)if(!b.ContainsKey(k))r.Add(new(){Kind=k.StartsWith("REG|")?"Registry":"FileSystem",Path=k[(k.IndexOf('|')+1)..],ChangeType="Removed",BeforeValue=a[k]});
  return r;
 }
}
