using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;

namespace AutoBuy
{
    public class BuyService
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public IWebDriver WebDriver { get; set; }
        public WebDriverWait WebDriverWait { get; set; }

        public Boolean IsRunning = false;
        public BuyService(string localBuyDir, int order)
        {
            var buyDir = localBuyDir + order;
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
            option.AddArgument(CommonUtil.UserData + buyDir);
            //None: wait specific time, Eager: wait until - need loaded page
            option.PageLoadStrategy = PageLoadStrategy.Eager;
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            try
            {
                WebDriver = new ChromeDriver(driverService, option);
                WebDriverWait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                WebDriver?.Dispose();
                WebDriver = null;
                WebDriverWait = null;

                log.Error(ex.Message);
                log.Error(ex.StackTrace);
            }
        }
    }
}
