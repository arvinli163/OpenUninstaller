using System.Windows;

namespace OpenUninstaller;

public partial class MonitorNameWindow : Window
{
    public string SessionName => NameBox.Text.Trim();

    public MonitorNameWindow() => InitializeComponent();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("请输入软件名称。");
            return;
        }

        DialogResult = true;
    }
}
