using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NEL_FutureDao_API.Service
{
    public class UserService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string userInfoCol { get; set; } = "daoUserInfo";
        public OssHelper oss { get; set;}
        public string bucketName { get; set; }
        //
        public static int usernameLenMin { get; set; } = 2;
        public static int usernameLenMax { get; set; } = 24;
        public static int passwordLenMin { get; set; } = 8;
        public string defaultHeadIconUrl { get; set; }
        public string prefixPassword { get; set; }

        //
        public JArray checkUsername(string username)
        {
            if(!checkUsernameLen(username))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidUsername } } };
            }
            
            string findStr = new JObject { { "username", username} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr) > 0)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.usernameHasRegisted } } };
            }
            return new JArray { new JObject { { "res", true},{ "code", ""} } };
        }
        public JArray checkEmail(string email)
        {
            if(!EmailHelper.checkEmail(email))
            {
                return new JArray { new JObject { { "res", false},{ "code", UserReturnCode.invalidEmail} } };
            }
            string findStr = new JObject { { "email", email } }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr) > 0)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.emailHasRegisted } } };
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }
        
        //a.
        //b.send.register
        //c.
        public JArray register(string username, string email, string password)
        {
            var checkRes = checkUsername(username);
            if (!(bool)checkRes[0]["res"])
            {
                return checkRes;
            }
            checkRes = checkEmail(email);
            if (!(bool)checkRes[0]["res"])
            {
                return checkRes;
            }

            if(!checkPasswordLen(password))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidPasswordLen } } };
            }
            //
            var time = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                {"userId", "" },
                {"username", username },
                {"password", toPasswordHash(password) },
                {"email", email },
                {"emailVerifyCode", ""},
                {"emailVerifyState", EmailState.sendBeforeState},
                {"headIconUrl", defaultHeadIconUrl },
                {"brief", ""},
                {"time",  time},
                {"lastUpdateTime", time },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, newdata);

            return new JArray { new JObject { { "res", true},{ "code",""} } };
        }
        public JArray verifyRegister(string username, string email, string verifyCode)
        {
            string findStr = new JObject { { email, "email" } }.ToString();
            string fieldStr = new JObject { { "username",1},{ "emailVerifyCode", 1},{ "emailVerifyState",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["emailVerifyCode"].ToString() != verifyCode)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidVerifyCode} } };
            }
            if(queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerify)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", EmailState.hasVerify },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp()}
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true},{ "code", ""} } };
        }

        public JArray login(string username, string email, string password)
        {
            string findStr = new JObject { { "email", email },{ "username", username },{ "password", toPasswordHash(password)} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr) == 0)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidLoginInfo } } };
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }

        private bool checkUsernameLen(string username)
        {
            return username.Length > usernameLenMin && username.Length <= usernameLenMax;
        }
        private bool checkPasswordLen(string password)
        {
            return password.Length >= passwordLenMin;
        }
        private string toPasswordHash(string password)
        {
            byte[] binaryData = Encoding.UTF8.GetBytes(prefixPassword + password);
            var stream = new MemoryStream(binaryData);

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(stream);
            return toStr(retVal);
        }
        private string toStr(byte[] retVal)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        //a.
        //b.send.reset
        //c.
        public JArray resetPassword(string username, string email)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1},{ "emailVerifyState",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0 || queryRes[0]["username"].ToString() != username)
            {
                return new JArray { new JObject { { "res", false},{ "code", UserReturnCode.notFindUserInfo } } };
            }

            if(queryRes[0]["emailVerifyState"].ToString() != EmailState.sendBeforeStateAtResetPassword)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", EmailState.sendBeforeStateAtResetPassword },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }
        public JArray verifyReset(string username, string email, string password, string emailVerifyCode)
        {
            string findStr = new JObject { { "email", email} }.ToString();
            string fieldStr = new JObject { { "username", username }, { "password", 1},{ "emailVerifyCode", 1 },{ "emailVerifyState",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["emailVerifyCode"].ToString() != emailVerifyCode)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidVerifyCode } } };
            }
            if(!checkPasswordLen(password))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidPasswordLen } } };
            }
            var pswd = toPasswordHash(password);
            if (queryRes[0]["password"].ToString() != pswd
                || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtResetPassword)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "password", pswd },
                    { "emailVerifyState", EmailState.hasVerifyAtResetPassword},
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }

            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }

        public JArray getUserInfo(string username, string email)
        {
            string findStr = new JObject { { "email", email} }.ToString();
            string fieldStr = new JObject { { "username",1},{ "email",1},{ "headIconUrl",1},{ "brief",1},{ "_id",0} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return queryRes;

            if (queryRes[0]["username"].ToString() != username) return new JArray { };
            return queryRes;
        }
        public JArray modifyUserIcon(string username, string email, string password, string headIconUrl)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "password", 1 }, { "headIconUrl", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["password"].ToString() != toPasswordHash(password))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.notFindUserInfo } } };
            }

            //
            var fileName = queryRes[0]["headIconUrl"].ToString().toFileName();
            if(!defaultHeadIconUrl.EndsWith(fileName))
            {
                try
                {
                    oss.CopyObject(bucketName, fileName, fileName.toBak());
                }
                catch { }
            }
            fileName = headIconUrl.toFileName();
            oss.CopyObject(bucketName, fileName.toTemp(), fileName);
            //
            if (queryRes[0]["headIconUrl"].ToString() != headIconUrl)
            {
                var updateStr = new JObject { "$set", new JObject{
                    { "headIconUrl", headIconUrl},
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                }}.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }
        public JArray modifyUserBrief(string username, string email, string password, string brief)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "brief", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["password"].ToString() != toPasswordHash(password))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.notFindUserInfo } } };
            }

            if(queryRes[0]["brief"].ToString() != brief)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "brief", brief },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }
        public JArray modifyPassword(string username, string email, string password, string newpassword)
        {
            if(!checkPasswordLen(newpassword))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidPasswordLen } } };
            }
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "password", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.notFindUserInfo } } };
            }

            var pswd = toPasswordHash(password);
            if (queryRes[0]["password"].ToString() != pswd)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.passwordError } } };
            }
            if (password != newpassword)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "password", pswd },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }

        // a.
        // b.send.modifyEmail
        // c.
        public JArray modifyEmail(string username, string email, string password, string newemail)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0 
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["password"].ToString() != toPasswordHash(password))
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.notFindUserInfo } } };
            }
            string subfindStr = new JObject { { "email", newemail} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, subfindStr) > 0)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.emailHasRegisted } } };
            }
            if(email != newemail)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "email", newemail },
                    { "emailVerifyState", EmailState.sendBeforeStateAtChangeEmail },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }
        public JArray verifyEmail(string username, string email, string verifyCode)
        {
            if(verifyCode.Trim().Length == 0)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidVerifyCode } } };
            }
            string findStr = new JObject { {"email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "emailVerifyCode", 1 }, { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.notFindUserInfo } } };
            }
            if(queryRes[0]["emailVerifyCode"].ToString() != verifyCode)
            {
                return new JArray { new JObject { { "res", false }, { "code", UserReturnCode.invalidVerifyCode } } };
            }
            if(queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtChangeEmail)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", EmailState.hasVerifyAtChangeEmail },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return new JArray { new JObject { { "res", true }, { "code", "" } } };
        }
    }
    class EmailState
    {
        public const string sendBeforeState = "10100";
        public const string sendAfterState = "10101";
        public const string hasVerify = "10102";
        public const string sendBeforeStateAtResetPassword = "10103";
        public const string sendAfterStateAtResetPassword = "10104";
        public const string hasVerifyAtResetPassword = "10105";
        public const string sendBeforeStateAtChangeEmail = "10106";
        public const string sendAfterStateAtChangeEmail = "10107";
        public const string hasVerifyAtChangeEmail = "10108";
    }
    class UserReturnCode
    {
        public const string invalidUsername = "10200";      // 不合法用户名
        public const string usernameHasRegisted = "10201";  // 用户名已注册
        public const string invalidEmail = "10202";         // 不合法的邮箱
        public const string emailHasRegisted = "10203";     // 邮箱已注册
        public const string invalidPasswordLen = "10204";   // 不合法的密码
        public const string passwordError = "10205";        // 密码错误
        public const string invalidVerifyCode = "10206";    // 不合法的验证码
        public const string invalidLoginInfo = "10207";     // 无效的登录信息(即用户名/邮箱/密码错误)
        public const string notFindUserInfo = "10208";      // 没有找到用户信息
    }
}
