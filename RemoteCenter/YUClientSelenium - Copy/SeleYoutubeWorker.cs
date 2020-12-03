using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace YUClientSelenium
{
    class SeleYoutubeWorker
    {
        private IWebDriver webDriver;
        WebDriverWait webDriverWait;
        public SeleYoutubeWorker()
        {
            webDriver = new ChromeDriver();
            webDriverWait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));
        }

        public void OpenYoutubeUrl(string url)
        {
            //TODO: Check if webdriver ready

            webDriver.Navigate().GoToUrl(url);
            //webDriverWait.Until(S)
        }

        public void Like()
        {
            //find like button
            //var likeBtn = webDriver.FindElement(By.)

        }

        public void DisLike()
        {

        }

        public void Subcribe()
        {

        }

        public void Comment(string comment)
        {

        }

        public void SaveCache()
        {

        }

        public void LoadCache()
        {

        }

    }
}
