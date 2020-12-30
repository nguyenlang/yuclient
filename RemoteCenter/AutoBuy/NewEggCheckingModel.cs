using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ZaloDotNetSDK;

namespace AutoBuy
{
    public class NewEggCheckingModel

    {
        private static readonly log4net.ILog logs =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _homeUrl;
        private string _avaiableSignalXPath;
        private string _priceSignal;
        private string _captureScreenFolder;
        private string _listItemFileName;
        private string _userDir;

        private string _localBuyDir;

        IWebDriver _webDriver = null;
        public object LockObject = new object();
        private bool _isRunning = false;
        CancellationTokenSource _cancelSource;
        CancellationToken _cancelToken;

        public ObservableCollection<BuyItemModel> CheckList { set; get; } = new ObservableCollection<BuyItemModel>();
        //Create checking thread
        public ObservableCollection<string> LogList { set; get; } = new ObservableCollection<string>();

        public TimeSpan repeatTime = TimeSpan.FromSeconds(20);
        public TimeSpan delayToLoadElement = TimeSpan.FromSeconds(3);

        private IWebDriver _settingWebDriver = null;
        public NewEggCheckingModel(int platform = 0)
        {
            InitialPath();
            _localBuyDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + _userDir;
            InitialWebDriver();
        }

        public void InitialWebDriver()
        {
            try
            {
                //checking headless
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("headless");
                options.PageLoadStrategy = PageLoadStrategy.None; //not wait to completed

                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;
                _webDriver = new ChromeDriver(driverService, options);

            }
            catch (Exception e)
            {
                logs.Error(e.Message);
            }
        }

        public void InitialPath()
        {
            _homeUrl = NewEgg.HomrUrl;
            _avaiableSignalXPath = NewEgg.AvaiableSignalPath;
            _priceSignal = NewEgg.PriceSignalCSS;
            _listItemFileName = NewEgg.NewEggListItemFile;
            _userDir = NewEgg.UserDir;
            _captureScreenFolder = NewEgg.ScreenShortDir;
        }
        public void DestroyWebDriver()
        {
            try
            {
                _webDriver?.Dispose();
                _webDriver = null;
            }
            catch (Exception e)
            {
                logs.Error(e.Message);
            }

        }

