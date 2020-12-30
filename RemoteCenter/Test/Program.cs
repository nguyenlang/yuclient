using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZaloDotNetSDK;
namespace Test
{
    class Program
    {
        //public const long AppId = 981929095118321266;
        //public const string AppAccessKey = "IIHDPX5HJ23GK7Zv04ih";
        //public const string OAcode = "8kkb5_Q14bSziR0knuD1Stk9ko3rsMbODux9AThyAb4vgumbm9qjMZdTpowqxpT-FRhW1lZGK6StdumOm_yTRJxBk1V1ys1bTV3h4iAaF7m4y88s-BX6MJBNepheptyPNQRh28YA5KK6pDrIdiKfHYJ8uqUaXqKqHBBlMAkY21ChckC8eyWgQ1FVod-xzaLI9F2cFj_605mZyDW9tBj7TI-HYnModt0BNwR_9-2cGKHjow9dpwvr8ao3Zc_6rrOVFfx-NDEpTNLchekMdQiR-YUgquEphLZe3SEOh_yUS3rjG9nokTtsmdFOPuwy-SwpNzb0zBEQzEeRsp6xdusKtHkpGydsy_RN7ATGyoOvndGIwvvFM0";public const long AppId = 981929095118321266;

        //public const long AppId = 3867748108188614513;
        //public const string AppAccessKey = "66I9a5WoGfnfY1wWQFMn";
        ////3 month, Hiep account 28 Dec
        //public const string OAcode = "cIbls2Hw75swIaNd3Iu_UOW2HCXO0JDPnWinoWnITIs6F1UPA0XC8fqQCfOX3amY_WW-icy91GkUK7dj4pyRMBeEG-KEQm9xZ4vBl7vrFodvSJgU8XL1OBzT4bC4J6UHxcHzGmyW6_778drx8dWbbx0MI0PZ96dlaKKZKIGiSF3aG1bF8Y4QpSbdHJX77LcZgtea50bFTgcqCpKd2orSr_9fCrKQAdMWbIXT4cCNUhlD8ISQUqLqZBqeQKONDW-8rnTIT1q5UiVBF01X1bmOdUfX1GnJP2M0zZuHJJ9e2Epl4tKISkeqfCVURXfYmNB6XeXUOJFRVgBeWOXo1uwapjNubiZUzDDsQPUG-D66u7uN3AVikX-LDLXAqCNY7mKga4LFenKNhGxzj-I96uM3UlBXbgOyvluYt-cByMwboJ72f-lF6A7dRAoszP1P9g5MXmvtTo88";

        public const long AppId = 2360084446121911173;
        public const string AppAccessKey = "u9Yd2MQII6cBAbuI676x";
        public const string RedirectLink = "https://laptrinhvb.net";

        public string AllowAccess = $@"https://oauth.zaloapp.com/v3/auth?app_id=2360084446121911173&redirect_uri=https://laptrinhvb.net";
        public const string OAcode = "eJZBjE4SysBVS-g9sKlVV9KPaQZj4gX7sHxZeVv7-cw9Nj2AXpV2PeEvvBJoqA9YZzoSkAZwwsMPdyAixOxgGE3izA_pxSXwzwA-h8YnWaYXlCYIYCd6Qhh8ogBC_PeLav_7x-BgY2k2WEgIuTFc3SR8ejMtsPGmjwEU_fkRbX3_kD_0dgMLP-drYzoBmj1DlypzgxEuv4EWdfFUiDgXTjJ7niYudfrZsgZczBEflKdwdFhovghn2PBVkj_YqhWctTRZcRlbaXEIPB_6g1xW6QiK_CYlHuCantsBSqUehxz6_R-mePcCPPh3ffhuZSri-VBWhUMHibotXu2O__l0CV-D-w7WXVGhNbacSFzmRQ9csqm";

        class User
        {
            public string name;
            public long id;
        }
        class Error
        {
            public string error;
            public string message;
        }
        static void Main(string[] args)
        {
            //ZaloOaInfo zaloOaInfo = new ZaloOaInfo();
            ZaloAppInfo zaloAppInfo = new ZaloAppInfo(AppId, AppAccessKey, "https://laptrinhvb.net");
            ZaloAppClient zaloAppClient = new ZaloAppClient(zaloAppInfo);

            //string loginUrl = zaloAppClient.getLoginUrl();
            //Process.Start(loginUrl);

            //var token = zaloAppClient.getAccessToken(OAcode);
            var access_token = "xxx";

            var profile = zaloAppClient.getProfile(access_token, "id, name, birthday");
            
            var friends = zaloAppClient.getFriends(access_token, 0, 100, "id, name");
            var vovin = friends.ToString().ToLower();
            
            JArray array = (JArray)friends["data"];
            //var friendList = array.ToObject<List<User>>();


            //JsonConvert.DeserializeObject(friends);
            //id a Minh: 7751366822681432042, id mình: 5303635736398383040
            var sendMessage = zaloAppClient.sendMessage(access_token, 5303635736398383040, "API Testing", "");


            //textBox1.Clear();
            //textBox1.Focus();


        }
    }
}
