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
    public class CheckingModel
    {

        private static readonly log4net.ILog logs =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _homeUrl;
        private string _avaiableSignalXPath;
        private string _priceSignal;
        public string _captureScreenFolder;
        public string _listItemFileName { get; set; }

        private int _buyPlatform = 0; //hardcode 0 NewEgg, 1 Bestbuy

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

        public CheckingModel(int platform = 0)
        {
            _buyPlatform = platform;
            InitialPath();

            InitialWebDriver();
        }

        public void InitialWebDriver()
        {
            try
            {
                if(_buyPlatform == 0)
                {
                    ChromeOptions options = new ChromeOptions();
                    //options.AddArgument("headless");

                    string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string newEggChromeDir = NewEgg.UserData + userPath + NewEgg.UserDir;
                    options.AddArgument(newEggChromeDir);
                    options.PageLoadStrategy = PageLoadStrategy.None; //not wait to completed

                    var driverService = ChromeDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    _webDriver = new ChromeDriver(driverService, options);
                    _webDriver.
                }
                
            }
            catch (Exception e)
            {
                logs.Error(e.Message);
            }
        }

        public void InitialPath()
        {
            if(_buyPlatform == 0)
            {
                _homeUrl = NewEgg.HomrUrl;
                _avaiableSignalXPath = NewEgg.AvaiableSignalPath;
                _priceSignal = NewEgg.PriceSignalCSS;
                _listItemFileName = NewEgg.NewEggListItemFile;
            }
            else
            {
                _avaiableSignalXPath = BestBuy.AvaiableSignalPath;
                _priceSignal = NewEgg.PriceSignalCSS;
                _listItemFileName = NewEgg.NewEggListItemFile;
            }    

        }
        public void DestroyWebDriver()
        {

            _webDriver?.Dispose();
            _webDriver = null;
        }

        private void AddLog(string log)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                var logTime = String.Format("{0:d/M/yyyy HH:mm}", DateTime.Now);
                LogList.Add(logTime + ":" +log);
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
                if (index > CheckList.Count -1)
                {
                    index = 0;
                }
                asin = CheckList[index];
                index++;

                if (asin.Status == BuyStatus.BUYING) continue;
                UpdateAsinStatus(asin, BuyStatus.CHECKING);

                try
                {
                    if(_buyPlatform == 0) // NewEgg
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
                            foreach(var el in priceList)
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
                            }
                    }
                    }
                    else //best buy
                    {

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
            SendZaloMessage($"{asin.Name} {asin.Asin} stock at {asin.Price} buy it: {_homeUrl}p/{asin.Asin}");
            await Task.Delay(TimeSpan.FromMinutes(10)); // wait 10 minute before check again
            UpdateAsinStatus(asin, BuyStatus.OUT_OF_STOCK); 
        }

        public void AddItem(BuyItemModel buyItem)
        {
            var item = CheckList.FirstOrDefault(i => i.Asin.Equals(buyItem.Asin, StringComparison.OrdinalIgnoreCase));
            if(item == null)
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

        //Zalo
        private void SendZaloMessage(string message)
        {
            try
            {
                ZaloAppInfo zaloAppInfo = new ZaloAppInfo(ZaloValue.AppId, ZaloValue.AppAccessKey, ZaloValue.RedirectLink);
                ZaloAppClient zaloAppClient = new ZaloAppClient(zaloAppInfo);

                var token = zaloAppClient.getAccessToken(ZaloValue.OAcode);
                var access_token = token["access_token"].ToString();

                var friends = zaloAppClient.getFriends(access_token, 0, 10, "id, name");
                if(friends.ToString().ToLower().Contains("error"))
                {
                    var prob = friends.ToObject<ZaloError>();
                    AddLog($"Zalo error code {prob.error}");
                    logs.Error($"{prob.error} {prob.message}");
                }
                JArray array = (JArray)friends["data"];
                var friendList = array.ToObject<List<ZaloUser>>();

                foreach(var user in friendList)
                {
                    //id a Minh: 7751366822681432042, id Lang: 5303635736398383040
                    var sendMessage = zaloAppClient.sendMessage(access_token, 7751366822681432042, message, "");
                    if (sendMessage.ToString().ToLower().Contains("error"))
                    {
                        var prob = sendMessage.ToObject<ZaloError>();
                        AddLog($"Zalo error code {prob.error}");
                        logs.Error($"{prob.error} {prob.message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logs.Error(ex.Message);
            }

        }
    }
}
