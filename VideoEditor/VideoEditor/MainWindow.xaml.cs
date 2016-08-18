using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VideoEditor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private FfmpegControl fControl = new FfmpegControl();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click_Switch(object sender, RoutedEventArgs e)
        {
            string Parameters = String.Format("-i {0} -y -qscale 4 -acodec copy -f avi E:\\newFileMp4.avi", videoPath.Text);
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                String str = fControl.RunProcess(Parameters);
                textBox1.Text = str;
            }));

        }

        private void button_Click_Water(object sender, RoutedEventArgs e)
        {
            string Parameters = String.Format("-i {0}  -i {1} -filter_complex \"overlay=50:30\"  -y -qscale 4 -acodec copy E:\\newFileWater.mp4", videoPath.Text, waterPath.Text);
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                String str = fControl.RunProcess(Parameters);
                textBox1.Text = str;
            }));
        }

        private void button_Click_Content(object sender, RoutedEventArgs e)
        {
            FFmpegMediaInfo info = new FFmpegMediaInfo("E:\\newFileMp4.mp4");
            double length = info.Duration.TotalSeconds;
            textBox1.Text = String.Format("时长:{0}秒\r高:{1}\r宽:{2}\r", length, info.Height, info.Width);
        }

        private void Button_Click_VideoEditor(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog =
                new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "视频文件|*.*";
            if (dialog.ShowDialog() == true)
            {
                videoPath.Text = dialog.FileName;
            }
        }

        private void Button_Click_WaterPath(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog dialog =
                new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "水印图片|*.png";
            if (dialog.ShowDialog() == true)
            {
                waterPath.Text = dialog.FileName;
            }
        }
    }
}
