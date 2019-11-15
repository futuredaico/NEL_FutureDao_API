using System.IO;
using System.Linq;
using System.Text;
using NEL_FutureDao_API.Service.Help;
using Newtonsoft.Json.Linq;

namespace NEL_FutureDao_API.lib
{
    public class FileHelper
    {
        private const string headInfo = "订单编号,项目名称,回报名称,数量,收件人,手机,收货地址,留言";
        public static string Store(string filename, JArray queryRes)
        {
            var dataList = 
            queryRes.Select(p =>
            {
                var sb = new StringBuilder();
                sb.Append(p["orderId"].ToString());
                sb.Append(",").Append(p["projName"].ToString());
                sb.Append(",").Append(p["rewardName"].ToString());
                sb.Append(",").Append(p["amount"].ToString());
                sb.Append(",").Append(p["connectorName"].ToString());
                sb.Append(",").Append(p["connectorTel"].ToString());
                sb.Append(",").Append(p["connectorAddress"].ToString());
                sb.Append(",").Append(p["connectorMessage"].ToString());
                return sb.ToString();
            }).ToArray();

            StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
            //
            var bt = new byte[] { (byte)0xEF, (byte)0xBB, (byte)0xBF };
            var hd = Encoding.UTF8.GetString(bt);
            sw.WriteLine(hd);
            sw.WriteLine(headInfo);
            foreach (var data in dataList)
            {
                sw.WriteLine(data);
            }
            sw.Dispose();
            return filename;
        }
    }
}
