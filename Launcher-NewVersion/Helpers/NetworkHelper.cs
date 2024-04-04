using Launcher.Exceptions;
using Launcher.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
                FileHelpers.WriteLog(ex.ToString());
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
        public static void DownloadFile(string url, string filename)
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
                FileHelpers.WriteLog(ex.ToString());
                Debug.WriteLine($"Error in DownloadFile {ex}");
            }
        }

        /// <summary>
        /// Download file from multiple URLs
        /// </summary>
        /// <param name="baseUrls"></param>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        public static void DownloadFileFromMultipleUrls(this List<string> baseUrls, string fileName, int timeout = 900000)
        {
            try
            {
                fileName = Path.GetFullPath(fileName) + FileExtentions.HlZip.GetDescription();
                string destinationDirectory = Path.GetDirectoryName(fileName);

                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                foreach (var url in baseUrls)
                {
                    try
                    {
                        using (TimeOutWebClientHelper wc = new TimeOutWebClientHelper(timeout))
                        {
                            var fullPath = url;
                            wc.DownloadFile(fullPath, fileName);
                            break;
                        }
                    }
                    catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
                    {
                        FileHelpers.WriteLog(ex.ToString());
                        throw;
                    }
                    catch (WebException ex) when (ex.Status == WebExceptionStatus.SendFailure)
                    {
                        FileHelpers.WriteLog(ex.ToString());
                        if (url == baseUrls.Last()) throw;
                    }
                    catch (Exception ex)
                    {
                        var specificException = ex.GetType();
                        Debug.WriteLine($"Error in DownloadFileFromMultipleUrls {ex}");
                        FileHelpers.WriteLog(ex.ToString());
                        if(url == baseUrls.Last()) throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DownloadFileFromMultipleUrls {ex}");
                FileHelpers.WriteLog(ex.ToString());
                throw;
            }
        }
        


        /// <summary>
        /// Calculate the fastest link to download from a list of links
        /// </summary>
        /// <param name="uris">list of links</param>
        /// <returns>string</returns>
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
                            FileHelpers.WriteLog(ex.ToString());
                            throw;
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
                var specificException = ex.GetType();
                Debug.WriteLine($"Error in GetFastestLink {ex}");
                FileHelpers.WriteLog(ex.ToString());
                return uris.FirstOrDefault();
            }
        }

        /// <summary>
        /// Fetch data from multiple URIs
        /// </summary>
        /// <param name="uris">list of links</param>
        /// <param name="encoding">type of encoding for WebClient</param>
        /// <param name="fileName">name of file to get</param>
        /// <param name="timeout">timeout</param>
        /// <returns>JSON Object</returns>
        public static JObject FetchDataFromMultipleUris(this List<string> uris, Encoding encoding, string fileName = "", int timeout = 900000)
        {
            JObject json = null;
            foreach (var uri in uris)
            {
                try
                {
                    using (TimeOutWebClientHelper wc = new TimeOutWebClientHelper(timeout))
                    {
                        wc.Encoding = encoding;
                        string downloadfile = wc.DownloadString(new Uri(uri + fileName));
                        json = JObject.Parse(downloadfile);
                    }
                    break;
                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
                {
                    FileHelpers.WriteLog(ex.ToString());

                    throw;
                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.SendFailure)
                {
                    ex.Message.ToString();
                    var respone = (HttpWebResponse) ex.Response;
                    FileHelpers.WriteLog(ex.ToString());
                    if (uri == uris.Last()) throw;
                }
                catch (Exception ex)
                {                    
                    Debug.WriteLine($"Error in FetchFromMultipleUris {uri + fileName}: {ex}");
                    FileHelpers.WriteLog(ex.ToString());
                    if (uri == uris.Last()) throw new FetchingErrorException("Fetch error");
                }
            }
            return json;
        }
    }
}