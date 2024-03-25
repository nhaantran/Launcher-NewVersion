using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows;
using Ionic.Zip;
using System.Threading;
using System.Windows.Documents;
using System.Collections.Generic;

namespace Launcher_NewVersion
{
    static class Utils
    {
        public static string CalculateMD5(string filename)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        public static void openLink(string url)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = url;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Can not open link: " + url + "\nDetails: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }
        public static string GetPublicIpAddress()
        {
            var request = (HttpWebRequest)WebRequest.Create("http://ifconfig.me");

            request.UserAgent = "curl"; // this will tell the server to return the information as if the request was made by the linux "curl" command

            string publicIPAddress;

            request.Method = "GET";
            using (WebResponse response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    publicIPAddress = reader.ReadToEnd();
                }
            }

            return publicIPAddress.Replace("\n", "");
        }

        public static void Extract(string sourceFile, string destinationDirectory)
        {
            sourceFile = Path.GetFullPath(sourceFile);
            destinationDirectory = Path.GetFullPath(destinationDirectory);

            if (!File.Exists(sourceFile))
            {
                return;
            }

            destinationDirectory = Path.GetDirectoryName(destinationDirectory);

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            ExtractZipFile(sourceFile, destinationDirectory);
        }

        public static void ExtractZipFile(string sourceFile, string destinationDirectory)
        {
            using (ZipFile zip = ZipFile.Read(sourceFile))
            {
                foreach (ZipEntry e in zip)
                {
                    e.Extract(destinationDirectory, ExtractExistingFileAction.OverwriteSilently);
                }
            }
        }

        public static JObject ReadConfig()
        {
            try
            {
                string configFilePath = Path.GetFullPath("launcher.json");
                JObject configFile = JObject.Parse(File.ReadAllText(configFilePath));

                if (configFile == null)
                    throw new Exception("Can not find config file");

                return configFile;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
                return null;
            }
        }




        //public class DownloadProgressTracker
        //{
        //    private long _totalFileSize;
        //    private readonly int _sampleSize;
        //    private readonly TimeSpan _valueDelay;

        //    private DateTime _lastUpdateCalculated;
        //    private long _previousProgress;

        //    private double _cachedSpeed;

        //    private Queue<Tuple<DateTime, long>> _changes = new Queue<Tuple<DateTime, long>>();

        //    public DownloadProgressTracker(int sampleSize, TimeSpan valueDelay)
        //    {
        //        _lastUpdateCalculated = DateTime.Now;
        //        _sampleSize = sampleSize;
        //        _valueDelay = valueDelay;
        //    }

        //    public void NewFile()
        //    {
        //        _previousProgress = 0;
        //    }

        //    public void SetProgress(long bytesReceived, long totalBytesToReceive)
        //    {
        //        _totalFileSize = totalBytesToReceive;

        //        long diff = bytesReceived - _previousProgress;
        //        if (diff <= 0)
        //            return;

        //        _previousProgress = bytesReceived;

        //        _changes.Enqueue(new Tuple<DateTime, long>(DateTime.Now, diff));
        //        while (_changes.Count > _sampleSize)
        //            _changes.Dequeue();
        //    }

        //    public double GetProgress()
        //    {
        //        return _previousProgress / (double)_totalFileSize;
        //    }

        //    public string GetProgressString()
        //    {
        //        return String.Format("{0:P0}", GetProgress());
        //    }

        //    public string GetBytesPerSecondString()
        //    {
        //        double speed = GetBytesPerSecond();
        //        var prefix = new[] { "", "K", "M", "G" };

        //        int index = 0;
        //        while (speed > 1024 && index < prefix.Length - 1)
        //        {
        //            speed /= 1024;
        //            index++;
        //        }

        //        int intLen = ((int)speed).ToString().Length;
        //        int decimals = 3 - intLen;
        //        if (decimals < 0)
        //            decimals = 0;

        //        string format = String.Format("{{0:F{0}}}", decimals) + "{1}B/s";

        //        return String.Format(format, speed, prefix[index]);
        //    }

        //    public double GetBytesPerSecond()
        //    {
        //        try
        //        {
        //            if (DateTime.Now >= _lastUpdateCalculated + _valueDelay)
        //            {
        //                _lastUpdateCalculated = DateTime.Now;
        //                _cachedSpeed = GetRateInternal();
        //            }
        //            return _cachedSpeed;
        //        }
        //        catch (Exception e)
        //        {
        //            return 0;
        //        }
        //    }

        //    private double GetRateInternal()
        //    {
        //        if (_changes.Count == 0)
        //            return 0;
        //        try
        //        {
        //            if(_changes.Last().Item1 != null && _changes.First().Item1 != null){
        //                TimeSpan timespan = _changes.Last().Item1 - _changes.First().Item1;

        //                long bytes = _changes.Sum(t => t.Item2);

        //                double rate = bytes / timespan.TotalSeconds;

        //                if (double.IsInfinity(rate) || double.IsNaN(rate))
        //                    return 0;

        //                return rate;
        //            }
        //            else
        //                return 0;
        //        }
        //        catch
        //        {
        //            return 0;
        //        }
        //    }
        //}
        public static void DownloadFile(string url, string filename, bool isFailed = false)
        {
            try
            {
                filename = Path.GetFullPath(filename);
                //Debug.WriteLine(filename);


                string destinationDirectory = Path.GetDirectoryName(filename);
                //Debug.WriteLine(destinationDirectory);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                WebClient wc = new WebClient();
                wc.DownloadFile(url, filename);
                //Debug.WriteLine(isFailed);
            }
            catch
            {
                isFailed = true;
                //Debug.WriteLine(isFailed);
            }
        }

        public static string GetFastestLink(this List<string> urls)
        {
            WebClient[] clients = new WebClient[urls.Count];
            Stopwatch[] stopwatches = new Stopwatch[urls.Count];
            ManualResetEvent[] doneEvents = new ManualResetEvent[urls.Count];

            for (int i = 0; i < urls.Count; i++)
            {
                clients[i] = new WebClient();
                stopwatches[i] = new Stopwatch();
                doneEvents[i] = new ManualResetEvent(false);

                ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
                {
                    int index = (int)state;

                    stopwatches[index].Start();

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urls[index]);   
                        request.AddRange(0, 999); // This will download the first 1000 bytes

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            byte[] buffer = new byte[1000];
                            responseStream.Read(buffer, 0, 1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exception
                        Console.WriteLine($"Error downloading from {urls[index]}: {ex.Message}");
                    }
                    finally
                    {
                        stopwatches[index].Stop();
                        doneEvents[index].Set();
                    }
                }), i);
            }

            foreach (var e in doneEvents)
                e.WaitOne();
            
            string fastestUrl = string.Empty;
            TimeSpan fastestTime = TimeSpan.MaxValue;

            for (int i = 0; i < urls.Count; i++)
            {
                if (stopwatches[i].Elapsed < fastestTime)
                {
                    fastestTime = stopwatches[i].Elapsed;
                    fastestUrl = urls[i];
                }
            }

            return fastestUrl;
        }

        public static JObject DownloadFromMultipleUris(List<string> uris)
        {
            WebClient wc = new WebClient();
            JObject json = null;
            foreach (var uri in uris)
            {
                try
                {
                    string downloadfile = wc.DownloadString(new Uri(uri));
                    json = JObject.Parse(downloadfile);
                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error in DownloadFromMultipleUris {uri}: {e}");
                }
            }
            return json;
        }
    }
}
