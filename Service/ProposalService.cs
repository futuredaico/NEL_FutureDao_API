using NEL.NNS.lib;
using NEL_FutureDao_API.Service.Help;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace NEL_FutureDao_API.Service
{
    public class ProposalService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr {get;set;}
        public string dao_mongodbDatabase { get; set; }
        public string projProposalCol { get; set; } = "daoprojproposalinfos";
        public string userInfoCol { get; set; } = "daouserinfos";

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);


        public JArray queryProposalList(string projId, int pageNum, int pageSize)
        {
            var findJo = new JObject { { "projId", projId } };
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projProposalCol, findJo.ToString());
            if(count == 0)
            {
                return getRes(new JObject { { "count", count },{ "list", new JArray()} });
            }
            var match = new JObject { { "$match", findJo } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "time", 1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var lookup = new JObject { { "$lookup", new JObject {
                { "from", userInfoCol},
                { "localField", "address"},
                { "foreignField", "ethAddress" },
                { "as", "us"}
            } } }.ToString();
            var project = new JObject { { "$project", new JObject { { "ps", 1 } } } }.ToString();
            var list = new List<string> { match, sort, skip, limit, lookup, project };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projProposalCol, list);
            if(queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", queryRes } });
            }
            var res = queryRes.Select(p => new JObject {
                {"proposalId",p["proposalId"]},
                {"proposalName",p["proposalName"]},
                {"proposalFundAmt",p["proposalFundAmt"]},
                {"address",p["address"]},
                {"distributeWay",p["distributeWay"]},
                {"proposalDetail",p["proposalDetail"]},
                {"headIconUrl",(p["us"] as JArray)[0]["headIconUrl"]},
            }).ToArray();

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        public JArray queryProposalDetail(string projId, string proposalId)
        {
            var findStr = new JObject { { "proposalId", proposalId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projProposalCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var headIconUrl = getHeadIconUrl(item["address"].ToString(), out string userId, out string userName);
            var res = new JObject {
                 {"proposalId",item["proposalId"]},
                {"proposalName",item["proposalName"]},
                {"proposalFundAmt",item["proposalFundAmt"]},
                {"address",item["address"]},
                {"distributeWay",item["distributeWay"]},
                {"proposalDetail",item["proposalDetail"]},
                {"headIconUrl",headIconUrl},
                {"userId",userId},
                {"userName",userName},
            };
            return getRes(res);
        }
        private string getHeadIconUrl(string address, out string userId, out string userName)
        {
            userId = "";
            userName = "";
            var findStr = new JObject { { "ethAddress", address } }.ToString();
            var fieldStr = new JObject { { "headIconUrl", 1 },{ "userId",1},{ "userName",1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return "";

            var item = queryRes[0];
            userId = item["userId"].ToString();
            userName = item["userName"].ToString();
            return queryRes[0]["headIconUrl"].ToString();
        }

        public JArray queryVoteInfo()
        {
            return null;
        }

    }

    class VoteState
    {                               // 投票选择     投票状态
        public string Not = "0";    // 反对         投票中
        public string Yes = "1";    // 赞成         
    }
    class VoteKey
    {

    }

}
