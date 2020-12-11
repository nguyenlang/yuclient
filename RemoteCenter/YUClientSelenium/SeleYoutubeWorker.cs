using System;
using System.Collections.Generic;
using System.Linq;  
using System.Text;
using System.Threading;
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
            //Save login session:
            //ChromeOptions option = new ChromeOptions();
            //option.AddArguments(Common.chromeDir);
            //webDriver = new ChromeDriver(option);
            
            //Login each time
            webDriver = new ChromeDriver();
            webDriverWait = new WebDriverWait(webDriver, TimeSpan.FromSeconds(10));
        }

        public void OpenYoutubeUrl(string url)
        {
            //TODO: Check if webdriver ready
            webDriver.Navigate().GoToUrl(url);
        }

        public async Task<bool> LoginAuthAccountAsync(string username, string pass)
        {
            try
            {
                webDriver.Navigate().GoToUrl("https://accounts.google.com/signin");
                var logBox = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(ElementIdentify.loginEditBoxId)));
                await Task.Delay(RandTimeWait());

                logBox.SendKeys(username);
                await Task.Delay(RandTimeWait());
                webDriver.FindElement(By.Id(ElementIdentify.loginNextId)).Click();

                var passBox = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Name(ElementIdentify.passEditBoxName)));
                await Task.Delay(RandTimeWait());
                passBox.SendKeys(pass);
                await Task.Delay(RandTimeWait());
                webDriver.FindElement(By.Id("passwordNext")).Click();

                //wait 500 ms back to youtube
                await Task.Delay(500);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }
        public async Task<bool> LoginNewAccountAsync(string username, string pass, string confirmMail)
        {
            try
            {
                //Go to stack overflow to authen
                webDriver.Navigate().GoToUrl("https://accounts.google.com/o/oauth2/auth/identifier?client_id=717762328687-iludtf96g1hinl76e4lc1b9a82g457nn.apps.googleusercontent.com&scope=profile%20email&redirect_uri=https%3A%2F%2Fstackauth.com%2Fauth%2Foauth2%2Fgoogle&state=%7B%22sid%22%3A1%2C%22st%22%3A%2259%3A3%3Abbc%2C16%3Af36644fc3bb3f4ff%2C10%3A1606815776%2C16%3A60732db463994e70%2C95e7e4cfd247ce507a411b20062791b5a2230cf027761818b4fd757091cb6817%22%2C%22cdl%22%3Anull%2C%22cid%22%3A%22717762328687-iludtf96g1hinl76e4lc1b9a82g457nn.apps.googleusercontent.com%22%2C%22k%22%3A%22Google%22%2C%22ses%22%3A%229e419e7d03fd4b6f9aa9077d19e92848%22%7D&response_type=code&flowName=GeneralOAuthFlow");
                await Task.Delay(100);
                
                IWebElement mailLogBox, passBox;
                mailLogBox = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id(ElementIdentify.loginEditBoxId)));
                mailLogBox.SendKeys(username);
                await Task.Delay(RandTimeWait());
                await Task.Delay(RandTimeWait());
                webDriver.FindElement(By.Id(ElementIdentify.loginNextId)).Click();

                passBox = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Name(ElementIdentify.passEditBoxName)));
                passBox.SendKeys(pass);
                await Task.Delay(RandTimeWait());
                webDriver.FindElement(By.Id("passwordNext")).Click();

                //confirm mail
                var mailConfirm = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.
                    ElementToBeClickable(By.XPath("//*[@id=\"view_container\"]/div/div/div[2]/div/div[1]/div/form/span/section/div/div/div/ul/li[4]")));
                await Task.Delay(RandTimeWait());
                mailConfirm.Click();

                mailLogBox = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.
                    ElementToBeClickable(By.Id("knowledge-preregistered-email-response")));
                mailLogBox.SendKeys(confirmMail);
                await Task.Delay(RandTimeWait());
                webDriver.FindElement(By.XPath("//*[@id=\"view_container\"]/div/div/div[2]/div/div[2]/div/div[1]/div")).Click();

                //wait 500 ms back to youtube
                await Task.Delay(500);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return false;
            }

            return true;
        }


        public void Like()
        {
            //find like button
            var likeBtn = webDriver?.FindElement(By.XPath(ElementIdentify.likeXpath));
            var cssclass = likeBtn.GetAttribute("class");
            if(cssclass.Contains("style-text"))
            {
                likeBtn?.Click();
            }    

        }

        public void Play()
        {
            try
            {
                var playBtn = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath(ElementIdentify.playBtnXpath)));
                var label = playBtn.GetAttribute("aria-label");
                if (label.Contains("Play"))
                {
                    playBtn.Click();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void DisLike()
        {
            //find like button
            var disLikeBtn = webDriver?.FindElement(By.XPath(ElementIdentify.disLikeXpath));
            var cssclass = disLikeBtn.GetAttribute("class");
            if (cssclass.Contains("style-text"))
            {
                disLikeBtn?.Click();
            }
        }

        public void Subcribe()
        {
            try
            {
                //find sub
                var subcribeBtn = webDriver?.FindElement(By.XPath(ElementIdentify.subcribeTextXpath));
                var paperCheckSub = webDriver?.FindElement(By.XPath(ElementIdentify.paperButtonCheckSub));
                var label = paperCheckSub.Text;
                if(label.Contains("Đăng ký") || label.Contains("SUBSCRIBE"))
                {
                    subcribeBtn?.Click();
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

        }

        public async Task CommentAsync(string comment)
        {
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)webDriver;

                js.ExecuteScript("window.scrollBy(0, document.body.scrollHeight || document.documentElement.scrollHeight)", "");
                await Task.Delay(RandTimeWait());

                var placeHolder = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"simplebox-placeholder\"]")));
                placeHolder.Click();
                await Task.Delay(RandTimeWait());

                //wait.Until(SeleniumExtras.)

                var textBox = webDriver.FindElement(By.XPath("//*[@id=\"contenteditable-root\"]"));
                textBox?.SendKeys(comment);
                await Task.Delay(RandTimeWait());

                var submitBtn = webDriverWait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.Id("submit-button")));
                submitBtn.Click();
                await Task.Delay(RandTimeWait());

                js.ExecuteScript("window.scrollBy(0, -document.body.scrollHeight || -document.documentElement.scrollHeight)", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private int RandTimeWait()
        {
            //Waiting in 100 - 500 ms
            Random rand = new Random();
            return rand.Next(100, 500);
        }

        private async void InputText(IWebElement element, string text)
        {
            element.SendKeys(text);
            await Task.Delay(RandTimeWait());
        }

        public void Close()
        {
            webDriver?.Dispose();
        }
    }
}
