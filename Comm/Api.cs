using Microsoft.AspNetCore.Mvc;
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
        private UserServiceV3 usV3;
        private ProjService ps;
        private FutureService fs2;
        private DiscussService ds;
        private FinanceService fs;
        private RewardService rs;
        private MoloService ms;
        private ShellService ss;

        public Api(string node)
        {
            initOss();
            netnode = node;
            switch (netnode)
            {
                case "testnet":
                    rs = new RewardService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        oss = oss,
                        bucketName = mh.bucketName_testnet,
                    };
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
                        bucketName = mh.bucketName_testnet
                    };
                    usV3 = new UserServiceV3
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        tokenUrl = mh.tokenUrl_testnet,
                        oss = oss,
                        bucketName = mh.bucketName_testnet
                    };
                    fs2 = new FutureService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        us = usV3,
                    };
                    ms = new MoloService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_testnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_testnet,
                        us = usV3,
                        oss = oss,
                        bucketName = mh.bucketName_testnet,
                        fs = fs2
                    };
                    ss = new ShellService
                    {
                        exeFileName = mh.exeFileName
                    };
                    break;
                case "mainnet":
                    usV3 = new UserServiceV3
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet,
                        tokenUrl = mh.tokenUrl_mainnet,
                        oss = oss,
                        bucketName = mh.bucketName_mainnet
                    };
                    ms = new MoloService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet,
                        us = usV3,
                        oss = oss,
                        bucketName = mh.bucketName_mainnet
                    };
                    rs = new RewardService
                    {
                        mh = mh,
                        dao_mongodbConnStr = mh.dao_mongodbConnStr_mainnet,
                        dao_mongodbDatabase = mh.dao_mongodbDatabase_mainnet,
                        oss = oss,
                        bucketName = mh.bucketName_mainnet,
                    };
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
                        bucketName = mh.bucketName_mainnet
                    };
                    break;
            }
        }
        public object getRes(JsonRPCrequest req, string reqAddr, Controller controller)
        {
            JArray result = null;
            try
            {
                switch (req.method)
                {
                    //
                    case "queryRewardDetail":
                        result = fs2.queryRewardDetail(
                            req.@params[0].ToString()
                            );
                        break;
                    case "queryRewardList":
                        result = fs2.queryRewardList(
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "getRewardInfo":
                        result = fs2.getRewardInfo(controller,
                            req.@params[0].ToString()
                            );
                        break;
                    case "saveRewardInfo":
                        result = fs2.saveRewardInfo(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            JObject.Parse(req.@params[3].ToString())
                            );
                        break;
                    case "getFContractInfo":
                        result = fs2.getContractInfo(controller,
                            req.@params[0].ToString()
                            );
                        break;
                    case "saveFContractInfo":
                        result = fs2.saveContractInfo(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString(),
                            JArray.Parse(req.@params[7].ToString()),
                            req.@params[8].ToString(),
                            req.@params[9].ToString()
                            );
                        break;
                    case "queryJoinOrgAddressList":
                        result = fs2.queryJoinOrgAddressList(controller,
                            int.Parse(req.@params[0].ToString()),
                            int.Parse(req.@params[1].ToString())
                            );
                        break;
                    // 
                    case "queryProjUpdateDetail":
                        result = fs2.queryProjUpdateDetail(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "queryProjUpdateList":
                        result = fs2.queryProjUpdateList(
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "queryProjTeam":
                        result = fs2.queryProjTeam(
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "queryProjDetail"://getProjdetailV3
                        result = fs2.queryProjDetail(controller, req.@params[0].ToString());
                        break;
                    case "queryFProjList":
                        // 共用getProjList();
                        break;
                    //
                    case "queryUpdateList":
                        result = fs2.queryUpdateList(controller,
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "queryUpdate":
                        result = fs2.queryUpdate(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "modifyUpdate":
                        result = fs2.modifyUpdate(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    case "deleteUpdate":
                        result = fs2.deleteUpdate(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "createUpdate":
                        result = fs2.createUpdate(controller, 
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "queryMemberList":
                        result = fs2.queryMemberList(controller,
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "deleteMember":
                        result = fs2.deleteMember(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "inviteMember":
                        result = fs2.inviteMember(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    //
                    case "queryProjCount":
                        result = fs2.queryProjCount(controller);
                        break;
                    case "queryProjListAtJoin":
                        result = fs2.queryProjListAtJoin(controller, 
                            int.Parse(req.@params[0].ToString()),
                            int.Parse(req.@params[1].ToString())
                            );
                        break;
                    case "queryProjListAtStar":
                        result = fs2.queryProjListAtStar(controller,
                            int.Parse(req.@params[0].ToString()),
                            int.Parse(req.@params[1].ToString()));
                        break;
                    case "queryProjListAtManage":
                        result = fs2.queryProjListAtManage(controller,
                            int.Parse(req.@params[0].ToString()),
                            int.Parse(req.@params[1].ToString()));
                        break;
                    case "queryProj":
                        result = fs2.queryProj(controller,
                            req.@params[0].ToString()
                            );
                        break;
                    case "modifyProj":
                        result = fs2.modifyProj(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString()
                            );
                        break;
                    case "deleteProj":
                        result = fs2.deleteProj(controller,
                            req.@params[0].ToString()
                            );
                        break;
                    case "createProj":
                        result = fs2.createProj(controller, 
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString()
                            );
                        break;

                    // molo_v2.0_ed
                    case "getLastUpdatorInfo":
                        result = ms.getLastUpdatorInfo(
                            req.@params[0].ToString()
                            );
                        break;
                    case "modifyProjInfo":
                        result = ms.modifyProjInfo(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString()
                            );
                        break;
                    case "getProjFundList":
                        result = ms.getProjFundList(controller, req.@params[0].ToString(),
                             int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString()));
                        break;
                    case "getProjBidPrice":
                        result = ms.getProjBidPrice();
                        break;
                    case "getProjFundTotal":
                        result = ms.getProjFundTotal(
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    // molo_v2.0_st
                    // ******************************************* v3.st
                    case "querySupportVersion":
                        result = ms.querySupportVersion(controller);
                        break;
                    case "queryContractInfo":
                        result = ms.queryContractInfo(controller, req.@params[0].ToString());
                        break;

                    case "saveContractInfo":
                        if(req.@params.Length > 21)
                        {
                            result = ms.saveContractInfo(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString(),
                            req.@params[7].ToString(),
                            long.Parse(req.@params[8].ToString()),
                            long.Parse(req.@params[9].ToString()),
                            long.Parse(req.@params[10].ToString()),
                            long.Parse(req.@params[11].ToString()),
                            long.Parse(req.@params[12].ToString()),
                            req.@params[13].ToString(),
                            req.@params[14].ToString(),
                            req.@params[15].ToString(),
                            JArray.Parse(req.@params[16].ToString()),
                            long.Parse(req.@params[17].ToString()),
                            long.Parse(req.@params[18].ToString()),
                            long.Parse(req.@params[19].ToString()),
                            JArray.Parse(req.@params[20].ToString()),
                            req.@params[21].ToString()
                            );
                            break;
                        }
                        result = ms.saveContractInfo(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString(),
                            req.@params[5].ToString(),
                            req.@params[6].ToString(),
                            req.@params[7].ToString(),
                            long.Parse(req.@params[8].ToString()),
                            long.Parse(req.@params[9].ToString()),
                            long.Parse(req.@params[10].ToString()),
                            long.Parse(req.@params[11].ToString()),
                            long.Parse(req.@params[12].ToString()),
                            req.@params[13].ToString(),
                            req.@params[14].ToString(),
                            req.@params[15].ToString(),
                            JArray.Parse(req.@params[16].ToString()),
                            long.Parse(req.@params[17].ToString()),
                            long.Parse(req.@params[18].ToString()),
                            long.Parse(req.@params[19].ToString()), 
                            JArray.Parse(req.@params[20].ToString())
                            );
                        break;
                    case "getVoteInfo":
                        result = ms.getVoteInfo(req.@params[0].ToString(), req.@params[1].ToString(), req.@params[2].ToString());
                        break;
                    case "getProjDepositInfo":
                        result = ms.getProjDepositInfo(controller, req.@params[0].ToString());
                        break;
                    case "getTokenBalanceFromUpStream":
                        result = ms.getTokenBalanceFromUpStream(controller, req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    case "getTokenBalance":
                        result = ms.getTokenBalance(controller, req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    #region discuss
                    // molo.prop.discuss
                    case "getMoloPropSubDiscussList":
                        result = ms.getMoloPropSubDiscussList(controller,
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "getMoloPropDiscussList":
                        result = ms.getMoloPropDiscussList(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            int.Parse(req.@params[2].ToString()),
                            int.Parse(req.@params[3].ToString())
                            );
                        break;
                    case "getMoloPropDiscuss":
                        result = ms.getMoloPropDiscuss(req.@params[0].ToString());
                        break;
                    case "zanMoloPropDiscuss":
                        result = ms.zanMoloPropDiscuss(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "addMoloPropDiscuss":
                        result = ms.addMoloPropDiscuss(controller,
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    // molo.discuss
                    case "getMoloSubDiscussList":
                        result = ms.getMoloSubDiscussList(controller,
                            req.@params[0].ToString(),
                            int.Parse(req.@params[1].ToString()),
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "getMoloDiscussList":
                        result = ms.getMoloDiscussList(controller, 
                            req.@params[0].ToString(), 
                            int.Parse(req.@params[1].ToString()), 
                            int.Parse(req.@params[2].ToString())
                            );
                        break;
                    case "getMoloDiscuss":
                        result = ms.getMoloDiscuss(req.@params[0].ToString());
                        break;
                    case "zanMoloDiscuss":
                        result = ms.zanMoloDiscuss(controller, 
                            req.@params[0].ToString(), 
                            req.@params[1].ToString()
                            );
                        break;
                    case "addMoloDiscuss":
                        result = ms.addMoloDiscuss(controller, 
                            req.@params[0].ToString(), 
                            req.@params[1].ToString(), 
                            req.@params[2].ToString()
                            );
                        break;
                    #endregion
                    //
                    case "getProjMemberListV3":
                        if(req.@params.Length > 3)
                        {
                            result = ms.getProjMemberList(
                                req.@params[0].ToString(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()),
                                req.@params[3].ToString());
                        } else
                        {
                            result = ms.getProjMemberList(
                                req.@params[0].ToString(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()));
                        }
                        break;
                    case "getProjProposalDetailV3":
                        result = ms.getProjProposalDetail(req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    case "getProjProposalListV3":
                        if(req.@params.Length < 5)
                        {
                            result = ms.getProjProposalList(req.@params[0].ToString(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()), req.@params[3].ToString());
                        } else
                        {
                            result = ms.getProjProposalList(req.@params[0].ToString(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()), req.@params[3].ToString(), req.@params[4].ToString());
                        }
                        break;
                    case "getProjdetailV3":
                        result = ms.getProjDetail(req.@params[0].ToString());
                        break;
                    case "getProjListV3":
                        result = ms.getProjList(int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()));
                        break;
                    //
                    case "modifyUserNameV3":
                        result = usV3.modifyUserName(controller, req.@params[0].ToString());
                        break;
                    case "modifyUserIconV3":
                        result = usV3.modifyUserIcon(controller, req.@params[0].ToString());
                        break;
                    case "logoutV3":
                        result = usV3.logout(controller);
                        break;
                    case "getUserInfoV3":
                        result = usV3.getUserInfo(controller);
                        break;
                    case "validateLoginV3":
                        result = usV3.validateLoginInfo(controller, req.@params[0].ToString(), req.@params[1].ToString());
                        break;
                    case "getLoginNonceV3":
                        result = usV3.getLoginNonce(req.@params[0].ToString());
                        break;
                    // ******************************************* v3.ed
                    /*
                    case "exportOrderInfo":
                        result = rs.exportOrderInfo(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "queryProjBuyOrderList":
                        result = rs.queryProjBuyOrderList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            int.Parse(req.@params[3].ToString()),
                            int.Parse(req.@params[4].ToString()),
                            int.Parse(req.@params[5].ToString()),
                            req.@params[6].ToString(),
                            req.@params[7].ToString(),
                            int.Parse(req.@params[8].ToString())
                            );
                        break;
                    case "queryProjBuyOrder":
                        result = rs.queryProjBuyOrder(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    case "queryBuyOrderList":
                        result = rs.queryBuyOrderList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            int.Parse(req.@params[2].ToString()),
                            int.Parse(req.@params[3].ToString())
                            );
                        break;
                    case "queryBuyOrder":
                        result = rs.queryBuyOrder(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    case "confirmDeliverBuyOrder":
                        result = rs.confirmDeliverBuyOrder(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString(),
                            req.@params[4].ToString()
                            );
                        break;
                    case "cancelBuyOrder":
                        result = rs.cancelBuyOrder(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString()
                            );
                        break;
                    case "confirmBuyOrder":
                        result = rs.confirmBuyOrder(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            req.@params[2].ToString(),
                            req.@params[3].ToString()
                            );
                        break;
                    case "initBuyOrder":
                        result = rs.initBuyOrder(
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
                            req.@params[10].ToString()
                            );
                        break;
                    // 
                    case "queryTokenBalanceInfo":
                        result = fs.queryTokenBalanceInfo(
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "queryTxList":
                        result = fs.queryTxList(
                            req.@params[0].ToString(),
                            req.@params[1].ToString(),
                            int.Parse(req.@params[2].ToString()),
                            int.Parse(req.@params[3].ToString())
                            );
                        break;
                    case "queryTokenHistPrice":
                        result = fs.queryTokenHistPrice(
                            req.@params[0].ToString(),
                            req.@params[1].ToString()
                            );
                        break;
                    case "queryReserveToken":
                        result = fs.queryReserveToken(
                           req.@params[0].ToString()
                           );
                        break;
                    case "queryRewardDetail":
                        result = fs.queryRewardDetail(
                           req.@params[0].ToString()
                           );
                        break;
                    case "queryRewardList":
                        result = fs.queryRewardList(
                             req.@params[0].ToString()
                             );
                        break;
                    case "queryTokenPrice":
                        result = fs.queryTokenPrice(
                             req.@params[0].ToString()
                             );
                        break;
                    case "queryProjContract":
                        result = fs.queryProjContract(req.@params[0].ToString());
                        break;
                    case "queryContractHash":
                        result = fs.queryContractHash(
                            req.@params[0].ToString()
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
                            JArray.Parse(req.@params[10].ToString())
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
                        if(req.@params.Length == 3)
                        {
                            result = us.getUserInfo(req.@params[0].ToString(), req.@params[1].ToString(), "1");
                            break;
                        }
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
                    */
                    case "listMethod":
                        result = ss.listMethod();
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
