using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.State;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NEL_FutureDao_API.Service.Help
{
    public class DaoInfoHelper
    {
        public static string toEmailState(string emailVerifyState)
        {
            if(emailVerifyState == EmailState.sendBeforeState
                || emailVerifyState == EmailState.sendBeforeStateAtResetPassword
                || emailVerifyState == EmailState.sendBeforeStateAtChangeEmail)
            {
                return DaoReturnCode.EmailNotVerify;
            }
            if (emailVerifyState == EmailState.sendAfterState
                || emailVerifyState == EmailState.sendAfterStateAtResetPassword
                || emailVerifyState == EmailState.sendAfterStateAtChangeEmail)
            {
                return DaoReturnCode.EmailVerifying;
            }
            if (emailVerifyState == EmailState.hasVerify
                || emailVerifyState == EmailState.hasVerifyAtResetPassword
                || emailVerifyState == EmailState.hasVerifyAtChangeEmail)
            {
                return DaoReturnCode.EmailVerifySucc;
            }
            return DaoReturnCode.EmailVerifyFail;
        }
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
        public static string genProjDiscussId(string projId, string preId, string content, string userId)
        {
            string data = string.Format("{0}.{1}.{2}.{3}.{4}", now, projId, preId, content, userId);
            return hash(data);
        }
        public static string genProjUpdateDiscussId(string updateId, string preId, string content, string userId)
        {
            string data = string.Format("{0}.{1}.{2}.{3}.{4}", now, updateId, preId, content, userId);
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

        public static bool StoreFile(OssHelper oss, string bucketName, string oldFileUrl, string fileUrl)
        {
            // tmp -> origin
            string fileName = fileUrl.toFileName();
            if (oss.ExistKey(bucketName, fileName))
            {
                return true;
            }
            if (!oss.ExistKey(bucketName, fileName.toTemp()))
            {
                return false;
            }
            try
            {
                oss.CopyObject(bucketName, fileName.toTemp(), fileName);
            }
            catch
            {
                return false;
            }
            // delete tmp
            fileName = oldFileUrl.toFileName();
            if (fileName != "")
            {
                try
                {
                    oss.DeleteObject(bucketName, fileName);
                }
                catch { }
            }
            return true;
        }
    }
}
