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

        private string _homeUrl = "https://www.newegg.com/"; //https://www.newegg.com/p/N82E16819113497;
        private string _loginUrl = "https://secure.newegg.com/NewMyAccount/AccountLogin.aspx?nextpage=https%3A%2F%2Fwww.newegg.com%2F";

        private string _avaiableSignalXPath = "//*[@id=\"ProductBuy\"]/div/div[2]/button";
        private string _priceSignal = ".product-price .price-current";
        private string _localBuyDir;

        IWebDriver _webCheckDriver = null;
        IWebDriver _webBuyDriver = null;

        public object LockObject = new object();
        private bool _isRunning = false;
        private bool _isBuying = false;

        private string _account = "nguyen.lang2505@gmail.com";
        private string _passw = "Jacky1988";

        CancellationTokenSource _cancelSource;
        CancellationToken _cancelToken;

        public ObservableCollection<BuyItemModel> CheckList { set; get; } = new ObservableCollection<BuyItemModel>();
        //Create checking thread
        public ObservableCollection<string> LogList { set; get; } = new ObservableCollection<string>();

        public TimeSpan repeatTime = TimeSpan.FromSeconds(20);
        public TimeSpan delayToLoadElement = TimeSpan.FromSeconds(3);

        public TimeSpan RefreshBuyDriverTime = TimeSpan.FromMinutes(30);

        public NewEggCheckingModel()
        {
            _localBuyDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + NewEgg.UserDir;
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
                _webCheckDriver = new ChromeDriver(driverService, options);

                //buying driver
                ChromeOptions buyOptions = new ChromeOptions();
                buyOptions.AddArgument(CommonUtil.UserData + _localBuyDir);
                _webBuyDriver = new ChromeDriver(driverService, buyOptions);

                //Refresh task
                RefreshBuyChromeDriver();
                //Task.Run(() => RefreshBuyChromeDriver());
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

        private async void RefreshBuyChromeDriver() 
        {
            try
            {
                if (!_isBuying)
                {
                    _webBuyDriver?.Navigate().GoToUrl(_loginUrl);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    var emailBox = _webBuyDriver.FindElements(By.Id("labeled-input-signEmail"));
                    if (emailBox.Count > 0) //login new 
                    {
                        emailBox[0].SendKeys(_account);
                        await Task.Delay(50);

                        var submitBtn = _webBuyDriver.FindElement(By.Id("signInSubmit"));
                        submitBtn.Click();
                        await Task.Delay(1000);

                        var passwBox = _webBuyDriver.FindElement(By.Id("labeled-input-password"));
                        passwBox.SendKeys(_passw);
                        await Task.Delay(100);

                        var signInSumbit = _webBuyDriver.FindElement(By.Id("signInSubmit"));
                        signInSumbit.Click();
                    }
                    else //login again if it requests (already login)
                    {
                        await Task.Delay(1500);
                        var signInSumbit = _webBuyDriver.FindElement(By.Id("signInSubmit"));
                        signInSumbit.Click();
                    }
                    EmptyCard();
                }
            }
            catch (Exception e)
            {
                logs.Error(e.Message);
            }
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

                if (asin.Status == BuyStatus.BUYING || asin.Status == BuyStatus.BOUGHT) continue;
                UpdateAsinStatus(asin, BuyStatus.CHECKING);

                try
                {
                    string url = $@"{_homeUrl}p/{asin.Asin}";
                    _webCheckDriver.Navigate().GoToUrl(url);

                    await Task.Delay(delayToLoadElement);
                    var buyBtn = _webCheckDriver.FindElement(By.XPath(_avaiableSignalXPath));

                    if (buyBtn.Text.ToLower().Contains("add to cart"))
                    {

                        double priceVal = double.MaxValue;
                        //get price
                        var priceList = _webCheckDriver.FindElements(By.CssSelector(_priceSignal));
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
                            await Task.Run(() => BuyItemAsync(asin), _cancelToken);
                            _isBuying = false;
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

        private async void EmptyCard()
        {
            //Clear cart
            try
            {
                _webBuyDriver.Navigate().GoToUrl(_homeUrl);

                await Task.Delay(2000);
                var cartBtn = _webBuyDriver.FindElement(By.XPath("//*[@aria-label= 'Shopping Cart']"));
                cartBtn.Click();

                await Task.Delay(2000);
                var removeAll = _webBuyDriver.FindElement(By.XPath("//*[@data-target= '#Popup_Remove_All']"));
                removeAll.Click();

                await Task.Delay(3000);
                var confirmReAll = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'Yes, Remove all')]"));
                confirmReAll.Click();
                //OK done
            }
            catch (Exception ex)
            {
                logs.Error(ex.Message);
            }
        }

        private async Task BuyItemAsync(BuyItemModel asin)
        {
            //Create buydriver
            //var buyService = asin.BuyService = new BuyService(_localBuyDir, asin.BuyServiceIndex, PageLoadStrategy.None);
            _isBuying = true;
            string url = $@"{_homeUrl}p/{asin.Asin}";

            while (true)
            {
                if (_cancelToken.IsCancellationRequested)
                {
                    return;
                }
                _webBuyDriver.Navigate().GoToUrl(url);

                try
                {
                    await Task.Delay(4000);
                    var buyBtn = _webBuyDriver.FindElement(By.XPath(_avaiableSignalXPath));
                    if (!buyBtn.Text.ToLower().Contains("add to cart"))
                    {
                        UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
                        return;
                    }
                    buyBtn.Click();

                    double priceVal = double.MaxValue;
                    //get price
                    var priceList = _webCheckDriver.FindElements(By.CssSelector(_priceSignal));
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
                        return;
                    }

                    try
                    {
                        await Task.Delay(4000);
                        var element = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'No, thanks') or contains(text(),'View Cart')]"));
                        if (element.Text.ToLower().Contains("no, thanks"))
                        {
                            element.Click();
                            await Task.Delay(1000);
                            var viewcart = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'View Cart')]"));
                            viewcart.Click();
                        }
                        else
                        {
                            element.Click();
                        }
                        await Task.Delay(2000);
                        var secureCheckout = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'Secure Checkout')]"));
                        secureCheckout.Click();

                        //sign in
                        await Task.Delay(2000);
                        var signin = _webBuyDriver.FindElements(By.Id("signInSubmit"));
                        if(signin.Count > 0)
                        {
                            var emailBox = _webBuyDriver.FindElements(By.Id("labeled-input-signEmail"));
                            if (emailBox.Count > 0) //login new 
                            {
                                emailBox[0].SendKeys(_account);
                                await Task.Delay(50);

                                var submitBtn = _webBuyDriver.FindElement(By.Id("signInSubmit"));
                                submitBtn.Click();
                                await Task.Delay(1000);

                                var passwBox = _webBuyDriver.FindElement(By.Id("labeled-input-password"));
                                passwBox.SendKeys(_passw);
                                await Task.Delay(100);

                                var signInSumbit = _webBuyDriver.FindElement(By.Id("signInSubmit"));
                                signInSumbit.Click();
                            }
                            else //login again if it requests (already login)
                            {
                                await Task.Delay(1500);
                                var signInSumbit = _webBuyDriver.FindElement(By.Id("signInSubmit"));
                                signInSumbit.Click();
                            }
                            await Task.Delay(2000);
                        }    
                        
                        IJavaScriptExecutor js = (IJavaScriptExecutor)_webBuyDriver;
                        js.ExecuteScript("window.scrollBy(0, document.body.scrollHeight || document.documentElement.scrollHeight)", "");
                        await Task.Delay(500);
                        var continueToPay = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'Continue to payment')]"));
                        continueToPay.Click();

                        await Task.Delay(1000);
                        var inputCCV = _webBuyDriver.FindElement(By.ClassName("mask-cvv-4"));    //("//input[@aria-label='Security code']"));
                        inputCCV.Click();
                        inputCCV.SendKeys("218"); // CCV the card 8760 3.75$ a Minh

                        js.ExecuteScript("window.scrollBy(0, document.body.scrollHeight || document.documentElement.scrollHeight)", "");
                        await Task.Delay(1000);
                        var reviewYourOrder = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'Review your order')]"));
                        reviewYourOrder.Click();

                        await Task.Delay(2000);
                        var placeOrder = _webBuyDriver.FindElement(By.Id("btnCreditCard"));
                        placeOrder.Click();

                        await Task.Delay(6000);
                        var thankYou = _webBuyDriver.FindElement(By.XPath("//*[contains(text(),'Thank you for your order')]"));
                        //find thankyou
                        TakeSnapShot(_webBuyDriver);
                        IncreaseAsinBought(asin);
                        if (asin.NumberBought >= asin.BuyLimit)
                        {
                            UpdateAsinStatus(asin, BuyStatus.BOUGHT);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        logs.Debug(ex.Message);
                        logs.Error(ex.StackTrace);
                        TakeSnapShot(_webBuyDriver);
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        continue;
                    }
                }
                catch (Exception ex)
                {
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
                    var folderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, NewEgg.ScreenShortDir, String.Format("{0:ddMMM}", DateTime.Now));
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
            TakeSnapShot(_webCheckDriver);
            UpdateAsinStatus(asin, BuyStatus.BOUGHT);
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

                _webBuyDriver?.Dispose();
                _isRunning = false;
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
                _webCheckDriver?.Dispose();
                _webBuyDriver?.Dispose();
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
                writer = new StreamWriter(NewEgg.NewEggListItemFile, false);
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

        public void SettingBrowser_Click()
        {
            return;
        }

        internal void TurnOnNoti(BuyItemModel asin)
        {
            UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
        }
    }
}
