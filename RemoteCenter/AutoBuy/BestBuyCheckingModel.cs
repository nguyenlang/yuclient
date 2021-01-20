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
    public class BestBuyCheckingModel

    {
        private static readonly log4net.ILog logs =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        private string _homeUrl = "https://www.bestbuy.com/site/"; //format: https://www.bestbuy.com/site/6438943.p?skuId=6438943;
        private string _addToCartBtn = "//*[starts-with(@id,'fulfillment-add-to-cart-button-')]/div/div/div/button";
        
        private string _priceSignal; //can not get price, dynamic id and xpath
        private string _listItemFileName = "bestBuyList.json";
        private string _chromeCacheDir = @"\AppData\Local\Google\Chrome\User Data\BestBuy";

        public object LockObject = new object();
        private bool _isRunning = false;
        CancellationTokenSource _cancelSource;
        CancellationToken _cancelToken;

        public ObservableCollection<BuyItemModel> CheckList { set; get; } = new ObservableCollection<BuyItemModel>();
        public ObservableCollection<string> LogList { set; get; } = new ObservableCollection<string>();

        public TimeSpan repeatTime = TimeSpan.FromSeconds(20);
        private IWebDriver _checkDriver = null;
        public BestBuyCheckingModel()
        {
            _chromeCacheDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + _chromeCacheDir;
            InitialWebDriver();
        }

        public void InitialWebDriver()
        {
            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument(CommonUtil.UserData + _chromeCacheDir);
                options.PageLoadStrategy = PageLoadStrategy.Eager; //not wait to load completed

                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;
                _checkDriver = new ChromeDriver(driverService, options);
                _checkDriver.Navigate().GoToUrl(_homeUrl);
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

                if (asin.Status == BuyStatus.BUYING || asin.Status == BuyStatus.BOUGHT) continue;
                UpdateAsinStatus(asin, BuyStatus.CHECKING);

                try
                {
                    string url = $@"{_homeUrl}{asin.Asin}.p?skuId={asin.Asin}";
                    _checkDriver.Navigate().GoToUrl(url);

                    await Task.Delay(4000);
                    var AddToCartBtn = _checkDriver.FindElement(By.XPath(_addToCartBtn));
                    if (AddToCartBtn.Text.ToLower().Contains("add to cart"))
                    {
                        UpdateAsinStatus(asin, BuyStatus.BUYING);
                        AddLog($"{asin.Name} {asin.Asin} stock");
                        Task.Run(() => SendNotifyAsync(asin));
                    }
                    else
                    {
                        UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
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

        private async Task SendNotifyAsync(BuyItemModel asin)
        {
            ZaloHelper.SendZaloMessage($"{asin.Name} {asin.Asin} stock buy it: {_homeUrl}{asin.Asin}.p?skuId={asin.Asin}");
            UpdateAsinStatus(asin, BuyStatus.BOUGHT);
        }

        public void AddItem(BuyItemModel buyItem)
        {
            var item = CheckList.FirstOrDefault(i => i.Asin.Equals(buyItem.Asin, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
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
                _checkDriver?.Dispose();
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

        internal void TurnOnNoti(BuyItemModel asin)
        {
            UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK);
        }
    }
}
