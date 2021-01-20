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
