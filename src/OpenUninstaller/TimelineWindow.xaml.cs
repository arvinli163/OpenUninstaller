using OpenUninstaller.Models;
using System.Windows;
namespace OpenUninstaller;
public partial class TimelineWindow:Window
{
 public TimelineWindow(IEnumerable<TimelineEvent> events){InitializeComponent();EventsGrid.ItemsSource=events.OrderBy(x=>x.Timestamp).ToList();}
}
