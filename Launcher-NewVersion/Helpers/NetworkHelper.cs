using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;

namespace Launcher.Helpers
{
    public static class NetworkHelper
    {
        public static void OpenLink(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = url
                };
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
        public static void DownloadFile(string url, string filename, bool isFailed = false)
        {
            try
            {
                filename = Path.GetFullPath(filename);
                string destinationDirectory = Path.GetDirectoryName(filename);

                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                WebClient wc = new WebClient();
                wc.DownloadFile(url, filename);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DownloadFile {ex}");
                isFailed = true;
            }
        }

        public static string GetFastestLink(this List<string> uris)
        {
            WebClient[] clients = new WebClient[uris.Count];
            Stopwatch[] stopwatches = new Stopwatch[uris.Count];
            ManualResetEvent[] doneEvents = new ManualResetEvent[uris.Count];
            try
            {
                for (int i = 0; i < uris.Count; i++)
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
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uris[index]);
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
                            Console.WriteLine($"Error downloading from {uris[index]}: {ex.Message}");
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

                for (int i = 0; i < uris.Count; i++)
                {
                    if (stopwatches[i].Elapsed < fastestTime)
                    {
                        fastestTime = stopwatches[i].Elapsed;
                        fastestUrl = uris[i];
                    }
                }
                return fastestUrl;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetFastestLink {ex}");
                return uris.FirstOrDefault();
            }
        }

        public static JObject FetchFromMultipleUris(List<string> uris)
        {
            WebClient wc = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8
            };
            JObject json = null;
            foreach (var uri in uris)
            {
                try
                {
                    string downloadfile = wc.DownloadString(new Uri(uri));
                    json = JObject.Parse(downloadfile);
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in FetchFromMultipleUris {uri}: {ex}");
                }
            }
            return json;
        }
    }
}
