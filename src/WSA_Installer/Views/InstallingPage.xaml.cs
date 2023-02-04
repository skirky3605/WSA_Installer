using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WSA_Installer.Models.Web;
using System.Net.Http;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Web;
using Downloader;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using System.IO.Compression;
using Microsoft.UI.Xaml.Shapes;
using Windows.Management.Deployment;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using WSA_Installer.Models.Files;

#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行

namespace WSA_Installer.Views
{
    public sealed partial class InstallingPage : Page
    {
        private bool NoDownload = false;

        private float[] downloads = {0, 0, 0, 0};

        DownloadService[] Downloaders = { null, null, null, null};

        public static IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> WSADeploymenter;

        object[] ConfigList = new object[] { };

        bool NoChange;

        string ReleaseType;

        string arch;

        bool CanDeplotment;

        string TargetPath;

        ContentDialog WSA_Writing_dialog = new ContentDialog()
        {
            Title = "请稍侯,正在写入文件",
            Content = new ProgressRing() { IsIndeterminate = true},
        };

        readonly string[] ReleaseList =
        {
            "retail",
            "RP",
            "WIS",
            "WIF"
        };

        public InstallingPage()
        {
            this.InitializeComponent();

            WSA_Writing_dialog.XamlRoot = this.XamlRoot;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ConfigList = (object[])e.Parameter;

            ReleaseType = ReleaseList[(int)ConfigList[1]];

            TargetPath = ConfigList[6].ToString();

            if (!(bool)ConfigList[2] && ((int)ConfigList[4]) == 0 && !(bool)ConfigList[7] && !(bool)ConfigList[8])
            {
                NoChange = true;
            }
            else
            {
                NoChange = false;
            }


            if ((int)ConfigList[0] == 1)
            {
                arch = "x64";
            }
            else
            {
                arch = "arm64";
            }

            if ((int)ConfigList[0] == ((int)ConfigList[12]))
            {
                CanDeplotment = true;
                DeploymentProgress_Expander.Visibility = Visibility.Collapsed;
            }
            else
            {
                CanDeplotment = false;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (NoDownload)
            {
                var result = await Models.Files.extractWSA.ExtractWSA(ConfigList[10].ToString() + "\\WSA.zip", TargetPath, arch);

                if (result is bool)
                {
                    if ((bool)result)
                    {
                        AddModels.addModels(ConfigList, TargetPath);
                    }
                }

                return;
            }

            var d = new ContentDialog()
            {
                Content = "正在获取下载链接",
                XamlRoot = this.XamlRoot,
            };

            d.ShowAsync();

            string[] UrlList = await generateWSALinks.generateWSALink(arch, ReleaseType);

            d.Hide();

            var DownloadOpt = new DownloadConfiguration
            {
                ChunkCount = ((int)ConfigList[9]),
                ParallelDownload = true,
                MaximumBytesPerSecond = ((long)ConfigList[11]),
            };

            var WSAdownloader = new DownloadService(DownloadOpt);

            Downloaders[0] = WSAdownloader;

            WSAdownloader.DownloadStarted += WSA_DownloadStarted;

            //WSAdownloader.DownloadFileCompleted += DownloadersDownloadFileCompleted;

            //WSAdownloader.DownloadFileCompleted += (_s, _e) =>
            //{

            //            await new ContentDialog()
            //            {
            //                Title = "WSA下载完成",
            //                CloseButtonText = "关闭",
            //                XamlRoot = this.XamlRoot
            //            }.ShowAsync();

            //            TryInstallPackage();
            //};

            WSAdownloader.DownloadProgressChanged += WSA_DownloadProgressChanged;

            string file = ConfigList[10].ToString() + "\\WSA.zip";

            Downloaders[0].DownloadFileTaskAsync(UrlList[0], file);

            var DepsDownloadPath = new DirectoryInfo(ConfigList[10].ToString() + "\\WSA依赖");

            //foreach (string dep_url in UrlList.Skip(1))
            //{
            //    DepsDownloader.DownloadFileTaskAsync(dep_url, DepsDownloadPath);
            //}

            for (int i = 1; i < 4; i++)
            {
                var DepsDownloader = new DownloadService(DownloadOpt);

                Downloaders[i] = DepsDownloader;

                DepsDownloader.DownloadStarted += DepsDownloader_DownloadStarted;

                DepsDownloader.DownloadFileTaskAsync(UrlList[i], DepsDownloadPath);
            }
        }

        private void DepsDownloader_DownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            var DepsDownloader = sender as DownloadService;

            DepsDownloader.DownloadProgressChanged += DepsDownloader_DownloadProgressChanged;
            DepsDownloader.DownloadProgressChanged += UpdateDownloadProgressBar;
            DepsDownloader.DownloadFileCompleted += DownloadersDownloadFileCompleted;

            this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                () => Download.IsExpanded = true);
        }

