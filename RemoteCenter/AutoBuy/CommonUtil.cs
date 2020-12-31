namespace AutoBuy
{
    class CommonUtil
    {
        public const string UserData = @"user-data-dir=";
    }
    class Amazon
    {
        public const string HomeUrl = "https://www.amazon.com/";
        //use for checking:
        public const string UserCheckDir = @"\AppData\Local\Google\Chrome\User Data\amazonCheck";
        public const string UserBuyDir = @"\AppData\Local\Google\Chrome\User Data\Amazon";
        public const string AmazonListItemFile = "amazonList.json";
        public const string ScreenShortDir = "AmazonScreenShort";

    }

    class NewEgg
    {
        public const string HomrUrl = "https://www.newegg.com/"; //https://www.newegg.com/p/N82E16819113497
        public const string UserDir = @"\AppData\Local\Google\Chrome\User Data\NewEgg";
        public const string AvaiableSignalPath = "//*[@id=\"ProductBuy\"]/div/div[2]/button";
        public const string PriceSignalCSS = ".product-price .price-current";
        public const string NewEggListItemFile = "newEggList.json";
        public const string ScreenShortDir = "NeweggScreenShort";
    }

    class ZaloValue
    {
        //App: Alarm Item avaiable
        public const long AppId = 2360084446121911173;
        public const string AppAccessKey = "u9Yd2MQII6cBAbuI676x";
        public const string RedirectLink = "https://laptrinhvb.net"; //fake redirect
        public string AllowAccess = $@"https://oauth.zaloapp.com/v3/auth?app_id=2360084446121911173&redirect_uri=https://laptrinhvb.net"; //to get OAuth, need active app to get it
        //3 Month OAuth from 28 Dec
        public const string OAcode = "eJZBjE4SysBVS-g9sKlVV9KPaQZj4gX7sHxZeVv7-cw9Nj2AXpV2PeEvvBJoqA9YZzoSkAZwwsMPdyAixOxgGE3izA_pxSXwzwA-h8YnWaYXlCYIYCd6Qhh8ogBC_PeLav_7x-BgY2k2WEgIuTFc3SR8ejMtsPGmjwEU_fkRbX3_kD_0dgMLP-drYzoBmj1DlypzgxEuv4EWdfFUiDgXTjJ7niYudfrZsgZczBEflKdwdFhovghn2PBVkj_YqhWctTRZcRlbaXEIPB_6g1xW6QiK_CYlHuCantsBSqUehxz6_R-mePcCPPh3ffhuZSri-VBWhUMHibotXu2O__l0CV-D-w7WXVGhNbacSFzmRQ9csqm";
    }

    class ZaloUser
    {
        public string name;
        public string userId;
    }

    class ZaloError
    {
        public string error;
        public string message;
    }
}
