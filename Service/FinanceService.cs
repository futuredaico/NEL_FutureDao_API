using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;
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
        public string projInfoCol { get; set; } = "daoProjInfo";
        public string projTeamInfoCol { get; set; } = "daoProjTeamInfo";
        public string projFinanceCol { get; set; } = "daoProjFinanceInfo";
        public string projRewardCol { get; set; } = "daoProjRewardInfo";
        public string projFundCol { get; set; } = "daoProjFundInfo";
        public string tokenUrl { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        private bool checkToken(string userId, string accessToken, out string code)
            => TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out code);

        public  JArray publishContract(string userId, string accessToken, 
            string projId, 
            string type, 
            string platform, 
            string token, 
            string projTokenName, 
            string projTokenSymbol, 
            string reserveTokenFlag, 
            JObject reserveTokenInfo) {
            // TODO: 参数检查

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
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            findStr = new JObject { { "projId", projId } }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "type", type},
                { "platform", platform},
                { "tokenName", token},
                { "projTokenName", projTokenName},
                { "projTokenSymbol", projTokenSymbol},
                { "reserveTokenFlag", reserveTokenFlag},
                { "reserveTokenInfo", reserveTokenInfo},
                { "contractTxid", ""},
                { "contractHash", ""},
                { "connectorName", ""},
                { "connectTel", ""},
                { "reserveFundRatio", "0"},
                { "financeStartFlag", ""},
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
                "projId","type", "platform", "token","projTokenName","projTokenSymbol",
                "reserveTokenFlag","reserveTokenInfo","contractTxid","contractHash"
            }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();
            return getRes(queryRes[0]);
        }
        public JArray saveReward(string userId, string accessToken, string projId, string connectorName, string connectTel, JObject info)
        {
            // TODO: 参数检查

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
            string fieldStr = new JObject { { "connectorName", 1 }, { "connectTel", 1 },{ "tokenName",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            if (queryRes[0]["connectorName"].ToString() != connectorName
                || queryRes[0]["connectTel"].ToString() != connectTel)
            {
                string updateStr = new JObject { { "$set", new JObject { { "connectorName", connectorName }, { "connectTel", connectTel } } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, updateStr, findStr);
            }
            //
            string tokenName = queryRes[0]["tokenName"].ToString();
            var rewardList = (JArray)info["info"];

            // TODO: 增删改查
            var nlist = new List<JToken>();
            findStr = new JObject { { "projId", projId }, { "activeState", RewardActiveState.Valid_Yes } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projRewardCol, findStr);
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
                        && item["giftTokenPrice"].ToString() == tItem["giftTokenPrice"].ToString()
                        && item["limitFlag"].ToString() == tItem["limitFlag"].ToString()
                        && item["limitMax"].ToString() == tItem["limitMax"].ToString()
                        && item["distributeTimeFlag"].ToString() == tItem["distributeTimeFlag"].ToString()
                        && item["distributeTime"].ToString() == tItem["distributeTime"].ToString()
                        && item["distributeWay"].ToString() == tItem["distributeWay"].ToString()
                        ;
                    if (eq) continue;
                    var updateStr = new JObject { { "$set", new JObject { { "activeState", RewardActiveState.Valid_Not } } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projRewardCol, updateStr, findStr);
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
                        mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projRewardCol, updateStr, findStr);
                    }
                }
            }
            //
            var res = nlist.Select(p =>
            {
                p["projId"] = projId;
                p["rewardId"] = DaoInfoHelper.genProjRewardId(projId, p["rewardName"].ToString());
                p["giftTokenName"] = tokenName;
                p["activeState"] = RewardActiveState.Valid_Yes;
                return p;
            }).ToArray();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projRewardCol, new JArray { res });
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
                "rewardId", "projId","name","desc", "giftTokenName","giftTokenPrice",
                "limitFlag","limitMax", "distributeTimeFlag","distributeTime","distributeWay","note"
            }).ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projRewardCol, findStr, fieldStr);
            var res = new JObject { { "connectorName", connectorName }, { "connectTel", connectTel },{"info", queryRes} };
            return getRes(res);
        }
        public JArray applyFinanceFund(string userId, string accessToken, string projId, string txid)
        {
            string findStr = new JObject { { "projId", projId},{ "txid", txid } }.ToString();
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
            string fieldStr = new JObject { { "managePoolTotal", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFundCol, findStr, fieldStr);
            string poolTotal = "0";
            if(queryRes.Count > 0)
            {
                poolTotal = queryRes[0]["managePoolTotal"].ToString();
            }
            return getRes(new JObject { { "poolTotal", poolTotal} });
        }
        public JArray queryReserveFundRatio(string userId, string accessToken, string projId)
        {
            if (!checkToken(userId, accessToken, out string code)) return getErrorRes(code);

            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = new JObject { { "reserveFundRatio", 1 },{ "_id",0} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            string ratio = "0";
            if (queryRes.Count > 0)
            {
                ratio = queryRes[0]["reserveFundRatio"].ToString();
            } 
            return getRes(new JObject { { "ratio", ratio} });
        }
        public JArray queryContractHash(string userId, string accessToken, string projId)
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
            string fieldStr = new JObject { { "contractHash", 1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();
            return getRes(queryRes[0]["contractHash"]);
        }

        public JArray startFinance(string userId, string accessToken, string projId)
        {
            return null;
        }

        public JArray queryProjContract(string userId, string accessToken, string projId)
        {
            return null;
        }
        public JArray queryTokenHistPrice(string userId, string accessToken, string projId)
        {
            return null;
        }
        public JArray queryRewardList(string userId, string accessToken, string projId)
        {
            return null;
        }
        public JArray queryReserveToken(string userId, string accessToken, string projId)
        {
            return null;
        }
    }
    class RewardActiveState
    {
        public const string Valid_Yes = "1"; // 有效
        public const string Valid_Not = "0"; // 无效
    }
}
