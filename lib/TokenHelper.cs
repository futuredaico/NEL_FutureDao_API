using NEL.NNS.lib;
using Newtonsoft.Json.Linq;
using System.Text;

namespace NEL_FutureDao_API.lib
{
    public class TokenHelper
    {
        // 供临时测试使用
        public static string applyAccessToken(string url, string userId)
        {
            if (url == "") return "123456789012";
            var data = new JObject {
                { "jsonrpc", "2.0"},
                { "method", "applyAccessToken"},
                { "params", new JArray(new string[]{ userId})},
                { "id","1" }
            }.ToString();
            var res = HttpHelper.Post(url, data, Encoding.UTF8, 1);
            var result = ((JArray)JObject.Parse(res)["result"])[0];
            if (result["resultCode"].ToString() == "00000")
            {
                return result["data"]["accessToken"].ToString();
            }
            return "";
        }
        public static bool checkAccessToken(string url, string userId, string accessToken, out string code)
        {
            if (url == "" || url == null)
            {
                code = "";
                return true;
            }
            var data = new JObject {
                { "jsonrpc", "2.0"},
                { "method", "checkAccessToken"},
                { "params", new JArray(new string[]{ userId, accessToken})},
                { "id","1" }
            }.ToString();
            var res = HttpHelper.Post(url, data, Encoding.UTF8, 1);
            var result = ((JArray)JObject.Parse(res)["result"])[0];
            code = result["resultCode"].ToString();
            return code == "00000";
        }

    }
}
