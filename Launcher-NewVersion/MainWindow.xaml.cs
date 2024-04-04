using Launcher;
using Launcher.Exceptions;
using Launcher.Helpers;
using Launcher.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
        private int TIMEOUT = 0;
        //Hash sum
        FileStream fsHashSum = null;

        //Download json from server
        private string versionFile;
        private string gameExe;
        private JObject hashSumData = null;
        private JObject clientFiles = new JObject();
        private string priorityMirror = "";
        protected bool isFailed = false;

        protected string localBannerDir = "Banner";

        // As global variables
        private JObject news = null;
        private List<News> newsData = new List<News>();
        private List<Button> newsControls = new List<Button>();
        private List<Button> newsTime = new List<Button>();
        private JArray updateFiles = new JArray();
        private bool isUpdating = false;
        private MessageBoxContent _messageBoxContent = null;
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
                            UpdateStatus.Text = "Cập nhật thất bại"; UStatus.Text = "(Cập nhật ngay)"; UpdateStatus_btn.IsEnabled = true;
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
                        UpdateStatus_btn.IsEnabled = false; UpdateStatus_btn.Opacity = 1;
                        PlayGame.IsEnabled = false; PlayGame.Opacity = 1;
                        break;
                    case LauncherStatus._verifying:
                        UpdateStatus.Text = "Xác thực dữ liệu ..."; UpdateStatus_btn.IsEnabled = false; UpdateStatus_btn.Opacity = 1;
                        break;
                    default:
                        break;
                }
            }
        }

        private List<KeyValuePair<MessageBoxTitle, string>> _messageBoxDescription = new List<KeyValuePair<MessageBoxTitle, string>>();
        #endregion

        #region Methods
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Status = LauncherStatus.Checking;
            JObject configFile = ConfigHelper.ReadConfig();
            SetupConfigFile(configFile);
            SetUpMessageBoxContent(); 
            UsingDynamicControlsGeneration();
            SetDefaultPropertiesForControls();
            FetchingNewsFromSeverInBackground();
            FetchingFileHashSumFromServerInBackground();
        }

        private void FetchingFileHashSumFromServerInBackground()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                try
                {
                    hashSumData = DownloadFileUri.FetchDataFromMultipleUris(Encoding.Default, Settings.HashSumFile);
                    ev.Result = hashSumData;
                }
                catch (Exception ex)
                {
                    ev.Result = ex;
                    FileHelpers.WriteLog(ex.ToString());
                }
            };
            worker.RunWorkerCompleted += (s, ev) =>
            {
                if (ev.Result is WebException webExcetion)
                {
                    switch (webExcetion.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ConnectionTimeout),
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        case WebExceptionStatus.SendFailure:
                            MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.TlsError),
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        default:
                            var exception = (WebException)ev.Result;
                            MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                    EnableButtonsIfError();
                }
                else if (ev.Result is FetchingErrorException)
                {
                    MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.GetServerDataFailed),
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EnableButtonsIfError();
                }
                else if (ev.Result is Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EnableButtonsIfError();
                }
                else if (ev.Error == null)
                {
                    var forcedUpdate = false;
                    if (!File.Exists(Path.GetFullPath(Settings.LibFile)))
                    {
                        fsHashSum = new FileStream(Path.GetFullPath(Settings.LibFile), FileMode.Create, FileAccess.ReadWrite);
                        fsHashSum.Close();
                        forcedUpdate = true;
                    }
                    IsRequireUpdate(result =>
                    {
                        if (result)
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
                            //selectMode.IsEnabled = true;
                            //setMode();
                            if (!File.Exists(Path.GetFullPath(Settings.IPSavedFile)))
                                File.Create(Path.GetFullPath(Settings.IPSavedFile)).Close();
                            File.WriteAllText(Path.GetFullPath(Settings.IPSavedFile), NetworkHelper.GetPublicIpAddress());
                        }
                    }, forcedUpdate);
                }
            };
            worker.RunWorkerAsync();
        }

        private void SetUpMessageBoxContent()
        {
            try
            {
                _messageBoxContent = ConfigHelper.ReadMessageBoxContent();
                var defaultLanguage = _messageBoxContent.DefaultLanguage;
                GetMessageContent(defaultLanguage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.PrepareDataFailed), 
                    "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                FileHelpers.WriteLog(ex.ToString());
                Environment.Exit(1);
            }
            
        }

        private void GetMessageContent(MessageBoxLanguage defaultLanguage)
        {
            foreach (var data in _messageBoxContent.MessageBoxData)
            {
                var message = data.MessageBoxDescriptions
                    .Where(x => x.Language.Equals(defaultLanguage))
                    .Select(x => x.Message).FirstOrDefault();
                _messageBoxDescription.Add(new KeyValuePair<MessageBoxTitle, string>(data.Title, message));
            }
        }

        private void SelectMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //try
            //{
            //    File.Copy(Path.GetFullPath(paths[selectMode.SelectedIndex]), Path.GetFullPath(@"Bin\" + "FairyResources.cfg"), true);
            //    File.WriteAllText(Path.GetFullPath(modeSavedFile), selectMode.SelectedIndex.ToString());
            //}
            //catch (Exception webExcetion)
            //{
            //    Debug.WriteLine("Error in selectMode_SelectionChanged: " + webExcetion.ToString());
            //    string path = Path.GetFullPath(@"Bin\Launcher");
            //    NetworkHelper.DownloadFileFromMultipleUrls(DownloadFileUri, @"Bin\Launcher\Setting.txt");
            //    FileExtentions.HlZip.Extract(path + "Setting.txt.hlzip", path + "Setting.txt.hlzip");
            //    File.Delete(path + "Setting.txt.hlzip");
            //}
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
        private void EnableButtonsIfError()
        {
            PlayGame.IsEnabled = true;
            FixClient.IsEnabled = true;
        }

        private void FetchingNewsFromSeverInBackground()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                try
                {
                    if (news == null)
                    {
                        news = newsUri.FetchDataFromMultipleUris(Encoding.UTF8, timeout: TIMEOUT);
                        newsData = news[NewsValue.Data].ToObject<List<News>>();
                        InitNews();
                    }
                }
                catch (Exception ex)
                {
                    ev.Result = ex;
                    FileHelpers.WriteLog(ex.ToString());
                }
            };
            worker.RunWorkerCompleted += (s, ev) =>
            {
                if(ev.Result is WebException webExcetion)
                {
                    switch (webExcetion.Status)
                    {
                        case WebExceptionStatus.Timeout:
                            MessageBox.Show($"News: {_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ConnectionTimeout)}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                            case WebExceptionStatus.SendFailure:
                                MessageBox.Show($"News: {_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.TlsError)}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        default:
                            var exception = (WebException) ev.Result;
                            MessageBox.Show($"News: {exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                    EnableButtonsIfError();
                }
                else if (ev.Result is FetchingErrorException)
                {
                    MessageBox.Show($"News: {_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.GetServerDataFailed)}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EnableButtonsIfError();
                }
                else if (ev.Result is Exception ex)
                {
                    MessageBox.Show($"News: {ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EnableButtonsIfError();
                }
            };
            worker.RunWorkerAsync();
        }
        
        private void SetupConfigFile(JObject config)
        {
            try
            {
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

                if (url[LauncherFileValue.TIMEOUT] == null || url[LauncherFileValue.TIMEOUT].ToString() == "")
                {
                    this.TIMEOUT = 900000;
                }
                else
                {
                    this.TIMEOUT = int.Parse(
                    (double.Parse(url[LauncherFileValue.TIMEOUT].ToString(), CultureInfo.InvariantCulture) * 1000).ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in Setup: {ex}");
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.PrepareDataFailed), 
                    "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                FileHelpers.WriteLog(ex.ToString());
                Environment.Exit(1);
            }
        }

        private void DownloadFileZip(List<string> urls, string fileName)
        {
            urls.DownloadFileFromMultipleUrls(fileName, timeout: TIMEOUT);
            string path = Path.GetFullPath(fileName) + FileExtentions.HlZip.GetDescription();
            FileExtentions.HlZip.Extract(path, path);
            File.Delete(path);
        }
        //Load news
        private void InitNews()
        {
            try
            {
                //int maxNews = Math.Max(this.newsControls.Count, ((JArray)this.news["data"]["news"]).Count);
                //int maxNews = Math.Min(this.newsControls.Count, ((JArray)this.news["data"]).Count);
                int maxNews = Math.Min(this.newsControls.Count, newsData.Count);

                for (int i = 0; i < maxNews; i++)
                {
                    string title = newsData[i].PostTitle;
                    string str_date = newsData[i].UpdatedAt;
                    string[] date = str_date.Substring(0, 6).Split('/');
                    
                    //string id = news["data"]["news"][count]["id"].ToString();
                    string url = newsData[i].Link;
                    

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
                FileHelpers.WriteLog(ex.ToString());
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
        

        private void IsRequireUpdate(Action<bool> callback, bool isForcedUpdate = false)
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
                string version = "";
                if (hashSumData == null)
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (s, e) =>
                    {
                        hashSumData = DownloadFileUri.FetchDataFromMultipleUris(Encoding.Default, Settings.HashSumFile);
                    };

                    worker.RunWorkerCompleted += (s, e) =>
                    {
                        if (e.Error == null)
                        {
                            version = hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString();
                            Debug.WriteLine("FetchHashSumAsync start");
                            VerSer.Text = $"({version})";
                            Debug.WriteLine("FetchHashSumAsync end");
                            if (isForcedUpdate)
                            {
                                callback(true);
                            }
                            else if (!localVersion.IsDifferentThan(new Version(version)))
                            {
                                callback(false);
                            }
                            else
                            {
                                callback(true);
                            }
                        }
                    };

                    worker.RunWorkerAsync();
                }
                else
                {
                    version = hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString();
                    VerSer.Text = $"({version})";
                    if (isForcedUpdate)
                    {
                        callback(true);
                    }
                    else if (!localVersion.IsDifferentThan(new Version(version)))
                    {
                        callback(false);
                    }
                    else
                    {
                        callback(true);
                    }
                }
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
            {
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ConnectionTimeout),
                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FileHelpers.WriteLog(ex.ToString());
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.SecureChannelFailure)
            {
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.TlsError),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FileHelpers.WriteLog(ex.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ErrorWhileDownloading), 
                    "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                VerSer.Text = $"(Không thể kết nối với máy chủ)";
                Debug.WriteLine($"IsRequireUpdate error: {ex}");
                FileHelpers.WriteLog(ex.ToString());
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
                //bool isFileinUse = IsFileInUse(gameExe);
                bool isFileinUse = IsFileInUse(gameExe);

                if (isFileinUse)
                {
                    MessageBoxResult mbr = MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.TurnOffGame), 
                        "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
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
                    catch (Exception ex)
                    {
                        FileHelpers.WriteLog(ex.ToString());
                    }

                    Thread.Sleep(300);
                }
                if (isFailed)
                {
                    MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ErrorWhileDownloading),
                        "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (completed == total && total > 0)
                {
                    File.WriteAllText(Path.GetFullPath(HashSumFileValue.VersionKey), hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString());
                    File.WriteAllText(Path.GetFullPath(Settings.LibFile), this.updateFiles.ToString());
                    Thread.Sleep(2000);
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
                    //selectMode.IsEnabled = true;
                    PlayGame.IsEnabled = true;
                    PlayGame.Opacity = 1;
                    FixClient.IsEnabled = true;
                    FixClient.Opacity = 1;
                    Ver.Text = hashSumData[HashSumFileValue.Data][HashSumFileValue.Version].ToString();
                    //setMode();
                };
                this.Dispatcher.Invoke(action);
                if (fsHashSum != null)
                    fsHashSum.Close();
            });
            progressThread.Priority = ThreadPriority.Lowest;
            progressThread.Start();

        }
        protected string setting = @"Bin\Launcher\Setting.txt";
        string[] names_path;
        string[] names;
        string[] paths;
        private string modeSavedFile = @"Bin\mode.cfig";

        private void setMode()
        {
            try
            {
                if (!File.Exists(Path.GetFullPath(setting)))
                {
                    NetworkHelper.DownloadFile(DownloadFileUri + @"Bin\Launcher\Setting.txt.hlzip", @"Bin\Launcher\Setting.txt.hlzip");
                    string path = Path.GetFullPath(@"Bin\Launcher\Setting.txt.hlzip");
                    FileExtentions.HlZip.Extract(path, path);
                    File.Delete(path);
                }
                names_path = File.ReadAllLines(Path.GetFullPath(setting));
                names = new string[names_path.Length];
                paths = new string[names_path.Length];
                int index = 0;
                foreach (string name in names_path)
                {
                    string[] subname = name.Split(',');
                    names[index] = subname[0];
                    paths[index++] = subname[1];
                }
                ////Debug.WriteLine("names: " + names, "paths: " + paths);
                List<string> selection = new List<string>();
                for (int i = 0; i < names_path.Length; i++)
                    selection.Add(names[i]);
                selectMode.ItemsSource = selection;
                //selectMode.SelectedItem = selection[0]; 

                if (!File.Exists(Path.GetFullPath(modeSavedFile)))
                    File.Create(Path.GetFullPath(modeSavedFile)).Close();
                //FileStream modeFile = File.OpenRead(Path.GetFullPath(modeSavedFile));
                var _modeIndex = "";
                int modeIndex;
                try
                {
                    _modeIndex = File.ReadAllText(System.IO.Path.GetFullPath(modeSavedFile));
                    modeIndex = _modeIndex == "" ? 0 : Int32.Parse(_modeIndex);
                    selectMode.SelectedItem = selection[modeIndex];
                }
                catch (Exception ex)
                {
                    selectMode.SelectedItem = selection[0];
                    modeIndex = 0;
                    FileHelpers.WriteLog(ex.ToString());
                }
                File.Copy(Path.GetFullPath(paths[selectMode.SelectedIndex]), Path.GetFullPath(@"Bin\" + "FairyResources.cfg"), true);
                File.WriteAllText(Path.GetFullPath(modeSavedFile), selectMode.SelectedIndex.ToString());
            }
            catch (Exception ex)
            {
                FileHelpers.WriteLog(ex.ToString());
            }
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
            catch (Exception ex)
            {
                Action action = () => { Status = LauncherStatus.failed; };
                this.Dispatcher.Invoke(action);
                isFailed = true;
                FileHelpers.WriteLog(ex.ToString());
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
                    if (priorityMirror != "")
                    {
                        fileUri = downloadLinkDetails
                        .Where(downloadLinkDetail => downloadLinkDetail.Mirror == priorityMirror)
                        .Select(downloadLinkDetail => downloadLinkDetail.Url).FirstOrDefault();
                        
                        DownLoadFile(downloadFilePath, localFilePath, fileUri);
                    }
                    else
                    {
                        foreach(var downloadLinkDetail in downloadLinkDetails)
                        {
                            try
                            {
                                var isLast = downloadLinkDetail == downloadLinkDetails.Last();
                                DownLoadFile(downloadFilePath, localFilePath, downloadLinkDetail.Url, isLast);
                                FileInfo fileInfo = new FileInfo(downloadFilePath);

                                if (fileInfo.Exists && fileInfo.Length == 0)
                                {
                                    File.Delete(downloadFilePath);
                                    throw new Exception("Download failed");
                                }
                                break;
                            }
                            catch (Exception ex)
                            {
                                FileHelpers.WriteLog(ex.ToString());
                                if (downloadLinkDetail == downloadLinkDetails.Last()) throw;
                                continue;
                            }
                        }
                        //fileUri = downloadLinkDetails.FirstOrDefault().Url;
                    }
                    //DownLoadFile(downloadFilePath, localFilePath, fileUri);
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
                        
                        MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.UpdateFailed), 
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        file[LibFileValue.State] = StateValue.failed.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in DownloadThread: " + ex.ToString());
                FileHelpers.WriteLog(ex.ToString());
                Action action = () =>
                {
                    Status = LauncherStatus.failed;
                    FixClient.IsEnabled = true;
                    FixClient.Opacity = 1;
                };
                this.Dispatcher.Invoke(action);
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.NetworkError), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void DownLoadFile(string downloadFilePath, string localFilePath, string fileUri, bool isLast = false)
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

                    //// Create a timer that checks every second if the download has exceeded the timeout
                    //System.Timers.Timer timer = new System.Timers.Timer(1000);
                    //timer.Elapsed += (sender, e) =>
                    //{
                    //    if (stopwatch.Elapsed.TotalMinutes > 3)
                    //    {
                    //        timer.Stop();
                    //        wc.CancelAsync();
                    //        MessageBoxContent.Show("Download timed out after 3 minutes.");
                    //    }
                    //};
                    //timer.Start();

                    Monitor.Wait(syncObject);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DownLoadFile: {ex}");
                FileHelpers.WriteLog(ex.ToString());
                if (isLast) this.isFailed = true;
                throw;
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
                FileHelpers.WriteLog(ex.ToString());
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
                    string downloadFilePath = file[LibFileValue.Path].ToString() + FileExtentions.HlZip.GetDescription();
                    bool _isExtracting = true;
                    try
                    {
                        FileExtentions.HlZip.Extract(downloadFilePath, downloadFilePath);
                        File.Delete(downloadFilePath);
                        _isExtracting = false;
                    }
                    catch (Exception ex)
                    {
                        FileHelpers.WriteLog(ex.ToString());
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
            catch (Exception ex)
            {
                FileHelpers.WriteLog(ex.ToString());
                MessageBox.Show("Đã xảy ra lỗi! Nhấn Sửa lỗi để khắc phục!");
            }
        }

        private HashSumFileDetail CreateHashSumFileDetail(KeyValuePair<string, JToken> file)
        {
            var downloadLinks = JsonConvert.DeserializeObject<List<DownloadLinkDetail>>(file.Value[HashSumFileValue.DownLoadUri].ToString());
            return new HashSumFileDetail
            {
                Path = file.Key,
                Hash = file.Value[HashSumFileValue.Hash].ToString(),
                State = StateValue.added.ToString(),
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
                    var fileObj = CreateHashSumFileDetail(file);
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
                FileHelpers.WriteLog(ex.ToString());
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
            catch (Exception ex)
            {
                FileHelpers.WriteLog(ex.ToString());
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
                    catch (Exception ex)
                    {
                        FileHelpers.WriteLog(ex.ToString());
                        ExtractFileHashSum(ref hashSumFileDetail);
                    }
                }
                var hashSumFileDetailUrls = hashSumFileDetail
                    .Where(HashSumFileDetail => HashSumFileDetail.Path == Settings.LoginServerFile)
                    .FirstOrDefault();

                var loginServerFileUris = hashSumFileDetailUrls.DownloadLink.Select(linkDetail => linkDetail.Url).ToList();
                DownloadFileZip(loginServerFileUris, Settings.LoginServerFile);

                if (!isClick && !isEmty)
                    ExtractFileHashSum(ref hashSumFileDetail, updateFiles, StateValue.done);
                else
                    ExtractFileHashSum(ref hashSumFileDetail, updateFiles);
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
            {
                FileHelpers.WriteLog(ex.ToString());
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ConnectionTimeout), 
                                       "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.SendFailure)
            {
                FileHelpers.WriteLog(ex.ToString());
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.TlsError),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                FileHelpers.WriteLog(ex.ToString());
                Debug.WriteLine("Error in AnalyzeRequiredFiles: " + ex.ToString());
                MessageBoxResult mbr = MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.GetServerDataFailed), 
                    "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBoxResult mbr = MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.GameNotFound), 
                        "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                    switch (mbr)
                    {
                        case MessageBoxResult.OK:
                            IsRequireUpdate(result => 
                            {
                                if (result)
                                {
                                    Update();
                                }
                            },true);
                            //if (IsRequireUpdate(true))
                            //{
                            //    Update();
                            //}
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
                FileHelpers.WriteLog(ex.ToString());
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
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.Overload));
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
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.Overload));
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
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.Overload));
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
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.Overload));
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
                IsRequireUpdate(result =>
                {
                    if (result)
                    {
                        Update();
                    }
                }, true);
                //this.Update(true);
                isFailed = false;
            }
            catch
            {
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ErrorOccurred));
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

                    FileHelpers.WriteLog(ex.ToString());
                    MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.ErrorOccurred), 
                        "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(_messageBoxDescription.GetMessageBoxDescription(MessageBoxTitle.GameNotFound), 
                    "TLBB", MessageBoxButton.OK, MessageBoxImage.Error);
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
                IsRequireUpdate(result =>
                {
                    if (result)
                    {
                        Update();
                    }
                }, true);
                //if (IsRequireUpdate(true))
                //{
                //    Update();
                //}
                isClick = false;
                isFailed = false;
            }
            catch (Exception ex)
            {
                FileHelpers.WriteLog(ex.ToString());
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
