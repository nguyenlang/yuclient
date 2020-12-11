using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace YUClientSelenium
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SeleYoutubeWorker youtubeWorker;
        private String defaultVideoLink = "https://www.youtube.com/watch?v=y8M2M1kM6H8";
        private String playUrl;


        //list account password: load form file
        private List<UserAccount> accounts = new List<UserAccount>();
        private DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            accounts.Add(
                new UserAccount { UserName = "jeenjohny34", PassWord = "Test4Farm" });
            accounts.Add(
                new UserAccount { UserName = "lunglinhxinh69", PassWord = "Test4Farm", confirmMail= "jeenjohny34@gmail.com" });

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromMinutes(5);   //video duration
            dispatcherTimer.Tick += RunvideoRepeatly;
        }

        private void RunvideoRepeatly(object sender, EventArgs e)
        {
            youtubeWorker?.OpenYoutubeUrl(playUrl);
        }

        private async void playVideo_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer?.Stop();
            youtubeWorker?.Close();

            playUrl = videoUrl.Text;
            if(string.IsNullOrEmpty(playUrl))
            {
                playUrl = defaultVideoLink;
            }

            youtubeWorker = new SeleYoutubeWorker();

            //login
            await youtubeWorker.LoginAuthAccountAsync(accounts[0].UserName, accounts[0].PassWord);
            await Task.Delay(500);

            //open video
            youtubeWorker?.OpenYoutubeUrl(playUrl);
            dispatcherTimer.Start();
            await Task.Delay(10 * 1000);
            youtubeWorker?.Play();

            await Task.Delay(30 * 1000);
            youtubeWorker?.Like();

            await Task.Delay(10* 1000);
            youtubeWorker?.Subcribe();

            await Task.Delay(5000);
            await youtubeWorker?.CommentAsync("I Like this video \n Can not imaging how they can do like that");
        }

        private async void changeAccount_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer?.Stop();
            youtubeWorker?.Close();

            youtubeWorker = new SeleYoutubeWorker();
            await youtubeWorker.LoginNewAccountAsync(accounts[1].UserName, accounts[1].PassWord, accounts[1].confirmMail);

            playUrl = videoUrl.Text;
            if (string.IsNullOrEmpty(playUrl))
            {
                playUrl = defaultVideoLink;
            }

            youtubeWorker?.OpenYoutubeUrl(playUrl);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

     }
}
