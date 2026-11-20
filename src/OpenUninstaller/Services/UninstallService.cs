using System.Diagnostics;
using OpenUninstaller.Models;
namespace OpenUninstaller.Services;
public sealed class UninstallService
{
 public Process? Start(InstalledApp app)
 {
  var command=app.UninstallString;
  if(string.IsNullOrWhiteSpace(command))throw new InvalidOperationException("该软件没有登记卸载命令。");
  command=Environment.ExpandEnvironmentVariables(command.Trim());
  string exe,args;
  if(command.StartsWith('"')) { var end=command.IndexOf('"',1); exe=command[1..end]; args=command[(end+1)..].Trim(); }
  else { var i=command.IndexOf(".exe",StringComparison.OrdinalIgnoreCase); if(i>=0){exe=command[..(i+4)].Trim('"');args=command[(i+4)..].Trim();} else {var s=command.IndexOf(' ');exe=s<0?command:command[..s];args=s<0?"":command[(s+1)..];}}
  return Process.Start(new ProcessStartInfo(exe,args){UseShellExecute=true});
 }
}
