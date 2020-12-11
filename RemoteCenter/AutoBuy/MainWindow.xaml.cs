using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace AutoBuy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //key and xpath
        private string amazon = "https://www.amazon.com/";
        //private string searchBoxID = "twotabsearchtextbox";
        //private string searchBtnID = "nav-search-submit-text";

        //private string firstSearchItemXPath = "//*[@id=\"search\"]/div[1]/div[2]/div/span[3]/div[2]/div[2]/div/span/div";
        //private string firstSearchItemTxtPath = "//*[@id=\"search\"]/div[1]/div[2]/div/span[3]/div[2]/div[2]/div/span/div/div/div[2]/div[2]/div/div[1]/div/div/div[1]/h2/a/span";


        private string orderPriceId = "price_inside_buybox"; //only for buybox
        private string buyNowBtnId = "submit.buy-now";

        private string closeAddFramePath = "//*[contains(@id,'a-popover')]/div/header/button";

        private string buyIframeID = "turbo-checkout-iframe";
        private string oneClickPaidId = "turbo-checkout-place-order-button";

        //use for checking:
        public const String webdriverCheckDir = @"user-data-dir=C:\Users\Admin\AppData\Local\Google\Chrome\User Data\amazonCheck";
        //use for place order
        public const String webdriverBuyDir = @"user-data-dir=C:\Users\Admin\AppData\Local\Google\Chrome\User Data\Amazon";

        public ObservableCollection<BuyItemModel> AsinList { get; set; } = new ObservableCollection<BuyItemModel>();
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        IWebDriver webCheckDriver;
        WebDriverWait webCheckDriverWait;

        IWebDriver webOrderkDriver;
        WebDriverWait webOrderDriverWait;

        DispatcherTimer timer = new DispatcherTimer();

        object lockObject = new object();
        private bool isRunning = false;

        CancellationTokenSource cancellSource;
        CancellationToken cancelToken;

        //Config time
        TimeSpan delayTime = TimeSpan.FromSeconds(2);

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();
            options.AddArgument(webdriverBuyDir);

            webOrderkDriver = new ChromeDriver(driverService, options);
            webOrderDriverWait = new WebDriverWait(webOrderkDriver, TimeSpan.FromSeconds(10));

            webOrderkDriver.Navigate().GoToUrl(amazon);

        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if(!isRunning)
            {
                //Check product availabel
                ChromeOptions option = new ChromeOptions();
                //option.AddArguments(new string[] { webdriverCheckDir, "headless" });
                option.AddArgument(webdriverCheckDir);
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                webCheckDriver?.Dispose();

                webCheckDriver = new ChromeDriver(driverService, option);
                webCheckDriverWait = new WebDriverWait(webCheckDriver, TimeSpan.FromMilliseconds(100));

                cancellSource = new CancellationTokenSource();
                cancelToken = cancellSource.Token;
                isRunning = true;
                await Task.Run(() => checkAvailable(), cancelToken);
            }   
        }

        private async void checkAvailable()
        {
            int index = 0;
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    return;

                BuyItemModel asin;
                if (AsinList.Count > 0)
                {
                    if (index >= AsinList.Count)
                    {
                        index = 0;
                    }

                    asin = AsinList[index];
                    index++;

                    if (asin.Status == 2) continue;
                    UpdateAsinStatus(asin, 1);

                    try
                    {
                        string url = $@"{amazon}dp/{asin.Asin}";
                        webCheckDriver.Navigate().GoToUrl(url);

                        var buyBtn = webCheckDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(buyNowBtnId)));
                        var priceBox = webCheckDriver.FindElement(By.Id(orderPriceId));

                        //remove $ 
                        string priceValue = priceBox.Text;
                        priceValue = priceValue.Substring(1);
                        double price = Double.Parse(priceValue);
                        UpdateAsinStatus(asin, 2);
                        UpdateAsinPrice(asin, price);

                        if (asin.MaxPrice == 0 || price <= asin.MaxPrice)
                        {
                            BuyItem(url);
                        }
                        else
                        {
                            //over price
                            UpdateAsinStatus(asin, 3);
                        }
                        await Task.Delay(delayTime);
                    }
                    catch (Exception)
                    {
                        UpdateAsinStatus(asin, 0);
                        await Task.Delay(delayTime);
                    }
                }
                else
                {
                    await Task.Delay(200);
                }
            }
        }

        private async void BuyItem(string url)
        {
            webOrderkDriver.Navigate().GoToUrl(url);
            var mainWindow = webOrderkDriver.CurrentWindowHandle;

            try
            {
                var buyBtn = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(buyNowBtnId)));
                var priceBox = webOrderkDriver.FindElement(By.Id(orderPriceId));
                buyBtn.Click();

                //popup show, check 
                try
                {
                    //close add cart if it show
                    webCheckDriverWait.Timeout = TimeSpan.FromMilliseconds(100);
                    var closeAddBtn = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath(closeAddFramePath)));
                    closeAddBtn.Click();
                    await Task.Delay(10);
                }
                catch (Exception)
                {
                    Console.WriteLine("Add cart doesn't show");
                }

                var buyFrame = webOrderDriverWait.Until((SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyIframeID))));
                webOrderkDriver.SwitchTo().Frame(buyFrame);
                var placeOrderBtn = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(oneClickPaidId)));
                placeOrderBtn.Click();
                //test buy
                await Task.Delay(TimeSpan.FromSeconds(1));
                webOrderkDriver.SwitchTo().DefaultContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void AddAsin(object sender, RoutedEventArgs e)
        {
            string limitPrice = txtLimitPrice.Text;
            try
            {
                var item = new BuyItemModel
                {
                    Asin = txtFindAsin.Text,
                    Name = txtName.Text,
                    MaxPrice = string.IsNullOrEmpty(limitPrice)? 0 : Double.Parse(limitPrice)
                };

                lock (lockObject)
                {
                    var existItem = AsinList.FirstOrDefault(it => it.Asin.Equals(item.Asin));
                    if(existItem == null)
                    {
                        AsinList.Add(item);
                    }
                    else
                    {
                        existItem.MaxPrice = item.MaxPrice;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void UpdateAsinStatus(BuyItemModel asin, int status)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                try
                {
                    asin.Status = status;
                }
                catch (Exception)
                {
                    //
                }
                
            }));
        }
        private void UpdateAsinPrice(BuyItemModel asin, double price)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                try
                {
                    asin.Price = price;
                }
                catch (Exception)
                {
                    //
                }

            }));
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            webCheckDriver.Dispose();
            isRunning = false;
            cancellSource?.Cancel();
            cancellSource?.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            webCheckDriver?.Dispose();
            webOrderkDriver?.Dispose();
        }

        private void Config_Click(object sender, RoutedEventArgs e)
        {
            //config for check browser
            cancellSource?.Cancel();
            cancellSource?.Dispose();

            webCheckDriver?.Dispose();

            ChromeOptions options = new ChromeOptions();
            options.AddArgument(webdriverCheckDir);

            webCheckDriver = new ChromeDriver(options);
            webCheckDriver.Navigate().GoToUrl(amazon);
        }
    }
}
