using NEL.API.RPC;
using NEL.NNS.lib;
using NEL_FutureDao_API.Service;
using Newtonsoft.Json.Linq;
using System;

namespace NEL.Comm
{
    public class Api
    {
        private string netnode;
        private static Api testApi = new Api("testnet");
        private static Api mainApi = new Api("mainnet");
        public static Api getTestApi() { return testApi; }
        public static Api getMainApi() { return mainApi; }
        //
        private HttpHelper hh = new HttpHelper();
        private MongoHelper mh = new MongoHelper();
        //
        private UserService us;


        public Api(string node)
        {
            netnode = node;
            switch (netnode)
            {
                case "testnet":
                    us = new UserService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet
                    };
                    break;
                case "mainnet":
                    us = new UserService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet
                    };
                    break;
            }
        }
        public object getRes(JsonRPCrequest req, string reqAddr)
        {
            JArray result = null;
            try
            {
                switch (req.method)
                {
                    case "verifyEmail":
                        result = us.verifyEmail(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "modifyEmail":
                        result = us.modifyEmail(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString(), req.@params[3].ToString());
                        break;
                    case "modifyPassword":
                        result = us.modifyPassword(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString(), req.@params[3].ToString());
                        break;
                    case "modifyUserBrief":
                        result = us.modifyUserBrief(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString(), req.@params[3].ToString());
                        break;
                    case "modifyUserIcon":
                        result = us.modifyUserIcon(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "getUserInfo":
                        result = us.getUserInfo(req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    case "verifyReset":
                        result = us.verifyReset(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString(), req.@params[3].ToString());
                        break;
                    case "resetPassword":
                        result = us.resetPassword(req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    case "login":
                        result = us.login(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "verifyRegister":
                        result = us.verifyRegister(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "register":
                        result = us.register(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "checkEmail":
                        result = us.checkEmail(req.@params[0].ToString());
                        break;
                    case "checkUsername":
                        result = us.checkUsername(req.@params[0].ToString());
                        break;
                    //
                    case "getnodetype":
                        result = new JArray { new JObject { { "nodeType", netnode } } };
                        break;
                }
                //
                if (result == null || result.Count == 0)
                {
                    JsonPRCresponse_Error resE = new JsonPRCresponse_Error(req.id, -1, "No Data", "Data does not exist");
                    return resE;
                }
            }
            catch (Exception e)
            {
                JsonPRCresponse_Error resE = new JsonPRCresponse_Error(req.id, -100, "Parameter Error", e.Message);
                return resE;
            }

            JsonPRCresponse res = new JsonPRCresponse();
            res.jsonrpc = req.jsonrpc;
            res.id = req.id;
            res.result = result;
            return res;
        }
    }
}
