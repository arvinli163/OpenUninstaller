using Microsoft.Win32;
using OpenUninstaller.Models;
namespace OpenUninstaller.Services;
public sealed class SoftwareScanner
{
 const string PathPart = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
 public List<InstalledApp> Scan()
 {
  var found = new Dictionary<string,InstalledApp>(StringComparer.OrdinalIgnoreCase);
  ScanHive(RegistryHive.LocalMachine,RegistryView.Registry64,"HKLM 64-bit",found);
  ScanHive(RegistryHive.LocalMachine,RegistryView.Registry32,"HKLM 32-bit",found);
  ScanHive(RegistryHive.CurrentUser,RegistryView.Default,"HKCU",found);
  return found.Values.OrderBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
 }
 static void ScanHive(RegistryHive hive,RegistryView view,string arch,Dictionary<string,InstalledApp> found)
 {
  try {
   using var baseKey=RegistryKey.OpenBaseKey(hive,view); using var root=baseKey.OpenSubKey(PathPart);
   if(root==null)return;
   foreach(var sub in root.GetSubKeyNames()) try {
    using var key=root.OpenSubKey(sub); if(key==null)continue;
    var name=key.GetValue("DisplayName") as string;
    if(string.IsNullOrWhiteSpace(name))continue;
    if(Convert.ToInt32(key.GetValue("SystemComponent")??0)==1 || Convert.ToInt32(key.GetValue("NoDisplay")??0)==1)continue;
    var app=new InstalledApp {Name=name.Trim(),Version=key.GetValue("DisplayVersion") as string,
     Publisher=key.GetValue("Publisher") as string,InstallDate=key.GetValue("InstallDate") as string,
     InstallLocation=key.GetValue("InstallLocation") as string,UninstallString=key.GetValue("UninstallString") as string,
     QuietUninstallString=key.GetValue("QuietUninstallString") as string,
     RegistryKeyPath=$@"{hive}\{PathPart}\{sub}",Architecture=arch};
    found.TryAdd($"{app.Name}|{app.Publisher}|{app.Version}",app);
   } catch {}
  } catch {}
 }
}
