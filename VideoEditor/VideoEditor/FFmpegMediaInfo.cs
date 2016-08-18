using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VideoEditor
{
    public class FFmpegMediaInfo
    {
        //private static string FFMPEG_EXE_PATH = CheckRelativePath(@"ffmpeg\ffmpeg.exe");
        private static string FFPROBE_EXE_PATH = CheckRelativePath(@"ffmpeg\ffprobe.exe");

        #region static helpers

        /// <summary>
        /// Safely converts a string in format h:m:s.f to a TimeSpan using Regex allowing every part being as long as is
        /// </summary>
        private static TimeSpan ConvertFFmpegTimeSpan(string value)
        {
            Match m = _rexTimeSpan.Match(value);
            double v = 0.0;
            if (m == null || !m.Success) return new TimeSpan();

            if (!String.IsNullOrEmpty(m.Groups["h"].Value))
                v += Convert.ToInt32(m.Groups["h"].Value);
            v *= 60.0;

            if (!String.IsNullOrEmpty(m.Groups["m"].Value))
                v += Convert.ToInt32(m.Groups["m"].Value);
            v *= 60.0;

            if (!String.IsNullOrEmpty(m.Groups["s"].Value))
                v += Convert.ToInt32(m.Groups["s"].Value);

            if (!String.IsNullOrEmpty(m.Groups["f"].Value))
                v += Convert.ToDouble(String.Format("0{1}{0}", m.Groups["f"].Value, CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator));

            return TimeSpan.FromSeconds(v);
        }
        private static readonly Regex _rexTimeSpan = new Regex(@"^(((?<h>\d+):)?(?<m>\d+):)?(?<s>\d+)([\.,](?<f>\d+))?$", RegexOptions.Compiled);

        /// <summary>
        /// Tries to parse the value
        /// </summary>
        /// <param name="onErrorValue">what to return in case parsing fails</param>
        private static int TryParseInt32(string value, int onErrorValue)
        {
            try { return Convert.ToInt32(value); }
            catch { return onErrorValue; }
        }
        /// <summary>
        /// Tries to parse the value
        /// </summary>
        /// <param name="onErrorValue">what to return in case parsing fails</param>
        private static long TryParseInt64(string value, long onErrorValue)
        {
            try { return Convert.ToInt64(value); }
            catch { return onErrorValue; }
        }

        /// <summary>
        /// Checks if the passed path is rooted and if not resolves it relative to the calling assembly
        /// </summary>
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
        /// Executes a process and passes its command-line output back after the process has exitted
        /// </summary>
        private static string Execute(string exePath, string parameters)
        {
            string result = String.Empty;

            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = parameters;
                p.Start();
                p.WaitForExit();

                result = p.StandardOutput.ReadToEnd();
            }

            return result;
        }

        #endregion

        #region Properies

        /// <summary>The filename of this instance (is used for Bitmap extraction)</summary>
        public string Filename { get; set; }
        /// <summary>The duration of the loaded file</summary>
        public TimeSpan Duration { get; set; }
        /// <summary>The format name of the file (e.g. mov,mp4,m4a,3gp,3g2,mj2)</summary>
        public string FormatName { get; set; }
        /// <summary>The format description (e.g. QuickTime / MOV)</summary>
        public string FormatNameLong { get; set; }
        /// <summary>The average bit rate</summary>
        public string BitRate { get; set; }

        /// <summary>Information about contained streams</summary>
        public List<FFmpegStreamInfo> Streams { get; set; }
        /// <summary>Value pair information that wasn't parsed into this class</summary>
        public List<KeyValuePair<string, string>> OtherValues { get; set; }

        /// <summary>The width of the first video stream - if any</summary>
        public int Width { get; set; }
        /// <summary>The height of the first video stream - if any</summary>
        public int Height { get; set; }

        #endregion

        private FFmpegMediaInfo()
        {
            Streams = new List<FFmpegStreamInfo>();
            OtherValues = new List<KeyValuePair<string, string>>();
        }
        public FFmpegMediaInfo(string filename)
            : this()
        {
            this.Filename = filename;

            try
            {
                // Generate command line
                string cmdParams = String.Format("-hide_banner -show_format -show_streams -pretty {1}{0}{1}", filename, filename.Contains(' ') ? "\"" : "");
                // Execute command and get all output lines (replacing is used to compensate different newline methods)
                string[] lines = Execute(FFPROBE_EXE_PATH, cmdParams).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                // Parse lines and extract information
                int curr = 0;
                FFmpegStreamInfo stream = null;
                StringComparison sc = StringComparison.OrdinalIgnoreCase;
                string[] parts;
                foreach (string line in lines)
                {
                    if (line.StartsWith("[/")) // Section ending
                        curr = 0;
                    else if (line.StartsWith("[FORMAT]")) // Begin section FORMAT
                        curr = 1;
                    else if (line.StartsWith("[STREAM]")) // Begin section STREAM
                    {
                        // Append new stream information
                        stream = new FFmpegStreamInfo();
                        this.Streams.Add(stream);
                        curr = 2;
                    }
                    else if (curr > 0)
                    {
                        // Split name and value
                        parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length != 2) continue;

                        if (curr == 1)
                        {
                            #region FFmpegMediaInfo fields
                            if (parts[0].Equals("format_name", sc))
                                this.FormatName = parts[1];
                            else if (parts[0].Equals("format_long_name", sc))
                                this.FormatNameLong = parts[1];
                            else if (parts[0].Equals("duration", sc))
                                this.Duration = ConvertFFmpegTimeSpan(parts[1]);
                            else if (parts[0].Equals("bit_rate", sc))
                                this.BitRate = parts[1];
                            else
                                this.OtherValues.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
                            #endregion
                        }
                        else if (curr == 2)
                        {
                            #region FFmpegStreamInfo fields
                            if (parts[0].Equals("index", sc))
                                stream.Index = TryParseInt32(parts[1], -1);
                            else if (parts[0].Equals("codec_name", sc))
                                stream.CodecName = parts[1];
                            else if (parts[0].Equals("codec_long_name", sc))
                                stream.CodecNameLong = parts[1];
                            else if (parts[0].Equals("codec_type", sc))
                                stream.CodecType = parts[1];
                            else if (parts[0].Equals("codec_time_base", sc))
                                stream.CodecTimeBase = parts[1];
                            else if (parts[0].Equals("codec_tag_string", sc))
                                stream.CodecTagString = parts[1];
                            else if (parts[0].Equals("codec_tag", sc))
                                stream.CodecTag = parts[1];
                            else if (parts[0].Equals("width", sc))
                                stream.Width = TryParseInt32(parts[1], 0);
                            else if (parts[0].Equals("height", sc))
                                stream.Height = TryParseInt32(parts[1], 0);
                            else if (parts[0].Equals("sample_aspect_ratio", sc))
                                stream.SampleAspectRatio = parts[1];
                            else if (parts[0].Equals("display_aspect_ratio", sc))
                                stream.DisplayAspectRation = parts[1];
                            else if (parts[0].Equals("pix_fmt", sc))
                                stream.PixelFormat = parts[1];
                            else if (parts[0].Equals("r_frame_rate", sc))
                                stream.FrameRate = parts[1];
                            else if (parts[0].Equals("start_time", sc))
                                stream.StartTime = ConvertFFmpegTimeSpan(parts[1]);
                            else if (parts[0].Equals("duration", sc))
                                stream.Duration = ConvertFFmpegTimeSpan(parts[1]);
                            else if (parts[0].Equals("nb_frames", sc))
                                stream.FrameCount = TryParseInt64(parts[1], 0);
                            else
                                stream.OtherValues.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
                            #endregion
                        }
                    }
                }

                // Search the first video stream and copy video size to FFmpegMediaInfo for easier access
                FFmpegStreamInfo video = this.Streams.FirstOrDefault(s => s.CodecType.Equals("video", sc));
                if (video != null)
                {
                    this.Width = video.Width;
                    this.Height = video.Height;
                }
            }
            catch { }

        }

        /// <summary>
        /// Extract a frame from the video of this instance (this.Filename is used!)
        /// </summary>
        /// <param name="atPositioin">A time position within the duration of the video</param>
        //public Bitmap GetSnapshot(TimeSpan atPositioin)
        //{
        //    // Prepare filename for commandline usage
        //    string filename = this.Filename;
        //    if (filename.Contains(' '))
        //        filename = "\"" + filename + "\"";

        //    // Prepare a temporary file for ommandline usage
        //    string tmpFileName = Path.GetTempFileName();
        //    if (tmpFileName.Contains(' '))
        //        tmpFileName = "\"" + tmpFileName + "\"";

        //    // Generate command to extract one frame at the passed position
        //    string cmdParams = String.Format("-hide_banner -ss {0} -i {1} -r 1 -t 1 -f image2 {2}", atPositioin, filename, tmpFileName);

        //    Bitmap result = null;
        //    try
        //    {
        //        // Execute command to let FFMPEG extract the frame
        //        Execute(FFMPEG_EXE_PATH, cmdParams);

        //        // If the file was created, read the image
        //        if (File.Exists(tmpFileName))
        //        {
        //            // Do not open the Bitmap directly from the file, because then the file is locked until the Bitmap is disposed!
        //            byte[] fileData = File.ReadAllBytes(tmpFileName);
        //            result = new Bitmap(new MemoryStream(fileData));
        //            File.Delete(tmpFileName);
        //        }
        //    }
        //    catch { }

        //    return result;
        //}
    }

    public class FFmpegStreamInfo
    {
        /// <summary>The index of this stream in the media file</summary>
        public int Index { get; set; }
        /// <summary>The codec name (e.g. h264)</summary>
        public string CodecName { get; set; }
        /// <summary>The codec description (e.g. H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10)</summary>
        public string CodecNameLong { get; set; }
        /// <summary>The codec type ("video", "audio", or something else)</summary>
        public string CodecType { get; set; }
        /// <summary>Time per frame in second fractions</summary>
        public string CodecTimeBase { get; set; }
        /// <summary>The FourCC codec tag (e.g. avc1)</summary>
        public string CodecTagString { get; set; }
        /// <summary>The FourCC codex integer value (e.g. 0x31637661)</summary>
        public string CodecTag { get; set; }
        /// <summary>The width in pixel - in case this is a video stream</summary>
        public int Width { get; set; }
        /// <summary>The height in pixel - in case this is a video stream</summary>
        public int Height { get; set; }
        /// <summary>The aspect ratio of the video data (e.g. 64:45)</summary>
        public string SampleAspectRatio { get; set; }
        /// <summary>The aspect ratio the video should be displayed at (e.g. 16:9)</summary>
        public string DisplayAspectRation { get; set; }
        /// <summary>The video pixel format (e.g. yuv420p)</summary>
        public string PixelFormat { get; set; }
        /// <summary>The stream frame rate (e.g. 25/1)</summary>
        public string FrameRate { get; set; }
        /// <summary>The number of frames</summary>
        public long FrameCount { get; set; }
        /// <summary>The audio sample rate (e.g. 48000 KHz)</summary>
        public string SampleRate { get; set; }
        /// <summary>The number of audio channels (e.g. 2)</summary>
        public int Channels { get; set; }
        /// <summary>The name of the audio channel profile (e.g. stereo)</summary>
        public string ChannelLayout { get; set; }
        /// <summary>The start time of this stream (can be negative, e.g. 0:00:00.000000)</summary>
        public TimeSpan StartTime { get; set; }
        /// <summary>The duration of this stream (e.g. 1:34:56.320000)</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Value pair information that wasn't parsed into this class</summary>
        public List<KeyValuePair<string, string>> OtherValues { get; set; }

        public FFmpegStreamInfo()
        {
            OtherValues = new List<KeyValuePair<string, string>>();
        }
    }
}
