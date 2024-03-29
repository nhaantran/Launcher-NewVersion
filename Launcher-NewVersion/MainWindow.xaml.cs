﻿using Launcher;
using Launcher.Helpers;
using Launcher.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Launcher_NewVersion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            versionFile = Path.GetFullPath(HashSumFileValue.VersionKey);
            gameExe = Path.GetFullPath(Settings.GameExePath);
        }

        /*
         * For publish purpose
         * OPEN TERMINAL IN PROJECT FOLDER
         * USE THIS COMMAND TO PUBLISH
          msbuild /p:TargetFramework=net35 /p:Configuration=Release /p:PublishSingleFile=true
        */
        #region Configurable Varibles
        private static readonly double WIDTH = 1080f;
        private static readonly double HEIGHT = 740f;
        private List<string> newsUri = null;
        private List<string> DownloadFileUri = null;
        private string HOME_URL = null;
        private string REGISTER_URL = null;
        private string RECHARGE_URL = null;
        private string FANPAGE_URL = null;
        private string GROUP_URL = null;
        private string MORE_URL = null;

        //Hash sum
        FileStream fsHashSum = null;

        //Download json from server
        private string versionFile;
        private string gameExe;
        private JObject hashSumData = null;
        private JObject clientFiles = new JObject();
        private string priorityMirror;
        protected bool isFailed = false;

        protected string localBannerDir = "Banner";

        // As global variables
        private JObject news = null;
        private List<News> newsData = new List<News>();
        private List<Button> newsControls = new List<Button>();
        private List<Button> newsTime = new List<Button>();
        private JArray updateFiles = new JArray();
        private bool isUpdating = false;

        //Check FixClient on click
        private bool isClick = false;

        //CLock
        private readonly Stopwatch stopwatch = new Stopwatch();

        //Launcher Status
        private LauncherStatus _status;

        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        UpdateStatus.Text = "Nhấn Start để bắt đầu"; UStatus.Text = "(Không cần cập nhật)"; FixClient.IsEnabled = true;
                        break;
                    case LauncherStatus.failed:
                        if (isClick)
                        {
                            UpdateStatus.Text = "Sửa thất bại! Vui lòng thử lại";
                            UpdateStatus_btn.IsEnabled = false;
                            UpdateStatus_btn.Opacity = 1;
                            FixClient.IsEnabled = true;
                        }
                        else
                        {
                            UpdateStatus.Text = "Cập nhật thất bạn"; UStatus.Text = "(Cập nhật ngay)"; UpdateStatus_btn.IsEnabled = true;
                        }
                        break;
                    case LauncherStatus.downloadingUpdate:
                        if (isClick)
                        {
                            UpdateStatus.Text = "Đang sửa"; UpdateStatus_btn.IsEnabled = false; UpdateStatus_btn.Opacity = 1;
                            UStatus.Text = "(Đang sửa ... )";
                        }
                        else
                        {
                            UpdateStatus.Text = "Đang cập nhật"; UStatus.Text = "(Hãy cập nhật phiên bản mới)";
                        }
                        break;
                    case LauncherStatus.Checking:
                        UpdateStatus.Text = "Kiểm tra cập nhật";
                        break;
                    case LauncherStatus._verifying:
                        UpdateStatus.Text = "Xác thực dữ liệu ..."; UpdateStatus_btn.IsEnabled = false; UpdateStatus_btn.Opacity = 1;
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Methods
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            JObject configFile = ConfigHelper.ReadConfig();
            Setup(configFile);
            try
            {
                UsingDynamicControlsGeneration();
                SetDefaultPropertiesForControls();
                FetchingDataFromSeverInNewThread();

                Status = LauncherStatus._verifying;

                hashSumData = DownloadFileUri.FetchDataFromMultipleUris(Encoding.Default, Settings.HashSumFile);

                var forcedUpdate = false;
                if (!File.Exists(Path.GetFullPath(Settings.LibFile)))
                {
                    fsHashSum = new FileStream(Path.GetFullPath(Settings.LibFile), FileMode.Create, FileAccess.ReadWrite);
                    fsHashSum.Close();
                    forcedUpdate = true;
                }
                if (IsRequireUpdate(forcedUpdate))
                {
                    Update();
                }
                else
                {
                    Status = LauncherStatus.ready;
                    Progress.Visibility = Visibility.Hidden;
                    OneFileProgress.Visibility = Visibility.Hidden;
                    FileName.Visibility = Visibility.Hidden;
                    PlayGame.IsEnabled = true;
                    if (!File.Exists(Path.GetFullPath(Settings.ModeSavedFile)))
                        File.Create(Path.GetFullPath(Settings.ModeSavedFile)).Close();

                    if (!File.Exists(Path.GetFullPath(Settings.IPSavedFile)))
                        File.Create(Path.GetFullPath(Settings.IPSavedFile)).Close();
                    File.WriteAllText(Path.GetFullPath(Settings.IPSavedFile), NetworkHelper.GetPublicIpAddress());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Window_ContentRendered: {ex}");
            }
        }
        private void UsingDynamicControlsGeneration()
        {
            this.newsControls.Add(News1);
            this.newsControls.Add(News2);
            this.newsControls.Add(News3);
            this.newsControls.Add(News4);
            this.newsControls.Add(News5);
            this.newsControls.Add(News6);
            this.newsControls.Add(News7);
            this.newsControls.Add(News8);

            this.newsTime.Add(lbl1);
            this.newsTime.Add(lbl2);
            this.newsTime.Add(lbl3);
            this.newsTime.Add(lbl4);
            this.newsTime.Add(lbl5);
            this.newsTime.Add(lbl6);
            this.newsTime.Add(lbl7);
            this.newsTime.Add(lbl8);
        }
        private void SetDefaultPropertiesForControls()
        {
            UpdateStatus_btn.IsEnabled = false;
            UpdateStatus_btn.Opacity = 1;
            Loading.Maximum = 100;
            Loading_Copy.Maximum = 100;
        }

        private void FetchingDataFromSeverInNewThread()
        {
            Thread wdr = new Thread(() =>
            {
                var numOfRetries = 0;
                var maxRetries = 3;
                bool check = false;
                do
                {
                    try
                    {
                        if (news == null)
                        {
                            news = newsUri.FetchDataFromMultipleUris(Encoding.UTF8);
                            //var newsData = news.ToObject<JObject>();
                            //newsData = JsonConvert.DeserializeObject<List<News>>(news[NewsValue.Data].ToString());

                            //JsonSerializerSettings settings = new JsonSerializerSettings
                            //{
                            //    DateParseHandling = DateParseHandling.None
                            //};
                            //settings.Converters.Add(new IsoDateTimeConverter());

                            //newsData = JsonConvert.DeserializeObject<List<News>>(news[NewsValue.Data].ToString(), settings);
                            ////ExtractNews(ref newsData);
                            //var something = newsData;
                            InitNews();
                        }
                        check = true;
                    }
                    catch (Exception ex)
                    {
                        numOfRetries++;
                        if (numOfRetries == maxRetries)
                        {
                            MessageBox.Show($"{MessageBoxContent.GetServerDataFailed.GetDescription()}" +
                                $"\n News sẽ không xuất hiện", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        Debug.WriteLine($"Error in FetchingDataFromSeverInNewThread: {ex}");
                    }
                } while (!check || numOfRetries < maxRetries);
            });
            wdr.Start();
        }
        //private News CreateNewsData(KeyValuePair<string, JToken> json)
        //{
        //    return new News
        //    {
        //        Title = json.Value[NewsValue.Title].ToString(),
        //        Id = json.Value[NewsValue.Id].ToString(),
        //        Slug = json.Value[NewsValue.Slug].ToString(),
        //        CreatedAt = DateTime.Parse(json.Value[NewsValue.CreatedAt].ToString()),
        //        UpdatedAt = DateTime.Parse(json.Value[NewsValue.UpdatedAt].ToString()),
        //        PublishedAt = DateTime.Parse(json.Value[NewsValue.PublishedAt].ToString()),
        //        Url = json.Value[NewsValue.Url].ToString(),
        //        PostTitle = json.Value[NewsValue.PostTitle].ToString(),
        //        Link = json.Value[NewsValue.Link].ToString()
        //    };
        //}
        //private void ExtractNews(ref JObject newsData)
        //{
        //    foreach (var news in newsData)
        //    {
        //        var newsObj = CreateNewsData(news);
        //        this.newsData.Add(newsObj);
        //    }
        //}

        private void Setup(JObject config)
        {
            try
            {
                var something = config;
                var url = config[LauncherFileValue.url];
                this.newsUri = url[LauncherFileValue.NewsUri].ToObject<List<string>>();
                //this.HashSumUri = url[LauncherFileValue.HashSumUri].ToObject<List<string>>();
                this.DownloadFileUri = url[LauncherFileValue.DownloadFileUri].ToObject<List<string>>();
                this.HOME_URL = url[LauncherFileValue.HOME_URL].ToString();
                this.REGISTER_URL = url[LauncherFileValue.REGISTER_URL].ToString();
                this.RECHARGE_URL = url[LauncherFileValue.RECHARGE_URL].ToString();
                this.FANPAGE_URL = url[LauncherFileValue.FANPAGE_URL].ToString();
                this.GROUP_URL = url[LauncherFileValue.GROUP_URL].ToString();
                this.MORE_URL = url[LauncherFileValue.MORE_URL].ToString();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Setup: {ex}");
                MessageBox.Show($"{MessageBoxContent.PrepareDataFailed.GetDescription()}", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

        }

        private void DownloadFileZip(List<string> urls)
        {
            urls.DownloadFileFromMultipleUrls(Settings.LoginServerFile);
            string path = Path.GetFullPath(Settings.LoginServerFile);
            FileExtentions.HlZip.Extract(path, path);
            File.Delete(path);
        }
        //Load news
        private void InitNews()
        {
            try
            {
                //int maxNews = Math.Max(this.newsControls.Count, ((JArray)this.news["data"]["news"]).Count);
                int maxNews = Math.Min(this.newsControls.Count, ((JArray)this.news["data"]).Count);

                for (int i = 0; i < maxNews; i++)
                {
                    string title = news["data"][i]["post_title"].ToString();
                    string str_date = news["data"][i]["updated_at"].ToString();
                    string[] date = str_date.Substring(0, 6).Split('/');
                    
                    //string id = news["data"]["news"][count]["id"].ToString();
                    string url = news["data"][i]["link"].ToString();
                    

                    Button newsControl = this.newsControls[i];
                    Button newsLabel = this.newsTime[i];
                    newsControl.Dispatcher.Invoke(new Action(() =>
                    {
                        if (title.Length > 46)
                            newsControl.Content = title.Substring(0, 46) + " ...";
                        else
                            newsControl.Content = title;
                        if (date[1].Length == 1)
                            date[1] = "0" + date[1];
                        if (date[0].Length == 1)
                            date[0] = "0" + date[0];
                        newsLabel.Content = "(" + date[1] + "/" + date[0] + ")";
                        var isUrl = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
                        
                        if (isUrl)
                        {
                            newsControl.IsEnabled = true;
                            newsControl.DataContext = url;
                            newsControl.ToolTip = "Nhấn để mở";
                            newsLabel.IsEnabled = true;
                            newsLabel.DataContext = url;
                            newsLabel.ToolTip = "Nhấn để mở";
                            Debug.WriteLine("ok");
                        }
                        else
                        {
                            newsControl.ToolTip = "Không thể mở";
                            newsLabel.ToolTip = "Không thể mở";
                        }
                        newsControl.Click += NewsControl_Click;
                    }));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in InitsNews: " + ex.ToString());
                for (int i = 0; i < this.newsControls.Count; i++)
                {
                    Button newsControl = this.newsControls[i];
                    newsControl.Dispatcher.Invoke(new Action(() =>
                    {
                        newsControl.IsEnabled = false;
                        newsControl.Opacity = 0.7;
                    }));
                }
            }
        }

        private bool IsRequireUpdate(bool isForcedUpdate = false)
        {
            try
            {
                
                Version localVersion;
                if (File.Exists(versionFile))
                {
                    localVersion = new Version(File.ReadAllText(versionFile));
                    Ver.Text = localVersion.ToString();
                }
                else
                {
                    localVersion = Version.zero;
                    Ver.Text = localVersion.ToString();
                }


                var version = hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString();

                //var versionFileDetail = something
                //    .Where(file => file[HashSumFileValue.FileName].Value<string>() == Settings.VersionFile)
                //    .FirstOrDefault();
                //if (versionFileDetail != null)
                //{
                //    var versionFileUris = versionFileDetail[LibFileValue.DownloadLink]
                //        .Select(linkDetail => linkDetail[LibFileValue.Url].Value<string>()).ToList();
                //    DownloadFileZip(versionFileUris);
                //}
                File.WriteAllText(Path.GetFullPath(Settings.VersionFile), version);

                Version onlineVersion = new Version(File.ReadAllText(versionFile)); //Server verion
                VerSer.Text = $"({onlineVersion})";
                if (isForcedUpdate)
                {
                    return true;
                }
                else if (onlineVersion.Equals(localVersion))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{MessageBoxContent.ErrorWhileDownloading.GetDescription()}" ,"TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                VerSer.Text = $"(Không thể kết nối với máy chủ)";
                Debug.WriteLine($"IsRequireUpdate error: {ex}");
            }
            return true;
        }



        private void Update(bool forceRecheck = false)
        {
            Status = LauncherStatus.Checking;
            UpdateStatus_btn.IsEnabled = false;
            UpdateStatus_btn.Opacity = 1;
            Loading.Visibility = Visibility.Visible;
            Progress.Visibility = Visibility.Visible;
            Analyzing.Visibility = Visibility.Visible;
            PlayGame.IsEnabled = false;
            
            PlayGame.Opacity = 0.7;
            FixClient.IsEnabled = false;
            FixClient.Opacity = 0.7;

            if (File.Exists(gameExe))
            {
                bool isFileinUse = IsFileInUse(gameExe);
                if (isFileinUse)
                {
                    MessageBoxResult mbr = MessageBox.Show(MessageBoxContent.TurnOffGame.GetDescription(), "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                    switch (mbr)
                    {
                        case MessageBoxResult.OK:
                            Process[] game = Process.GetProcessesByName("Game");
                            game[0].Kill();
                            break;
                        case MessageBoxResult.Cancel:
                            Environment.Exit(0);
                            break;
                        default:
                            Environment.Exit(0);
                            break;
                    }
                }
            }

            AnalyzeRequiredFiles();
            
            this.isUpdating = true;
            this.Status = LauncherStatus.downloadingUpdate;
            
            Thread preVerifyThread = new Thread(() => { this.PreVerifyThread(); });
            preVerifyThread.Start();

            Thread downloadThread = new Thread(() => { this.DownloadThread(); });
            downloadThread.Priority = ThreadPriority.Highest;
            downloadThread.Start();

            Thread extractThread = new Thread(() => { this.ExtractThread(); });
            extractThread.Start();

            Thread progressThread = new Thread(() => {
                //Summary
                int total = this.updateFiles.Count;
                int completed = 0;
                while (this.isUpdating && !this.isFailed && completed != total)
                {
                    completed = 0;
                    foreach (var x in this.updateFiles)
                    {
                        if (x[LibFileValue.State].ToString() == StateValue.done.ToString())
                            completed++;
                    }

                    try
                    {
                        int completedPercent = (int)(completed * 100 / total);
                        Progress.Dispatcher.Invoke(new Action(() => { Progress.Content = completedPercent.ToString() + "%"; }));
                        Loading_Copy.Dispatcher.Invoke(new Action(() => { Loading_Copy.Value = completedPercent; }));
                    }
                    catch
                    {

                    }

                    Thread.Sleep(300);
                }

                if (completed == total)
                {
                    File.WriteAllText(Path.GetFullPath(HashSumFileValue.VersionKey), hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString());
                    File.WriteAllText(Path.GetFullPath(Settings.LibFile), this.updateFiles.ToString());
                    Thread.Sleep(2000);
                }

                if (isFailed)
                {
                    MessageBox.Show(MessageBoxContent.ErrorWhileDownloading.GetDescription(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Action action = () =>
                {
                    Status = LauncherStatus.ready;
                    Analyzing.Opacity = 0;
                    UpdateStatus_btn.IsEnabled = false;
                    UpdateStatus_btn.Opacity = 1;
                    Loading.Value = 100;
                    Loading_Copy.Value = 100;
                    Progress.Visibility = Visibility.Hidden;
                    OneFileProgress.Visibility = Visibility.Hidden;
                    FileName.Visibility = Visibility.Hidden;
                    PlayGame.IsEnabled = true;
                    PlayGame.Opacity = 1;
                    FixClient.IsEnabled = true;
                    FixClient.Opacity = 1;
                    Ver.Text = hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString();
                };
                this.Dispatcher.Invoke(action);
                if (fsHashSum != null)
                    fsHashSum.Close();
            });
            progressThread.Priority = ThreadPriority.Lowest;
            progressThread.Start();

        }

        private void PreVerifyThread(bool forceRecheck = false)
        {
            try
            {
                for (int i = 0; i < this.updateFiles.Count && this.isUpdating; i++)
                {
                    var file = this.updateFiles[i];
                    if (file[LibFileValue.State].ToString() == StateValue.done.ToString())
                    {
                        continue;
                    }
                    string filePath = file[LibFileValue.Path].ToString();
                    string hash;
                    if (File.Exists(Path.GetFullPath(filePath)))
                    {
                        hash = filePath.CalculateMD5();
                        if (hash == file[LibFileValue.Hash].ToString())
                        {
                            file[LibFileValue.State] = StateValue.done.ToString();
                            continue;
                        }
                        file[LibFileValue.Hash] = hash;
                    }
                    file[LibFileValue.State] = StateValue.preVerified.ToString();
                }
                File.WriteAllText(Path.GetFullPath(Settings.LibFile), updateFiles.ToString());
            }
            catch
            {
                Action action = () => { Status = LauncherStatus.failed; };
                this.Dispatcher.Invoke(action);
                isFailed = true;
            }
        }

        private void DownloadThread()
        {
            try
            {
                int waitPoint = 0;
                for (int i = 0; i < this.updateFiles.Count && this.isUpdating && !this.isFailed; i++)
                {
                    var file = this.updateFiles[i];
                    //Waiting for state is preVerified
                    while (this.isUpdating)
                    {
                        if (file[LibFileValue.State].ToString() == StateValue.preVerified.ToString() || file[LibFileValue.State].ToString() == StateValue.done.ToString())
                            break;
                        Action action1 = () =>
                        {
                            if (waitPoint == 0)
                                Analyzing.Content = " Đang phân tích";
                            if (waitPoint < 3)
                            {
                                Analyzing.Content += ".";
                                waitPoint++;
                            }
                            else
                                waitPoint = 0;
                        };
                        this.Dispatcher.Invoke(action1);
                        Thread.Sleep(1000);
                    }
                    if (isFailed)
                        break;
                    //Skip welldone json
                    if (file[LibFileValue.State].ToString() == StateValue.done.ToString() || file[LibFileValue.State].ToString() == StateValue.downloaded.ToString())
                    {
                        continue;
                    }
                    Action action = () => {
                        Analyzing.Visibility = Visibility.Hidden;
                        OneFileProgress.Visibility = Visibility.Visible;
                        FileName.Visibility = Visibility.Visible;
                    };
                    this.Dispatcher.Invoke(action);
                    //Download json
                    string localFilePath = file[LibFileValue.Path].ToString();
                    string downloadFilePath = file[LibFileValue.Path].ToString() + FileExtentions.HlZip.GetDescription();
                    string fileUri = DownloadFileUri + file[LibFileValue.Path].ToString() + FileExtentions.HlZip.GetDescription();

                    var downloadLinkDetails = file[LibFileValue.DownloadLink].ToObject<List<DownloadLinkDetail>>();
                    if(priorityMirror != "")
                    {
                        fileUri = downloadLinkDetails
                        .Where(downloadLinkDetail => downloadLinkDetail.Mirror == priorityMirror)
                        .Select(downloadLinkDetail => downloadLinkDetail.Url).FirstOrDefault();
                    }
                    else fileUri = downloadLinkDetails.FirstOrDefault().Url;
                                        
                    DownLoadFile(downloadFilePath, localFilePath, fileUri);
                    if (!isFailed)
                    {
                        file[LibFileValue.State] = StateValue.downloaded.ToString();
                    }
                    else
                    {
                        action = () =>
                        {
                            Status = LauncherStatus.failed;
                        };
                        this.Dispatcher.Invoke(action);
                        MessageBox.Show(MessageBoxContent.UpdateFailed.GetDescription(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        file[LibFileValue.State] = StateValue.failed.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in DownloadThread: " + ex.ToString());
                Action action = () =>
                {
                    Status = LauncherStatus.failed;
                    FixClient.IsEnabled = true;
                    FixClient.Opacity = 1;
                };
                this.Dispatcher.Invoke(action);
                MessageBox.Show(MessageBoxContent.NetworkError.GetDescription(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void DownLoadFile(string downloadFilePath, string localFilePath, string fileUri)
        {
            try
            {
                downloadFilePath = Path.GetFullPath(downloadFilePath);

                string destinationDirectory = Path.GetDirectoryName(downloadFilePath);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                string[] nameoffile = localFilePath.Split('/');
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Wc_DownloadProgressChanged);
                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                var syncObject = new Object();
                lock (syncObject)
                {
                    wc.DownloadFileAsync(new Uri(fileUri), downloadFilePath, syncObject);
                    FileName.Dispatcher.Invoke(new Action(() => {
                        stopwatch.Start();
                        FileName.Content = nameoffile[nameoffile.Length - 1];
                    }));
                    Monitor.Wait(syncObject);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DownLoadFile: {ex}");
                this.isFailed = true;
            }
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            lock (e.UserState)
            {
                isFailed = false;
                stopwatch.Reset();
                Monitor.PulseAll(e.UserState);
            }
        }
        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                string downloadSpeed = string.Format("{0} MB/s", (e.BytesReceived / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds).ToString("0.00"));
                Action action = () => {
                    OneFileProgress.Content = " - " + downloadSpeed + " - " + e.ProgressPercentage + "%";
                    Loading.Value = e.ProgressPercentage;
                };
                this.Dispatcher.Invoke(action);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Wc_DownloadProgressChanged failed: {ex}");
            }
        }

        private void ExtractThread()
        {
            try
            {
                for (int i = 0; i < this.updateFiles.Count && this.isUpdating; i++)
                {
                    var file = this.updateFiles[i];
                    
                    //Waiting for state is preVerified
                    while (this.isUpdating)
                    {
                        if (file[LibFileValue.State].ToString() == StateValue.downloaded.ToString() || file[LibFileValue.State].ToString() == StateValue.done.ToString())
                            break;

                        Thread.Sleep(10);
                    }

                    //Skip well done json
                    if (file[LibFileValue.State].ToString() == StateValue.done.ToString())
                    {
                        continue;
                    }

                    //Extract json
                    string localFilePath = file[LibFileValue.Path].ToString();
                    string downloadFilePath = file[LibFileValue.Path].ToString() + FileExtentions.HlZip;
                    bool _isExtracting = true;
                    try
                    {
                        FileExtentions.HlZip.Extract(downloadFilePath, downloadFilePath);
                        File.Delete(downloadFilePath);
                        _isExtracting = false;
                    }
                    catch (Exception ex)
                    {
                        file[LibFileValue.State] = StateValue.failed.ToString();
                        File.Delete(downloadFilePath);
                        Debug.WriteLine(ex.ToString());
                        continue;
                    }

                    if (!_isExtracting)
                    {
                        file[LibFileValue.State] = StateValue.done.ToString();
                    }
                    else
                    {
                        Debug.WriteLine("ExtractThread: Failed");
                        isFailed = true;
                        file[LibFileValue.State] = StateValue.failed.ToString();
                        this.Dispatcher.Invoke(new Action(() => { Status = LauncherStatus.failed; }));
                    }
                }
            }
            catch
            {
                MessageBox.Show("Đã xảy ra lỗi! Nhấn Sửa lỗi để khắc phục!");
            }
        }

        private HashSumFileDetail CreateHashSumFileDetail(KeyValuePair<string, JToken> file, StateValue state)
        {
            var downloadLinks = JsonConvert.DeserializeObject<List<DownloadLinkDetail>>(file.Value[HashSumFileValue.DownLoadUri].ToString());
            return new HashSumFileDetail
            {
                Path = file.Key,
                Hash = file.Value[HashSumFileValue.Hash].ToString(),
                State = state.ToString(),
                DownloadLink = downloadLinks ?? new List<DownloadLinkDetail>()
            };
        }

        private void ExtractFileHashSum(
            ref List<HashSumFileDetail> hashSumFileDetails,
            JArray updateFiles = null,
            StateValue state = StateValue.added)
        {
            try
            {
                HashSumFileDetail lastFileToDownload = null;
                int count = 0;
                foreach (var file in clientFiles)
                {
                    if (HashSumFileValue.VersionKey.Contains(file.Key))
                    {
                        continue;
                    }
                    var fileObj = CreateHashSumFileDetail(file, state);
                    if (state == StateValue.done)
                    {
                        if (file.Key == hashSumFileDetails[count].Path.ToString())
                        {
                            if (file.Value[HashSumFileValue.Hash].ToString() == hashSumFileDetails[count].Hash 
                                && hashSumFileDetails[count].State == state.ToString())
                            {
                                fileObj.State = state.ToString();
                            }
                            count++;
                            updateFiles.Add(JObject.FromObject(fileObj));
                        }
                        else if (file.Key != Settings.GameFilePath)
                        {
                            updateFiles.Add(JObject.FromObject(fileObj));
                        }
                        else
                        {
                            if (file.Value[HashSumFileValue.Hash].ToString() == hashSumFileDetails[hashSumFileDetails.Count - 1].Hash 
                                && hashSumFileDetails[hashSumFileDetails.Count - 1].State == state.ToString())
                            {
                                fileObj.State = state.ToString();
                            }
                            lastFileToDownload = fileObj;
                        }
                    }
                    else if (fileObj.Path == Settings.GameFilePath && updateFiles != null)
                    {
                        lastFileToDownload = fileObj;
                        continue;
                    }
                    else if(updateFiles != null)
                    {
                        updateFiles.Add(JObject.FromObject(fileObj));
                    }
                    else hashSumFileDetails.Add(fileObj);
                }
                if(lastFileToDownload != null)
                    updateFiles.Add(JObject.FromObject(lastFileToDownload));

                if(updateFiles != null)
                    File.WriteAllText(Path.GetFullPath(Settings.LibFile), JsonConvert.SerializeObject(updateFiles));
                else
                    File.WriteAllText(Path.GetFullPath(Settings.LibFile), JsonConvert.SerializeObject(hashSumFileDetails));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExtractFileHashSum: {ex}");
            }
        }

        private void GetTheFastestMirrorUri()
        {
            try
            {
                var mirrors = hashSumData[HashSumFileValue.Mirrors].ToObject<List<Mirror>>();
                var speedTestUris = mirrors.Select(mirror => mirror.TestFile).ToList();
                var fastestUri = speedTestUris.GetFastestLink();
                priorityMirror = mirrors.FirstOrDefault(mirror => mirror.TestFile == fastestUri).Id;
            }
            catch (Exception)
            {
                priorityMirror = "";
            }
        }

        private void AnalyzeRequiredFiles()
        {
            try
            {
                //hashSumData = DownloadFileUri.FetchDataFromMultipleUris(Encoding.Default, Settings.HashSumFile);
                clientFiles = hashSumData[HashSumFileValue.Data].ToObject<JObject>();
                GetTheFastestMirrorUri();
                var hashSumFileDetail = new List<HashSumFileDetail>();
                bool isEmty = false;
                if (File.ReadAllText(Path.GetFullPath(Settings.LibFile)) == "")
                {
                    ExtractFileHashSum(ref hashSumFileDetail);
                    isEmty = true;
                }
                else
                {
                    try
                    {
                        var fileLibValue = File.ReadAllText(Path.GetFullPath(Settings.LibFile));
                        hashSumFileDetail = JsonConvert.DeserializeObject<List<HashSumFileDetail>>(fileLibValue);
                    }
                    catch
                    {
                        ExtractFileHashSum(ref hashSumFileDetail);
                    }
                }
                var hashSumFileDetailUrls = hashSumFileDetail
                    .Where(HashSumFileDetail => HashSumFileDetail.Path == Settings.LoginServerFile)
                    .FirstOrDefault();
                var loginServerFileUris = hashSumFileDetailUrls.DownloadLink.Select(linkDetail => linkDetail.Url).ToList();
                DownloadFileZip(loginServerFileUris);

                if (!isClick && !isEmty)
                    ExtractFileHashSum(ref hashSumFileDetail, updateFiles, StateValue.done);
                else
                    ExtractFileHashSum(ref hashSumFileDetail, updateFiles);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in AnalyzeRequiredFiles: " + ex.ToString());
                MessageBoxResult mbr = MessageBox.Show($"{MessageBoxContent.GetServerDataFailed.GetDescription()}", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                switch (mbr)
                {
                    case MessageBoxResult.OK:
                        Environment.Exit(0);
                        break;
                    default:
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private bool IsFileInUse(string path)
        {
            try
            {
                if (File.Exists(path))
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) { }
                else
                {
                    MessageBoxResult mbr = MessageBox.Show("Không thể tìm thấy trò chơi! Nhấn Sửa lỗi để khắc phục", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                    switch (mbr)
                    {
                        case MessageBoxResult.OK:
                            this.Update(true);
                            break;
                        default:
                            return true;
                            //break;
                    }
                    return false;
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IsFileInUse: File is in use {ex}");
                return true;
            }
            return false;
        }

        #endregion

        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            else return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue) { }

        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion

        #region Grid Events
        private void MainGrid_SizeChanged(object sender, EventArgs e) => CalculateScale();

        private void CalculateScale()
        {
            double xScale = ActualWidth / WIDTH;
            double yScale = ActualHeight / HEIGHT;
            double value = Math.Min(xScale, yScale);

            ScaleValue = (double)OnCoerceScaleValue(myMainWindow, value);
        }
        #endregion

        #region Button Events
        private void Exit_Button(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Minimize_Button(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void NewsControl_Click(object sender, RoutedEventArgs e)
        {
            Button senderButton = (Button)sender;
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = (string)senderButton.DataContext;
                Process.Start(psi);
            }
            catch
            {

            }
        }
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = HOME_URL;
                Process.Start(psi);
            }
            catch
            {
                MessageBox.Show("Máy đang quá tải!");
            }
        }
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = REGISTER_URL;
                Process.Start(psi);
            }
            catch
            {
                MessageBox.Show("Máy đang quá tải!");
            }
        }

        private void Recharge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = RECHARGE_URL;
                Process.Start(psi);
            }
            catch
            {
                MessageBox.Show("Máy đang quá tải!");
            }
        }

        private void Fanpage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = FANPAGE_URL;
                Process.Start(psi);
            }
            catch
            {
                MessageBox.Show("Máy đang quá tải!");
            }
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = GROUP_URL;
                Process.Start(psi);
            }
            catch
            {

            }
        }

        #region More_btn
        private void More_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = true;
                psi.FileName = MORE_URL;
                Process.Start(psi);
            }
            catch
            {

            }
        }
        #endregion 
        #region UpdateStatus_btn
        private void UpdateStatus_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                updateFiles.Clear();
                File.Delete(Path.GetFullPath("Lib.txt"));
                fsHashSum = new FileStream(Path.GetFullPath("Lib.txt"), FileMode.Create, FileAccess.ReadWrite);
                fsHashSum.Close();
                this.Update(true);
                isFailed = false;
            }
            catch
            {
                MessageBox.Show("Đã xảy ra lỗi! Nhấn Sửa lỗi để khắc phục!");
            }
        }
        private void UpdateStatus_btn_MouseEnter(object sender, MouseEventArgs e)
        {
            if (UpdateStatus_btn.IsEnabled)
                UpdateStatus_btn.Opacity = 0.95;
        }
        private void UpdateStatus_btn_MouseLeave(object sender,MouseEventArgs e)
        {
            if (UpdateStatus_btn.IsEnabled)
                UpdateStatus_btn.Opacity = 1;
        }
        #endregion
        #region PlayGame_btn
        private void PlayGame_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (PlayGame.IsEnabled)
                PlayGame.Opacity = 0.95;
        }
        private void PlayGame_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (PlayGame.IsEnabled)
                PlayGame.Opacity = 1;
        }
        private void PlayGame_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                string strCmdText = "cd Bin\r\nstart Game.exe -fl\r\ngoto exit";
                string cmdFileName = "StartGame.cmd";

                try
                {
                    if (!File.Exists(System.IO.Path.GetFullPath(cmdFileName)))
                    {
                        fsHashSum = new FileStream(System.IO.Path.GetFullPath(cmdFileName), FileMode.Create, FileAccess.ReadWrite);
                        fsHashSum.Close();
                        File.WriteAllText(System.IO.Path.GetFullPath(cmdFileName), strCmdText);
                    }

                    Process.Start(cmdFileName);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {

                    MessageBox.Show("Có lỗi xảy ra: " + ex.ToString(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Không thể tìm thấy trò chơi! Nhấn Sửa lỗi để khắc phục", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
        #region FixClient_btn
        private void FixClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isClick = true;
                updateFiles.Clear();
                File.Delete(Path.GetFullPath("Lib.txt"));
                fsHashSum = new FileStream(Path.GetFullPath("Lib.txt"), FileMode.Create, FileAccess.ReadWrite);
                fsHashSum.Close();
                this.Update(true);
                isClick = false;
                isFailed = false;
            }
            catch
            {
                MessageBox.Show("Đã xảy ra lỗi! Nhấn Sửa lỗi để khắc phục!");
            }
        }

        private void FixClient_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (FixClient.IsEnabled)
                FixClient.Background.Opacity = 0.95;

        }
        private void FixClient_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (FixClient.IsEnabled)
                FixClient.Background.Opacity = 1;
        }

        #endregion
        #endregion

        #region Window Events
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    this.DragMove();
            }
            catch
            {
                
            }
        }
        #endregion
    }
}
