using Microsoft.Win32;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using log4net.Config;

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
        private string buyConfirmPageId = "a-page";

        //use for checking:
        private const string userData = @"user-data-dir=";
        private string userCheckDir = @"\AppData\Local\Google\Chrome\User Data\amazonCheck";
        private string userBuyDir = @"\AppData\Local\Google\Chrome\User Data\Amazon";
        private string localCheckDir;
        private string localBuyDir;

        private string amazonListItemFile = "amazonList.json";
        private string ScreenShortDir = "ScreenShortDebug";

        public ObservableCollection<BuyItemModel> AsinList { get; set; } = new ObservableCollection<BuyItemModel>();
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        object lockObject = new object();
        private bool isAmazonRunning = false;
        CancellationTokenSource cancellSource;
        CancellationToken cancelToken;

        IWebDriver checkDriver, buyDriver;
        #endregion

        #region NewEgg
        //public string NewEgg = "https://www.newegg.com/"; //https://www.newegg.com/d/N82E16819113497 - id
        //public string NEAddToCartPath = "//*[@id=\"ProductBuy\"]/div[1]/div[2]/button";

        //public ObservableCollection<BuyItemModel> NEUIdList { get; set; } = new ObservableCollection<BuyItemModel>();
        //public ObservableCollection<string> NEULogs { get; set; } = new ObservableCollection<string>();

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
        private const int noPerBrowser = 5;
        public Boolean IsHeadless { get; set; }

        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            localCheckDir = userPath + userCheckDir;
            localBuyDir = userPath + userBuyDir;
            userCheckDir = userData + localCheckDir;
            userBuyDir = userData + localBuyDir;

            cancellSource = new CancellationTokenSource();
            cancelToken = cancellSource.Token;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!isAmazonRunning && AsinList.Count > 0)
            {
                try
                {
                    int timeStam = int.Parse(txtTimeStamp.Text);
                    repeatTime = TimeSpan.FromMilliseconds(timeStam);
                }
                catch (Exception)
                {
                    //Ignore user timestam
                }

                isAmazonRunning = true;
                cancellSource = new CancellationTokenSource();
                cancelToken = cancellSource.Token;
                int no = (AsinList.Count - 1) / noPerBrowser;
                for (int i = 0; i <= no; i++)
                {
                    await Task.Run(() => checkAvailable(i), cancelToken);
                }
            }
        }

        private IWebDriver CreateWebDriver(int index)
        {
            ChromeOptions options = new ChromeOptions();
            //Check product availabel
            //if (chkBoxHeadless.IsChecked == true)
            //{
            //    option.AddArguments("headless");
            //}
            //else
            //{
            //copy cache
            //Now Create all of the directories

            var checkDir = localCheckDir + index;
            if (!Directory.Exists(checkDir))
            {
                foreach (string dirPath in Directory.GetDirectories(localCheckDir, "*",
                                    SearchOption.AllDirectories))
                {
                    string subPath = dirPath.Replace(localCheckDir, checkDir);
                    if (!Directory.Exists(subPath))
                    {
                        Directory.CreateDirectory(subPath);
                    }
                }

                foreach (string newPath in Directory.GetFiles(localCheckDir, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(localCheckDir, checkDir), true);
            }

            options.AddArgument(userCheckDir + index);
            options.PageLoadStrategy = PageLoadStrategy.Eager;

            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;


            var webdriver = new ChromeDriver(driverService, options);
            webdriver.Manage().Window.Position = new System.Drawing.Point(0, 0);
            webdriver.Manage().Window.Size = new System.Drawing.Size(800, 600);
            return webdriver;
        }

        private IWebDriver CreateBuyWebDriver(int index)
        {

            var buyDir = localBuyDir + index;
            if (!Directory.Exists(buyDir))
            {
                foreach (string dirPath in Directory.GetDirectories(localBuyDir, "*",
                SearchOption.AllDirectories))
                {
                    string subPath = dirPath.Replace(localBuyDir, buyDir);
                    if (!Directory.Exists(subPath))
                    {
                        Directory.CreateDirectory(subPath);
                    }
                }
                foreach (string newPath in Directory.GetFiles(localBuyDir, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(localBuyDir, buyDir), true);
            }

            ChromeOptions option = new ChromeOptions();
            option.AddArgument(userBuyDir + index);

            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            var webdriver = new ChromeDriver(driverService, option);
            return webdriver;
        }

        private async void checkAvailable(int Number) //start from 0
        {
            int startIndex = Number * noPerBrowser;
            if (startIndex >= AsinList.Count) return;

            int maxIndex;
            int index = startIndex;

            var checkDriver = CreateWebDriver(Number);
            var checkDriverWait = new WebDriverWait(checkDriver, TimeSpan.FromMilliseconds(200));

            var buyDriver = CreateBuyWebDriver(Number);
            var buyDriverWait = new WebDriverWait(buyDriver, TimeSpan.FromSeconds(3));

            while (true)
            {
                if (cancelToken.IsCancellationRequested || startIndex >= AsinList.Count) //stop thread
                {
                    checkDriver?.Dispose();
                    buyDriver?.Dispose();
                    return;
                }
                maxIndex = (startIndex + noPerBrowser - 1);
                maxIndex = (maxIndex > AsinList.Count - 1) ? AsinList.Count - 1 : maxIndex;

                BuyItemModel asin;
                if (index > maxIndex)
                {
                    index = startIndex;
                }
                asin = AsinList[index];
                index++;

                if (asin.Status == 4) continue;
                UpdateAsinStatus(asin, 1);

                try
                {
                    string url = $@"{amazon}dp/{asin.Asin}";
                    checkDriver.Navigate().GoToUrl(url);

                    var buyBtn = checkDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyNowBtnId)));
                    IWebElement priceBox = null;
                    var listPriceItem = checkDriver.FindElements(By.Id(priceBuyBoxId));
                    if (listPriceItem.Count <= 0)
                        listPriceItem = checkDriver.FindElements(By.Id(priceNewBuyBoxId));

                    if (listPriceItem.Count > 0)
                        priceBox = listPriceItem[0];
                    else
                    {
                        UpdateAsinStatus(asin, 3);
                        log.Info($"{asin.Name} {asin.Asin} stock unknow price");

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
                        log.Info($"{asin.Name} {asin.Asin} stock {asin.Price}");

                        for (int i = 0; i < asin.BuyLimit; i++)
                        {
                            if (true == await BuyItem(buyDriver, buyDriverWait, url))
                            {
                                asin.BuyLimit--;
                            }
                        }

                        if (asin.BuyLimit <= 0)
                        {
                            UpdateAsinStatus(asin, 4);
                        }
                        else
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
                        log.Info($"{asin.Name} {asin.Asin} stock {asin.Price}");
                    }
                    await Task.Delay(repeatTime);
                }
                catch (Exception)
                {
                    UpdateAsinStatus(asin, 0);
                    log.Info($"{asin.Name} {asin.Asin} out of stock");
                    await Task.Delay(repeatTime);
                }
            }
        }

        private async Task<Boolean> BuyItem(IWebDriver driver, WebDriverWait driverWait, string url)
        {
            //reopen browser if user close it
            driver.Navigate().GoToUrl(url);

            try
            {
                var buyBtn = driverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyNowBtnId)));
                buyBtn.Click();

                //Place order show in page
                try
                {
                    var element = driverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath($"id('{buyBtnInPageId}') | id('{buyIframeID}')")));
                    if(element.GetAttribute("id").Equals(buyBtnInPageId))
                    {
                        element.Click();
                    }
                    else
                    {
                        var buyFrame = driverWait.Until((SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyIframeID))));
                        driver.SwitchTo().Frame(buyFrame);
                        var placeOrder = driverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(oneClickPaidId)));
                        placeOrder.Click();
                        driver.SwitchTo().DefaultContent();
                    }

                    var thankPage = driverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(buyConfirmPageId)));
                    await Task.Delay(200);
                    TakeSnapShot(driver);
                    return true;
                }
                catch(Exception ex)
                {
                    log.Debug(ex.Message);
                    log.Error(ex.StackTrace);
                    TakeSnapShot(driver);
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex.Message);
                TakeSnapShot(driver);
                return false;
            }
        }

        private async void AddAsin(object sender, RoutedEventArgs e)
        {
            string limitPrice = txtLimitPrice.Text;
            string limitBuy = txtBuyLimit.Text;

            try
            {
                var item = new BuyItemModel
                {
                    Asin = txtFindAsin.Text,
                    Name = txtName.Text,
                    MaxPrice = string.IsNullOrEmpty(limitPrice) ? 0 : Double.Parse(limitPrice),
                    BuyLimit = string.IsNullOrEmpty(limitBuy) ? 1 : int.Parse(limitBuy)
                };

                bool isAdd = false;
                lock (lockObject)
                {
                    var existItem = AsinList.FirstOrDefault(it => it.Asin.Equals(item.Asin));
                    if (existItem == null)
                    {
                        AsinList.Add(item);
                        isAdd = true;
                    }
                    else
                    {
                        existItem.MaxPrice = item.MaxPrice;
                    }
                }
                if (isAdd && (AsinList.Count - 1) % noPerBrowser == 0)
                {
                    isAmazonRunning = true;
                    await Task.Run(() => checkAvailable((AsinList.Count - 1) / noPerBrowser));
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex.Message);
                log.Error(ex.StackTrace);
            }
        }

        private void UpdateAsinStatus(BuyItemModel asin, int status)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
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
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                Logs.Add(log);
            }));
        }

        private void ClearLog()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Logs.Clear();
            }));
        }
        private void UpdateAsinPrice(BuyItemModel asin, double price)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
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
            isAmazonRunning = false;
            cancellSource?.Cancel();
            cancellSource?.Dispose();
            cancellSource = null;

            checkDriver?.Dispose();
            buyDriver?.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cancellSource?.Cancel();
            checkDriver?.Dispose();
            buyDriver?.Dispose();
        }

        private void MenuItem_Remove(object sender, RoutedEventArgs e)
        {
            if (ListAsin.SelectedItem != null)
            {
                var select = ListAsin.SelectedItem as BuyItemModel;
                lock (lockObject)
                {
                    AsinList.Remove(select);
                }
            }
        }

        private void saveList_Click(object sender, RoutedEventArgs e)
        {
            //Save laste lbi objec
            SaveList();
        }

        private void SaveList()
        {
            if (IsAmazon.IsChecked == true && AsinList.Count > 0)
            {
                TextWriter writer = null;
                try
                {
                    var contentToWrite = JsonConvert.SerializeObject(AsinList);
                    writer = new StreamWriter(amazonListItemFile, false);
                    writer.Write(contentToWrite);
                }
                finally
                {
                    writer?.Close();
                }

            }
        }

        private async void loadList_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Json list|*.json"; ;
            openDialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            if (true == openDialog.ShowDialog())
            {
                var filePath = openDialog.FileName;
                if (IsAmazon.IsChecked == true)
                {
                    TextReader reader = null;
                    try
                    {
                        reader = new StreamReader(filePath);
                        var fileContent = reader.ReadToEnd();
                        var tempList = JsonConvert.DeserializeObject<List<BuyItemModel>>(fileContent);
                        foreach (var item in tempList)
                        {
                            var exist = AsinList.FirstOrDefault(i => i.Asin.Equals(item.Asin, StringComparison.OrdinalIgnoreCase));
                            if (exist == null)
                            {
                                lock (lockObject)
                                {
                                    AsinList.Add(item);
                                }
                                if ((AsinList.Count - 1) % noPerBrowser == 0)
                                {
                                    isAmazonRunning = true;
                                    await Task.Run(() => checkAvailable((AsinList.Count - 1) / noPerBrowser));
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }

            }
        }

        private void ClearAllLogs(object sender, RoutedEventArgs e)
        {
            Logs.Clear();
        }

        private async void SettingBrowser_Click(object sender, RoutedEventArgs e)
        {
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;

            //clear old cache file
            for (int i = 1; i < 10; i++)
            {
                if (Directory.Exists(localBuyDir + i))
                {
                    Directory.Delete(localBuyDir + i, true);
                }
                if (Directory.Exists(localCheckDir + i))
                {
                    Directory.Delete(localCheckDir + i, true);
                }
            }

            ChromeOptions buyOptions = new ChromeOptions();
            buyOptions.AddArgument(userBuyDir);
            buyDriver = new ChromeDriver(driverService, buyOptions);

            ChromeOptions checkOptions = new ChromeOptions();
            checkOptions.AddArgument(userCheckDir);
            checkDriver = new ChromeDriver(driverService, checkOptions);

            await Task.Delay(TimeSpan.FromMinutes(2));
            checkDriver?.Dispose();
            buyDriver?.Dispose();
        }

        private async void TakeSnapShot(IWebDriver driver)
        {
            await Task.Run(() =>
            {
                try
                {
                    var ss = (driver as ITakesScreenshot).GetScreenshot();
                    var fileName = String.Format("{0:HH-mm-ss}", DateTime.Now) + ".jpg";
                    var folderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, ScreenShortDir, String.Format("{0:ddMMM}", DateTime.Now));
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    var filePath = Path.Combine(folderPath, fileName);
                    ss.SaveAsFile(filePath, ScreenshotImageFormat.Jpeg);
                }
                catch (Exception ex)
                {
                    log.Debug(ex.Message);
                    log.Error(ex.StackTrace);
                }
            });
        }

    }
}
