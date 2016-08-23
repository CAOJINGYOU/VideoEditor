using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Reflection;

namespace VideoEditor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click_Switch(object sender, RoutedEventArgs e)
        {
            progressbar.Value = 0;
            button1.IsEnabled = false;
            labelpro.Content = "0%";

            string Parameters = String.Format("-i {0} -y -qscale 4 -acodec copy -f avi E:\\newFileMp4.avi", videoPath.Text);

            Thread thread = new Thread(new ThreadStart(() =>
            {
                String str = RunProcess(Parameters);
                System.Windows.Threading.Dispatcher.Run();
            }));

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

        }


        private void button_Click_Water(object sender, RoutedEventArgs e)
        {
            progressbar.Value = 0;
            button2.IsEnabled = false;
            labelpro.Content = "0%";

            string Parameters = String.Format("-i {0}  -i {1} -filter_complex \"overlay=50:30\"  -y -qscale 4 -acodec copy E:\\newFileWater.mp4", videoPath.Text, waterPath.Text);

            Thread thread = new Thread(new ThreadStart(() =>
            {
                String str = RunProcess(Parameters);
                System.Windows.Threading.Dispatcher.Run();
            }));

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
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


        private static string FFmpegPath = CheckRelativePath(@"ffmpeg\ffmpeg.exe");
        string strOut;

        /// <summary>
        /// 视频处理器ffmpeg.exe的位置
        /// </summary>
        //public string FFmpegPath { get; set; }

        private static string CheckRelativePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                string appDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().GetName().CodeBase);
                path = Path.Combine(appDir, path);
            }

            if (path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(6);

            return path;
        }

        /// <summary>
        /// 调用ffmpeg.exe 执行命令
        /// </summary>
        /// <param name="Parameters">命令参数</param>
        /// <returns>返回执行结果</returns>
        public string RunProcess(string Parameters)
        {
            strOut = "";
            //创建一个ProcessStartInfo对象 并设置相关属性
            var oInfo = new ProcessStartInfo(FFmpegPath, Parameters);
            oInfo.UseShellExecute = false;//获取或设置一个值指示是否使用操作系统shell启动过程。
            oInfo.CreateNoWindow = true;//获取或设置一个值指示是否开始在一个新的窗口过程。
            oInfo.RedirectStandardOutput = true;//获取或设置指示是否将应用程序的文本输出写入 Process.StandardOutput 流中的值。
            oInfo.RedirectStandardError = true;//获取或设置指示是否将应用程序的错误输出写入 Process.StandardError 流中的值。
            oInfo.RedirectStandardInput = true;//获取或设置一个值,指出是否输入读取应用程序的过程。StandardInput流。

            //try
            {
                //调用ffmpeg开始处理命令
                var proc = Process.Start(oInfo);
                proc.EnableRaisingEvents = true;
                proc.Exited += new EventHandler(Proc_Exited);
                proc.ErrorDataReceived += new DataReceivedEventHandler(Output);

                proc.BeginErrorReadLine();

                proc.WaitForExit();

                proc.Close();//关闭进程
                proc.Dispose();//释放资源
            }
            return "";
        }

        private void Output(object sendProcess, DataReceivedEventArgs output)
        {
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                strOut += output.Data;
                strOut += "\r\n";
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (progressbar.Value < 100)
                    {
                        labelpro.Content = progressbar.Value + "%";
                        progressbar.Value++;
                    }
                    textBox1.Text = output.Data;
                }));
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                progressbar.Value = 100;
                labelpro.Content = progressbar.Value + "%";
                textBox1.Text = strOut;
                button1.IsEnabled = true;
                button2.IsEnabled = true;
            }));
            System.Windows.MessageBox.Show("完成");
        }
    }
}
