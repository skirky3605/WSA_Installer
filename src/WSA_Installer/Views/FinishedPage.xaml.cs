using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;

namespace WSA_Installer.Views
{
    public sealed partial class FinishedPage : Page
    {
        public FinishedPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var pm = new PackageManager();

            var result = pm.FindPackagesForUser(string.Empty, "MicrosoftCorporationII.WindowsSubsystemForAndroid_8wekyb3d8bbwe").ToList();

            if (result.Count > 0)
            {
#pragma warning disable CA1416 // 验证平台兼容性
                await result[0].GetAppListEntries()[0].LaunchAsync();
#pragma warning restore CA1416 // 验证平台兼容性
            }
            else
            {
                await new ContentDialog()
                {
                    Title = "失败",
                    Content = "未找到已安装的WSA,请检查后重试",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot,
                }.ShowAsync();
            }
        }
    }
}
