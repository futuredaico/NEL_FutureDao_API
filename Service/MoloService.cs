using NEL.NNS.lib;
using NEL_FutureDao_API.Service.Help;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace NEL_FutureDao_API.Service
{
    public class MoloService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string dao_projInfoCol { get; set; } = "daomolochprojinfos";
        public string dao_projmemberCol { get; set; } = "daomolochprojmembers";
        public string dao_projProposalCol { get; set; } = "daomolochprojproposals";

        //
        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);

        public JArray getProjList(int pageNum, int pageSize)
        {
            var findStr = "{}";
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, dao_projInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0},{ "list", new JArray()} });

            var fieldStr = new JObject { { "_id",0} }.ToString();
            var sortStr = "{'time':-1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, dao_projInfoCol, findStr, sortStr, (pageNum - 1) * pageSize, pageSize, fieldStr);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var rr = queryRes.Select(p => {
                var members = getProjTeamCount(p["projId"].ToString(), out long shares);
                var jo = new JObject();
                jo.Add("projId", p["projId"]);
                jo.Add("projName", p["projName"]);
                jo.Add("projType", p["projType"]);
                jo.Add("projBrief", p["description"]);
                jo.Add("projCoverUrl", "");
                jo.Add("shares", shares);
                jo.Add("members", members);
                return jo;
                
            });

            var res = new JObject { { "count", count }, { "list", new JArray { rr } } };
            return getRes(res);
        }
        private long getProjTeamCount(string projId, out long shares)
        {
            shares = 0;
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "memberAddress", 1 }, { "shares", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, dao_projmemberCol, findStr, fieldStr);

            var count = queryRes.Count;
            if(count > 0)
            {
                shares = queryRes.Sum(p => long.Parse(p["shares"].ToString()));
            }
            return count;
        }

        public JArray getProjDetail(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, dao_projInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];

            var members = getProjTeamCount(item["projId"].ToString(), out long shares);
            var jo = new JObject();
            jo.Add("projId", item["projId"]);
            jo.Add("projName", item["projName"]);
            jo.Add("projType", item["projType"]);
            jo.Add("projBrief", item["description"]);
            jo.Add("projDetail", "");
            jo.Add("projCoverUrl", "");
            jo.Add("projFundTotal", "0");
            jo.Add("projFundSymbol", "eth");
            jo.Add("shares", shares);
            jo.Add("valuePerShare", 0);
            jo.Add("projOfficialWeb", item["projUrl"]);
            jo.Add("discussCount", 0);
            jo.Add("members", members);

            var periodDuration = (long)item["periodDuration"];
            var votingPeriodDuration = (long)item["votingPeriodDuration"];
            var gracePeriodDuration = (long)item["gracePeriodDuration"];
            var votePeriod = periodDuration * votingPeriodDuration;
            var gracePeriod = periodDuration * gracePeriodDuration;
            jo.Add("votePeriod", votePeriod);
            jo.Add("gracePeriod", gracePeriod);
            return getRes(jo);
        }

        public JArray getProjProposalList(string projId, int pageNum, int pageSize, string address = "")
        {
            var findStr = new JObject { { "projId", projId} }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, dao_projProposalCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var sortStr = "{'timestamp':-1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, dao_projProposalCol, findStr, sortStr, (pageNum-1)*pageSize, pageSize);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var rr = queryRes.Select(p => {
                var jo = new JObject();
                jo.Add("projId", p["projId"]);
                jo.Add("proposalIndex", p["proposalIndex"]);
                jo.Add("proposalTitle", p["title"]);
                jo.Add("sharesRequested", p["sharesRequested"]);
                jo.Add("tokenTribute", p["tokenTribute"]);
                jo.Add("tokenTributeSymbol", "eth");
                jo.Add("timestamp", p["timestamp"]);
                jo.Add("yesShares", p["yesShares"]);
                jo.Add("noShares", p["noShares"]);
                jo.Add("hasVote", p["proposer"].ToString() == address);
                jo.Add("proposalState", ProposalState.Voting);
                return jo;
            });
            return getRes(new JObject { { "count", count }, { "list", new JArray { rr } } });
        }
        public JArray getProjProposalDetail(string projId, string proposalIndex)
        {
            var findStr = new JObject { {"projId", projId},{ "proposalIndex", proposalIndex } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, dao_projProposalCol, findStr);
            if (queryRes.Count == 0) getRes();

            var item = queryRes[0];
            var jo = new JObject();
            jo["projId"] = projId;
            jo["proposalIndex"] = proposalIndex;
            jo["proposalTitle"] = item["title"];
            jo["proposer"] = item["proposer"];
            jo["username"] = "";
            jo["headIconUrl"] = "";
            jo["proposalDetail"] = item["description"];
            jo.Add("sharesRequested", item["sharesRequested"]);
            jo.Add("tokenTribute", item["tokenTribute"]);
            jo.Add("tokenTributeSymbol", "eth");
            jo.Add("applicant", item["applicant"]);
            jo.Add("applicantUsername", "");
            jo.Add("applicantHeadIconUrl", "");
            return getRes(jo);
        }
        public JArray getProjMemberList(string projId, int pageNum, int pageSize)
        {
            var findStr = new JObject { {"projId", projId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, dao_projmemberCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0},{ "list", new JArray()} });

            var sortStr = "{'_id': -1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, dao_projmemberCol, findStr, sortStr, (pageNum-1)*pageSize, pageSize);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var rr = queryRes.Select(p => {
                var jo = new JObject();
                jo.Add("username","");
                jo.Add("headIconUrl", "");
                jo.Add("address", p["memberAddress"]);
                jo.Add("shares", p["shares"]);
                return jo;
            });
            return getRes(new JObject { { "count", count},{ "list", new JArray { rr} } });
        }

    }
    class ProposalState
    {
        public const string Voting = "10151";    // 投票中
        public const string Public = "10152";    // 公式中
        public const string Passed = "10153";    // 已通过
        public const string NotPassed= "10154";  // 未通过
    }
}
