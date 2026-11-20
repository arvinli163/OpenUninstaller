using System.IO;
using Microsoft.Data.Sqlite;
using OpenUninstaller.Models;
namespace OpenUninstaller.Services;
public sealed class DatabaseService
{
 readonly string cs;
 public DatabaseService()
 {
  var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenUninstaller");
  Directory.CreateDirectory(dir); cs=new SqliteConnectionStringBuilder{DataSource=Path.Combine(dir,"openuninstaller.db")}.ToString();
  using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
  cmd.CommandText="CREATE TABLE IF NOT EXISTS MonitorSessions(Id INTEGER PRIMARY KEY AUTOINCREMENT,Name TEXT,StartedAt TEXT,FinishedAt TEXT,Status TEXT,Notes TEXT); CREATE TABLE IF NOT EXISTS MonitorChanges(Id INTEGER PRIMARY KEY AUTOINCREMENT,SessionId INTEGER,Kind TEXT,Path TEXT,ChangeType TEXT,BeforeValue TEXT,AfterValue TEXT);";
  cmd.ExecuteNonQuery();
 }
 public long NewSession(string name)
 {
  using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
  cmd.CommandText="INSERT INTO MonitorSessions(Name,StartedAt,Status) VALUES($n,$d,'Recording'); SELECT last_insert_rowid();";
  cmd.Parameters.AddWithValue("$n",name);cmd.Parameters.AddWithValue("$d",DateTime.Now.ToString("O"));return (long)cmd.ExecuteScalar()!;
 }
 public void Save(long id,List<ChangeItem> items)
 {
  using var c=new SqliteConnection(cs);c.Open();using var tx=c.BeginTransaction();
  foreach(var x in items){using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO MonitorChanges(SessionId,Kind,Path,ChangeType,BeforeValue,AfterValue) VALUES($i,$k,$p,$t,$b,$a)";
   cmd.Parameters.AddWithValue("$i",id);cmd.Parameters.AddWithValue("$k",x.Kind);cmd.Parameters.AddWithValue("$p",x.Path);cmd.Parameters.AddWithValue("$t",x.ChangeType);
   cmd.Parameters.AddWithValue("$b",(object?)x.BeforeValue??DBNull.Value);cmd.Parameters.AddWithValue("$a",(object?)x.AfterValue??DBNull.Value);cmd.ExecuteNonQuery();}
  using var finish=c.CreateCommand();finish.Transaction=tx;finish.CommandText="UPDATE MonitorSessions SET FinishedAt=$d,Status='Completed',Notes=$n WHERE Id=$i";
  finish.Parameters.AddWithValue("$d",DateTime.Now.ToString("O"));finish.Parameters.AddWithValue("$n",$"Detected {items.Count} persistent changes");finish.Parameters.AddWithValue("$i",id);finish.ExecuteNonQuery();tx.Commit();
 }
}
