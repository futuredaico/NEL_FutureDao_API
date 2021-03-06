﻿using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
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
        public string userInfoCol { get; set; } = "daouserinfos";
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }
        //
        public static int usernameLenMin { get; set; } = 2;
        public static int usernameLenMax { get; set; } = 24;
        public static int passwordLenMin { get; set; } = 8;
        public string prefixPassword { get; set; }
        public string tokenUrl { get; set; } = "";

        //
        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        private bool checkResCode(JArray res) => RespHelper.checkResCode(res);
        //
        public JArray checkUsername(string username)
        {
            if (!checkUsernameLen(username))
            {
                return getErrorRes(DaoReturnCode.invalidUsername);
            }

            string findStr = new JObject { { "username", username } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.usernameHasRegisted);
            }
            return getRes();
        }
        public JArray checkEmail(string email)
        {
            if (!EmailHelper.checkEmail(email))
            {
                return getErrorRes(DaoReturnCode.invalidEmail);
            }
            string findStr = new JObject { { "email", email } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.emailHasRegisted);
            }
            return getRes();
        }

        //a.
        //b.send.register
        //c.
        public JArray register(string username, string email, string password)
        {
            var checkRes = checkUsername(username);
            if (!checkResCode(checkRes))
            {
                return checkRes;
            }
            checkRes = checkEmail(email);
            if (!checkResCode(checkRes))
            {
                return checkRes;
            }

            if (!checkPasswordLen(password))
            {
                return getErrorRes(DaoReturnCode.invalidPasswordLen);
            }
            //
            var pswd = toPasswordHash(password);
            var time = TimeHelper.GetTimeStamp();
            var userId = DaoInfoHelper.genUserId(username, email, pswd);
            var newdata = new JObject {
                {"userId",  userId},
                {"username", username },
                {"password", pswd },
                {"email", email },
                {"emailVerifyCode", ""},
                {"emailVerifyState", EmailState.sendBeforeState},
                {"headIconUrl", "" },
                {"brief", ""},
                {"time",  time},
                {"lastUpdateTime", time },
                {"ethAddress", "" },
                {"neoAddress", "" },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, newdata);

            return getRes();
        }
        public JArray verifyRegister(string username, string email, string verifyCode)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "emailVerifyCode", 1 }, { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["emailVerifyCode"].ToString() != verifyCode)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            if (queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerify)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", EmailState.hasVerify },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp()}
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }

        public JArray login(string email, string password)
        {
            string findStr = new JObject { { "email", email }, { "password", toPasswordHash(password) } }.ToString();
            string fieldStr = new JObject { { "userId", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.invalidLoginInfo);

            var userId = queryRes[0]["userId"].ToString();
            var accessToken = TokenHelper.applyAccessToken(tokenUrl, userId);
            return getRes(new JObject { { "userId", userId }, { "accessToken", accessToken } });
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
        public JArray resetPassword(string email)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.notFindUserInfo);
            }

            if (queryRes[0]["emailVerifyState"].ToString() != EmailState.sendBeforeStateAtResetPassword)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", EmailState.sendBeforeStateAtResetPassword },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray verifyReset(string username, string email, string password, string emailVerifyCode)
        {
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", username }, { "password", 1 }, { "emailVerifyCode", 1 }, { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username
                || queryRes[0]["emailVerifyCode"].ToString() != emailVerifyCode)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            if (!checkPasswordLen(password))
            {
                return getErrorRes(DaoReturnCode.invalidPasswordLen);
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

            return getRes();
        }

        public JArray getUserInfo(string userId, string accessToken)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "email", 1 }, { "emailVerifyState", 1 }, { "headIconUrl", 1 }, { "brief", 1 }, { "neoAddress", 1 }, { "ethAddress", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            item["emailVerifyState"] = DaoInfoHelper.toEmailState(item["emailVerifyState"].ToString());
            return getRes(item);
        }
        public JArray modifyUserIcon(string userId, string accessToken, string headIconUrl)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "password", 1 }, { "headIconUrl", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.notFindUserInfo);
            }

            //
            string oldHeadIconUrl = queryRes[0]["headIconUrl"].ToString();
            if (!DaoInfoHelper.StoreFile(oss, bucketName, oldHeadIconUrl, headIconUrl, out string newHeadIconUrl))
            {
                return getErrorRes(DaoReturnCode.headIconNotUpload);
            }
            //
            if (oldHeadIconUrl != newHeadIconUrl)
            {
                var updateStr = new JObject { {"$set", new JObject{
                    { "headIconUrl", newHeadIconUrl},
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } }}.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }

        public JArray modifyUserBrief(string userId, string accessToken, string brief)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "brief", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.notFindUserInfo);
            }

            if (queryRes[0]["brief"].ToString() != brief)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "brief", brief },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray modifyPassword(string userId, string accessToken, string password, string newpassword)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!checkPasswordLen(newpassword))
            {
                return getErrorRes(DaoReturnCode.invalidPasswordLen);
            }
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "password", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.notFindUserInfo);
            }

            var pswd = toPasswordHash(password);
            if (queryRes[0]["password"].ToString() != pswd)
            {
                return getErrorRes(DaoReturnCode.passwordError);
            }
            if (password != newpassword)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "password", toPasswordHash(newpassword) },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }

        // a.
        // b.send.modifyEmail
        // c.
        public JArray modifyEmail(string userId, string accessToken, string newemail, string password)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "email", 1 }, { "password", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.notFindUserInfo);
            }
            if (queryRes[0]["password"].ToString() != toPasswordHash(password))
            {
                return getErrorRes(DaoReturnCode.passwordError);
            }
            var email = queryRes[0]["email"].ToString();
            if (email == newemail) return getRes();

            //
            var checkRes = checkEmail(newemail);
            if (!checkResCode(checkRes))
            {
                return checkRes;
            }
            if (email != newemail)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "email", newemail },
                    { "emailVerifyState", EmailState.sendBeforeStateAtChangeEmail },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray verifyEmail(string username, string email, string verifyCode)
        {
            if (verifyCode.Trim().Length == 0)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "emailVerifyCode", 1 }, { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username)
            {
                return getErrorRes(DaoReturnCode.notFindUserInfo);
            }
            if (queryRes[0]["emailVerifyCode"].ToString() != verifyCode)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            if (queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtChangeEmail)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", EmailState.hasVerifyAtChangeEmail },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes();
        }


        public JArray reSendVerify(string userId, string accessToken)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 }, { "lastUpdateTime", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.T_InvalidTargetUserId);
            }
            string state = queryRes[0]["emailVerifyState"].ToString();
            string newState = "";
            if (state == EmailState.sendAfterState)
            {
                newState = EmailState.sendBeforeState;
            }
            if (state == EmailState.sendAfterStateAtChangeEmail)
            {
                newState = EmailState.sendBeforeStateAtChangeEmail;
            }

            if (newState != "")
            {
                long now = TimeHelper.GetTimeStamp();
                long lastUpdateTime = long.Parse(queryRes[0]["lastUpdateTime"].ToString());
                if ((now > lastUpdateTime + 60))
                {
                    // 60s 内不能重复操作
                    var updateStr = new JObject { { "$set", new JObject { { "emailVerifyState", newState }, { "lastUpdateTime", now } } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
                }
            }
            return getRes();
        }

        public JArray bindAddress(string userId, string accessToken, string type, string address)
        {
            address = address.ToLower();
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            type = type.ToLower();
            if (type != "neo" && type != "eth")
            {
                return getErrorRes("");
            }
            var findStr = new JObject { { "userId", userId } }.ToString();
            var fieldStr = new JObject { { "neoAddress", 1 }, { "ethAddress", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || (queryRes[0]["neoAddress"].ToString() == address
                    || queryRes[0]["ethAddress"].ToString() == address))
            {
                return getRes();
            }

            var updateJo = new JObject();
            if (type == "neo") updateJo.Add("neoAddress", address);
            if (type == "eth") updateJo.Add("ethAddress", address);
            var updateStr = new JObject { { "$set", updateJo } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            return getRes();
        }


        ///
        /// v3
        /// 
        public JArray getLoginNonce(string address)
        {
            if (address == "") return getErrorRes(DaoReturnCode.C_InvalidUserInfo);
            var nonceStr = "";
            var now = TimeHelper.GetTimeStamp();
            var findStr = new JObject { { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr);
            if(queryRes.Count == 0)
            {
                nonceStr = DaoInfoHelper.genUserId(address, address, address);
                var newdata = new JObject {
                    { "userId", ""},
                    { "username", "" },
                    { "address", address},
                    { "headIconUrl", ""},
                    { "nonceStr", nonceStr},
                    { "nonceState", StateValidityOp.Yes},
                    { "time", now},
                    { "lastUpdateTime", now},
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, newdata);
            } else
            {
                var item = queryRes[0];
                if(long.Parse(item["lastUpdateTime"].ToString())  + 10/* 10s内同一地址不能重复申请*/ > now)
                {
                    return getErrorRes(DaoReturnCode.RepeatOperate);
                }
                nonceStr = DaoInfoHelper.genUserId(address, address, address);
                var updateStr = new JObject {{"$set", new JObject{
                    { "nonceStr", nonceStr},
                    { "nonceState", StateValidityOp.Yes},
                    { "lastUpdateTime", now},
                } }}.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            }
            return getRes(new JObject { { "nonceStr", nonceStr} });
        }
        public JArray validateLoginInfo(string address, string signData)
        {
            //
            var nonceStr = getNonceStr(address);
            if (!verify(address, signData, nonceStr))
            {
                return getErrorRes(DaoReturnCode.C_InvalidUserInfo);
            }
            var userId = getUserId(address);
            if(userId == "")
            {
                return getErrorRes(DaoReturnCode.C_InvalidUserInfo);
            }
            var accessToken = TokenHelper.applyAccessToken(tokenUrl, userId);
            return getRes(new JObject { { "userId", userId },{ "accessToken", accessToken } });
        }
        private string getNonceStr(string address)
        {
            var findStr = new JObject { { "address", address } }.ToString();
            var fieldStr = new JObject { { "nonceStr", 1 },{ "nonceState",1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return "";
            //
            var item = queryRes[0];
            var nonceState = int.Parse(item["nonceState"].ToString());
            if (nonceState == StateValidityOp.Not) return "";

            return item["nonceStr"].ToString();
        }
        private bool verify(string address, string signData, string nonceStr)
        {
            if(address == "" || signData == "" || nonceStr == "")
            {
                return false;
            }
            return true;
        }
        private string getUserId(string address)
        {
            var findStr = new JObject { { "address", address } }.ToString();
            var fieldStr = new JObject { { "userId", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return "";
            //
            var item = queryRes[0];
            var updateJo = new JObject {
                { "nonceState", StateValidityOp.Not},
                { "lastUpdateTime", TimeHelper.GetTimeStamp()}
            };
            var userId = item["userId"].ToString();
            if(userId == "")
            {
                userId = DaoInfoHelper.genUserId(address, address, address);
                updateJo.Add("userId", userId);
            }
            var updateStr = new JObject { { "$set", updateJo} }.ToString();
            mh.UpdateData(dao_mongodbConnStr,dao_mongodbDatabase,userInfoCol, updateStr, findStr);
            return userId;
        }

        public JArray getUserInfo(string userId, string accessToken, string v3Flag="0")
        {
            if (v3Flag == "1") return getUserInfoV3(userId, accessToken);
            return getUserInfo(userId, accessToken);
        }
        public JArray getUserInfoV3(string userId, string accessToken)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "userId", userId } }.ToString();
            var fieldStr = new JObject { {"username",1},{"address",1},{"headIconUrl",1},{ "_id",0} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();
            return getRes(queryRes[0]);
        }



        public class StateValidityOp
        {
            public const int Yes = 1;
            public const int Not = 0;
        }

    }
}