        private void DepsDownloader_DownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            var p = (float)Math.Round(e.ProgressPercentage,1);

            var filename = (sender as DownloadService).Package.FileName;

            if (filename.ToLower().Contains("microsoft.ui.xaml"))
            {
                downloads[1] = p;
            }else if (filename.ToLower().Contains("vclibs"))
            {
                if (filename.ToLower().Contains("uwpdesktop"))
                {
                    downloads[2] = p;
                }
                else
                {
                    downloads[3] = p;
                }
            }

            this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                async () => Deps_DownloadProgress.Value = downloads.Sum()/3);
        }

        private async void DownloadersDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {

            if (e.Cancelled)
            {
                this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                async () =>
                {
                    await new ContentDialog()
                        {
                            Title = "下载终止",
                            Content = "已取消",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot,
                        }.ShowAsync();
                });
                this.Frame.Navigate(typeof(FinishedPage), 1);
            }
            else if (e.Error != null)
            {
                this.DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                    async () =>
                    {
                        await new ContentDialog()
                        {
                            Title = "下载失败",
                            Content = e.Error.Message,
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot,
                        }.ShowAsync();

                        this.Frame.Navigate(typeof(FinishedPage), e.Error.Message);
                    });
            }
            else
            {
                this.DispatcherQueue.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                    async () =>
                    {
                        TryInstallPackage();
                    });
            }
        }

        private void UpdateTotalProgressBar()
        {
            this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                async () => TotalProgress.Value = downloads.Sum() / 5);
        }

        private void UpdateDownloadProgressBar(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                async () => DownloadProgressBar.Value = downloads.Sum() / 4);

            UpdateTotalProgressBar();
        }

        private void WSA_DownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            (sender as DownloadService).DownloadFileCompleted += DownloadersDownloadFileCompleted;

            this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                () => Download.IsExpanded = true);
        }

        private async Task UnpackWSA()
        {
            string[] LanguagePacks = { };

            string[] ScalePacks = { };

            string WSAPack;

            using (var zip = ZipFile.OpenRead(ConfigList[10].ToString() + "\\WSA.zip"))
            {
                //zip.ExtractToDirectory(System.IO.Path.GetTempPath() + "WSA安装器临时数据");
                //var f_list = Directory.GetFiles(System.IO.Path.GetTempPath() + "WSA安装器临时数据");
                foreach (var f in zip.Entries)
                {
                    if (f.Name.ToLower().Contains("language"))
                    {
                        LanguagePacks.Append(f.Name);
                    }else if (f.Name.ToLower().Contains("scale"))
                    {
                        ScalePacks.Append(f.Name);
                    }else if (f.Name.ToLower().Contains(arch))
                    {
                        WSAPack = f.Name;
                    }
                }
            }
        }

        private async void WSA_DownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            var p = (float)Math.Round(e.ProgressPercentage,1);
            downloads[0] = p;
            this.DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                async () =>
                {
                    WSA_DownloadProgress.Value = p;
                    WSA_Pro.Text = p.ToString();
                });
        }

        private async Task TryInstallPackage()
        {
            foreach (var download_ in Downloaders)
            {
                if (download_.Status != DownloadStatus.Completed)
                {
                    return;
                }
            }

            Download.IsExpanded = false;
            DeploymentProgress_Expander.IsExpanded = true;

            var deps_paths = new Uri[] { };

            foreach (var path in Downloaders.Skip(1))
            {
                deps_paths.Append(new Uri(path.Package.FileName));
            }

            var pm = new PackageManager();

            var result = pm.FindPackagesForUser(string.Empty, "MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe").ToList();

            var c = result.Count;

            bool KeepDataWhileUinstall = false;

            if (result.Count > 0)
            {
                var dialog = new ContentDialog()
                {
                    Title = "提示",
                    Content = "WSA已安装,是否保留数据并重新安装",
                    PrimaryButtonText = "保留数据",
                    CloseButtonText = "不保留数据",
                    XamlRoot = this.XamlRoot,
                };

                dialog.PrimaryButtonClick += async (_s, _e) =>
                {
                    dialog.Hide();

                    KeepDataWhileUinstall = true;
                };

                await dialog.ShowAsync();

                await UninstallOldAppx(KeepDataWhileUinstall);
            }

            //if (NoChange)
            //{


            //    //var pm = new PackageManager();

            //    WSADeploymenter = new PackageManager().AddPackageAsync(new Uri(ConfigList[10].ToString() + "\\WSA.zip"), deps_paths, DeploymentOptions.None);

            //    //await WSA_Installer.Deployment.Deloyment.InstallAppxPackage(new Uri(ConfigList[10].ToString() + "\\WSA.zip"), deps_paths);

            //    WSADeploymenter.Progress = (sender, e) =>
            //    {
            //        this.DispatcherQueue.TryEnqueue(
            //            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            //            async () =>
            //            {
            //                WSA_DeploymentProgress.Value = e.percentage;
            //            });
            //    };

            //    WSADeploymenter.Completed = async (sender, e) =>
            //    {
            //        var result = sender.GetResults();

            //        if (result.IsRegistered)
            //        {
            //            this.DispatcherQueue.TryEnqueue(
            //            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            //            () =>
            //            {
            //                this.Frame.Navigate(typeof(FinishedPage));
            //            });
            //        }
            //        else if (result.ExtendedErrorCode != null)
            //        {
            //            this.DispatcherQueue.TryEnqueue(
            //            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            //            async () =>
            //            {
            //                var dialog = new ContentDialog()
            //                {
            //                    Title = "安装WSA时遇到错误",
            //                    Content = new TextBox() { Text = result.ExtendedErrorCode.ToString(),TextWrapping = TextWrapping.Wrap },
            //                    PrimaryButtonText = "重试",
            //                    CloseButtonText = "取消",
            //                    XamlRoot = this.XamlRoot,
            //                };

            //                dialog.PrimaryButtonClick += (_s, _e) =>
            //                {
            //                    //InstallAppxAsync(new Uri(ConfigList[10].ToString() + "\\WSA.zip"), deps_paths);
            //                    dialog.Hide();

            //                    TryInstallPackage();
            //                };

            //                dialog.CloseButtonClick += (_s, _e) =>
            //                {
            //                    this.Frame.Navigate(typeof(FailedPage), result.ErrorText.ToString());
            //                };

            //                await dialog.ShowAsync();
            //            });
            //        }
            //};
            //}
            //else
            //{
            //    UnpackWSA();
            //}

            await Models.Files.extractWSA.ExtractWSA(ConfigList[10].ToString() + "\\WSA.zip", TargetPath, arch);
        }

        private async Task UninstallOldAppx(bool BackUpUserData)
        {
            if (BackUpUserData)
            {
                string local = Environment.GetEnvironmentVariable("LOCALAPPDATA");

                Directory.Move(local + "\\Packages\\MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe", local + "\\Packages\\MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe.bak");

                var pm = new PackageManager();

                await pm.RemovePackageAsync(pm.FindPackagesForUser(string.Empty, "MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe").ToList()[0].Id.FullName);

                Directory.Move(local + "\\Packages\\MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe.bak", local + "\\Packages\\MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe");
            }
            else
            {
                var pm = new PackageManager();

                await pm.RemovePackageAsync(pm.FindPackagesForUser(string.Empty, "MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe").ToList()[0].Id.FullName);
            }
            
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(FailedPage), 1);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(FailedPage), "错误码示例");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(FinishedPage));
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            NoDownload = true;
            Button_Click(sender, e);
        }

        private void Cancel_AllTask(object sender, RoutedEventArgs e)
        {
            foreach (var download in Downloaders)
            {
                if (download != null && download.Status != DownloadStatus.Completed)
                {
                    download.CancelAsync();
                }
            }

            WSADeploymenter.Cancel();
        }
    }
}
