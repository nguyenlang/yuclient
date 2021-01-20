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
using SeleniumExtras.WaitHelpers;

namespace AutoBuy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Amazon field

        private string localCheckDir, localBuyDir;

        public ObservableCollection<BuyItemModel> AsinList { get; set; } = new ObservableCollection<BuyItemModel>();
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        object lockObject = new object();
        private bool isAmazonRunning = false;
        CancellationTokenSource cancellSource;
        CancellationToken cancellToken;

        IWebDriver checkDriver, buyDriver;
        #endregion

        //Config time
        private TimeSpan repeatTime = TimeSpan.FromSeconds(4);
        private TimeSpan delayToLoadElement = TimeSpan.FromSeconds(3);
        private const int noPerBrowser = 5;
        public Boolean IsHeadless { get; set; }

        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public NewEggCheckingModel NewEggChecking { set; get; }
        public BestBuyCheckingModel BestBuyChecking { set; get; }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            NewEggChecking = new NewEggCheckingModel();
            BestBuyChecking = new BestBuyCheckingModel();

            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            localCheckDir = userPath + Amazon.UserCheckDir;
            localBuyDir = userPath + Amazon.UserBuyDir;
        }

        #region Amazon
        private IWebDriver CreateWebDriver(int index)
        {
            ChromeOptions options = new ChromeOptions();
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

            options.AddArgument(CommonUtil.UserData + checkDir);
            options.PageLoadStrategy = PageLoadStrategy.None;

            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;


            var webdriver = new ChromeDriver(driverService, options);
            webdriver.Manage().Window.Position = new System.Drawing.Point(0, 0);
            webdriver.Manage().Window.Size = new System.Drawing.Size(800, 600);
            return webdriver;
        }

        private async void checkAvailable(int Number) //start from 0
        {
            int startIndex = Number * noPerBrowser;
            if (startIndex >= AsinList.Count) return;

            int maxIndex;
            int index = startIndex;

            var checkDriver = CreateWebDriver(Number);
            //var checkDriverWait = new WebDriverWait(checkDriver, TimeSpan.FromMilliseconds(2));

            while (true)
            {
                if (cancellToken.IsCancellationRequested || startIndex >= AsinList.Count || AsinList.Count == 0) //stop thread
                {
                    checkDriver?.Dispose();
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

                if (asin.Status == BuyStatus.BUYING | asin.Status == BuyStatus.BOUGHT) continue;
                UpdateAsinStatus(asin, BuyStatus.CHECKING);

                try
                {
                    string url = $@"{Amazon.HomeUrl}dp/{asin.Asin}";
                    checkDriver.Navigate().GoToUrl(url);

                    await Task.Delay(delayToLoadElement);
                    var buyBtn = checkDriver.FindElement(By.Id("buy-now-button"));
                    UpdateAsinStatus(asin, BuyStatus.BUYING);
                    Task.Run(() => BuyItem(asin));
                    await Task.Delay(repeatTime);
                }
                catch (Exception)
                {
                    UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                    log.Info($"{asin.Name} {asin.Asin} out of stock");
                    await Task.Delay(repeatTime);
                }
            }
        }

        private async void BuyItemNoElementWait(BuyItemModel asin)
        {
            BuyService buyService = asin.BuyService;
            if (buyService?.WebDriver == null)
            {
                var index = GetSmallestFreeIndex();
                asin.BuyServiceIndex = index;
                buyService = asin.BuyService = new BuyService(localBuyDir, index);
            }

            string url = $@"{Amazon.HomeUrl}dp/{asin.Asin}";
            while (true)
            {
                //reopen browser if user close it
                buyService.WebDriver.Navigate().GoToUrl(url);
                try
                {
                    await Task.Delay(delayToLoadElement);
                    var buyBtn = buyService.WebDriver.FindElement(By.Id("buy-now-button"));

                    //Check price
                    IWebElement priceBox = null;
                    var listPriceItem = buyService.WebDriver.FindElements(By.Id("price_inside_buybox"));
                    if (listPriceItem.Count <= 0)
                        listPriceItem = buyService.WebDriver.FindElements(By.Id("newBuyBoxPrice"));

                    double price = double.MaxValue;
                    var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                    if (listPriceItem.Count > 0)
                    {
                        priceBox = listPriceItem[0];
                        //remove $ 
                        string priceValue = priceBox.Text;
                        priceValue = priceValue.Substring(1);
                        Double.TryParse(priceValue, out price);
                        UpdateAsinPrice(asin, price);
                    }

                    if (asin.MaxPrice != 0 && price >= asin.MaxPrice)
                    {
                        AddLog(logTime + " " + asin.Name + " over price " + asin.Price);
                        log.Info($"{asin.Name} {asin.Asin} stock {asin.Price}");
                        UpdateAsinStatus(asin, BuyStatus.OVER_PRICE);
                        return;
                    }

                    AddLog(logTime + " " + asin.Name + " stock " + asin.Price);
                    log.Info($"{asin.Name} {asin.Asin} stock {asin.Price}");
                    buyBtn.Click();
                    ////
                    try
                    {
                        await Task.Delay(6000);
                        var element = buyService.WebDriver.FindElement(By.XPath($"id('submitOrderButtonId') | id('turbo-checkout-iframe')"));
                        if (element.GetAttribute("id").Equals("submitOrderButtonId"))
                        {
                            element.Click();
                        }
                        else
                        {
                            buyService.WebDriver.SwitchTo().Frame(element);
                            await Task.Delay(3000);
                            var placeOrder = buyService.WebDriver.FindElement(By.Id("turbo-checkout-place-order-button"));
                            placeOrder.Click();
                            buyService.WebDriver.SwitchTo().DefaultContent();
                        }
                        await Task.Delay(3000);
                        var thankLabel = buyService.WebDriver.FindElement(By.XPath("//*[@id=\"widget-purchaseConfirmationStatus\"]/div/h4"));
                        TakeSnapShot(buyService.WebDriver);
                        if (thankLabel.Text.ToLower().Contains("thank"))
                        {
                            IncreaseAsinBought(asin);
                            if (asin.NumberBought >= asin.BuyLimit)
                            {
                                UpdateAsinStatus(asin, BuyStatus.BOUGHT);
                            }
                            else
                            {
                                UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                            }
                            return;
                        }
                        else
                        {
                            await Task.Delay(repeatTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Debug(ex.Message);
                        log.Error(ex.StackTrace);
                        TakeSnapShot(buyService.WebDriver);
                        await Task.Delay(repeatTime);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    log.Debug(ex.Message);
                    TakeSnapShot(buyService.WebDriver);
                    //Clear status back to check
                    UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                    return;
                }

            }
        }

        private async Task BuyItem(BuyItemModel asin)
        {
            BuyService buyService = asin.BuyService;
            if (buyService?.WebDriver == null)
            {
                var index = GetSmallestFreeIndex();
                asin.BuyServiceIndex = index;
                buyService = asin.BuyService = new BuyService(localBuyDir, index);
            }

            string url = $@"{Amazon.HomeUrl}dp/{asin.Asin}";
            while (true)
            {
                //reopen browser if user close it
                buyService.WebDriver.Navigate().GoToUrl(url);
                try
                {
                    var buyBtn = buyService.WebDriverWait.Until(ExpectedConditions.ElementIsVisible(By.Id("buy-now-button")));

                    //Check price
                    IWebElement priceBox = null;
                    var listPriceItem = buyService.WebDriver.FindElements(By.Id("price_inside_buybox"));
                    if (listPriceItem.Count <= 0)
                        listPriceItem = buyService.WebDriver.FindElements(By.Id("newBuyBoxPrice"));

                    double price = double.MaxValue;
                    var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                    if (listPriceItem.Count > 0)
                    {
                        priceBox = listPriceItem[0];
                        //remove $ 
                        string priceValue = priceBox.Text;
                        priceValue = priceValue.Substring(1);
                        Double.TryParse(priceValue, out price);
                        UpdateAsinPrice(asin, price);
                    }

                    if (asin.MaxPrice != 0 && price >= asin.MaxPrice)
                    {
                        AddLog(logTime + " " + asin.Name + " over price " + asin.Price);
                        log.Info($"{asin.Name} {asin.Asin} {asin.Price} =>> over price");
                        UpdateAsinStatus(asin, BuyStatus.OVER_PRICE);
                        return;
                    }

                    AddLog(logTime + " " + asin.Name + " stock at" + asin.Price);
                    log.Info($"{asin.Name} {asin.Asin} stock at {asin.Price}");
                    buyBtn.Click();

                    //Place order show in page
                    try
                    {
                        //check buy popup window
                        var element = buyService.WebDriverWait.Until(ExpectedConditions.ElementIsVisible(By.XPath($"//*[contains(@id,'a-popover-header-') or @id='submitOrderButtonId']")));
                        if (element.GetAttribute("id").Equals("submitOrderButtonId"))
                        {
                            element.Click();
                        }
                        else
                        {
                            var title = element.Text;
                            if(title.ToLower().Contains("add to your order"))
                            {
                                //Find nothank button
                                var nothanksBtn = buyService.WebDriver.FindElements(By.XPath("//*[contains(text(),'No Thanks')]"));
                                nothanksBtn?[0].Click();
                            }

                            //find and switch to iframe
                            var buyFrame = buyService.WebDriverWait.Until(ExpectedConditions.ElementIsVisible(By.Id("turbo-checkout-iframe")));
                            buyService.WebDriver.SwitchTo().Frame(buyFrame);

                            var placeOrderBtn = buyService.WebDriverWait.Until(ExpectedConditions.ElementIsVisible(By.Id("turbo-checkout-place-order-button")));
                            placeOrderBtn.Click();
                            buyService.WebDriver.SwitchTo().DefaultContent();
                        }

                        var thankLabel = buyService.WebDriverWait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"widget-purchaseConfirmationStatus\"]/div/h4")));
                        await Task.Delay(300);
                        TakeSnapShot(buyService.WebDriver);
                        if (thankLabel.Text.ToLower().Contains("thank"))
                        {
                            IncreaseAsinBought(asin);
                            if (asin.NumberBought >= asin.BuyLimit)
                            {
                                UpdateAsinStatus(asin, BuyStatus.BOUGHT);
                            }
                            else
                            {
                                UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                            }
                            return;
                        }
                        else
                        {
                            await Task.Delay(repeatTime);
                        }

                    }
                    catch (Exception ex)
                    {
                        log.Debug(ex.Message);
                        log.Error(ex.StackTrace);
                        TakeSnapShot(buyService.WebDriver);
                        await Task.Delay(repeatTime);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    log.Debug(ex.Message);
                    TakeSnapShot(buyService.WebDriver);
                    //Clear status back to check
                    UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                    return;
                }
            }
        }
        private void UpdateAsinStatus(BuyItemModel asin, BuyStatus status)
        {
            lock (lockObject)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        asin.Status = status;
                    }
                    catch (Exception)
                    {
                    }

                }));
            }
        }

        private void UpdateAsinPrice(BuyItemModel asin, double price)
        {
            lock (lockObject)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        asin.Price = price;
                    }
                    catch (Exception)
                    {
                    }
                }));
            }
        }
        private void IncreaseAsinBought(BuyItemModel asin)
        {
            lock (lockObject)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        asin.NumberBought++;
                    }
                    catch (Exception)
                    {
                    }
                }));
            }
        }
        private void AddLog(string log)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                Logs.Add(log);
            }));
        }

        private void ClearAllLogs(object sender, RoutedEventArgs e)
        {
            Logs.Clear();
        }

        private async void SettingBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (RbtnAmazon.IsChecked == true)
            {
                try
                {
                    checkDriver?.Dispose();
                    buyDriver?.Dispose();
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }

                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                //clear old cache file
                for (int i = 1; i < 30; i++)
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
                buyOptions.AddArgument(CommonUtil.UserData + localBuyDir);
                buyDriver = new ChromeDriver(driverService, buyOptions);

                ChromeOptions checkOptions = new ChromeOptions();
                checkOptions.AddArgument(CommonUtil.UserData + localCheckDir);
                checkDriver = new ChromeDriver(driverService, checkOptions);

                await Task.Delay(TimeSpan.FromMinutes(5));
                checkDriver?.Dispose();
                buyDriver?.Dispose();
            }
            else if (RbtnNewEgg.IsChecked == true)
            {
                NewEggChecking?.SettingBrowser_Click();
            }
            else if (RbtnBestBuy.IsChecked == true)
            {
                
            }
        }

        private async void TakeSnapShot(IWebDriver driver)
        {
            await Task.Run(() =>
            {
                try
                {
                    var ss = (driver as ITakesScreenshot).GetScreenshot();
                    var fileName = String.Format("{0:HH-mm-ss}", DateTime.Now) + ".jpg";
                    var folderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Amazon.ScreenShortDir, String.Format("{0:ddMMM}", DateTime.Now));
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

        private int GetSmallestFreeIndex()
        {
            for (int i = 0; ; i++)
            {
                var sv = AsinList.FirstOrDefault(a => a.BuyServiceIndex == i);
                if (sv == null)
                    return i;
            }
        }
        private void MenuItem_Remove(object sender, RoutedEventArgs e)
        {
            if (ListAsin.SelectedItem != null)
            {
                var select = ListAsin.SelectedItem as BuyItemModel;
                lock (lockObject)
                {
                    select.BuyService?.WebDriver?.Dispose();
                    AsinList.Remove(select);
                }
            }
        }

        private void MenuItem_Detail(object sender, RoutedEventArgs e)
        {
            if (ListAsin.SelectedItem != null)
            {
                var selectedItem = ListAsin.SelectedItem as BuyItemModel;
                var dialog = new DetailDiaglog();
                dialog.Initialize(selectedItem, lockObject);
                if (true == dialog.ShowDialog())
                {
                    //update data.
                }
            }
        }
        #endregion
     
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (RbtnAmazon.IsChecked == true)
            {
                AddLog("Start");
                if (!isAmazonRunning && AsinList.Count > 0)
                {
                    isAmazonRunning = true;
                    if (cancellSource == null)
                    {
                        cancellSource = new CancellationTokenSource();
                        cancellToken = cancellSource.Token;
                    }

                    int no = (AsinList.Count - 1) / noPerBrowser;
                    for (int i = 0; i <= no; i++)
                    {
                        await Task.Run(() => checkAvailable(i), cancellToken);
                    }
                }
            }
            else if (RbtnNewEgg.IsChecked == true)
            {
                NewEggChecking?.Start();
            }
            else if (RbtnBestBuy.IsChecked == true)
            {
                //TODO:
            }

        }

        private async void AddAsin(object sender, RoutedEventArgs e)
        {
            string limitPrice = txtLimitPrice.Text;
            string limitBuy = txtBuyLimit.Text;
            if(!IsSettingBrowse())
            {
                return;
            }

            try
            {
                var item = new BuyItemModel
                {
                    Asin = txtFindAsin.Text,
                    Name = txtName.Text,
                    MaxPrice = string.IsNullOrEmpty(limitPrice) ? 0 : Double.Parse(limitPrice),
                    BuyLimit = string.IsNullOrEmpty(limitBuy) ? 1 : int.Parse(limitBuy),
                };

                if (RbtnAmazon.IsChecked == true)
                {
                    item.BuyServiceIndex = GetSmallestFreeIndex();
                    item.BuyService = new BuyService(localBuyDir, item.BuyServiceIndex);
                    
                    bool isAdd = false;
                    var existItem = AsinList.FirstOrDefault(it => it.Asin.Equals(item.Asin));
                    if (existItem == null)
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            AsinList.Add(item);
                        }));
                        isAdd = true;
                    }
                    else
                    {
                        AddLog($"Item {item.Asin} is existed ");
                    }

                    if (isAdd && (AsinList.Count - 1) % noPerBrowser == 0)
                    {
                        isAmazonRunning = true;
                        if (cancellSource == null)
                        {
                            cancellSource = new CancellationTokenSource();
                            cancellToken = cancellSource.Token;
                        }
                        await Task.Run(() => checkAvailable((AsinList.Count - 1) / noPerBrowser));
                    }

                }
                else if (RbtnNewEgg.IsChecked == true)
                {
                    NewEggChecking?.AddItem(item);
               }
                else if (RbtnBestBuy.IsChecked == true)
                {
                    BestBuyChecking?.AddItem(item);
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex.Message);
                log.Error(ex.StackTrace);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if(RbtnAmazon.IsChecked == true)
            {
                isAmazonRunning = false;
                cancellSource?.Cancel();
                cancellSource?.Dispose();
                cancellSource = null;

                checkDriver?.Dispose();
                buyDriver?.Dispose();
                AddLog("Stop");
            }
            else if (RbtnNewEgg.IsChecked == true)
            {
                NewEggChecking?.Stop();
            }
            else if (RbtnBestBuy.IsChecked == true)
            {
                BestBuyChecking?.Stop();
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cancellSource?.Cancel();
            cancellSource?.Dispose();

            checkDriver?.Dispose();
            buyDriver?.Dispose();
            foreach (var serv in AsinList)
            {
                serv?.BuyService?.WebDriver?.Dispose();
            }

            NewEggChecking?.Close();
            BestBuyChecking?.Close();
        }

        private void saveList_Click(object sender, RoutedEventArgs e)
        {
            //Save last list object
            SaveList();
        }

        private void SaveList()
        {
            if (RbtnAmazon.IsChecked == true && AsinList.Count > 0)
            {
                TextWriter writer = null;
                try
                {
                    var contentToWrite = JsonConvert.SerializeObject(AsinList);
                    writer = new StreamWriter(Amazon.AmazonListItemFile, false);
                    writer.Write(contentToWrite);
                }
                finally
                {
                    writer?.Close();
                }

            }else if(RbtnNewEgg.IsChecked == true)
            {
                NewEggChecking.SaveList();
            }else if(RbtnBestBuy.IsChecked == true)
            {
                BestBuyChecking.SaveList();
            }
        }

        private async void loadList_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSettingBrowse())
                return;

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Json list|*.json"; ;
            openDialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            if (true == openDialog.ShowDialog())
            {
                var filePath = openDialog.FileName;
                if (RbtnAmazon.IsChecked == true)
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
                                item.BuyServiceIndex = GetSmallestFreeIndex();
                                item.BuyService = new BuyService(localBuyDir, item.BuyServiceIndex);
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    AsinList.Add(item);
                                }));
                                if ((AsinList.Count - 1) % noPerBrowser == 0)
                                {
                                    isAmazonRunning = true;
                                    if(cancellSource == null)
                                    {
                                        cancellSource = new CancellationTokenSource();
                                        cancellToken = cancellSource.Token;
                                    }
                                    
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
                else if (RbtnNewEgg.IsChecked == true)
                {
                    NewEggChecking.LoadList(filePath);
                }
                else if (RbtnBestBuy.IsChecked == true)
                {
                    BestBuyChecking.LoadList(filePath);
                }

            }
        }
        private void NewEggRemove(object sender, RoutedEventArgs e)
        {
            var select = NewEggList.SelectedItem as BuyItemModel;
            if (select != null)
                NewEggChecking.RemoveItem(select);
        }

        private void NeweggTurnOnNoti(object sender, RoutedEventArgs e)
        {
            var select = NewEggList.SelectedItem as BuyItemModel;
            if (select != null)
                NewEggChecking.TurnOnNoti(select);
        }


        private void NewEggDetail(object sender, RoutedEventArgs e)
        {
            var selectedItem = NewEggList.SelectedItem as BuyItemModel;
            var dialog = new DetailDiaglog();
            dialog.Initialize(selectedItem, NewEggChecking.LockObject);
            if (true == dialog.ShowDialog())
            {
                //update data.
            }
        }

        private void NewEggClearLog(object sender, RoutedEventArgs e)
        {
            NewEggChecking.ClearLog();
        }

        private void BestBuyRemove(object sender, RoutedEventArgs e)
        {
            var select = BestBuyList.SelectedItem as BuyItemModel;
            if (select != null)
                BestBuyChecking.RemoveItem(select);

        }

        private void SetTimeStamp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int timeStam = int.Parse(txtTimeStamp.Text);
                if(RbtnAmazon.IsChecked == true)
                {
                    repeatTime = TimeSpan.FromSeconds(timeStam);
                }else if(RbtnNewEgg.IsChecked == true)
                {
                    NewEggChecking.repeatTime = TimeSpan.FromSeconds(timeStam);
                }else if(RbtnBestBuy.IsChecked == true)
                {
                    BestBuyChecking.repeatTime = TimeSpan.FromSeconds(timeStam);
                }

            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

        }

        private void BestBuyDetail(object sender, RoutedEventArgs e)
        {
            var selectedItem = BestBuyList.SelectedItem as BuyItemModel;
            var dialog = new DetailDiaglog();
            dialog.Initialize(selectedItem, BestBuyChecking.LockObject);
            if (true == dialog.ShowDialog())
            {
                //update data.
            }
        }
        private void BestBuyTurnOnNoti(object sender, RoutedEventArgs e)
        {
            var selectedItem = BestBuyList.SelectedItem as BuyItemModel;
            BestBuyChecking.TurnOnNoti(selectedItem);
        }

        private void BestBuyClearLog(object sender, RoutedEventArgs e)
        {
            BestBuyChecking.ClearLog();
        }

        private bool IsSettingBrowse()
        {
            if (RbtnAmazon.IsChecked == true)
            {
                if (!Directory.Exists(localBuyDir))
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show("Setting browser for amazon checking first");
                    }));
                    return false;
                }

            }
            else if (RbtnNewEgg.IsChecked == true)
            {
                var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var NewEggDir = user + NewEgg.UserDir;
                if (!Directory.Exists(NewEggDir))
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show("Setting browser for newegg first");
                    }));
                    return false;
                }
            }
            else if (RbtnBestBuy.IsChecked == true)
            {
                
            }
            return true;
        }
    }
}
