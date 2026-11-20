namespace OpenUninstaller.Models;
public sealed class ResidualItem
{
 public bool Selected {get;set;}
 public string Kind {get;set;}="";
 public string Target {get;set;}="";
 public string Reason {get;set;}="";
 public string Risk {get;set;}="Medium";
 public int Confidence {get;set;}
 public string Evidence {get;set;}="";
}
