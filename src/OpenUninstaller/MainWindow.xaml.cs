using OpenUninstaller.Models;
using OpenUninstaller.Services;
using System.Windows;
namespace OpenUninstaller;
public partial class MainWindow:Window
{
 readonly SoftwareScanner scanner=new();readonly UninstallService uninstaller=new();readonly SnapshotService snapshots=new();readonly DatabaseService db=new();readonly ResidualScanner residualScanner=new();List<InstalledApp> all=new();
 public MainWindow(){InitializeComponent();Loaded+=async(_,_)=>await RefreshAsync();}
 async Task RefreshAsync(){Busy(true,"扫描软件列表…");try{all=await Task.Run(scanner.Scan);Filter();StatusText.Text="扫描完成";}catch(Exception ex){MessageBox.Show(ex.Message);}finally{Busy(false);}}
 void Filter(){var q=SearchBox.Text.Trim();AppsGrid.ItemsSource=string.IsNullOrEmpty(q)?all:all.Where(x=>$"{x.Name} {x.Publisher} {x.Version}".Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();CountText.Text=$"{AppsGrid.Items.Count} 个软件";}
 void Busy(bool b,string? msg=null){BusyBar.Visibility=b?Visibility.Visible:Visibility.Collapsed;if(msg!=null)StatusText.Text=msg;}
 async void Refresh_Click(object s,RoutedEventArgs e)=>await RefreshAsync();
 void SearchBox_TextChanged(object s,System.Windows.Controls.TextChangedEventArgs e)=>Filter();
 void Uninstall_Click(object s,RoutedEventArgs e){
  var apps=AppsGrid.SelectedItems.Cast<InstalledApp>().ToList();if(apps.Count==0){MessageBox.Show("请先选择软件。");return;}
  if(MessageBox.Show($"将启动 {apps.Count} 个软件自身的卸载程序，是否继续？","确认",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  foreach(var app in apps)try{uninstaller.Start(app);}catch(Exception ex){MessageBox.Show($"{app.Name}\n{ex.Message}","启动失败");}
  StatusText.Text="卸载程序已启动。完成后请刷新列表。";
 }

 void Residual_Click(object s,RoutedEventArgs e)
 {
  if(AppsGrid.SelectedItem is not InstalledApp app){MessageBox.Show("请先选择一个软件。");return;}
  try{var candidates=residualScanner.Find(app);new ResidualWindow(app,candidates){Owner=this}.ShowDialog();}
  catch(Exception ex){MessageBox.Show(ex.Message,"残留扫描失败");}
 }
 void ForceUninstall_Click(object s,RoutedEventArgs e)
 {
  if(AppsGrid.SelectedItem is not InstalledApp app){MessageBox.Show("请先选择一个软件。");return;}
  if(MessageBox.Show($"强制卸载会跳过厂商卸载程序，扫描可识别项目并交由你审核。\n目标：{app.Name}\n建议先尝试普通卸载。是否继续？","强制卸载确认",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  try{var candidates=residualScanner.Find(app);new ResidualWindow(app,candidates){Owner=this}.ShowDialog();}
  catch(Exception ex){MessageBox.Show(ex.Message,"强制卸载失败");}
 }

 async void Monitor_Click(object s,RoutedEventArgs e)
 {
  var dialog=new MonitorNameWindow{Owner=this};if(dialog.ShowDialog()!=true)return;
  var name=dialog.SessionName;long id=0;InstallTimelineMonitor? live=null;
  try
  {
   Busy(true,"创建安装前快照…");var progress=new Progress<string>(x=>StatusText.Text=x);
   var before=await snapshots.CaptureAsync(progress);id=db.NewSession(name);
   live=new InstallTimelineMonitor();live.EventReceived+=ev=>{try{db.AddTimelineEvent(id,ev);}catch{}};
   live.Start();
   var dlg=new Microsoft.Win32.OpenFileDialog{Title="选择安装程序（取消可自行安装）",Filter="安装程序|*.exe;*.msi|所有文件|*.*"};
   if(dlg.ShowDialog()==true)
   {
    if(dlg.FileName.EndsWith(".msi",StringComparison.OrdinalIgnoreCase))
     System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("msiexec.exe", $@"/i ""{dlg.FileName}""") { UseShellExecute = true });
    else System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName){UseShellExecute=true});
   }
   if(MessageBox.Show("监控已启动。请完成软件安装，然后选择“是”结束监控并查看时间线。","安装监控",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)
   {db.FinishSession(id,"用户提前结束监控");return;}
   live.Dispose();live=null;
   Busy(true,"创建安装后快照…");var after=await snapshots.CaptureAsync(progress);
   var changes=SnapshotService.Compare(before,after);db.Save(id,changes);
   var timeline=db.ReadTimeline(id);
   new TimelineWindow(timeline){Owner=this}.ShowDialog();
   MessageBox.Show($"快照差异 {changes.Count} 项；实时事件 {timeline.Count} 项。数据已保存到 SQLite。","监控完成");
   StatusText.Text=$"监控完成：{changes.Count} 项快照变化 / {timeline.Count} 项实时事件";
  }
  catch(Exception ex){MessageBox.Show(ex.ToString(),"监控失败");}
  finally{live?.Dispose();Busy(false);}
 }
}
