namespace OpenUninstaller.Models;
public sealed class TimelineEvent
{
 public long Id {get;set;}
 public long SessionId {get;set;}
 public DateTime Timestamp {get;set;} = DateTime.Now;
 public string Category {get;set;}="";
 public string Action {get;set;}="";
 public string Target {get;set;}="";
 public string? Details {get;set;}
 public string Source {get;set;}="UserMode";
}
