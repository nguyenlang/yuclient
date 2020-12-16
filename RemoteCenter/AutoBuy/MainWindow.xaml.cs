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
        #region Amazon field
        //key and xpath
        private string amazon = "https://www.amazon.com/";
 
        private string priceBuyBoxId = "price_inside_buybox"; //only for buybox
        private string priceNewBuyBoxId = "newBuyBoxPrice";
        private string buyNowBtnId = "buy-now-button";

        //Buy frame
        private string buyIframeID = "turbo-checkout-iframe";
        private string oneClickPaidId = "turbo-checkout-place-order-button";

        //Buy page
        private string buyBtnInPageId = "submitOrderButtonId";
        private string buyConfirmTextPath = "//*[@id=\"a-page\"]/div[3]/div[1]/div[1]/div/div/div/div/div[1]/h4";
        
        //use for checking:
        private const string userData = @"user-data-dir=";
        private string webdriverCheckDir = @"\AppData\Local\Google\Chrome\User Data\amazonCheck";
        //use for place order
        private string webdriverBuyDir = @"\AppData\Local\Google\Chrome\User Data\Amazon";

        
        public ObservableCollection<BuyItemModel> AsinList { get; set; } = new ObservableCollection<BuyItemModel>();
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        IWebDriver webCheckDriver;
        WebDriverWait webCheckDriverWait;

        IWebDriver webOrderkDriver;
        WebDriverWait webOrderDriverWait;
        DispatcherTimer timer = new DispatcherTimer();

        object lockObject = new object();
        private bool isAmazonRunning = false;
        CancellationTokenSource cancellSource;
        CancellationToken cancelToken;
        #endregion

        #region NewEgg
        public string NewEgg = "https://www.newegg.com/"; //https://www.newegg.com/d/N82E16819113497 - id
        public string NEAddToCartPath = "//*[@id=\"ProductBuy\"]/div[1]/div[2]/button";

        public ObservableCollection<BuyItemModel> NEUIdList { get; set; } = new ObservableCollection<BuyItemModel>();
        public ObservableCollection<string> NEULogs { get; set; } = new ObservableCollection<string>();

        //IWebDriver NEEggDriver;
        //WebDriverWait NEDriveWait;
        //DispatcherTimer NEReapeatTimer = new DispatcherTimer();
        //object NELockObj = new object();
        //private bool isNERunning = false;
        //CancellationToken NECancellSource;
        //CancellationToken NECancelToken;

        #endregion

        #region BestBuy

        #endregion

        //Config time
        private TimeSpan repeatTime = TimeSpan.FromSeconds(2);

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            webdriverCheckDir = userData + userPath + webdriverCheckDir;
            webdriverBuyDir = userData + userPath + webdriverBuyDir;


            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            ChromeOptions options = new ChromeOptions();
            options.AddArgument(webdriverBuyDir);

            webOrderkDriver = new ChromeDriver(driverService, options);
            webOrderDriverWait = new WebDriverWait(webOrderkDriver, TimeSpan.FromMilliseconds(1000));

            webOrderkDriver.Navigate().GoToUrl(amazon);
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if(!isAmazonRunning)
            {
                ChromeOptions option = new ChromeOptions();
                //Check product availabel
                if (chkBoxHeadless.IsChecked == true)
                {
                    option.AddArguments("headless");
                }
                else
                {
                    option.AddArgument(webdriverCheckDir);
                }

                try
                {
                    int timeStam = int.Parse(txtTimeStamp.Text);
                    repeatTime = TimeSpan.FromMilliseconds(timeStam);
                }
                catch (Exception)
                {
                    //Ignore user timestam
                }
                
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                webCheckDriver?.Dispose();
                webCheckDriver = new ChromeDriver(driverService, option);
                webCheckDriver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                webCheckDriver.Manage().Window.Size = new System.Drawing.Size(800,600);
                await Task.Delay(2000); // input location

                webCheckDriverWait = new WebDriverWait(webCheckDriver, TimeSpan.FromMilliseconds(100));

                cancellSource = new CancellationTokenSource();
                cancelToken = cancellSource.Token;
                isAmazonRunning = true;
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

                        var buyBtn = webCheckDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyNowBtnId)));
                        IWebElement priceBox  = null;
                        var listPriceItem = webCheckDriver.FindElements(By.Id(priceBuyBoxId));
                        if (listPriceItem.Count <= 0)
                            listPriceItem = webCheckDriver.FindElements(By.Id(priceNewBuyBoxId));

                        if (listPriceItem.Count > 0)
                            priceBox = listPriceItem[0];
                        else
                        {
                            UpdateAsinStatus(asin, 3);
                            var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                            AddLog(logTime + " " + asin.Name + " stock " + "unknow price");

                            await Task.Delay(repeatTime);
                            continue;
                        }

                        //remove $ 
                        string priceValue = priceBox.Text;
                        priceValue = priceValue.Substring(1);
                        double price = Double.Parse(priceValue);
                        UpdateAsinStatus(asin, 2);
                        UpdateAsinPrice(asin, price);

                        if (asin.MaxPrice == 0 || price <= asin.MaxPrice)
                        {
                            var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                            AddLog(logTime + " " + asin.Name + " stock " + asin.Price);
                            var ret = await BuyItem(url);
                            if (!ret)
                            {
                                UpdateAsinStatus(asin, 0);
                            }
                        }
                        else
                        {
                            //over price
                            UpdateAsinStatus(asin, 3);
                            var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                            AddLog(logTime + " " + asin.Name + " stock " + asin.Price);
                        }
                        await Task.Delay(repeatTime);
                    }
                    catch (Exception)
                    {
                        UpdateAsinStatus(asin, 0);
                        await Task.Delay(repeatTime);
                    }
                }
                else
                {
                    await Task.Delay(200);
                }
            }
        }

        private async Task<Boolean> BuyItem(string url)
        {
            webOrderkDriver.Navigate().GoToUrl(url);
            try
            {
                var buyBtn = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(buyNowBtnId)));
                buyBtn.Click();
                
                //Place order show in page
                try
                {
                    var placeOrder = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(buyBtnInPageId)));
                    placeOrder.Click();

                    //check buy success
                    var thankPage = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath(buyConfirmTextPath)));
                    return true;
                }
                catch (Exception ex)
                {
                    //time out continue
                }

                try
                {
                    var buyFrame = webOrderDriverWait.Until((SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyIframeID))));
                    webOrderkDriver.SwitchTo().Frame(buyFrame);
                }
                catch (TimeoutException)
                {
                    buyBtn.Click(); // click again if not found frame
                    var buyFrame = webOrderDriverWait.Until((SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyIframeID))));
                    webOrderkDriver.SwitchTo().Frame(buyFrame);
                }

                var placeOrderBtn = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(oneClickPaidId)));
                await Task.Delay(TimeSpan.FromMilliseconds(15));
                placeOrderBtn.Click();
                webOrderkDriver.SwitchTo().DefaultContent();
                try
                {
                    var thankPage = webOrderDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath(buyConfirmTextPath)));
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
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

        private void AddLog(string log)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                Logs.Add(log);
            }));
        }

        private void ClearLog()
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                Logs.Clear();
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
            isAmazonRunning = false;
            cancellSource?.Cancel();
            cancellSource?.Dispose();
            cancellSource = null;
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
            cancellSource = null;

            webCheckDriver?.Dispose();

            ChromeOptions options = new ChromeOptions();
            options.AddArgument(webdriverCheckDir);

            webCheckDriver = new ChromeDriver(options);
            webCheckDriver.Navigate().GoToUrl(amazon);
        }

        private void MenuItem_Remove(object sender, RoutedEventArgs e)
        {
            if(ListAsin.SelectedItem != null)
            {
                var select = ListAsin.SelectedItem as BuyItemModel;
                lock(lockObject)
                {
                    AsinList.Remove(select);
                }    
            }    
        }

        private void btnAddNeItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
