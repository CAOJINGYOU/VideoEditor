using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace VideoEditor
{
    class FfmpegControl
    {
        private static string FFmpegPath = CheckRelativePath(@"ffmpeg\ffmpeg.exe");

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
            //创建一个ProcessStartInfo对象 并设置相关属性
            var oInfo = new ProcessStartInfo(FFmpegPath, Parameters);
            oInfo.UseShellExecute = false;//获取或设置一个值指示是否使用操作系统shell启动过程。
            oInfo.CreateNoWindow = true;//获取或设置一个值指示是否开始在一个新的窗口过程。
            oInfo.RedirectStandardOutput = true;//获取或设置指示是否将应用程序的文本输出写入 Process.StandardOutput 流中的值。
            oInfo.RedirectStandardError = true;//获取或设置指示是否将应用程序的错误输出写入 Process.StandardError 流中的值。
            oInfo.RedirectStandardInput = true;//获取或设置一个值,指出是否输入读取应用程序的过程。StandardInput流。

            //创建一个字符串和StreamReader 用来获取处理结果
            string output = null;
            StreamReader srOutput = null;

            try
            {
                //调用ffmpeg开始处理命令
                var proc = Process.Start(oInfo);

                //proc.WaitForExit();


                //获取输出流
                srOutput = proc.StandardError;

                //转换成string
                output = srOutput.ReadToEnd();

                //关闭处理程序
                //proc.Close();

                proc.Close();//关闭进程
                proc.Dispose();//释放资源
            }
            catch (Exception)
            {
                output = string.Empty;
            }
            finally
            {
                //释放资源
                if (srOutput != null)
                {
                    srOutput.Close();
                    srOutput.Dispose();
                }
            }
            return output;
        }
    }
}
