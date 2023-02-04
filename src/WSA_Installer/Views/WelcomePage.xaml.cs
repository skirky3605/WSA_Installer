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
using System.Management;
using Windows.System;

namespace WSA_Installer.Views
{
    public sealed partial class WelcomePage : Page
    {
        string Arch;

        private bool Warning = false;

        public WelcomePage()
        {
            this.InitializeComponent();

            this.SizeChanged += (_s, _e) =>
            {
                list.Height = _e.NewSize.Height-160;
            };

            CompatibilityCheck();
        }

        private void CompatibilityCheck()
        {
            int memory = (int)((GetTotalPhysicalMemory() / 1024.0 / 1024.0 / 1024.0));
            var OSVersion = Environment.OSVersion.Version;

            OSVersionCheck.Title = "系统版本:" + OSVersion.ToString();
            if ((OSVersion.Build >= 19045 & OSVersion.Revision >= 2311) || (OSVersion.Build >= 22000))
            {
                OSVersionCheck.Severity = InfoBarSeverity.Success;
            }
            else
            {
                NextStep.IsEnabled = false;
                OSVersionCheck.Severity = InfoBarSeverity.Error;
                var OpenUpdate = new HyperlinkButton()
                {
                    Content = "打开Windows更新",
                };
                OpenUpdate.Click += (s, e) =>
                {
                    Launcher.LaunchUriAsync(new Uri("ms-settings:windowsupdate"));
                };
                OSVersionCheck.ActionButton = OpenUpdate;
            }

            MemoryCheck.Title = "内存大小:" + memory.ToString() + "GB";

            if (memory >= 8)
            {
                MemoryCheck.Severity = InfoBarSeverity.Success;
            }
            else
            {
                Warning = true;
                MemoryCheck.Severity = InfoBarSeverity.Warning;
            };

            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;

            CPUCheck.Title = $"CPU架构: {arch}";

            if (arch.ToString() == "X64" || arch.ToString() == "ARM64")
            {
                CPUCheck.Severity = InfoBarSeverity.Success;
                Arch = arch.ToString();
            }
            else
            {
                NextStep.IsEnabled = false;
                CPUCheck.Severity = InfoBarSeverity.Error;
            }
        }

        public static long GetTotalPhysicalMemory()
        {
            long capacity = 0;
            foreach (ManagementObject mo1 in new ManagementClass("Win32_PhysicalMemory").GetInstances())
            capacity += long.Parse(mo1.Properties["Capacity"].Value.ToString());
            return capacity;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Warning)
            {
                await new ContentDialog()
                {
                    Title = "提示",
                    Content = "由于您的系统内存小于最低要求,WSA可能无法很好的运行",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot,
                }.ShowAsync();
            }
            this.Frame.Navigate(typeof(Configs),new object[]{Arch });
        }
    }
}
