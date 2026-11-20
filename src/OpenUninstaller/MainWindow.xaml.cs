using OpenUninstaller.Models;
using OpenUninstaller.Services;
using System.Windows;
namespace OpenUninstaller;
public partial class MainWindow:Window
{
 readonly SoftwareScanner scanner=new();readonly UninstallService uninstaller=new();readonly SnapshotService snapshots=new();readonly DatabaseService db=new();List<InstalledApp> all=new();
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
 async void Monitor_Click(object s,RoutedEventArgs e){
  var dialog = new MonitorNameWindow { Owner = this }; if (dialog.ShowDialog() != true) return; var name = dialog.SessionName;
  try{
   Busy(true,"创建安装前快照…");var p=new Progress<string>(x=>StatusText.Text=x);var before=await snapshots.CaptureAsync(p);var id=db.NewSession(name);
   var dlg=new Microsoft.Win32.OpenFileDialog{Title="选择安装程序（取消可自行安装）",Filter="安装程序|*.exe;*.msi|所有文件|*.*"};
   if(dlg.ShowDialog()==true){if(dlg.FileName.EndsWith(".msi",StringComparison.OrdinalIgnoreCase))System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("msiexec.exe",$"/i \"{dlg.FileName}\""){UseShellExecute=true});else System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName){UseShellExecute=true});}
   if(MessageBox.Show("请完成软件安装，然后选择“是”进行安装后快照比较。","等待安装",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
   Busy(true,"创建安装后快照…");var after=await snapshots.CaptureAsync(p);var changes=SnapshotService.Compare(before,after);db.Save(id,changes);
   MessageBox.Show($"发现 {changes.Count} 项持久变化，已保存到本地 SQLite。\n\n注意：这是快照差异，不是内核级完整监控。","监控结果");
   StatusText.Text=$"监控完成：{changes.Count} 项变化";
  }catch(Exception ex){MessageBox.Show(ex.ToString(),"监控失败");}finally{Busy(false);}
 }
}
