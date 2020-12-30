namespace AutoBuy
{
    class CommonUtil
    {
        public const string UserData = @"user-data-dir=";
    }
    class Amazon
    {
        public const string HomeUrl = "https://www.amazon.com/";

        public const string PriceBuyBoxId = "price_inside_buybox"; //only for buybox
        public const string PriceNewBuyBoxId = "newBuyBoxPrice";
        public const string BuyNowBtnId = "buy-now-button";

        //Buy frame
        public const string BuyIframeID = "turbo-checkout-iframe";
        public const string OneClickPaidId = "turbo-checkout-place-order-button";

        //Buy page
        public const string BuyBtnInPageId = "submitOrderButtonId";

        //Add to cart popup display
        public const string NothankId = "siNoCoverage"; //dont understand why can not wait
        public const string PopupId = "a-popover";
        public const string PopUpTitleClass = "a-popover-header-content";
        public const string ClosePopupXpath = "//*[@id=\"a-popover-1\"]/div/header/button";

        //use for checking:
        public const string UserCheckDir = @"\AppData\Local\Google\Chrome\User Data\amazonCheck";
        public const string UserBuyDir = @"\AppData\Local\Google\Chrome\User Data\Amazon";

        public const string AmazonListItemFile = "amazonList.json";
        public const string ScreenShortDir = "ScreenShortDebug";

        //Buy confirmation
        public const string BuyConfirmStatusPath = "//*[@id=\"widget-purchaseConfirmationStatus\"]/div/h4";
    }

    class NewEgg
    {
        public const string HomrUrl = "https://www.newegg.com/"; //https://www.newegg.com/p/N82E16819113497
        public const string UserDir = @"\AppData\Local\Google\Chrome\User Data\NewEgg";
        public const string AvaiableSignalPath = "//*[@id=\"ProductBuy\"]/div/div[2]/button";
        public const string PriceSignalCSS = ".product-price .price-current";
        public const string NewEggListItemFile = "newEggList.json";
    }

    class BestBuy
    {
        public const string HomrUrl = "https://www.bestbuy.com/"; //https://www.newegg.com/p/N82E16819113497
        public const string AvaiableSignalPath = "//*[@id=\"fulfillment-add-to-cart-button-f9287a7a-994d-43a4-b5cb-082d55cd22d3\"]";
        public const string UserDir = @"\AppData\Local\Google\Chrome\User Data\BestBuy";
        ////*[@id="fulfillment-add-to-cart-button-c570b152-e70f-409e-861b-9a620c146eb5"]
        /////*[@id="fulfillment-add-to-cart-button-e2a2a3f8-b1e3-4d36-b740-b583610521d9"]
        /////*[@id="fulfillment-add-to-cart-button-85079c75-0c56-4c5b-a672-f1d637c1804b"]
        /////*[@id="fulfillment-add-to-cart-button-e2a2a3f8-b1e3-4d36-b740-b583610521d9"]
        /////*[@id="fulfillment-add-to-cart-button-e2a2a3f8-b1e3-4d36-b740-b583610521d9"]/div/div/div/button
        /////*[@id="fulfillment-add-to-cart-button-250bb3b5-39c0-42ed-9c3f-ff3b40d8ee93"]/div/div/div/button  //lay text cua button ok
        /////*[@id="fulfillment-add-to-cart-button-e2a2a3f8-b1e3-4d36-b740-b583610521d9"]/div/div/div/button
        public string PriceSignalXPath = "//*[@id=\"pricing-price-60522757\"]/div/div/div/div/div[2]/div[1]/div[1]/div/span[2]";
        
        public const string BestBuyListItemFile = "bestBuyList.json";
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
