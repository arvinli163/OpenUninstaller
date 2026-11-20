using System.IO;
using Microsoft.Win32;
using OpenUninstaller.Models;
namespace OpenUninstaller.Services;
/// <summary>Conservative candidate discovery. Nothing is deleted automatically.</summary>
public sealed class ResidualScanner
{
 static readonly string[] Roots={Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)};
 public List<ResidualItem> Find(InstalledApp app)
 {
  var list=new List<ResidualItem>();
  var tokens=new[]{app.Name,app.Publisher}.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(Norm).Where(x=>x.Length>=4).Distinct().ToArray();
  foreach(var root in Roots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))try{
   foreach(var d in Directory.EnumerateDirectories(root,"*",SearchOption.TopDirectoryOnly)){var n=Norm(Path.GetFileName(d));if(tokens.Any(t=>n.Contains(t,StringComparison.OrdinalIgnoreCase)))list.Add(new(){Kind="文件夹",Target=d,Reason="目录名与软件名称/发行商匹配",Risk="Medium"});}
   foreach(var f in Directory.EnumerateFiles(root,"*",SearchOption.TopDirectoryOnly)){var n=Norm(Path.GetFileNameWithoutExtension(f));if(tokens.Any(t=>n.Contains(t,StringComparison.OrdinalIgnoreCase)))list.Add(new(){Kind="文件",Target=f,Reason="文件名与软件名称/发行商匹配",Risk="Low"});}
  }catch{}
  if(!string.IsNullOrWhiteSpace(app.RegistryKeyPath))list.Add(new(){Kind="卸载注册表项",Target=app.RegistryKeyPath,Reason="软件原始卸载登记项",Risk="High"});
  return list.GroupBy(x=>x.Target,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
 }
 static string Norm(string s)=>new(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}
