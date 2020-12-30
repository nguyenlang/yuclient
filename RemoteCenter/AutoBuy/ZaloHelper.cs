using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZaloDotNetSDK;

namespace AutoBuy
{
    class ZaloHelper
    {
        private static readonly log4net.ILog logs =
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void SendZaloMessage(string message)
        {
            try
            {
                ZaloAppInfo zaloAppInfo = new ZaloAppInfo(ZaloValue.AppId, ZaloValue.AppAccessKey, ZaloValue.RedirectLink);
                ZaloAppClient zaloAppClient = new ZaloAppClient(zaloAppInfo);

                var token = zaloAppClient.getAccessToken(ZaloValue.OAcode);
                var access_token = token["access_token"].ToString();

                var friends = zaloAppClient.getFriends(access_token, 0, 10, "id, name");
                if (friends.ToString().ToLower().Contains("error"))
                {
                    var prob = friends.ToObject<ZaloError>();
                    logs.Error($"{prob.error} {prob.message}");
                }
                JArray array = (JArray)friends["data"];
                var friendList = array.ToObject<List<ZaloUser>>();

                foreach (var user in friendList)
                {
                    //id a Minh: 7751366822681432042, id Lang: 5303635736398383040
                    var sendMessage = zaloAppClient.sendMessage(access_token, 7751366822681432042, message, "");
                    if (sendMessage.ToString().ToLower().Contains("error"))
                    {
                        var prob = sendMessage.ToObject<ZaloError>();
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
