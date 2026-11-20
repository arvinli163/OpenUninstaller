using Microsoft.Data.Sqlite;
using OpenUninstaller.Models;
namespace OpenUninstaller.Services;
public sealed class DatabaseService
{
 readonly string cs;
 public DatabaseService()
 {
  var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenUninstaller");
  Directory.CreateDirectory(dir);cs=new SqliteConnectionStringBuilder{DataSource=Path.Combine(dir,"openuninstaller.db")}.ToString();
  using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
  cmd.CommandText="""
  CREATE TABLE IF NOT EXISTS MonitorSessions(Id INTEGER PRIMARY KEY AUTOINCREMENT,Name TEXT,StartedAt TEXT,FinishedAt TEXT,Status TEXT,Notes TEXT);
  CREATE TABLE IF NOT EXISTS MonitorChanges(Id INTEGER PRIMARY KEY AUTOINCREMENT,SessionId INTEGER,Kind TEXT,Path TEXT,ChangeType TEXT,BeforeValue TEXT,AfterValue TEXT);
  CREATE TABLE IF NOT EXISTS TimelineEvents(Id INTEGER PRIMARY KEY AUTOINCREMENT,SessionId INTEGER,Timestamp TEXT,Category TEXT,Action TEXT,Target TEXT,Details TEXT,Source TEXT);
  """;cmd.ExecuteNonQuery();
 }
 public long NewSession(string name)
 {
  using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
  cmd.CommandText="INSERT INTO MonitorSessions(Name,StartedAt,Status) VALUES($n,$d,'Recording'); SELECT last_insert_rowid();";
  cmd.Parameters.AddWithValue("$n",name);cmd.Parameters.AddWithValue("$d",DateTime.Now.ToString("O"));return (long)cmd.ExecuteScalar()!;
 }
 public void AddTimelineEvent(long sessionId,TimelineEvent e)
 {
  using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
  cmd.CommandText="INSERT INTO TimelineEvents(SessionId,Timestamp,Category,Action,Target,Details,Source) VALUES($s,$t,$c,$a,$p,$d,$o)";
  cmd.Parameters.AddWithValue("$s",sessionId);cmd.Parameters.AddWithValue("$t",e.Timestamp.ToString("O"));cmd.Parameters.AddWithValue("$c",e.Category);
  cmd.Parameters.AddWithValue("$a",e.Action);cmd.Parameters.AddWithValue("$p",e.Target);cmd.Parameters.AddWithValue("$d",(object?)e.Details??DBNull.Value);cmd.Parameters.AddWithValue("$o",e.Source);cmd.ExecuteNonQuery();
 }
 public List<TimelineEvent> ReadTimeline(long sessionId)
 {
  var result=new List<TimelineEvent>();using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
  cmd.CommandText="SELECT Id,SessionId,Timestamp,Category,Action,Target,Details,Source FROM TimelineEvents WHERE SessionId=$s ORDER BY Timestamp";cmd.Parameters.AddWithValue("$s",sessionId);
  using var r=cmd.ExecuteReader();while(r.Read())result.Add(new(){Id=r.GetInt64(0),SessionId=r.GetInt64(1),Timestamp=DateTime.Parse(r.GetString(2)),Category=r.GetString(3),Action=r.GetString(4),Target=r.GetString(5),Details=r.IsDBNull(6)?null:r.GetString(6),Source=r.GetString(7)});return result;
 }
 public void FinishSession(long id,string notes)
 {
  using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE MonitorSessions SET FinishedAt=$d,Status='Completed',Notes=$n WHERE Id=$i";
  cmd.Parameters.AddWithValue("$d",DateTime.Now.ToString("O"));cmd.Parameters.AddWithValue("$n",notes);cmd.Parameters.AddWithValue("$i",id);cmd.ExecuteNonQuery();
 }
 public void Save(long id,List<ChangeItem> items)
 {
  using var c=new SqliteConnection(cs);c.Open();using var tx=c.BeginTransaction();
  foreach(var x in items){using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO MonitorChanges(SessionId,Kind,Path,ChangeType,BeforeValue,AfterValue) VALUES($i,$k,$p,$t,$b,$a)";
   cmd.Parameters.AddWithValue("$i",id);cmd.Parameters.AddWithValue("$k",x.Kind);cmd.Parameters.AddWithValue("$p",x.Path);cmd.Parameters.AddWithValue("$t",x.ChangeType);
   cmd.Parameters.AddWithValue("$b",(object?)x.BeforeValue??DBNull.Value);cmd.Parameters.AddWithValue("$a",(object?)x.AfterValue??DBNull.Value);cmd.ExecuteNonQuery();}
  tx.Commit();FinishSession(id,$"Detected {items.Count} persistent snapshot changes");
 }
}
