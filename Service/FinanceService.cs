using MongoDB.Bson;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NEL_FutureDao_API.Service
{
    /// <summary>
    /// 
    /// 融资模块
    /// 
    /// 1. 查询合约 + 发布合约(接口存储 + 管理发布 + 设置预留代币)
    /// 2. 查询回报 + 设置回报
    /// 3. 查询融资(已融资金 + 储备比例) ==> 领取已融资金 + 修改储备比例
    /// 4. 启动融资
    /// 
    /// 
    /// </summary>
    public class FinanceService
    {

        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string projInfoCol { get; set; } = "daoprojinfos";
        public string projTeamInfoCol { get; set; } = "daoprojteaminfos";
        public string projFinanceCol { get; set; } = "daoprojfinanceinfos";
        public string projFinanceHashCol { get; set; } = "daoprojfinancehashinfos";
        public string projFinanceFundPoolCol { get; set; } = "daoprojfinancefundpoolinfos";
        public string projFinancePriceHistCol { get; set; } = "daoprojfinancepricehistinfos";
        public string projFinanceReserveTokenHistCol { get; set; } = "daoprojfinancereservetokenhistinfos";
        public string projFinanceRewardCol { get; set; } = "daoprojfinancerewardinfos";
        public string projFinanceBalanceCol { get; set; } = "daoprojfinancebalanceinfos";
        public string projFundCol { get; set; } = "daoprojfundinfos";
        public string projFundApplyCol { get; set; } = "daoprojfundapplyinfos";
        public string daoNotifyCol { get; set; } = "daonotifyinfos";
        public string tokenUrl { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        private bool checkToken(string userId, string accessToken, out string code)
            => TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out code);
        private bool checkIntFmt(JToken tp)
        {
            if (tp == null) return false;
            try
            {
                decimal.Parse(tp.ToString());
                var rr = tp.ToString().Split(".");
                if (rr.Length == 1) return rr[0].Length <= 9;
                else if (rr.Length == 2) return rr[0].Length <= 9 && rr[1].Length <= 4;
                else return false;
            }
            catch
            {
                return false;
            }
        }


        public  JArray publishContract(string userId, string accessToken, 
            string projId, 
            string type, 
            string platform, 
            string fundName, 
            string adminAddress, 
            string tokenName, 
            string tokenSymbol, 
            string reserveTokenFlag, 
            JArray reserveTokenInfos) {

            adminAddress = adminAddress.ToLower();
            // 参数检查
            type = type.ToLower();
            if(type != FinanceType.General
                && type != FinanceType.DAICO)
            {
                return getErrorRes(DaoReturnCode.C_InvalidFinanceType);
            }
            platform = platform.ToLower();
            if(platform != PlatformType.ETH
                && platform != PlatformType.NEO)
            {
                return getErrorRes(DaoReturnCode.C_InvalidPlatformType);
            }
            if(fundName.Length == 0 || fundName.Length > 5 
                || tokenName.Length > 18
                || tokenSymbol.Length == 0 || tokenSymbol.Length > 5)
            {
                return getErrorRes(DaoReturnCode.C_InvalidParamLen);
            }
            if (reserveTokenFlag != SelectKey.Yes) reserveTokenFlag = SelectKey.Not;
            if(reserveTokenFlag == SelectKey.Yes)
            {
                foreach(var reserveTokenInfo in reserveTokenInfos)
                {
                    var addr = reserveTokenInfo["address"];
                    if (addr == null)
                    {
                        return getErrorRes(DaoReturnCode.C_InvalidParamFmt);
                    }
                    var addrLen = addr.ToString().Length;
                    if (addrLen == 0 || addrLen > 64)
                    {
                        return getErrorRes(DaoReturnCode.C_InvalidParamLen);
                    }

                    var info = reserveTokenInfo["info"];
                    if (info == null)
                    {
                        return getErrorRes(DaoReturnCode.C_InvalidParamFmt);
                    }
                    var infoArr = (JArray)info;
                    if (infoArr.Count > 0)
                    {
                        if (!infoArr.All(p => checkIntFmt(p["amt"]) && checkIntFmt(p["days"])))
                        {
                            return getErrorRes(DaoReturnCode.C_InvalidParamFmt);
                        }
                    }
                    reserveTokenInfo["address"] = addr.ToString().ToLower();
                }
            }

            //
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId},{ "userId",userId},{ "role", TeamRoleType.Admin} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }

            //
            findStr = new JObject { { "projId", projId },{ "projState", ProjState.IdeaPub} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            findStr = new JObject { { "projId", projId } }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                // 部署合约
                { "type", type},
                { "platform", platform},
                { "fundName", fundName},
                { "adminAddress", adminAddress},
                { "tokenName", tokenName},
                { "tokenSymbol", tokenSymbol},
                { "reserveTokenFlag", reserveTokenFlag},
                { "reserveTokenInfo", reserveTokenInfos},
                { "deployContractFlag", SkOp.HandlingOp},
                { "reserveTokenSetFlag", SkOp.HandlingOp},
                // 设置回报
                { "rewardSetFlag", SkOp.NotOp},
                { "connectorName", ""},
                { "connectorTel", ""},
                // 存储金比例
                { "ratioSetFlag", SkOp.NotOp },
                { "reserveFundRatio", 0},
                // 启动融资
                { "financeStartFlag", SkOp.NotOp},
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, newdata);
            return getRes();
        }
        public JArray queryContract(string userId, string accessToken, string projId)
        {
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            //
            findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] {
                "projId","type", "platform", "fundName","adminAddress", "tokenName","tokenSymbol",
                "reserveTokenFlag","reserveTokenInfo","deployContractFlag","reserveTokenSetFlag", "rewardSetFlag","ratioSetFlag", "financeStartFlag"
            }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();
            return getRes(queryRes[0]);
        }
        public JArray saveReward(string userId, string accessToken, string projId, string connectorName, string connectTel, JObject info)
        {
            // TODO: 参数检查
            if (connectorName.Length > 40 || connectTel.Length > 40) return getErrorRes(DaoReturnCode.C_InvalidParamLen);
            var infoJA = info["info"] as JArray;
            if (infoJA != null && infoJA.Count > 0)
            {
                if (connectorName.Length == 0 || connectTel.Length == 0) return getErrorRes(DaoReturnCode.C_InvalidParamLen);
                if (!infoJA.All(p => {
                    if (p["rewardId"] == null
                        || p["rewardName"] == null
                        || p["rewardDesc"] == null
                        || p["price"] == null
                        || p["limitFlag"] == null
                        || p["distributeTimeFlag"] == null
                        || p["distributeWay"] == null
                        || p["note"] == null)
                    {
                        return false;
                    }
                    var len = p["rewardName"].ToString().Length;
                    if (len == 0 || len > 40) return false;
                    len = p["rewardDesc"].ToString().Length;
                    if (len == 0 || len > 500) return false;
                    if (!checkIntFmt(p["price"])) return false;
                    var tp = p["limitFlag"].ToString();
                    if (tp == SelectKey.Yes)
                    {
                        if (p["limitMax"] == null || !checkIntFmt(p["limitMax"])) return false;
                    }
                    len = p["note"].ToString().Length;
                    if (len > 100) return false;
                    return true;
                })) return getErrorRes(DaoReturnCode.C_InvalidParamFmt);
            }

            //
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            //
            findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = new JObject { { "connectorName", 1 }, { "connectorTel", 1 }, { "fundName", 1 }, { "deployContractFlag", 1 },{ "reserveTokenSetFlag",1 }, { "rewardSetFlag", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.InvalidOperate);
            //
            if (queryRes[0]["deployContractFlag"].ToString() != SkOp.FinishOp
                || queryRes[0]["reserveTokenSetFlag"].ToString() != SkOp.FinishOp)
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            if (queryRes[0]["connectorName"].ToString() != connectorName
                || queryRes[0]["connectorTel"].ToString() != connectTel
                || queryRes[0]["rewardSetFlag"].ToString() != SkOp.FinishOp)
            {
                string updateStr = new JObject { { "$set", new JObject { { "connectorName", connectorName }, { "connectorTel", connectTel },{ "rewardSetFlag", SkOp.FinishOp } } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, updateStr, findStr);
            }
            //
            string fundName = queryRes[0]["fundName"].ToString();
            var rewardList = (JArray)info["info"];

            // TODO: 增删改查
            var nlist = new List<JToken>();
            findStr = new JObject { { "projId", projId }, { "activeState", RewardActiveState.Valid_Yes } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, findStr);
            if(queryRes.Count == 0)
            {
                nlist = rewardList.ToList();
            } else
            {
                foreach (var item in rewardList)
                {
                    var id = item["rewardId"].ToString();
                    if (id.Trim().Length == 0)
                    {
                        nlist.Add(item);
                        continue;
                    }
                    var tItems = queryRes.Where(p => p["rewardId"].ToString() == id).ToArray();
                    if (tItems.Count() == 0) continue;

                    var tItem = tItems[0];
                    bool eq = item["rewardName"].ToString() == tItem["rewardName"].ToString()
                        && item["rewardDesc"].ToString() == tItem["rewardDesc"].ToString()
                        && item["price"].ToString() == tItem["price"].ToString()
                        && item["limitFlag"].ToString() == tItem["limitFlag"].ToString()
                        && item["limitMax"].ToString() == tItem["limitMax"].ToString()
                        && item["distributeTimeFlag"].ToString() == tItem["distributeTimeFlag"].ToString()
                        && item["distributeTimeFixYes"].ToString() == tItem["distributeTimeFixYes"].ToString()
                        && item["distributeTimeFixNot"].ToString() == tItem["distributeTimeFixNot"].ToString()
                        && item["distributeWay"].ToString() == tItem["distributeWay"].ToString()
                        ;
                    if (eq) continue;
                    findStr = new JObject { { "rewardId", item["rewardId"]} }.ToString();
                    var updateStr = new JObject { { "$set", new JObject { { "activeState", RewardActiveState.Valid_Not } } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, updateStr, findStr);
                    nlist.Add(item);
                }
                //
                var oIds = queryRes.Select(p => p["rewardId"].ToString()).ToArray();
                foreach(var id in oIds)
                {
                    if(rewardList.All(p => p["rewardId"].ToString() != id))
                    {
                        findStr = new JObject { { "projId", projId }, { "rewardId", id } }.ToString();
                        var updateStr = new JObject { { "$set", new JObject { { "activeState", RewardActiveState.Valid_Not } } } }.ToString();
                        mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, updateStr, findStr);
                    }
                }
            }
            //
            var res = nlist.Select(p =>
            {
                p["projId"] = projId;
                p["rewardId"] = DaoInfoHelper.genProjRewardId(projId, p["rewardName"].ToString());
                p["fundName"] = fundName;
                p["activeState"] = RewardActiveState.Valid_Yes;
                p["hasSellCount"] = 0;
                return p;
            }).ToArray();
            if(res.Count() > 0)
            {
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, new JArray { res });
            }
            return getRes();
        }
        public JArray queryReward(string userId, string accessToken, string projId)
        {
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            //
            findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = new JObject { { "connectorName",1},{ "connectTel", 1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            var connectorName = "";
            var connectTel = "";
            if (queryRes.Count > 0)
            {
                connectorName = queryRes[0]["connectorName"].ToString();
                connectTel = queryRes[0]["connectTel"].ToString();
            }

            findStr = new JObject { { "projId", projId },{ "activeState", RewardActiveState.Valid_Yes} }.ToString();
            fieldStr = MongoFieldHelper.toReturn(new string[] {
                "rewardId", "projId","rewardName","rewardDesc", "fundName","price",
                "limitFlag","limitMax", "distributeTimeFlag","distributeTimeFixYes","distributeTimeFixNot","distributeWay","note","hasSellCount"
            }).ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, findStr, fieldStr);
            var res = new JObject { { "connectorName", connectorName }, { "connectTel", connectTel },{"info", queryRes} };
            return getRes(res);
        }
        public JArray applyFinanceFund(string userId, string accessToken, string projId, string fundAmt)
        {
            // 前端发送
            /*
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            //
            var findBson = new BsonDocument { { "projId", projId},{ "fundManagePoolTotal", new BsonDocument { { "$lte", decimal.Parse(fundAmt).format()} } } };
            if(mh.GetDataCountBson(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceFundPoolCol, findBson) == 0)
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            findStr = new JObject { { "projId", projId },{ "state", SkOp.HandlingOp } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFundApplyCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            var newdata = new JObject {
                { "projId", projId },
                { "fundAmt", fundAmt},
                { "state", SkOp.HandlingOp},
                { "txid", "" },
                { "time", TimeHelper.GetTimeStamp() },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFundApplyCol, newdata);
            */
            return getRes();
        }
        public JArray queryFinanceFund(string userId, string accessToken, string projId)
        {
            //
            if (!checkToken(userId, accessToken, out string code)) return getErrorRes(code);

            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            //
            findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = new JObject { { "fundManagePoolTotal", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFundCol, findStr, fieldStr);
            string poolTotal = "0";
            if(queryRes.Count > 0)
            {
                poolTotal = queryRes[0]["fundManagePoolTotal"].ToString().formatDecimal().formatEth();
            }
            return getRes(new JObject { { "poolTotal", poolTotal} });
        }
        public JArray saveReserveFundRatio(string userId, string accessToken, string projId, string ratio)
        {
            // 是否需要记录
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            findStr = new JObject { { "projId", projId} }.ToString();
            string fieldStr = new JObject { { "rewardSetFlag", 1 }, { "reserveFundRatio",1 },{ "ratioSetFlag", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.InvalidOperate);
            // TODO: 只有部署合约成功后才能继续
            //
            if (queryRes[0]["rewardSetFlag"].ToString() != SkOp.FinishOp)
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }


            if (queryRes[0]["ratioSetFlag"].ToString() == SkOp.HandlingOp)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }

            if (queryRes[0]["reserveFundRatio"].ToString() != ratio)
            {
                var updateStr = new JObject { { "$set", new JObject { { "reserveFundRatio", ratio }, { "ratioSetFlag", SkOp.HandlingOp } } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray queryReserveFundRatio(string userId, string accessToken, string projId)
        {
            if (!checkToken(userId, accessToken, out string code)) return getErrorRes(code);

            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = new JObject { { "reserveFundRatio", 1 }, { "ratioSetFlag",1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            string ratio = "0";
            string ratioSetFlag = "0";
            if (queryRes.Count > 0)
            {
                ratio = queryRes[0]["reserveFundRatio"].ToString();
                ratioSetFlag = queryRes[0]["ratioSetFlag"].ToString();
            } 
            return getRes(new JObject { { "ratio", ratio},{ "ratioSetFlag", ratioSetFlag } });
        }
        public JArray queryContractHash(string projId)
        {
            //
            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "contractHash", "contractName"}).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceHashCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();
            return getRes(queryRes);
        }

        public JArray startFinance(string userId, string accessToken, string projId)
        {
            //
            if (!checkToken(userId, accessToken, out string code)) return getErrorRes(code);

            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "role", TeamRoleType.Admin } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                return getErrorRes(DaoReturnCode.T_NoPermissionStartFinance);
            }
            findStr = new JObject { { "projId", projId} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr);
            if(queryRes.Count == 0)
            {
                // 项目未发布, 不能启动
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            if (queryRes[0]["deployContractFlag"].ToString() != SkOp.FinishOp
                || queryRes[0]["reserveTokenSetFlag"].ToString() != SkOp.FinishOp
                || queryRes[0]["rewardSetFlag"].ToString() != SkOp.FinishOp
                || queryRes[0]["ratioSetFlag"].ToString() != SkOp.FinishOp
                )
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var startFlag = queryRes[0]["financeStartFlag"].ToString();
            if (startFlag == SkOp.HandlingOp || startFlag == SkOp.FinishOp)
            {
                // 已启动
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            if (!updateProjState(projId, out code))
            {
                return getErrorRes(code);
            }

            string updateStr = new JObject { { "$set", new JObject { { "financeStartFlag", SkOp.HandlingOp } } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, updateStr, findStr);
            return getRes();
        }
        private bool updateProjState(string projId, out string code)
        {
            string findStr = new JObject { { "projId", projId} }.ToString();
            string fieldStr = new JObject { { "projState", 1 }, { "projSubState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0)
            {
                code = DaoReturnCode.InvalidOperate;
                return false;
            }
            var item = queryRes[0];
            if(item["projSubState"].ToString() == ProjSubState.Auditing)
            {
                code = DaoReturnCode.RepeatOperate;
                return false;
            }
            if(item["projState"].ToString() != ProjState.Trading 
                || item["projSubState"].ToString() != ProjSubState.Init)
            {
                var updateStr = new JObject { { "$set", new JObject { { "projState", ProjState.Trading }, { "projSubState", ProjSubState.Init } } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            }
            code = "";
            return true;
        }

        public JArray queryProjContract(string projId)
        {
            string findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceFundPoolCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject {
                {"projId", projId },
                {"tokenSymbol", item["tokenName"] },
                {"tokenIssueTotal", item["hasIssueTokenTotal"].ToString().formatDecimal().ToString() },
                {"tokenUnlockNotAmount", item["tokenUnlockNotAmount"].ToString().formatDecimal().ToString() },
                {"tokenUnlockYesAmount", item["tokenUnlockYesAmount"].ToString().formatDecimal().ToString() },
                {"fundManagePoolTotal", item["fundManagePoolTotal"].ToString().formatDecimal().formatEth().ToString() },
                {"fundReservePoolTotal", item["fundReservePoolTotal"].ToString().formatDecimal().formatEth().ToString() },
                {"fundReserveRatio", item["fundReserveRatio"].ToString().formatDecimal().formatRatio().ToString() },
                {"priceRaiseSpeed", item["priceRaiseSpeed"].ToString().formatDecimal().formatRatio().ToString() }
            };
            return getRes(res);
        }
        public JArray queryTokenPrice(string projId)
        {
            var findStr = new JObject { { "projId", projId} }.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] { "ob_fundAmt", "ob_tokenAmt", "os_fundAmt", "os_tokenAmt"}).ToString();
            var sortStr = new JObject { { "blockNumber", -1} }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinancePriceHistCol, findStr, sortStr, 0, 1, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            item["ob_fundAmt"] = item["ob_fundAmt"].ToString().formatDecimal().formatEth().ToString();
            item["ob_tokenAmt"] = item["ob_tokenAmt"].ToString().formatDecimal().ToString();
            item["os_fundAmt"] = item["os_fundAmt"].ToString().formatDecimal().formatEth().ToString();
            item["os_tokenAmt"] = item["os_tokenAmt"].ToString().formatDecimal().ToString();
            return getRes(item);
        }
        public JArray queryRewardList(string projId)
        {
            var findStr = new JObject { { "projId", projId },{"activeState", RewardActiveState.Valid_Yes} }.ToString();
            var fieldStr = new JObject { { "activeState", 0 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes(queryRes);

            var res = new JObject {
                { "count", queryRes.Count},
                { "list", queryRes }
            };
            return getRes(res);
        }
        public JArray queryRewardDetail(string rewardId)
        {
            var findStr = new JObject { { "rewardId", rewardId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, findStr);
            return getRes(queryRes);
        }
        public JArray queryReserveToken(string projId)
        {
            var findStr = new JObject { { "projId", projId} }.ToString();
            var fieldStr = new JObject { { "tokenAmt", 1 }, { "timestamp", 1 }, { "blockNumber",1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceReserveTokenHistCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes(queryRes);

            var list = new List<JObject>();
            var now = TimeHelper.GetTimeStamp();
            var data = queryRes.OrderBy(p => long.Parse(p["blockNumber"].ToString())).ToArray();
            for(int i=0; i<data.Length; ++i)
            {
                var item = (JObject)data[i];
                item["order"] = i + 1;
                item["unlockFlag"] = long.Parse(item["timestamp"].ToString()) < now;
                item.Remove("blockNumber");
                item.Remove("_id");
                list.Add(item);
            }
            var lockTotal = list.Sum(p => long.Parse(p["tokenAmt"].ToString()));
            var res = new JObject {
                { "lockTotal", lockTotal},
                { "count", data.Length },
                { "list", new JArray{list } }
            };
            return getRes(res);
        }

        //
        public JArray queryTokenHistPrice(string projId, string recordType)
        {

            var findJo = new JObject { { "projId", projId } };
            if (RecordType.ByMonth == recordType)
            {
                findJo.Add("recordType", 4);
            }
            var findStr = findJo.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] { "ob_price", "os_price", "recordTime" }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinancePriceHistCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes(queryRes);

            var buydata = queryRes.Select(p => p["ob_price"].ToString().formatDecimal().formatEth()).ToArray();
            var selldata = queryRes.Select(p => p["os_price"].ToString().formatDecimal().formatEth()).ToArray();
            var timedata = queryRes.Select(p => p["recordTime"].ToString()).ToArray();
            var res = new JObject { { "buyInfo", new JArray { buydata } }, { "sellInfo", new JArray { selldata } }, { "timeInfo", new JArray { timedata } } };
            return getRes(res);
        }
        public JArray queryTxList(string projId, string address, int pageNum=1, int pageSize=10)
        {
            address = address.ToLower();
            var findJo = new JObject { { "projId", projId } };
            if(address != "" && address.ToLower() != "all")
            {
                findJo.Add("who", address);
            }
            findJo.Add("$or", new JArray { new JObject { { "event", "OnBuy" } }, new JObject { { "event", "OnSell"} } });
            var findStr = findJo.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, daoNotifyCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count},{ "list", new JArray()} });

            var fieldStr = MongoFieldHelper.toReturn(new string[] { "blockTime", "transactionHash","blockNumber", "address", "event","fundAmt","tokenAmt"}).ToString();
            var sortStr = new JObject { { "blockNumber", -1} }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr,dao_mongodbDatabase, daoNotifyCol, findStr, sortStr, pageSize*(pageNum-1), pageSize, fieldStr);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });
            //
            var arr = queryRes.Select(p =>
            {
                var jo = (JObject)p;
                jo["fundAmt"] = jo["fundAmt"].ToString().formatEth();
                return jo;
            }).ToArray();
            var res = new JObject {
                { "count", count },
                { "list", new JArray{ arr } }
            };
            return getRes(res);
        }

        //
        public JArray queryTokenBalanceInfo(string projId, string address)
        {
            address = address.ToLower();
            var findStr = new JObject { { "projId", projId},{ "address", address} }.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] { "transferFrom", "transferTo", "onSetFdtIn", "onGetFdtOut", "onPreMint", "onPreMintAtEnd", "onVote", "onProcess" }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceBalanceCol, findStr, fieldStr);
            if(queryRes.Count == 0)
            {
                var zeroD = decimal.Zero.ToString();
                return getRes(new JObject {
                    { "lastBuyPrice", getLastPrice(projId, "OnBuy").formatEth().ToString() },
                    { "lastSellPrice", getLastPrice(projId, "OnSell").formatEth().ToString() },
                    { "tokenAmt", zeroD },
                    { "shareAmt", zeroD },
                    { "availableAmt", zeroD },
                    { "lockAmt", zeroD },
                    { "chg24h", get24hChg(projId).ToString() } });
            }
            var item = queryRes[0];
            var tokenAmt = item["transferTo"].ToString().formatDecimalDouble() - item["transferFrom"].ToString().formatDecimalDouble();
            var shareAmt = item["onSetFdtIn"].ToString().formatDecimalDouble() 
                - item["onGetFdtOut"].ToString().formatDecimalDouble()
                + item["onPreMintAtEnd"].ToString().formatDecimalDouble()
                - item["onVote"].ToString().formatDecimalDouble()
                + item["onProcess"].ToString().formatDecimalDouble()
                ;
            var lockAmt = item["onPreMint"].ToString().formatDecimalDouble()
                - item["onPreMintAtEnd"].ToString().formatDecimalDouble()
                + item["onVote"].ToString().formatDecimalDouble()
                - item["onProcess"].ToString().formatDecimalDouble()
                ;
            var res = new JObject {
                {"lastBuyPrice", getLastPrice(projId, "OnBuy").formatEth().formatEth().ToString() },
                {"lastSellPrice", getLastPrice(projId, "OnSell").formatEth().formatEth().ToString() },
                {"tokenAmt", tokenAmt.ToString() },
                {"shareAmt", shareAmt.ToString() },
                {"availableAmt",(tokenAmt + shareAmt).ToString() },
                {"lockAmt", lockAmt.ToString() },
                {"chg24h", get24hChg(projId).ToString() }
            };
            return getRes(res); ;
        }

        private decimal getLastPrice(string projId, string type="OnBuy")
        {
            var findStr = new JObject { { "projId", projId }, { "event", type } }.ToString();
            var sortStr = new JObject { { "blockNumber", -1 } }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, daoNotifyCol, findStr, sortStr, 0, 1);
            if (queryRes.Count == 0) return decimal.Zero;

            var item = queryRes[0];
            var tokenAmt = decimal.Parse(item["tokenAmt"].ToString());
            var fundAmt = decimal.Parse(item["fundAmt"].ToString());
            var price = fundAmt / tokenAmt;
            return decimal.Parse(price.ToString("#0.0000"));
        }
        private decimal get24hChg(string projId)
        {
            var findStr = new JObject { {"projId", projId},{ "$or", new JArray { new JObject { {"event", "OnBuy" } },new JObject { { "event", "OnSell" } } } } }.ToString();
            var fieldStr = new JObject { { "blockTime", 1 }, { "tokenAmt", 1 }, { "fundAmt", 1 } }.ToString();
            var sortStr = new JObject { { "blockNumber",-1} }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, daoNotifyCol, findStr, sortStr, 0, 1, fieldStr);
            if (queryRes.Count == 0)
            {
                return decimal.Zero;
            }

            var item = queryRes[0];
            var blockTime = long.Parse(item["blockTime"].ToString());
            var zeorTime = TimeHelper.GetTimeStampZero();
            if(blockTime < zeorTime)
            {
                return decimal.Zero;
            }
            var tokenAmt = decimal.Parse(item["tokenAmt"].ToString());
            var fundAmt = decimal.Parse(item["fundAmt"].ToString());
            if (tokenAmt == 0) tokenAmt = 1;
            var price = fundAmt / tokenAmt;

            findStr = new JObject { { "blockTime", new JObject { { "$lte", zeorTime - TimeHelper.OneDaySeconds } } }, { "projId", projId }, { "$or", new JArray { new JObject { { "event", "OnBuy" } }, new JObject { { "event", "OnSell" } } } } }.ToString();
            queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, daoNotifyCol, findStr, sortStr, 0, 1, fieldStr);
            if(queryRes.Count == 0)
            {
                return decimal.Zero;
            }
            var tokenAmtBf = decimal.Parse(item["tokenAmt"].ToString());
            var fundAmtBf = decimal.Parse(item["fundAmt"].ToString());
            if (tokenAmtBf == 0) tokenAmtBf = 1;
            var priceBf = fundAmtBf / tokenAmtBf;
            var ratio = (price - priceBf) / priceBf;
            return decimal.Parse((ratio * 100).ToString("#0.0000"));
        }

    }
    class FinanceType
    {
        public const string General = "gen"; // 普通融资
        public const string DAICO = "daico"; // daico融资
    }
    class RewardActiveState
    {
        public const string Valid_Yes = "1"; // 有效
        public const string Valid_Not = "0"; // 无效
    }
    
    class SelectKey
    {                                  // 是否预留代币    |   是否限制    | 预发时间  | 发放方式   
                                       // -------------------------------------------------------
        public const string Yes = "1"; // 预留                限制          定期        实物
        public const string Not = "0"; // 不定期              不限制        不定期      虚拟
    }

    class SkOp
    {
        public const string NotOp = "3";        // 未操作 
        public const string HandlingOp = "4";   // 处理中
        public const string FinishOp = "5";     // 已完成
    }
    class RecordType
    {
        public const string ByWeek = "w"; // 按周显示
        public const string ByMonth = "m"; // 按月显示
    }
}
