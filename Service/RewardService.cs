using NEL.NNS.lib;
using NEL_FutureDao_API.Service.Help;
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
        private string projFinanceOrderCol { get; set; } = "daoprojfinanceorderinfos";
        private string projFinanceRewardCol { get; set; } = "daoprojfinancerewardinfos";

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        public JArray applyBuyOrder(
            string userId,
            string token,
            string projId,
            string rewardId,
            string rewardName,
            string price,
            string amt,
            string totalPrice,
            string senderName,
            string senderTel,
            string senderNote,
            string connectorName,
            string connectorTel,
            string connectorEmail,
            string connectorAddress,
            string connectorMessage
            )
        {

            var findStr = new JObject { { "rewardId", rewardId }}.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardCol, findStr);
            if(queryRes.Count == 0 
                || queryRes[0]["activeState"].ToString() != RewardActiveState.Valid_Yes
                || queryRes[0]["projId"].ToString() != projId
                )
            {
                // 无效订单
                return getErrorRes("");
            }
            var item = queryRes[0];

            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "rewardId", rewardId},
                { "orderId", DaoInfoHelper.genProjRewardOrderId(projId, rewardId, userId)},
                { "orderState", OrderState.WaitingPay},
                { "rewardName", rewardName},
                { "price", price},
                { "amount", amt},
                { "totalPrice", totalPrice},
                { "senderName", senderName},
                { "senderTel", senderTel},
                { "senderNote", senderNote},
                { "connectorName", connectorName},
                { "connectorTel", connectorTel},
                { "connectorEmail", connectorEmail},
                { "connectorAddress", connectorAddress},
                { "connectorMessage", connectorMessage},
                { "txid", ""},
                { "userId", userId},
                { "originInfo", item},
                { "time",  now},
                { "lastUpdateTime",  now}
            };
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, newdata);
            return getRes();
        }

        public JArray hasPayOrder(string userId, string token, string orderId, string txid)
        {
            var findStr = new JObject { { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if(queryRes.Count == 0
                || queryRes[0]["userId"].ToString() != userId)
            {
                // 无效订单
                return getErrorRes("");
            }

            var item = queryRes[0];
            if(item["orderState"].ToString() != OrderState.WaitingPay)
            {
                // 无效操作
                return getErrorRes("");
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
        public JArray cancelPayOrder(string userId, string token, string orderId)
        {
            var findStr = new JObject { { "orderId", orderId} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderCol, findStr);
            if(queryRes.Count == 0
                || queryRes[0]["userId"].ToString() != userId)
            {
                // 无效订单
                return getErrorRes("");
            }

            var item = queryRes[0];
            if(item["orderState"].ToString() != OrderState.WaitingPay)
            {
                // 无效操作
                return getErrorRes("");
            }

            return getRes();
        }
        public JArray queryBuyOrder(string userId, string token)
        {
            return null;
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
