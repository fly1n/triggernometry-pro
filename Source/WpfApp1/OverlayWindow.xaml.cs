using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Triggernometry;
namespace WpfApp1
{
    /// <summary>
    /// OverlayWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OverlayWindow : Window
    {
        public RealPlugin plug;
        public DataSet ds;
        private BindingList<UILogline> uiloglines = new BindingList<UILogline>();

        public OverlayWindow(RealPlugin plug)
        {
            this.InitializeComponent();
            dataGridLogs.ItemsSource = uiloglines;
            
            if (plug != null)
            {
                plug.logRefreshDelegate += OnLogRefresh;
            }
            
            
        }
        private void OnLogRefresh(InternalLog log) {
            this.Dispatcher.BeginInvoke(new System.Action(() => {

                UILogline line = new UILogline();
                line.Timestamp = log.Timestamp.ToString("HH:mm:ss:ffff");
                line.Message = log.Message;
                line.Level = log.Level.ToString();
                if (uiloglines.Count > 1000) {
                    uiloglines.RemoveAt(0);
                }
                if (checkErrorLog.IsChecked==true && log.Level == RealPlugin.DebugLevelEnum.Error) {
                    
                    uiloglines.Add(line);
                }
                if (checkWarningLog.IsChecked == true && log.Level == RealPlugin.DebugLevelEnum.Warning)
                {
                    uiloglines.Add(line);
                }
                if (checkInfoLog.IsChecked == true && log.Level == RealPlugin.DebugLevelEnum.Info)
                {
                    uiloglines.Add(line);
                }
                if (checkDebugLog.IsChecked == true && log.Level == RealPlugin.DebugLevelEnum.Verbose)
                {
                    uiloglines.Add(line);
                }
                if (VisualTreeHelper.GetChildrenCount(dataGridLogs) > 0)
                {
                    var x = VisualTreeHelper.GetChild(dataGridLogs, 0);
                    if (x == null) return;
                    if (VisualTreeHelper.GetChildrenCount(x) == 0) return;
                    ScrollViewer sc=VisualTreeHelper.GetChild(x, 0) as ScrollViewer;
                    sc.ScrollToEnd();
                }
                
            }));


        }

        private void Topboard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
    public class UILogline : INotifyPropertyChanged
    {
        private string _timestamp;
        private string _message;
        public string _level;

        public string Message
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("UILoglinesMessage"));
                }
            }
        }
        public string Level
        {
            get { return _level; }
            set
            {
                if (_level != value)
                {
                    _level = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("UILoglinesLevel"));
                }
            }
        }

        public string Timestamp
        {
            get { return _timestamp; }
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("UILoglinesTimestamp"));
                }
            }
        }
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                if (Convert.ToDouble(((System.Data.DataRowView)(e.Row.DataContext)).Row.ItemArray[3].ToString()) > 0)
                {
                    e.Row.Background = new SolidColorBrush(Colors.Green);
                }
                else if (Convert.ToDouble(((System.Data.DataRowView)(e.Row.DataContext)).Row.ItemArray[4].ToString()) < 0)
                {
                    e.Row.Background = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    e.Row.Background = new SolidColorBrush(Colors.WhiteSmoke);
                }
            }
            catch
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

    }
}
