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
using Windows.Storage.Pickers;
using Windows.UI.Core;
using WinRT.Interop;

namespace WSA_Installer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Configs : Page
    {
        bool ShowTip = true;

        int NativeArch;

        bool DefaultSupport = (Environment.OSVersion.Version.Build >= 22000);
        //bool DefaultSupport = false;

        bool InstallMagisk = false;

        bool InstallGapps = false;

        string DownloadPath = Path.GetTempPath() + "WSA下载";

        long MaxDownloadSpeed_ = 0;

        ComboBox MagiskVersion = new ComboBox()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            SelectedIndex = 0,
            Items =
            {
                "稳定版",
                "测试版",
                "金丝雀",
                "调试版",
                "已发布"
            }
        };

        TextBlock MagiskVersionHeader = new TextBlock()
        {
            Margin = new Thickness(10, 0, 0, 0),
            Text = "Magisk版本:",
            FontSize = 15,
            VerticalAlignment = VerticalAlignment.Center,
        };

        ComboBox GappsVariantMap = new ComboBox()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            SelectedIndex = 0,
            Items =
            {
                "super",
                "stock",
                "full",
                "mini",
                "micro",
                "nano",
                "pico",
                "tvstock",
                "tvmini",
            }
        };

        TextBlock GappsVariantMapHeader = new TextBlock()
        {
            Margin = new Thickness(10, 0, 0, 0),
            Text = "Gapps等级:",
            FontSize = 15,
            VerticalAlignment = VerticalAlignment.Center,
        };

        public Configs()
        {
            this.InitializeComponent();

            this.SizeChanged += (_s, _e) =>
            {
                ConfigsList.Height = _e.NewSize.Height - 80;
            };

            InstallPath_TextBox.Text = Environment.GetEnvironmentVariable("ProgramFiles") + "\\Windows Subsystem For Android";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var arch = (string)((object[])e.Parameter)[0];

            if (arch == "X64")
            {
                ArchSelect.SelectedIndex = NativeArch = 1;
            }else if (arch == "ARM64")
            {
                ArchSelect.SelectedIndex = NativeArch = 0;
            }

            if (!DefaultSupport)
            {
                Win10Support.IsChecked = true;
                //InstallOffline.IsEnabled = false;
            }
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex == 0)
            {
                if (!InstallMagisk)
                {
                    ConfigsList.Items.Insert(3,new Grid()
                    {
                        CornerRadius = new CornerRadius(4),
                        Width = 500,
                        Height = 40,
                        Margin = new Thickness(0, 0, 0, 10),
                        Background = (Brush)Application.Current.Resources["ExpanderHeaderBackground"],
                        Children =
                        {
                            MagiskVersionHeader,
                            MagiskVersion,
                            
                        }
                    });
                    InstallMagisk = true;
                }
            }
            else
            {
                if (InstallMagisk)
                {
                    ConfigsList.Items.RemoveAt(3);
                    InstallMagisk = false;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int ItemIndex = 4 + (InstallMagisk? 1 : 0);

            if (((ComboBox)sender).SelectedIndex == 0)
            {
                if (InstallGapps)
                {
                    ConfigsList.Items.RemoveAt(ItemIndex);
                    InstallGapps = false;
                }
            }
            else
            {
                if (!InstallGapps)
                {
                    ConfigsList.Items.Insert(ItemIndex, new Grid()
                    {
                        CornerRadius = new CornerRadius(4),
                        Width = 500,
                        Height = 40,
                        Margin = new Thickness(0, 0, 0, 10),
                        Background = (Brush)Application.Current.Resources["ExpanderHeaderBackground"],
                        Children =
                        {
                            GappsVariantMapHeader,
                            GappsVariantMap,
                        }
                    });
                    InstallGapps = true;
                }
            }
        }

        private void InstallOffline_Checked(object sender, RoutedEventArgs e)
        {
            int KeepCout = 2;

            RemoveAmazon.IsEnabled = false;
            Win10Support.IsEnabled = false;

            try
            {
                foreach (Grid g in ConfigsList.Items)
                {
                    foreach (object o in g.Children)
                    {
                        if (o is ComboBox)
                        {
                            if (KeepCout > 0)
                            {
                                KeepCout--;
                                continue;
                            }
                            (o as ComboBox).IsEnabled = false;
                        }
                    }
                };
            }
            catch { }
        }

        private void InstallOffline_Unchecked(object sender, RoutedEventArgs e)
        {
            RemoveAmazon.IsEnabled = true;
            Win10Support.IsEnabled = true;

            try
            {
                foreach (Grid g in ConfigsList.Items)
                {
                    foreach (object o in g.Children)
                    {
                        if (o is ComboBox)
                        {
                            (o as ComboBox).IsEnabled = true;
                        }
                    }
                }
            }
            catch { }
        }

        //private void ArchSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if ((sender as ComboBox).SelectedIndex != NativeArch)
        //    {
        //        DeployMode.SelectedIndex = 2;
        //        DeployMode.IsEnabled = false;
        //    }
        //    else
        //    {
        //        if ((DefaultSupport || (bool)Win10Support.IsChecked))
        //        {
        //            DeployMode.IsEnabled = true;
        //        }
        //    }
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(InstallingPage),new object[]
            {
                ArchSelect.SelectedIndex,          //0
                DownloadChannel.SelectedIndex,     //1
                InstallMagisk,                     //2
                MagiskVersion.SelectedIndex,       //3
                GappsBrand_.SelectedIndex,         //4
                GappsVariantMap.SelectedIndex,     //5
                InstallPath_TextBox.Text,          //6
                RemoveAmazon.IsChecked,            //7
                Win10Support.IsChecked,            //8
                (int)ThreadsCout.Value,            //9
                DownloadPath,                      //10
                MaxDownloadSpeed_,                 //11
                NativeArch,                        //12
            });
        }

        private void ThreadCoutBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (ShowTip)
            {
                Root.Children.Add(new InfoBar()
                {
                    Title = "提示",
                    Content = new TextBlock() {
                        Text = "如果你对你的电脑没有十分甚至九分的自信,请不要将线程数设置的太高" ,
                        Margin = new Thickness(0,-10,0,15),
                    },
                    Severity = InfoBarSeverity.Warning,
                    IsOpen = true,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                ShowTip = false;
            }

            if (args.NewValue > 255)
            {
                sender.Value = args.OldValue;
            }
            else
            {
                ThreadsCout.Value = sender.Value;
            }

        }

        //private void Win10Support_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (ArchSelect.SelectedIndex == NativeArch)
        //    {
        //        DeployMode.IsEnabled = true;
        //    }
        //}

        //private void Win10Support_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (!DefaultSupport)
        //    {
        //        DeployMode.SelectedIndex = 2;
        //        DeployMode.IsEnabled = false;
        //    }
        //}

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var DownloadPath_Box = new TextBox()
            {
                Text = DownloadPath,
            };

            var dialog = new ContentDialog()
            {
                Title = "更改下载目录",
                Content = DownloadPath_Box,
                PrimaryButtonText = "更改",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot,
            };
            dialog.PrimaryButtonClick += async (sender, args) =>
            {
                //var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                //folderPicker.FileTypeFilter.Add("*");
                //WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WinRT.Interop.WindowNative.GetWindowHandle(App.m_window));
                //var folder = await folderPicker.PickSingleFolderAsync();
                //DownloadPath = folder.Path;

                DownloadPath = DownloadPath_Box.Text;
            };

            await dialog.ShowAsync();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void MaxDownloadSpeed_TextChanging(object sender, TextBoxTextChangingEventArgs e)
        {
            string speed = (sender as TextBox).Text.Replace(" ",string.Empty);

            try
            {
                if (speed.ToLower().Contains("kb"))
                {
                    MaxDownloadSpeed_ = long.Parse(System.Text.RegularExpressions.Regex.Replace(speed, @"[^0-9]+", "")) * 1024;
                }
                else
                {
                    MaxDownloadSpeed_ = long.Parse(System.Text.RegularExpressions.Regex.Replace(speed, @"[^0-9]+", "")) * 1024 * 1024;
                }
            }catch { }
        }
    }
}
