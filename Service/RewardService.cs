using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NEL_FutureDao_API.Service
{
    public class RewardService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        private string projFinanceCol { get; set; } = "daoprojfinanceinfos";
        private string projFinanceOrderCol { get; set; } = "daoprojfinanceorderinfos";
        private string projFinanceRewardCol { get; set; } = "daoprojfinancerewardinfos";
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
            var orderId = DaoInfoHelper.genProjRewardOrderId(projId, rewardId, userId);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "rewardId", rewardId},
                { "orderId", orderId},
                { "orderState", OrderState.WaitingPay},
                { "rewardName", item["rewardName"]},
                { "price", item["price"]},
                { "priceUnit", fundName },
                { "amount", amount},
                { "totalCost", decimal.Parse(item["price"].ToString()) * decimal.Parse(amount)},
                { "totalCostUnit", fundName },
                { "rewardAmount", rewardAmount },
                { "rewardAmountUnit", tokenName },
                { "senderNote", ""},
                { "connectorName", connectorName},
                { "connectorTel", connectorTel},
                { "connectorEmail", connectorEmail},
                { "connectorAddress", connectorAddress},
                { "connectorMessage", connectorMessage},
                { "txid", ""},
                { "userId", userId},
                { "originInfo", item},
                { "time",  now},
                { "lastUpdateTime", now}
            };
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, newdata);
            return getRes(new JObject { { "orderId", orderId} });
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
            var updateStr = new JObject { { "$set", new JObject { { "orderState", OrderState.Canceled} } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, updateStr, findStr);
            return getRes();
        }
        public JArray queryBuyOrder(string userId, string token, string orderId)
        {
            return getRes();
        }

    }
    class OrderState
    {
        // 等待付款 + 等待确认 + 等待发货 + 已发货 + 取消订单 + 付款超时 + 交易失败
        public const string WaitingPay = "";
        public const string WaitingConfirm = "";
        public const string WaitingDeliverGoods = "";
        public const string hasDeliverGoods = "";
        public const string Canceled = "";
        public const string PayTimeout = "";
        public const string TxFailed = "";
    }
}
