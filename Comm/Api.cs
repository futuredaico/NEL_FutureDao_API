using NEL.API.RPC;
using NEL.NNS.lib;
using NEL_FutureDao_API;
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
        private MongoHelper mh = new MongoHelper();
        //
        private UserService us;
        private ProjService ps;
        private DiscussService ds;
        private FinanceService fs;

        public Api(string node)
        {
            initOss();
            netnode = node;
            switch (netnode)
            {
                case "testnet":
                    fs = new FinanceService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,

                    };
                    ds = new DiscussService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        tokenUrl = mh.tokenUrl_testnet,
                    };
                    ps = new ProjService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        tokenUrl = mh.tokenUrl_testnet,
                        oss = oss,
                        bucketName = mh.bucketName_testnet,
                    };
                    us = new UserService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        tokenUrl = mh.tokenUrl_testnet,
                        oss = oss,
                        bucketName = mh.bucketName_testnet,
                        prefixPassword = mh.prefixPassword
                    };
                    break;
                case "mainnet":
                    ds = new DiscussService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet,
                        tokenUrl = mh.tokenUrl_mainnet,
                    };
                    ps = new ProjService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet,
                        tokenUrl = mh.tokenUrl_mainnet,
                        oss = oss,
                        bucketName = mh.bucketName_mainnet,
                    };
                    us = new UserService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet,
                        tokenUrl = mh.tokenUrl_mainnet,
                        oss = oss,
                        bucketName = mh.bucketName_mainnet,
                        prefixPassword = mh.prefixPassword
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
                    case "queryReserveToken":
                        result = fs.queryReserveToken(
                           req.@params[0].ToString()
                           );
                        break;
                    case "queryRewardList":
                        result = fs.queryRewardList(
                             req.@params[0].ToString()
                             );
                        break;
                    case "queryTokenHistPrice":
                        result = fs.queryTokenHistPrice(
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "queryProjContract":
                        result = fs.queryProjContract(req.@params[0].ToString());
                        break;
                    case "queryContractHash":
                        result = fs.queryContractHash(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "startFinance":
                        result = fs.startFinance(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "saveReserveFundRatio":
                        result = fs.saveReserveFundRatio(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    case "queryReserveFundRatio":
                        result = fs.queryReserveFundRatio(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "queryFinanceFund":
                        result = fs.queryFinanceFund(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "queryReward":
                        result = fs.queryReward(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "saveReward":
                        result = fs.saveReward(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            JObject.Parse(req.@params[5].ToString())
                            );
                        break;
                    case "queryContract":
                        result = fs.queryContract(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "publishContract":
                        result = fs.publishContract(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString(),
                            req.@params[7].ToString(),
                            req.@params[8].ToString(),
                            req.@params[9].ToString(),
                            JObject.Parse(req.@params[10].ToString())
                            );
                        break;
                    case "bindAddress":
                        result = us.bindAddress(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    //
                    case "getStarMangeProjCount":
                        result = ps.getStarMangeProjCount(
                            req.@params[0].ToString(),
                            req.@params[1].ToString());
                        break;
                    //
                    case "zanUpdate":
                        result = ds.zanUpdate(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString());
                        break;
                    case "zanUpdateDiscuss":
                        result = ds.zanUpdateDiscuss(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString());
                        break;
                    case "zanProjDiscuss":
                        result = ds.zanProjDiscuss(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString());
                        break;
                    // 
                    case "getUpdateSubChildDiscussList":
                        result = ds.getUpdateSubChildDiscussList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            int.Parse(req.@params[2].ToString()),
                            int.Parse(req.@params[3].ToString()));
                        break;
                    case "getUpdateSubDiscussList":
                        result = ds.getUpdateSubDiscussList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            int.Parse(req.@params[3].ToString()),
                            int.Parse(req.@params[4].ToString()));
                        break;
                    
                    case "getUpdateDiscuss":
                        result = ds.getUpdateDiscuss(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString());
                        break;
                    case "delUpdateDiscuss":
                        result = ds.delUpdateDiscuss(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString());
                        break;
                    case "addUpdateDiscuss":
                        result = ds.addUpdateDiscuss(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString());
                        break;
                    case "getProjSubChildDiscussList":
                        result = ds.getProjSubChildDiscussList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            int.Parse(req.@params[2].ToString()),
                            int.Parse(req.@params[3].ToString()));
                        break;
                    case "getProjSubDiscussList":
                        result = ds.getProjSubDiscussList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            int.Parse(req.@params[3].ToString()),
                            int.Parse(req.@params[4].ToString()));
                        break;
                    case "getProjDiscuss":
                        result = ds.getProjDiscuss(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "addProjDiscuss":
                        result = ds.addProjDiscuss(
                            req.@params[0].ToString(), 
                            req.@params[1].ToString(), 
                            req.@params[2].ToString(), 
                            req.@params[3].ToString(), 
                            req.@params[4].ToString());
                        break;
                    // 
                    case "startSupportProj":
                        result = ps.startSupportProj(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "cancelStarProj":
                        result = ps.cancelStarProj(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "startStarProj":
                        result = ps.startStarProj(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    // 
                    case "commitProjAudit":
                        result = ps.commitProjAudit(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    //
                    case "queryUpdateList":
                        result = ps.queryUpdateList(req.@params[0].ToString(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()));
                        break;
                    case "queryProjTeamBrief":
                        result = ps.queryProjTeamBrief(req.@params[0].ToString(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()));
                        break;
                    case "getProjInfo":
                        result = ps.getProjInfo(req.@params[0].ToString());
                        break;
                    case "queryProjDetail":
                        result = ps.queryProjDetail(req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    case "queryProjListAtManage":
                        result = ps.queryProjListAtManage(req.@params[0].ToString(), req.@params[1].ToString(), int.Parse(req.@params[2].ToString()), int.Parse(req.@params[3].ToString()));
                        break;
                    case "queryProjListAtStar":
                        result = ps.queryProjListAtStar(req.@params[0].ToString(), req.@params[1].ToString(), int.Parse(req.@params[2].ToString()), int.Parse(req.@params[3].ToString()));
                        break;
                    case "queryProjList":
                        result = ps.queryProjList(int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()));
                        break;
                    //
                    case "queryUpdate":
                        result = ps.queryUpdate(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "modifyUpdate":
                        result = ps.modifyUpdate(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString()
                            );
                        break;
                    case "deleteUpdate":
                        result = ps.deleteUpdate(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    case "createUpdate":
                        result = ps.createUpdate(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString()
                            );
                        break;
                    //
                    case "queryProjTeam":
                        result = ps.queryProjTeam(
                                req.@params[0].ToString(),
                                req.@params[1].ToString(),
                                req.@params[2].ToString(),
                                int.Parse(req.@params[3].ToString()),
                                int.Parse(req.@params[4].ToString())
                            );
                        break;
                    case "modifyUserRole":
                        result = ps.modifyUserRole(
                                req.@params[0].ToString(),
                                req.@params[1].ToString(),
                                req.@params[2].ToString(),
                                req.@params[3].ToString(),
                                req.@params[4].ToString()
                            );
                        break;
                    case "deleteProjTeam":
                        result = ps.deleteProjTeam(
                                req.@params[0].ToString(),
                                req.@params[1].ToString(),
                                req.@params[2].ToString(),
                                req.@params[3].ToString()
                                );
                        break;
                    case "verifyInvite":
                        result = ps.verifyInvite(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString());
                        break;
                    case "inviteMember":
                        result = ps.inviteMember(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString());
                        break;
                    case "queryMember":
                        result = ps.queryMember(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            int.Parse(req.@params[3].ToString()),
                            int.Parse(req.@params[4].ToString())
                            );
                        break;
                    //
                    case "queryProj":
                        result = ps.queryProj(
                                req.@params[0].ToString(),
                                req.@params[1].ToString(),
                                req.@params[2].ToString()
                            );
                        break;
                    case "deleteProj":
                        result = ps.deleteProj(
                                req.@params[0].ToString(),
                                req.@params[1].ToString(),
                                req.@params[2].ToString()
                            );
                        break;
                    case "modifyProjName":
                        result = ps.modifyProjName(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString(),
                            req.@params[7].ToString());
                        break;
                    case "modifyProjEmail":
                        result = ps.modifyProjEmail(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString());
                        break;
                    case "modifyProjVideo":
                        result = ps.modifyProjVideo(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString());
                        break;
                    case "createProj":
                        result = ps.createProj(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString());
                        break;
                    //
                    case "reSendVerify":
                        result = us.reSendVerify(req.@params[0].ToString(), req.@params[1].ToString());
                        break;
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
                        result = us.modifyUserBrief(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
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
                        result = us.resetPassword(req.@params[0].ToString());
                        break;
                    case "login":
                        result = us.login(req.@params[0].ToString(), req.@params[1].ToString());
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
                Console.WriteLine(e.StackTrace);
                JsonPRCresponse_Error resE = new JsonPRCresponse_Error(req.id, -100, "Parameter Error", e.Message);
                return resE;
            }

            JsonPRCresponse res = new JsonPRCresponse();
            res.jsonrpc = req.jsonrpc;
            res.id = req.id;
            res.result = result;
            return res;
        }


        //
        private OssHelper oss;
        private void initOss()
        {
            oss = new OssHelper
            {
                endpoint = mh.endpoint,
                accessKeyId = mh.accessKeyId,
                accessKeySecret = mh.accessKeySecret,
                bucketName_testnet = mh.bucketName_testnet,
                bucketName_mainnet = mh.bucketName_mainnet,
                ossUrlPrefix = mh.ossUrlPrefix
            };
        }
        //
        public MongoHelper getMongoDB() => mh;
    }
}
