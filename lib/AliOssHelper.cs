using NEL.NNS.lib;
using Newtonsoft.Json.Linq;
using System.Text;

namespace NEL_FutureDao_API.lib
{
    public class AliOssHelper
    {
        public static bool Storage(string url, string bucketName, string oldFileUrl, string fileUrl, out string newFileUrl)
        {
            var data = new JObject {
                { "bucketName", bucketName},
                { "oldFileUrl", oldFileUrl},
                { "fileUrl", fileUrl}
            }.ToString();
            var res = HttpHelper.Post(url, data, Encoding.UTF8, 1);
            var result = JObject.Parse(res);
            if (result["code"].ToString() == "00000")
            {
                newFileUrl = result["result"].ToString();
                return true;
            }
            newFileUrl = "";
            return false;
        }
    }
}
