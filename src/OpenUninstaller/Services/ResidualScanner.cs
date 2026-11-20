using System.IO;
using Microsoft.Win32;
using OpenUninstaller.Models;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace OpenUninstaller.Services;

/// <summary>Multi-signal residual discovery. Results are ranked candidates, never proof of ownership.</summary>
public sealed class ResidualScanner
{
 private static readonly string[] Roots =
 {
  Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
  Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
  Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
  Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
 };
 private const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

 public List<ResidualItem> Find(InstalledApp app)
 {
  var results = new Dictionary<string,ResidualItem>(StringComparer.OrdinalIgnoreCase);
  var nameTokens=Tokens(app.Name); var publisherTokens=Tokens(app.Publisher);
  var install=Normalize(app.InstallLocation);
  var uninstallExe=ExtractExecutable(app.UninstallString);
  var uninstallDir=Normalize(Path.GetDirectoryName(uninstallExe) ?? "");
  var registryRefs=FindRegistryReferences(app);
  foreach(var root in Roots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
  {
   try
   {
    foreach(var path in Directory.EnumerateFileSystemEntries(root,"*",SearchOption.TopDirectoryOnly))
    {
     var leaf=Path.GetFileName(path); int score=0; var evidence=new List<string>();
     var normalized=Normalize(leaf);
     if(nameTokens.Any(t=>normalized.Contains(t,StringComparison.OrdinalIgnoreCase))){score+=20;evidence.Add("名称匹配");}
     if(publisherTokens.Any(t=>normalized.Contains(t,StringComparison.OrdinalIgnoreCase))){score+=10;evidence.Add("发行商匹配");}
     if(!string.IsNullOrEmpty(install) && IsSameOrChild(path,install)){score+=45;evidence.Add("位于登记安装路径");}
     if(!string.IsNullOrEmpty(uninstallDir) && IsSameOrChild(path,uninstallDir)){score+=35;evidence.Add("与卸载程序位于同一目录");}
     if(File.Exists(path))
     {
      var vi=FileVersionInfo.GetVersionInfo(path);
      if(!string.IsNullOrWhiteSpace(app.Version) && string.Equals(vi.FileVersion,app.Version,StringComparison.OrdinalIgnoreCase)){score+=15;evidence.Add("文件版本一致");}
      if(!string.IsNullOrWhiteSpace(app.Publisher) && !string.IsNullOrWhiteSpace(vi.CompanyName) && vi.CompanyName.Contains(app.Publisher,StringComparison.OrdinalIgnoreCase)){score+=15;evidence.Add("文件公司信息匹配");}
      var signer=GetTrustedSigner(path);
      if(!string.IsNullOrEmpty(signer) && !string.IsNullOrEmpty(app.Publisher) && signer.Contains(app.Publisher,StringComparison.OrdinalIgnoreCase)){score+=20;evidence.Add("签名证书主题匹配");}
     }
     if(registryRefs.Any(r=>r.Contains(path,StringComparison.OrdinalIgnoreCase))){score+=20;evidence.Add("注册表存在路径引用");}
     if(score>=20)
     {
      var kind=Directory.Exists(path)?"文件夹":"文件";
      results[path]=new(){Kind=kind,Target=path,Reason=$"综合关联评分 {score}",Confidence=Math.Min(score,100),Evidence=string.Join("；",evidence),Risk=score>=70?"Medium":"High"};
     }
    }
   }catch(UnauthorizedAccessException){}catch(IOException){}
  }
  if(!string.IsNullOrWhiteSpace(app.RegistryKeyPath))
   results[app.RegistryKeyPath]=new(){Kind="卸载注册表项",Target=app.RegistryKeyPath,Reason="软件原始卸载登记项",Risk="High",Confidence=100,Evidence="精确关联：当前软件登记项"};
  return results.Values.OrderByDescending(x=>x.Confidence).ToList();
 }

 private static List<string> FindRegistryReferences(InstalledApp app)
 {
  var found=new List<string>(); var terms=Tokens(app.Name);
  foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})
  foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
  {
   try
   {
    using var b=RegistryKey.OpenBaseKey(hive,view);
    foreach(var sub in new[]{Uninstall,@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",@"SYSTEM\CurrentControlSet\Services"})
    using(var k=b.OpenSubKey(sub))
    {
     if(k==null)continue;
     foreach(var childName in k.GetSubKeyNames())
     {
      using var child=k.OpenSubKey(childName); if(child==null)continue;
      var vals=child.GetValueNames().Select(n=>child.GetValue(n)?.ToString()??"");
      var joined=string.Join(" ",vals);
      if(terms.Any(t=>Normalize(childName+" "+joined).Contains(t,StringComparison.OrdinalIgnoreCase)))
       found.Add(joined);
     }
    }
   }catch{}
  }
  return found;
 }
 private static string[] Tokens(string? s)=> (s??"").Split(new[]{' ','-','_','.'},StringSplitOptions.RemoveEmptyEntries).Select(Normalize).Where(x=>x.Length>=3).Distinct().ToArray();
 private static string Normalize(string? s)=>new((s??"").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
 private static bool IsSameOrChild(string candidate,string parent)
 { try {var c=Path.GetFullPath(candidate).TrimEnd('\\')+"\\";var p=Path.GetFullPath(parent).TrimEnd('\\')+"\\";return c.StartsWith(p,StringComparison.OrdinalIgnoreCase);}catch{return false;} }
 private static string ExtractExecutable(string? command)
 { if(string.IsNullOrWhiteSpace(command))return "";var s=Environment.ExpandEnvironmentVariables(command.Trim());if(s.StartsWith('"')){var i=s.IndexOf('"',1);return i>1?s[1..i]:"";}var e=s.IndexOf(".exe",StringComparison.OrdinalIgnoreCase);return e>=0?s[..(e+4)]:s.Split(' ')[0];}
 private static string? GetTrustedSigner(string file)
 {
  try
  {
   using var cert=new X509Certificate2(X509Certificate.CreateFromSignedFile(file));
   using var chain=new X509Chain(); chain.ChainPolicy.RevocationMode=X509RevocationMode.NoCheck;
   return chain.Build(cert)?cert.GetNameInfo(X509NameType.SimpleName,false):null;
  }catch{return null;}
 }
}
