using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Security.Authentication;
using System.Security.Cryptography;


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
        }

        #region Configurable Varibles
        private static readonly double WIDTH = 1080f;
        private static readonly double HEIGHT = 740f;
        private string newsUri = null;
        private string HashSumUri = null;
        private string DownloadFileUri = null;
        private string HOME_URL = null;
        private string REGISTER_URL = null;
        private string RECHARGE_URL = null;
        private string FANPAGE_URL = null;
        private string GROUP_URL = null;
        private string NEWBIE_URL = null;
        private string MORE_URL = null;

        //Hash sum
        FileStream fsHashSum = null;

        //Download file from server
        private string versionFile;
        private string gameExe;
        private JObject json = null;
        private JObject clientFiles;
        protected bool isFailed = false;

        private List<string> bannersDatacontext = new List<string>();
        private int maxBanners = 6;
        protected string localBannerDir = "Banner";
        protected string setting = @"Bin\Launcher\Setting.txt";
        private string ipSavedFile = @"Bin\Launcher\Save.txt";
        private string modeSavedFile = @"Bin\mode.cfig";
        string[] names_path;
        string[] names;
        string[] paths;
        // As global variables
        private JObject news = null;
        private List<Button> newsControls = new List<Button>();
        private List<Button> newsTime = new List<Button>();
        private List<string> banners = new List<string>();
        private JArray updateFiles = new JArray();
        private bool isUpdating = false;

        //Check FixClient on click
        private bool isClick = false;

        //CLock
        private readonly Stopwatch stopwatch = new Stopwatch();

        // Tls12
        public const SslProtocols _Tls12 = (SslProtocols)0x00000C00;
        public const SecurityProtocolType Tls12 = (SecurityProtocolType)_Tls12;

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

        private JObject FetchNews()
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Encoding = System.Text.Encoding.UTF8;
                string newsData = wc.DownloadString(new Uri(newsUri));
                return JObject.Parse(newsData);
            }
            catch
            {
                return null;
            }
        }

        private void Setup(JObject config)
        {
            this.newsUri = config["url"]["NewsUri"].ToString();
            this.HashSumUri = config["url"]["HashSumUri"].ToString();
            this.DownloadFileUri = config["url"]["DownloadFileUri"].ToString();
            this.HOME_URL = config["url"]["HOME_URL"].ToString();
            this.REGISTER_URL = config["url"]["REGISTER_URL"].ToString();
            this.RECHARGE_URL = config["url"]["RECHARGE_URL"].ToString();
            this.FANPAGE_URL = config["url"]["FANPAGE_URL"].ToString();
            this.GROUP_URL = config["url"]["GROUP_URL"].ToString();
            this.NEWBIE_URL = config["url"]["NEWBIE_URL"].ToString();
            this.MORE_URL = config["url"]["MORE_URL"].ToString();

            try
            {
                string loginServer = @"Patch\LoginServer.txt.hlzip";
                Utils.DownloadFile(DownloadFileUri + "Patch/LoginServer.txt.hlzip", "Patch/LoginServer.txt.hlzip");
                string path = Path.GetFullPath(loginServer);
                Utils.Extract(path, path);
                File.Delete(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Set up failed: " + ex.ToString(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

        }
        //Load news
        private void InitsNews()
        {
            try
            {
                //int maxNews = Math.Max(this.newsControls.Count, ((JArray)this.news["data"]["news"]).Count);
                int maxNews = Math.Min(this.newsControls.Count, ((JArray)this.news["data"]["news"]).Count);

                for (int i = 0; i < maxNews; i++)
                {
                    string title = news["data"]["news"][i]["post_title"].ToString();
                    string str_date = news["data"]["news"][i]["post_date"].ToString();
                    string[] date = str_date.Substring(0, 6).Split('/');
                    //Debug.WriteLine(date[0] + "/" + date[1]);

                    //string id = news["data"]["news"][i]["id"].ToString();
                    string url = news["data"]["news"][i]["link"].ToString();
                    //Debug.WriteLine(url);

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
                        //Debug.WriteLine(isUrl);
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


        private bool IsRequireUpdate()
        {
            try
            {
                Version localVersion;
                if (File.Exists(versionFile))
                {
                    localVersion = new Version(File.ReadAllText(versionFile));
                    //Debug.WriteLine(localVersion.ToString());
                    Ver.Text = localVersion.ToString();
                }
                else
                {
                    localVersion = Version.zero;
                    Ver.Text = localVersion.ToString();
                }
                try
                {
                    Utils.DownloadFile(DownloadFileUri + @"(version).hlzip", "(version).hlzip");
                    Utils.Extract(Path.GetFullPath("(version).hlzip"), Path.GetFullPath("(version).hlzip"));
                    File.Delete(Path.GetFullPath("(version).hlzip"));
                    Version onlineVersion = new Version(File.ReadAllText(versionFile)); //Server verion
                    VerSer.Text = $"({onlineVersion})";
                    //Debug.WriteLine(onlineVersion.ToString());


                    if (onlineVersion.Equals(localVersion))
                    {
                        return false;
                    }
                    ////Debug.WriteLine(File.ReadAllText(Path.GetFullPath("(version)")));
                }
                catch (Exception ex)
                {

                }
            }
            catch
            {
            }
            return true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            JObject configFile = Utils.ReadConfig();
            Setup(configFile);
            try
            {
                // Todo: Using dynamic controls generation
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

                //Set default properties for controls
                UpdateStatus_btn.IsEnabled = false;
                UpdateStatus_btn.Opacity = 1;
                Loading.Maximum = 100;
                Loading_Copy.Maximum = 100;

                //Fetching data from server in new thread
                Thread wdr = new Thread(() =>
                {
                    bool check = false;
                    do
                    {
                        try
                        {
                            if (news == null)
                            {
                                news = FetchNews();
                                //Debug.WriteLine("Fetch news success");
                                InitsNews();
                                //Debug.WriteLine("Init news success");
                            }
                            check = true;
                        }
                        catch
                        {
                            
                        }
                    } while (!check);
                });
                wdr.Start();

                ////Debug.WriteLine("Checking update");
                bool _isRequireUpdate = false;
                Status = LauncherStatus._verifying;
                if (!File.Exists(Path.GetFullPath("Lib.txt")))
                {
                    _isRequireUpdate = true;
                    fsHashSum = new FileStream(Path.GetFullPath("Lib.txt"), FileMode.Create, FileAccess.ReadWrite);
                    fsHashSum.Close();
                }

                _isRequireUpdate = this.IsRequireUpdate();
                //Debug.WriteLine(_isRequireUpdate);
                if (_isRequireUpdate)
                {
                    ////Debug.WriteLine("UPdate required");
                    this.Update(true);
                }
                else
                {
                    //Debug.WriteLine("Done");
                    Status = LauncherStatus.ready;
                    Progress.Visibility = Visibility.Hidden;
                    OneFileProgress.Visibility = Visibility.Hidden;
                    FileName.Visibility = Visibility.Hidden;
                    PlayGame.IsEnabled = true;
                    if (!File.Exists(Path.GetFullPath(modeSavedFile)))
                        File.Create(Path.GetFullPath(modeSavedFile)).Close();

                    if (!File.Exists(Path.GetFullPath(ipSavedFile)))
                        File.Create(Path.GetFullPath(ipSavedFile)).Close();
                    File.WriteAllText(Path.GetFullPath(ipSavedFile), Utils.GetPublicIpAddress());
                }

            }
            catch
            {

            }
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
            //selectMode.IsEnabled = false;
            PlayGame.Opacity = 0.7;
            FixClient.IsEnabled = false;
            FixClient.Opacity = 0.7;

            if (File.Exists(gameExe))
            {
                bool isFileinUse = IsFileInUse(gameExe);
                if (isFileinUse)
                {
                    MessageBoxResult mbr = MessageBox.Show("Hãy tắt trò chơi", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
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
            this.AnalyzeRequiredFiles();
            //Debug.WriteLine("Analyze");
            this.isUpdating = true;
            this.Status = LauncherStatus.downloadingUpdate;
            //Debug.WriteLine("Update");
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
                        if (x["state"].ToString() == "done")
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
                    File.WriteAllText(Path.GetFullPath("(version)"), json["data"]["version"].ToString());
                    File.WriteAllText(Path.GetFullPath("Lib.txt"), this.updateFiles.ToString());
                    Thread.Sleep(2000);
                }

                if (isFailed)
                {
                    MessageBox.Show("Có lỗi xảy ra trong quá trình tải!", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    //selectMode.IsEnabled = true;
                    PlayGame.Opacity = 1;
                    FixClient.IsEnabled = true;
                    FixClient.Opacity = 1;
                    Ver.Text = json["data"]["version"].ToString();
                    //setMode();
                    ////Debug.WriteLine(selectMode.DataContext.ToString());
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
            ////Debug.WriteLine("Start: Pre Verify Thread");
            try
            {
                for (int i = 0; i < this.updateFiles.Count && this.isUpdating; i++)
                {
                    var file = this.updateFiles[i];
                    if (file["state"].ToString() == "done")
                    {
                        continue;
                    }
                    string filePath = file["path"].ToString();
                    string hash;
                    if (File.Exists(Path.GetFullPath(filePath)))
                    {
                        hash = Utils.CalculateMD5(filePath);
                        if (hash == file["hash"].ToString())
                        {
                            file["state"] = "done";
                            continue;
                        }
                        file["hash"] = hash;
                    }
                    file["state"] = "preVerified";
                }
                File.WriteAllText(Path.GetFullPath("Lib.txt"), updateFiles.ToString());
            }
            catch
            {
                Action action = () => { Status = LauncherStatus.failed; };
                this.Dispatcher.Invoke(action);
                isFailed = true;
            }

            ////Debug.WriteLine("Complete: Pre Verify Thread");
        }

        private void DownloadThread()
        {
            ////Debug.WriteLine("Start: Download Thread");
            try
            {
                int waitPoint = 0;
                for (int i = 0; i < this.updateFiles.Count && this.isUpdating && !this.isFailed; i++)
                {
                    var file = this.updateFiles[i];
                    ////Debug.WriteLine("Downloading " + file["path"]);
                    //Waiting for state is preVerified
                    while (this.isUpdating)
                    {
                        if (file["state"].ToString() == "preVerified" || file["state"].ToString() == "done")
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
                    //Skip welldone file
                    if (file["state"].ToString() == "done" || file["state"].ToString() == "downloaded")
                    {
                        continue;
                    }
                    Action action = () => {
                        Analyzing.Visibility = Visibility.Hidden;
                        OneFileProgress.Visibility = Visibility.Visible;
                        FileName.Visibility = Visibility.Visible;
                    };
                    this.Dispatcher.Invoke(action);
                    //Download file
                    string localFilePath = file["path"].ToString();
                    string downloadFilePath = file["path"].ToString() + ".hlzip";
                    string fileUri = DownloadFileUri + file["path"].ToString() + ".hlzip";
                    if (!file["download-link"].ToString().Equals(""))
                        fileUri = (string)file["download-link"][0];

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
                    catch
                    {
                        //MessageBox.Show("Server is overloading!!");
                        this.isFailed = true;
                    }
                    if (!isFailed)
                    {
                        file["state"] = "downloaded";
                    }
                    else
                    {
                        action = () =>
                        {
                            Status = LauncherStatus.failed;
                        };
                        this.Dispatcher.Invoke(action);
                        MessageBox.Show("Update failed!!!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        file["state"] = "failed";
                        break;
                    }

                    ////Debug.WriteLine("Downloaded: " + file["path"]);
                    //continue;
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
                MessageBox.Show("Lỗi đường truyền", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //Debug.WriteLine("Download Thread error: " + ex.Message);
            }
            ////Debug.WriteLine("Complete: Download Thread");

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
            //Action action = () => { OneFileProgress.Content = e.ProgressPercentage.ToString() + "%";  };
            try
            {
                string downloadSpeed = string.Format("{0} MB/s", (e.BytesReceived / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds).ToString("0.00"));
                Action action = () => {
                    OneFileProgress.Content = " - " + downloadSpeed + " - " + e.ProgressPercentage + "%";
                    Loading.Value = e.ProgressPercentage;
                };
                this.Dispatcher.Invoke(action);
                //Debug.WriteLine("DownloadSpeed:" + downloadSpeed);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
            }
        }

        private void ExtractThread()
        {
            ////Debug.WriteLine("Start: Extract Thread");
            try
            {
                for (int i = 0; i < this.updateFiles.Count && this.isUpdating; i++)
                {
                    var file = this.updateFiles[i];
                    ////Debug.WriteLine("Extracting: " + file["path"]);
                    //Waiting for state is preVerified
                    while (this.isUpdating)
                    {
                        if (file["state"].ToString() == "downloaded" || file["state"].ToString() == "done")
                            break;

                        Thread.Sleep(10);
                    }

                    //Skip welldone file
                    if (file["state"].ToString() == "done")
                    {
                        continue;
                    }

                    //Extract file
                    string localFilePath = file["path"].ToString();
                    string downloadFilePath = file["path"].ToString() + ".hlzip";
                    bool _isExtracting = true;
                    try
                    {
                        Utils.Extract(downloadFilePath, downloadFilePath);
                        File.Delete(downloadFilePath);
                        _isExtracting = false;
                    }
                    catch (Exception ex)
                    {
                        //string[] filename = localFilePath.Split("/");
                        //if (IsFileInUse(Path.GetFullPath(localFilePath)))
                        //{
                        //    MessageBoxResult mbr = MessageBox.Show("Please close " + filename[filename.Length - 1], "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        //    switch (mbr)
                        //    {
                        //        case MessageBoxResult.OK:
                        //            Environment.Exit(0);
                        //            break;
                        //        case MessageBoxResult.Cancel:
                        //            Environment.Exit(0);
                        //            break;
                        //        default:
                        //            Environment.Exit(0);
                        //            break;
                        //    }
                        //}
                        file["state"] = "failed";
                        File.Delete(downloadFilePath);
                        Debug.WriteLine(ex.ToString());
                        continue;
                    }

                    if (!_isExtracting)
                    {
                        file["state"] = "done";
                    }
                    else
                    {
                        Debug.WriteLine("Failed");
                        isFailed = true;
                        file["state"] = "failed";
                        this.Dispatcher.Invoke(new Action(() => { Status = LauncherStatus.failed; }));
                    }
                    ////Debug.WriteLine("Extracted: " + file["path"]);
                }
            }
            catch
            {
                MessageBox.Show("Đã xảy ra lỗi! Nhấn Sửa lỗi để khắc phục!");
            }
            ////Debug.WriteLine("Complete: Extract Thread");
        }

        private void AnalyzeRequiredFiles(bool forceRecheck = false)
        {
            try
            {
                this.json = this.FetchHashSum();
                if (this.json != null)
                {
                    this.clientFiles = this.json["data"].ToObject<JObject>();
                }
                else
                {
                    MessageBoxResult mbr = MessageBox.Show("Máy chủ đang quá tải!", "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
                Debug.WriteLine("Error in AnalyzeRequiredFiles: " + ex.ToString());
                MessageBoxResult mbr = MessageBox.Show("Máy chủ đang quá tải!\n Lỗi: " + ex.ToString(), "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
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
            JArray localFiles = new JArray();
            bool isEmty = false;
            if (File.ReadAllText(Path.GetFullPath("Lib.txt")) == "")
            {
                foreach (var file in clientFiles)
                {
                    if (file.Key == "(version)")
                    {
                        continue;
                    }
                    JObject fileObj = new JObject();
                    fileObj["path"] = file.Key;
                    fileObj["hash"] = "";
                    fileObj["state"] = "";
                    fileObj["download-link"] = "";
                    localFiles.Add(fileObj);
                }
                File.WriteAllText(Path.GetFullPath("Lib.txt"), localFiles.ToString());
                isEmty = true;
            }
            else
            {
                try
                {
                    string _localFiles = File.ReadAllText(Path.GetFullPath("Lib.txt"));
                    localFiles = JArray.Parse(_localFiles);
                }
                catch
                {
                    foreach (var file in clientFiles)
                    {
                        if (file.Key == "version")
                        {
                            continue;
                        }
                        JObject fileObj = new JObject();
                        fileObj["path"] = file.Key;
                        fileObj["hash"] = "";
                        fileObj["state"] = "";
                        fileObj["download-link"] = "";
                        localFiles.Add(fileObj);
                    }
                    File.WriteAllText(Path.GetFullPath("Lib.txt"), localFiles.ToString());
                }
            }
            int i = 0;
            JObject tempObj = null;
            if (!isClick && !isEmty)
            {
                foreach (var file in clientFiles)
                {
                    try
                    {
                        if (file.Key == "(version)")
                        {
                            continue;
                        }
                        JObject fileObj = new JObject();
                        fileObj["path"] = file.Key;
                        fileObj["hash"] = file.Value["hash"];
                        fileObj["state"] = "added";
                        if (file.Value["download"] != null)
                            fileObj["download-link"] = file.Value["download"];
                        else
                            fileObj["download-link"] = "";


                        if (file.Key == localFiles[i]["path"].ToString())
                        {
                            if (file.Value["hash"].ToString() == localFiles[i]["hash"].ToString() && localFiles[i]["state"].ToString() == "done")
                            {
                                fileObj["state"] = "done";
                            }
                            i++;
                            this.updateFiles.Add(fileObj);
                        }
                        else
                            if (file.Key != "Bin/OgreMain.dll")
                        {
                            this.updateFiles.Add(fileObj);
                        }
                        else
                        {
                            if (file.Value["hash"].ToString() == localFiles[localFiles.Count - 1]["hash"].ToString() && localFiles[localFiles.Count - 1]["state"].ToString() == "done")
                            {
                                fileObj["state"] = "done";
                            }
                            tempObj = fileObj;
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else
                foreach (var file in clientFiles)
                {
                    try
                    {
                        if (file.Key == "(version)")
                        {
                            continue;
                        }
                        JObject fileObj = new JObject();
                        fileObj["path"] = file.Key;
                        fileObj["hash"] = file.Value["hash"];
                        fileObj["state"] = "added";
                        if (file.Value["download"] != null)
                            fileObj["download-link"] = file.Value["download"];
                        else
                            fileObj["download-link"] = "";
                        if (file.Key == "Bin/OgreMain.dll")
                        {
                            tempObj = fileObj;
                            continue;
                        }
                        this.updateFiles.Add(fileObj);
                    }
                    catch
                    {

                    }
                }
            if (tempObj != null)
                this.updateFiles.Add(tempObj);
            File.WriteAllText(Path.GetFullPath("Lib.txt"), updateFiles.ToString());
        }

        private JObject FetchHashSum()
        {
            try
            {
                ServicePointManager.SecurityProtocol = Tls12;
                WebClient wc = new WebClient();
                string downloadfile = wc.DownloadString(new Uri(HashSumUri));
                JObject json = JObject.Parse(downloadfile);
                return json;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //private void setMode()
        //{
        //    try
        //    {
        //        if (!File.Exists(Path.GetFullPath(setting)))
        //        {
        //            Utils.DownloadFile(DownloadFileUri + @"Bin\Launcher\Setting.txt.hlzip", @"Bin\Launcher\Setting.txt.hlzip");
        //            string path = Path.GetFullPath(@"Bin\Launcher\Setting.txt.hlzip");
        //            Utils.Extract(path, path);
        //            File.Delete(path);
        //        }
        //        names_path = File.ReadAllLines(Path.GetFullPath(setting));
        //        names = new string[names_path.Length];
        //        paths = new string[names_path.Length];
        //        int index = 0;
        //        foreach (string name in names_path)
        //        {
        //            string[] subname = name.Split(",");
        //            names[index] = subname[0];
        //            paths[index++] = subname[1];
        //        }
        //        ////Debug.WriteLine("names: " + names, "paths: " + paths);
        //        List<string> selection = new List<string>();
        //        for (int i = 0; i < names_path.Length; i++)
        //            selection.Add(names[i]);
        //        selectMode.ItemsSource = selection;
        //        //selectMode.SelectedItem = selection[0]; 

        //        if (!File.Exists(Path.GetFullPath(modeSavedFile)))
        //            File.Create(Path.GetFullPath(modeSavedFile)).Close();
        //        //FileStream modeFile = File.OpenRead(Path.GetFullPath(modeSavedFile));
        //        var _modeIndex = "";
        //        int modeIndex;
        //        try
        //        {
        //            _modeIndex = File.ReadAllText(Path.GetFullPath(modeSavedFile));
        //            modeIndex = _modeIndex == "" ? 0 : Int32.Parse(_modeIndex);
        //            selectMode.SelectedItem = selection[modeIndex];
        //        }
        //        catch (Exception)
        //        {
        //            selectMode.SelectedItem = selection[0];
        //            modeIndex = 0;
        //        }
        //        File.Copy(Path.GetFullPath(paths[selectMode.SelectedIndex]), Path.GetFullPath(@"Bin\" + "FairyResources.cfg"), true);
        //        File.WriteAllText(Path.GetFullPath(modeSavedFile), selectMode.SelectedIndex.ToString());
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

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
            catch (IOException)
            {
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
