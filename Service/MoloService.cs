using Microsoft.AspNetCore.Mvc;
using NEL.NNS.lib;
using NEL_FutureDao_API.Service.Help;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
        public string projMoloDiscussInfoCol { get; set; } = "daomolodiscussinfos";
        public string projMoloDiscussZanInfoCol { get; set; } = "daomolodiscusszaninfos";
        public string userInfoCol { get; set; } = "daouserinfos";

        public UserServiceV3 us { get; set; }

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




        //molo.discuss
        private string getRootId(string preDiscussId, string discussId = "")
        {
            if (preDiscussId == "") return discussId;

            string findStr = new JObject { { "discussId", preDiscussId } }.ToString();
            string fieldStr = new JObject { { "rootId", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return discussId;
            return queryRes[0]["rootId"].ToString();
        }
        // 添加评论
        public JArray addMoloDiscuss(Controller controller, string projId, string preDiscussId, string discussContent)
        {
            if(!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var discussId = DaoInfoHelper.genProjMoloDiscussId(projId, preDiscussId, discussContent, userId);
            var rootId = getRootId(preDiscussId, discussId);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                {"projId", projId },
                {"preDiscussId", preDiscussId },
                {"discussId", discussId },
                {"discussContent", discussContent },
                {"userId", userId },
                {"zanCount", 0 },
                {"rootId", rootId },
                {"time", now },
                {"lastUpdateTime", now },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, newdata);
            return getRes(new JObject { { "discussId", discussId } });
        }
        // 点赞评论
        public JArray zanMoloDiscuss(Controller controller, string projId, string discussId)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "discussId", discussId } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussZanInfoCol, findStr) == 0)
            {
                var newdata = new JObject {
                    {"projId", projId },
                    {"discussId", discussId },
                    {"userId", userId },
                    {"time", TimeHelper.GetTimeStamp() },
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussZanInfoCol, newdata);
            }
            return getRes();
        }
        // 查看单条评论
        public JArray getMoloDiscuss(string projId, string curId)
        {
            var findStr = new JObject { { "projId", projId }, { "discussId", curId } }.ToString();
            var fieldStr = new JObject { { "preDiscussId", 1 }, { "discussContent", 1 }, { "zanCount", 1 }, { "time", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            return getRes(item);
        }
        // 一级评论列表
        public JArray getMoloDiscussList(Controller controller, string projId, int pageNum = 1, int pageSize = 10)
        {
            //
            var findJo = new JObject { { "projId", projId }, { "preDiscussId", "" } };
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, findJo.ToString());
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });
            //
            var match = new JObject { { "$match", findJo } }.ToString();
            var lookup = new JObject{{"$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "userId" },
                {"foreignField", "userId" },
                { "as", "us"}
            } } }.ToString();
            var project = new JObject { { "$project", new JObject {
                { "_id", 0 },
                { "preDiscussId", 1 },
                { "discussId", 1 },
                { "discussContent", 1 },
                { "userId", 1 },
                { "zanCount", 1 },
                { "rootId", 1 },
                { "time", 1 },
                { "us.username", 1 },
                { "us.headIconUrl", 1 },
            } } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "time", -1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var list = new List<string>
            {
                match, lookup, sort, skip, limit, project
            };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            // 是否已点赞
            var userId = getUserId(controller);
            var idArr = queryRes.Select(p => p["discussId"].ToString()).Distinct().ToArray();
            var zanDict = getIsZanDict(idArr, userId);

            // 子评论数量
            var rootIdArr = queryRes.Select(p => p["rootId"].ToString()).Distinct().ToArray();
            var rootIdDict = getSubSizeDict(rootIdArr);

            var res = queryRes.Select(p => {
                var jo = (JObject)p;
                var id = jo["discussId"].ToString();
                var cid = jo["rootId"].ToString();
                jo["username"] = ((JArray)jo["us"])[0]["username"].ToString();
                jo["headIconUrl"] = ((JArray)jo["us"])[0]["headIconUrl"].ToString();
                jo.Remove("us");
                jo["isZan"] = zanDict.GetValueOrDefault(id, false);
                var subSize = rootIdDict.GetValueOrDefault(cid, 0);
                if (subSize > 0) subSize -= 1;
                jo["subSize"] = subSize;
                return jo;
            }).ToArray();

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        // 二级评论列表
        public JArray getMoloSubDiscussList(Controller controller, string rootId, int pageNum = 1, int pageSize = 10)
        {
            var findJo = new JObject { { "rootId", rootId }, { "preDiscussId", new JObject { { "$ne", "" } } } };
            var findStr = findJo.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var match = new JObject { { "$match", findJo } }.ToString();
            var lookup = new JObject{{"$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "userId" },
                {"foreignField", "userId" },
                { "as", "us"}
            } } }.ToString();
            var project = new JObject { { "$project", new JObject {
                { "_id", 0 },
                { "preDiscussId", 1 },
                { "discussId", 1 },
                { "discussContent", 1 },
                { "userId", 1 },
                { "zanCount", 1 },
                { "rootId", 1 },
                { "time", 1 },
                { "us.username", 1 },
                { "us.headIconUrl", 1 },
            } } }.ToString();
            var list = new List<string> { match, lookup, project };
            // 
            lookup = new JObject{{"$lookup", new JObject {
                {"from", projMoloDiscussInfoCol },
                {"localField", "preDiscussId" },
                {"foreignField", "discussId" },
                { "as", "preDiscuss"}
            } } }.ToString();
            project = new JObject { { "$project", new JObject {
                { "_id", 0 },
                { "preDiscussId", 1 },
                { "discussId", 1 },
                { "discussContent", 1 },
                { "userId", 1 },
                { "zanCount", 1 },
                { "rootId", 1 },
                { "time", 1 },
                { "us.username", 1 },
                { "us.headIconUrl", 1 },
                { "preDiscuss.userId",1}
            } } }.ToString();
            list.Add(lookup);
            list.Add(project);
            //
            lookup = new JObject{{"$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "preDiscuss.userId" },
                {"foreignField", "userId" },
                { "as", "preUs"}
            } } }.ToString();
            project = new JObject { { "$project", new JObject {
                { "_id", 0 },
                { "discussId", 1 },
                { "discussContent", 1 },
                { "userId", 1 },
                { "zanCount", 1 },
                { "time", 1 },
                { "us.username", 1 },
                { "us.headIconUrl", 1 },
                { "preUs.userId",1},
                { "preUs.username",1}
            } } }.ToString();
            list.Add(lookup);
            list.Add(project);
            var sort = new JObject { { "$sort", new JObject { { "time", -1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            list.Add(sort);
            list.Add(skip);
            list.Add(limit);

            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            // 是否已点赞
            var userId = getUserId(controller);
            var idArr = queryRes.Select(p => p["discussId"].ToString()).Distinct().ToArray();
            var zanDict = getIsZanDict(idArr, userId);

            var res = queryRes.Select(p =>
            {
                var jo = (JObject)p;
                jo["username"] = ((JArray)jo["us"])[0]["username"].ToString();
                jo["headIconUrl"] = ((JArray)jo["us"])[0]["headIconUrl"].ToString();
                jo["isZan"] = zanDict.GetValueOrDefault(jo["discussId"].ToString(), false);
                jo["preUserId"] = ((JArray)jo["preUs"])[0]["userId"].ToString();
                jo["preUsername"] = ((JArray)jo["preUs"])[0]["username"].ToString();
                jo.Remove("us");
                jo.Remove("preUs");
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        //
        private string getUserId(Controller controller)
        {
            var userId = controller.Request.Cookies["userId"];
            if (userId == null || userId == "") return "";
            var ss = userId.Split("_");
            if (ss == null || ss.Count() == 0) return "";

            userId = ss[0];
            return userId;
        }
        private Dictionary<string, bool> getIsZanDict(string[] discussIdArr, string userId)
        {
            var joArr = discussIdArr.Select(p => new JObject { { "discussId", p }, { "userId", userId } }).ToArray();
            var findStr = new JObject { { "$or", new JArray { joArr } } }.ToString();
            var fieldStr = new JObject { { "discussId", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussZanInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return new Dictionary<string, bool>();
            return queryRes.ToDictionary(k => k["discussId"].ToString(), v => true);
        }
        private Dictionary<string, long> getSubSizeDict(string[] rooIdArr)
        {
            var match = new JObject { { "$match", MongoFieldHelper.toFilter(rooIdArr, "rootId") } }.ToString();
            var group = new JObject { { "$group", new JObject { { "_id", "$rootId" }, { "sum", new JObject { { "$sum", 1 } } } } } }.ToString();
            var list = new List<string> { match, group };
            var subRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projMoloDiscussZanInfoCol, list);
            if (subRes.Count == 0) return new Dictionary<string, long>();
            return subRes.ToDictionary(k => k["_id"].ToString(), v => long.Parse(v["sum"].ToString()));
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