        private void AddLog(string log)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                LogList.Add(logTime + ":" + log);
            }));
        }

        public void ClearLog()
        {
            LogList?.Clear();
        }

        private async void checkAvailable()
        {
            int index = 0;
            while (true)
            {
                if (_cancelToken.IsCancellationRequested || CheckList.Count <= 0) //stop thread
                {
                    _isRunning = false;
                    return;
                }

                BuyItemModel asin;
                if (index > CheckList.Count - 1)
                {
                    index = 0;
                }
                asin = CheckList[index];
                index++;

                if (asin.Status == BuyStatus.BUYING) continue;
                UpdateAsinStatus(asin, BuyStatus.CHECKING);

                try
                {
                    string url = $@"{_homeUrl}p/{asin.Asin}";
                    _webDriver.Navigate().GoToUrl(url);

                    await Task.Delay(delayToLoadElement);
                    var buyBtn = _webDriver.FindElement(By.XPath(_avaiableSignalXPath));

                    if (buyBtn.Text.ToLower().Contains("add to cart"))
                    {

                        double priceVal = double.MaxValue;
                        //get price
                        var priceList = _webDriver.FindElements(By.CssSelector(_priceSignal));
                        foreach (var el in priceList)
                        {
                            double checkPrice = priceVal;
                            string content = el.Text;
                            if (!string.IsNullOrEmpty(content))
                            {
                                content = content.Substring(1);
                                Double.TryParse(content, out checkPrice);
                                if (checkPrice < priceVal)
                                {
                                    priceVal = checkPrice;
                                    UpdateAsinPrice(asin, priceVal);
                                }
                            }
                        }

                        if (asin.MaxPrice != 0 && priceVal >= asin.MaxPrice)
                        {
                            AddLog(asin.Name + " over price " + asin.Price);
                            logs.Info($"{asin.Name} {asin.Asin} stock at {asin.Price} => over price");
                            UpdateAsinStatus(asin, BuyStatus.OVER_PRICE);
                        }
                        else
                        {
                            UpdateAsinStatus(asin, BuyStatus.BUYING);
                            AddLog($"{asin.Name} {asin.Asin} stock at {asin.Price}");
                            Task.Run(() => SendNotifyAsync(asin));

                            //asin.CancelSource = new CancellationTokenSource();
                            //Task.Run(() => BuyItemAsync(asin), asin.CancelSource.Token);
                        }
                    }

                    await Task.Delay(repeatTime);
                }
                catch (Exception)
                {
                    UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                    logs.Info($"{asin.Name} {asin.Asin} out of stock");
                    await Task.Delay(repeatTime);
                }
            }
        }

        private async Task BuyItemAsync(BuyItemModel asin)
        {
            //Create buydriver
            var buyService = asin.BuyService = new BuyService(_localBuyDir, asin.BuyServiceIndex, PageLoadStrategy.None);
            string url = $@"{_homeUrl}p/{asin.Asin}";

            //Clear cart
            try
            {
                buyService.WebDriver.Navigate().GoToUrl(_homeUrl);

                await Task.Delay(1000);
                var cartBtn = buyService.WebDriver.FindElement(By.XPath("//*[@aria-label= 'Shopping Cart']"));
                cartBtn.Click();

                await Task.Delay(1000);
                var removeAll = buyService.WebDriver.FindElement(By.XPath("//*[@data-target= '#Popup_Remove_All']"));
                removeAll.Click();

                await Task.Delay(2000);
                var confirmReAll = buyService.WebDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath("//*[contains(text(),'Yes, Remove all')]")));
                confirmReAll.Click();
                //OK done
            }
            catch (Exception ex)
            {
                logs.Error(ex.Message);
            }

            while (true)
            {
                if (asin.CancelSource.IsCancellationRequested)
                {
                    asin.BuyService.Close();
                    return;
                }

                buyService.WebDriver.Navigate().GoToUrl(url);

                try
                {
                    await Task.Delay(4000);
                    //var buyBtn = buyService.WebDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath(_avaiableSignalXPath)));
                    var buyBtn = buyService.WebDriver.FindElement(By.XPath(_avaiableSignalXPath));
                    if (!buyBtn.Text.ToLower().Contains("add to cart"))
                    {
                        asin.BuyService.Close();
                        UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                        return;
                    }
                    buyBtn.Click();

                    try
                    {
                        await Task.Delay(4000);
                        var element = buyService.WebDriver.FindElement(By.XPath("//*[contains(text(),'No, thanks') or contains(text(),'View Cart')]"));
                        if (element.Text.ToLower().Contains("no, thanks"))
                        {
                            element.Click();
                            await Task.Delay(1000);
                            var viewcart = buyService.WebDriver.FindElement(By.XPath("//*[contains(text(),'View Cart')]"));
                            viewcart.Click();
                        }
                        else
                        {
                            element.Click();
                        }
                        await Task.Delay(3000);
                        var secureCheckout = buyService.WebDriver.FindElement(By.XPath("//*[contains(text(),'Secure Checkout')]"));
                        secureCheckout.Click();

                        await Task.Delay(2000);
                        var continueToPay = buyService.WebDriver.FindElement(By.XPath("//*[contains(text(),'Continue to payment')]"));
                        continueToPay.Click();

                        await Task.Delay(2000);
                        var inputCCV = buyService.WebDriver.FindElement(By.XPath("//input[@aria-label='Security code']"));
                        inputCCV.SendKeys("218"); // CCV the card 8760 3.75$ a Minh

                        await Task.Delay(100);
                        var reviewYourOrder = buyService.WebDriver.FindElement(By.XPath("//*[contains(text(),'Review your order')]"));
                        reviewYourOrder.Click();

                        await Task.Delay(1000);
                        var placeOrder = buyService.WebDriver.FindElement(By.Id("btnCreditCard"));
                        placeOrder.Click();

                        await Task.Delay(4000);
                        var thankYou = buyService.WebDriver.FindElement(By.XPath("//*[contains(text(),'Thank you for your order')]"));
                        //find thankyou
                        TakeSnapShot(buyService.WebDriver);
                        IncreaseAsinBought(asin);
                        if (asin.NumberBought >= asin.BuyLimit)
                        {
                            UpdateAsinStatus(asin, BuyStatus.BOUGHT);
                            asin.BuyService.Close();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        logs.Debug(ex.Message);
                        logs.Error(ex.StackTrace);
                        TakeSnapShot(buyService.WebDriver);
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    asin.BuyService.Close();
                    UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                    return;
                }

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
                    var folderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, _captureScreenFolder, String.Format("{0:ddMMM}", DateTime.Now));
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    var filePath = Path.Combine(folderPath, fileName);
                    ss.SaveAsFile(filePath, ScreenshotImageFormat.Jpeg);
                }
                catch (Exception ex)
                {
                    logs.Debug(ex.Message);
                    logs.Error(ex.StackTrace);
                }
            });
        }

        private void IncreaseAsinBought(BuyItemModel asin)
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

        private async Task SendNotifyAsync(BuyItemModel asin)
        {
            ZaloHelper.SendZaloMessage($"{asin.Name} {asin.Asin} stock at {asin.Price} buy it: {_homeUrl}p/{asin.Asin}");
            //await Task.Delay(TimeSpan.FromMinutes(10)); // wait 10 minute before check again
            //UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
        }

        public void AddItem(BuyItemModel buyItem)
        {
            var item = CheckList.FirstOrDefault(i => i.Asin.Equals(buyItem.Asin, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                buyItem.BuyServiceIndex = GetSmallestFreeIndex();
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        CheckList.Add(buyItem);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }));

                if (CheckList.Count == 1 && !_isRunning)
                {
                    _cancelSource?.Cancel();
                    _cancelSource?.Dispose();

                    //start 
                    _cancelSource = new CancellationTokenSource();
                    _cancelToken = _cancelSource.Token;
                    _isRunning = true;
                    Task.Run(() => checkAvailable(), _cancelToken);
                }
            }
            else
            {
                AddLog($"Item {buyItem.Asin} is existed");
            }
        }

        public void RemoveItem(BuyItemModel removeItem)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    removeItem?.BuyService?.Close();
                    CheckList.Remove(removeItem);
                }
                catch (Exception)
                {
                    return;
                }
            }));
        }

        public void Start()
        {
            if (!_isRunning)
            {
                AddLog("Start");
                _cancelSource?.Cancel();
                _cancelSource?.Dispose();

                _cancelSource = new CancellationTokenSource();
                _cancelToken = _cancelSource.Token;

                _isRunning = true;
                Task.Run(() => checkAvailable(), _cancelToken);
            }

        }

        public void Stop()
        {
            try
            {
                AddLog("Stop");
                _cancelSource?.Cancel();
                _cancelSource?.Dispose();

                _settingWebDriver?.Dispose();

                foreach (var asin in CheckList)
                {
                    asin.CancelSource?.Cancel();
                    asin.BuyService?.Close();
                }
            }
            catch (Exception)
            {
                //
            }
        }

        public void Close()
        {
            try
            {
                _cancelSource?.Cancel();
                _webDriver?.Dispose();

                _settingWebDriver?.Dispose();
                foreach (var asin in CheckList)
                {
                    asin.CancelSource?.Cancel();
                    asin.BuyService?.Close();
                }
            }
            catch (Exception)
            {
                //
            }
        }

        private void UpdateAsinStatus(BuyItemModel asin, BuyStatus status)
        {
            lock (LockObject)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        asin.Status = status;
                    }
                    catch (Exception e)
                    {
                        logs.Error(e.Message);
                    }
                }));
            }
        }

        private void UpdateAsinPrice(BuyItemModel asin, double price)
        {
            lock (LockObject)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        asin.Price = price;
                    }
                    catch (Exception ex)
                    {
                        logs.Error(ex.Message);
                    }
                }));
            }
        }

        internal void SaveList()
        {
            TextWriter writer = null;
            try
            {
                var contentToWrite = JsonConvert.SerializeObject(CheckList);
                writer = new StreamWriter(_listItemFileName, false);
                writer.Write(contentToWrite);
            }
            finally
            {
                writer?.Close();
            }
        }

        internal void LoadList(string filePath)
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContent = reader.ReadToEnd();
                var tempList = JsonConvert.DeserializeObject<List<BuyItemModel>>(fileContent);
                foreach (var item in tempList)
                {
                    var exist = CheckList.FirstOrDefault(i => i.Asin.Equals(item.Asin, StringComparison.OrdinalIgnoreCase));
                    if (exist == null)
                    {
                        CheckList.Add(item);

                        if (CheckList.Count == 1 && !_isRunning)
                        {
                            _cancelSource?.Cancel();
                            _cancelSource?.Dispose();

                            _cancelSource = new CancellationTokenSource();
                            _cancelToken = _cancelSource.Token;
                            _isRunning = true;
                            Task.Run(() => checkAvailable(), _cancelToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logs.Error(ex.Message);
            }
        }
        private int GetSmallestFreeIndex()
        {
            for (int i = 0; ; i++)
            {
                var sv = CheckList.FirstOrDefault(a => a.BuyServiceIndex == i);
                if (sv == null)
                    return i;
            }
        }

        public async void SettingBrowser_Click()
        {
            try
            {
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                //clear old cache file
                for (int i = 1; i < 30; i++)
                {
                    if (Directory.Exists(_localBuyDir + i))
                    {
                        Directory.Delete(_localBuyDir + i, true);
                    }
                }

                ChromeOptions buyOptions = new ChromeOptions();
                buyOptions.AddArgument(CommonUtil.UserData + _localBuyDir);
                _settingWebDriver = new ChromeDriver(driverService, buyOptions);

                await Task.Delay(TimeSpan.FromMinutes(5));
                _settingWebDriver?.Dispose();
            }
            catch (Exception ex)
            {
                logs.Error(ex.Message);
            }
        }

        //Zalo

    }
}
