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
        public static string genProjRewardId(string projId, string rewardName)
        {
            string data = string.Format("{0}.{1}.{2}", now, projId, rewardName);
            return hash(data);
        }
        public static string genProjRewardOrderId(string projId, string rewardId, string userId)
        {
            string data = string.Format("{0}.{1}.{2}.{3}", now, projId, rewardId, userId);
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

        public static bool StoreFile(OssHelper oss, string bucketName, string oldFileUrl, string fileUrl, out string newFileUrl)
        {
            newFileUrl = "";
            // tmp -> origin
            string fileName = fileUrl.toFileName();
            if(fileName.Trim().Length != 0)
            {
                string fileNameTemp = fileName.toTemp();
                string fileNameNormal = fileName.toNormal();
                newFileUrl = fileUrl.Replace(fileName, fileNameNormal);
                if (oss.ExistKey(bucketName, fileNameNormal))
                {
                    return true;
                }
                if (!oss.ExistKey(bucketName, fileNameTemp))
                {
                    return false;
                }
                try
                {
                    oss.CopyObject(bucketName, fileNameTemp, fileNameNormal);
                }
                catch
                {
                    return false;
                }
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

        public static string StoreFile(OssHelper oss, string bucketName, string filename, Stream stream)
        {
            return oss.PutObject(bucketName, filename, stream);
        }
    }
    public static class DaoHelper
    {
        public static string toNormalId(this string projId)
        {
            if (projId.StartsWith("temp_")) return projId.Substring(5);
            return projId;
        }
    }
}
