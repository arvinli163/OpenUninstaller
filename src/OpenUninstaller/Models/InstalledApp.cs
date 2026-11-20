namespace OpenUninstaller.Models;
public sealed class InstalledApp
{
 public string Name {get;set;} = ""; public string? Version {get;set;}
 public string? Publisher {get;set;} public string? InstallDate {get;set;}
 public string? InstallLocation {get;set;} public string? UninstallString {get;set;}
 public string? QuietUninstallString {get;set;} public string RegistryKeyPath {get;set;} = "";
 public string Architecture {get;set;} = "";
}
public sealed class ChangeItem
{
 public string Kind {get;set;} = ""; public string Path {get;set;} = "";
 public string ChangeType {get;set;} = ""; public string? BeforeValue {get;set;} public string? AfterValue {get;set;}
}
