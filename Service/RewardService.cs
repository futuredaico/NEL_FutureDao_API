using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;

namespace NEL_FutureDao_API.Service
{
    public class RewardService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string projInfoCol { get; set; } = "daoprojinfos";
        public string projTeamInfoCol { get; set; } = "daoprojteaminfos";
        public string projFinanceCol { get; set; } = "daoprojfinanceinfos";
        public string projFinanceOrderCol { get; set; } = "daoprojfinanceorderinfos";
        public string projFinanceRewardCol { get; set; } = "daoprojfinancerewardinfos";
        public string tokenUrl { get; set; } = "";

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        public JArray initBuyOrder(
            string userId,
            string accessToken,
            string projId,
            string rewardId,
            string amount,
            string rewardAmount,
            string connectorName,
            string connectorTel,
            string connectorAddress,
            string connectorEmail,
            string connectorMessage
            )
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "rewardId", rewardId }}.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, findStr);
            if(queryRes.Count == 0 
                || queryRes[0]["activeState"].ToString() != RewardActiveState.Valid_Yes
                || queryRes[0]["projId"].ToString() != projId
                )
            {
                // 无效回报id
                return getErrorRes(DaoReturnCode.Invalid_RewardId);
            }
            var item = queryRes[0];
            
            if(!getProjTokenName(projId, out string tokenName, out string fundName))
            {
                return getErrorRes(DaoReturnCode.S_InvalidProjId);
            }
            string projName = getProjName(projId);
            var orderId = DaoInfoHelper.genProjRewardOrderId(projId, rewardId, userId);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "projName", projName},
                { "rewardId", rewardId},
                { "orderId", orderId},
                { "orderState", OrderState.WaitingPay},
                { "rewardName", item["rewardName"]},
                { "price", item["price"]},
                { "priceUnit", fundName },
                { "amount", amount},
                { "totalCost", (decimal.Parse(item["price"].ToString()) * decimal.Parse(amount)).ToString()},
                { "totalCostUnit", fundName },
                { "rewardAmount", rewardAmount },
                { "rewardAmountUnit", tokenName },
                { "senderNote", ""},
                { "connectorName", connectorName},
                { "connectorTel", connectorTel},
                { "connectorEmail", connectorEmail},
                { "connectorAddress", connectorAddress},
                { "connectorMessage", connectorMessage},
                { "distributeWay", item["distributeWay"]},
                { "txid", ""},
                { "userId", userId},
                { "originInfo", item},
                { "markTime",  now},
                { "time",  now},
                { "lastUpdateTime", now}
            };
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, newdata);
            return getRes(new JObject { { "orderId", orderId},{ "time", now} });
        }
        private bool getProjTokenName(string projId, out string tokenName, out string fundName)
        {
            tokenName = "";
            fundName = "";
            var findStr = new JObject { { "projId", projId} }.ToString();
            var fieldStr = new JObject { { "tokenSymbol", 1 }, { "fundName", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            tokenName = item["tokenSymbol"].ToString();
            fundName = item["fundName"].ToString();
            return true;
        }
        private string getProjName(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "projName", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return "";
            return queryRes[0]["projName"].ToString();
        }

        public JArray confirmBuyOrder(string userId, string accessToken, string orderId, string txid)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }

            var findStr = new JObject { { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if(queryRes.Count == 0
                || queryRes[0]["userId"].ToString() != userId)
            {
                // 无效订单id
                return getErrorRes(DaoReturnCode.Invalid_OrderId);
            }

            var item = queryRes[0];
            if(item["orderState"].ToString() != OrderState.WaitingPay)
            {
                // 无效操作
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            if(item["txid"].ToString() != txid)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "orderState", OrderState.WaitingConfirm},
                    { "txid", txid},
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray cancelBuyOrder(string userId, string accessToken, string orderId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "orderId", orderId} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if(queryRes.Count == 0
                || queryRes[0]["userId"].ToString() != userId)
            {
                // 无效订单id
                return getErrorRes(DaoReturnCode.Invalid_OrderId);
            }

            var item = queryRes[0];
            if(item["orderState"].ToString() != OrderState.WaitingPay)
            {
                // 无效操作
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var updateStr = new JObject { { "$set", new JObject { { "orderState", OrderState.Canceled},{ "markTime", TimeHelper.GetTimeStamp()} } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, updateStr, findStr);
            return getRes();
        }
        public JArray confirmDeliverBuyOrder(string userId, string accessToken, string projId, string orderId, string note)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }

            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var findStr = new JObject { { "projId", projId }, { "orderId", orderId } }.ToString();
            var fieldStr = new JObject { { "orderState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.Invalid_OrderId);

            var item = queryRes[0];
            if (item["orderState"].ToString() != OrderState.WaitingDeliverGoods)
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var updateStr = new JObject { { "$set", new JObject { { "orderState", OrderState.hasDeliverGoods }, { "senderNote", note } } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, updateStr, findStr);
            return getRes();
        }

        public JArray queryBuyOrderList(string userId, string accessToken, int pageNum, int pageSize)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "userId", userId} }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count},{ "list", new JArray()} });

            var sortStr = new JObject { { "time", -1} }.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "rewardId", "rewardName","orderId",
                "price","priceUnit","amount","totalCost","totalCostUnit","orderState","time","connectorName"
            }).ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count},{ "list", queryRes} });

            return getRes(new JObject { { "count", count},{ "list", queryRes } });
        }
        public JArray queryBuyOrder(string userId, string accessToken, string projId, string orderId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId }, { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if (queryRes.Count == 0 || queryRes[0]["userId"].ToString() != userId) return getRes();

            var item = queryRes[0];
            getProjRewardConnector(projId, out string senderName, out string senderTel);

            var res = new JObject {
                {"projId", projId},
                {"projName", item["projName"]},
                {"rewardId", item["rewardId"]},
                {"rewardName", item["rewardName"]},
                {"orderId", item["orderId"]},
                {"orderState", item["orderState"]},
                {"price", item["price"]},
                {"priceUnit", item["priceUnit"]},
                {"amount", item["amount"]},
                {"totalCost", item["totalCost"]},
                {"totalCostUnit", item["totalCostUnit"]},
                {"time", item["time"]},
                { "connectorName", item["connectorName"]},
                { "connectorTel", item["connectorTel"]},
                { "connectorAddress", item["connectorAddress"]},
                { "connectorEmail", item["connectorEmail"]},
                { "connectorMessage", item["connectorMessage"]},
                { "senderName", senderName},
                { "senderTel", senderTel},
                { "senderNote", item["senderNote"]},
            };
            return getRes(res);
        }

        public JArray queryProjBuyOrderList(string userId, string accessToken, string projId, int pageNum, int pageSize, int isDelivery, string buyerName, string orderId, int orderType)
        {
            if(orderId != "")
            {
                var sRes = queryProjBuyOrder(userId, accessToken, projId, orderId);
                var sCnt = sRes.Count;
                return getRes(new JObject { { "count", sCnt},{ "list", sRes} });
            }

            // 
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var findJo = new JObject { { "projId", projId } };
            // 待发货/已发货(没有则默认待发货)
            if (isDelivery == 0)
            {
                findJo.Add("orderState", OrderState.WaitingDeliverGoods);
            } else if(isDelivery == 1)
            {
                findJo.Add("orderState", OrderState.hasDeliverGoods);
            } else
            {
                findJo.Add("orderState", OrderState.WaitingDeliverGoods);
            }

            // 买价姓名(没有则为全部)
            if(buyerName != "")
            {
                findJo.Add("connectorName", buyerName);
            }

            // 订单编号(已优先处理)

            // 订单发放类型(没有则为全部)
            if (orderType == 0)
            {
                findJo.Add("distributeWay", SelectKey.Not);
            } else if(orderType == 1)
            {
                findJo.Add("distributeWay", SelectKey.Yes);
            }
            //
            var findStr = findJo.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var sortStr = new JObject { { "time", -1 } }.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "rewardId", "rewardName","orderId",
                "price","priceUnit","amount","totalCost","totalCostUnit","orderState","time","connectorName"
            }).ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });

            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }
        public JArray queryProjBuyOrderList(string userId, string accessToken, string projId, int pageNum, int pageSize)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }

            if(!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var findStr = new JObject { { "projId", projId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var sortStr = new JObject { { "time", -1 } }.ToString();
            var fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "rewardId", "rewardName","orderId",
                "price","priceUnit","amount","totalCost","totalCostUnit","orderState","time","connectorName"
            }).ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });

            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }
        public JArray queryProjBuyOrder(string userId, string accessToken, string projId, string orderId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }

            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var findStr = new JObject { { "projId", projId }, { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            getProjRewardConnector(projId, out string senderName, out string senderTel);

            var res = new JObject {
                {"projId", projId},
                {"projName", item["projName"]},
                {"rewardId", item["rewardId"]},
                {"rewardName", item["rewardName"]},
                {"orderId", item["orderId"]},
                {"orderState", item["orderState"]},
                {"price", item["price"]},
                {"priceUnit", item["priceUnit"]},
                {"amount", item["amount"]},
                {"totalCost", item["totalCost"]},
                {"totalCostUnit", item["totalCostUnit"]},
                {"time", item["time"]},
                { "connectorName", item["connectorName"]},
                { "connectorTel", item["connectorTel"]},
                { "connectorAddress", item["connectorAddress"]},
                { "connectorEmail", item["connectorEmail"]},
                { "connectorMessage", item["connectorMessage"]},
                { "senderName", senderName},
                { "senderTel", senderTel},
                { "senderNote", item["senderNote"]},
            };
            return getRes(res);
        }
        private bool isProjMember(string projId, string userId, bool isNeedAdmin = false)
        {
            if (userId == "") return false;
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 }, { "role", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return false;
            }
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            if (isNeedAdmin)
            {
                return item["emailVerifyState"].ToString() == EmailState.hasVerifyAtInvitedYes
                    && item["role"].ToString() == TeamRoleType.Admin;
            }
            return item["emailVerifyState"].ToString() == EmailState.hasVerifyAtInvitedYes;
        }
        private bool getProjRewardConnector(string projId, out string connectorName, out string connectorTel)
        {
            connectorName = "";
            connectorTel = "";
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "connectorName", 1 }, { "connectorTel",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            connectorName = item["connectorName"].ToString();
            connectorTel = item["connectorTel"].ToString();
            return true;
        }

    }
}
