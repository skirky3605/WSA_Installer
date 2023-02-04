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
using Windows.System;

#pragma warning disable CS4014

namespace WSA_Installer.Views
{
    public sealed partial class FailedPage : Page
    {
        public FailedPage()
        {
            this.InitializeComponent();

            SizeChanged += (_s, _e) =>
            {
                ErrorMessage.Width = _e.NewSize.Width / 2;
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is int)
            {
                switch ((int)e.Parameter)
                {
                    case 1:
                        ErrorMessage.Text = "用户取消了操作";
                        FeedBackButton.Visibility = Visibility.Collapsed;
                        break;
                }
            }else if (e.Parameter is string)
            {
                ErrorMessage.Text = e.Parameter.ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri("mailto:skirky@126.com"));
        }
    }
}
