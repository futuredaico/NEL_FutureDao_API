using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NEL_FutureDao_API.Service.Help
{
    public class DaoInfoHelper
    {
        public static string now => DateTime.Now.ToString("u");
        public static string genUserId(string username, string email, string pswd)
        {
            string data = string.Format("{0}.{1}.{2},{3}", now, username, email, pswd);
            return hash(data);
        }
        public static string genProjId(string name, string title)
        {
            string data = string.Format("{0}.{1}.{2}", now, name, title);
            return hash(data);
        }
        public static string genProjUpdateId(string projId, string updateTile)
        {
            string data = string.Format("{0}.{1}.{2}", now, projId, updateTile);
            return hash(data);
        }
        private static string hash(string data)
        {
            byte[] binaryData = Encoding.UTF8.GetBytes(data);
            var stream = new MemoryStream(binaryData);

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(stream);
            return toStr(retVal);
        }
        private static string toStr(byte[] retVal)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
