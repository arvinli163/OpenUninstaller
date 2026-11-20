using OpenUninstaller.Models;
using OpenUninstaller.Services;
using System.Windows;
namespace OpenUninstaller;
public partial class ResidualWindow:Window
{
 readonly List<ResidualItem> items;readonly SafeCleanupService cleanup=new();
 public ResidualWindow(InstalledApp app,List<ResidualItem> candidates){InitializeComponent();items=candidates;AppTitle.Text=$"{app.Name}：发现 {items.Count} 个候选项";ItemsGrid.ItemsSource=items;}
 void SelectAll_Click(object s,RoutedEventArgs e){foreach(var x in items)x.Selected=true;ItemsGrid.Items.Refresh();}
 void Cleanup_Click(object s,RoutedEventArgs e){
  ItemsGrid.CommitEdit();var chosen=items.Where(x=>x.Selected).ToList();if(chosen.Count==0){MessageBox.Show("请先选择候选项。");return;}
  if(MessageBox.Show($"将处理 {chosen.Count} 项。文件会移动至隔离区，注册表卸载项会先导出 .reg 备份。继续？","确认清理",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  try{var done=cleanup.Execute(chosen);MessageBox.Show($"已处理 {done.Count} 项。\n隔离/备份目录：\n{cleanup.BackupRoot}\n\n文件恢复请从隔离目录手动移回原位置。","清理完成");Close();}
  catch(Exception ex){MessageBox.Show(ex.Message,"清理中止",MessageBoxButton.OK,MessageBoxImage.Error);}
 }
}
