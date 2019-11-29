using Microsoft.AspNetCore.Mvc;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;
using System;

namespace NEL_FutureDao_API.Service
{
    public class UserServiceV3
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string userInfoCol { get; set; } = "daouserinfos";
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }
        public string tokenUrl { get; set; } = "";

        //
        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);

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
            if (queryRes.Count == 0)
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
            }
            else
            {
                var item = queryRes[0];
                if (long.Parse(item["lastUpdateTime"].ToString()) + 30/* 30s内同一地址不能重复申请*/ > now)
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
            return getRes(new JObject { { "nonceStr", nonceStr } });
        }
        public JArray validateLoginInfo(Controller controller, string address, string signData)
        {
            //
            var nonceStr = getNonceStr(address);
            if (!verify(address, signData, nonceStr))
            {
                return getErrorRes(DaoReturnCode.C_InvalidUserInfo);
            }
            var userId = getUserId(address);
            if (userId == "")
            {
                return getErrorRes(DaoReturnCode.C_InvalidUserInfo);
            }
            var accessToken = TokenHelper.applyAccessToken(tokenUrl, userId);
            setUserInfo(controller, userId, accessToken);
            //return getRes(new JObject { { "userId", userId }, { "accessToken", accessToken } });
            return getRes();
        }

        private void setUserInfo(Controller controller, string userId, string accessToken)
        {
            controller.Response.Cookies.Delete("userId");
            controller.Response.Cookies.Delete("accessToken");
            controller.Response.Cookies.Append("userId", userId, new Microsoft.AspNetCore.Http.CookieOptions() { Path="/", HttpOnly = true, MaxAge = TimeSpan.MaxValue });
            controller.Response.Cookies.Append("accessToken", accessToken, new Microsoft.AspNetCore.Http.CookieOptions() { Path = "/", HttpOnly = true, MaxAge = TimeSpan.MaxValue });
            controller.Response.Headers["Access-Control-Allow-Origin"] = "http://192.168.1.160:3000";
            //controller.Response.Headers.Remove("Set-Cookie");
            //controller.Response.Headers.Add("Set-Cookie", "userId=" + userId + "; accessToken=" + accessToken + "; Path=/; HttpOnly");
        }
        private bool getUserInfo(Controller controller, out string code, out string userId)
        {
            code = "";
            userId = controller.Request.Cookies["userId"];
            var accessToken = controller.Request.Cookies["accessToken"];
            if(userId == null || userId == "" || accessToken == null || accessToken == "")
            {
                return false;
            }
            return TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out code);
        }

        private string getNonceStr(string address)
        {
            var findStr = new JObject { { "address", address } }.ToString();
            var fieldStr = new JObject { { "nonceStr", 1 }, { "nonceState", 1 } }.ToString();
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
            if (address == "" || signData == "" || nonceStr == "")
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
            if (userId == "")
            {
                userId = DaoInfoHelper.genUserId(address, address, address);
                updateJo.Add("userId", userId);
            }
            var updateStr = new JObject { { "$set", updateJo } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, updateStr, findStr);
            return userId;
        }

        public JArray getUserInfo(Controller controller)
        {
            if (!getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "userId", userId } }.ToString();
            var fieldStr = new JObject { { "username", 1 }, { "address", 1 }, { "headIconUrl", 1 }, { "_id", 0 } }.ToString();
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
