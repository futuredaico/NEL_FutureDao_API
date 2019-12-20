﻿using Microsoft.AspNetCore.Mvc;
using NEL.NNS.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NEL_FutureDao_API.Service
{
    public class MoloService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string projMoloInfoCol { get; set; } = "moloprojinfos";
        public string projMoloHashInfoCol { get; set; } = "moloprojhashinfos";
        public string projMoloBalanceInfoCol { get; set; } = "moloprojbalanceinfos";
        public string projMoloDiscussInfoCol { get; set; } = "moloprojdiscussinfos";
        public string projMoloDiscussZanInfoCol { get; set; } = "moloprojdiscusszaninfos";
        public string projMoloProposalInfoCol { get; set; } = "moloproposalinfos";
        public string projMoloProposalDiscussInfoCol { get; set; } = "moloproposaldiscussinfos";
        public string projMoloProposalDiscussZanInfoCol { get; set; } = "moloproposaldiscusszaninfos";
        public string userInfoCol { get; set; } = "daouserinfos";

        public UserServiceV3 us { get; set; }

        //
        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        // 项目
        public JArray getProjList(int pageNum, int pageSize)
        {
            var findStr = "{}";
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0},{ "list", new JArray()} });

            var fieldStr = new JObject { { "_id",0} }.ToString();
            var sortStr = "{'time':-1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr, sortStr, (pageNum - 1) * pageSize, pageSize, fieldStr);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var rr = queryRes.Select(p => {
                var members = getProjTeamCount(p["projId"].ToString(), out long shares);
                var jo = new JObject();
                jo.Add("projId", p["projId"]);
                jo.Add("projName", p["projName"]);
                jo.Add("projType", p["projType"]);
                jo.Add("projBrief", p["projBrief"]);
                jo.Add("projCoverUrl", p["projCoverUrl"]);
                jo.Add("shares", shares);
                jo.Add("members", members);
                return jo;
            }).ToArray();

            var res = new JObject { { "count", count }, { "list", new JArray { rr } } };
            return getRes(res);
        }
        private long getProjTeamCount(string projId, out long shares)
        {
            shares = 0;
            var findStr = new JObject { { "projId", projId },{ "proposalIndex",""} }.ToString();
            var fieldStr = new JObject { { "balance", 1 }}.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr, fieldStr);
            var count = queryRes.Where(p => long.Parse(p["balance"].ToString()) > 0).ToArray().Count();
            if(count > 0)
            {
                shares = queryRes.Sum(p => long.Parse(p["balance"].ToString()));
            }
            return count;
        }
        public JArray getProjDetail(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];

            var members = getProjTeamCount(item["projId"].ToString(), out long shares);
            var jo = new JObject();
            jo.Add("projId", item["projId"]);
            jo.Add("projName", item["projName"]);
            jo.Add("projType", item["projType"]);
            jo.Add("projBrief", item["projBrief"]);
            jo.Add("projDetail", item["projDetail"]);
            jo.Add("projCoverUrl", item["projCoverUrl"]);
            jo.Add("officailWeb", item["officailWeb"]);
            var fundTotal = getProjFundTotal(projId);
            jo.Add("fundTotal", fundTotal);
            jo.Add("fundSymbol", item["fundSymbol"]);
            jo.Add("shares", shares);
            jo.Add("member", members);
            //
            var val = shares == 0 ? 0 : decimal.Parse(fundTotal) / new decimal(shares);
            var valStr = val.ToString();
            if (valStr.Contains(".")) valStr = val.ToString("0.0000");
            jo.Add("valuePerShare", valStr); ;
            jo.Add("discussCount", item["discussCount"]);
            jo.Add("votePeriod", item["votePeriod"]);
            jo.Add("notePeriod", item["notePeriod"]);
            jo.Add("cancelPeriod", item["cancelPeriod"]);
            jo.Add("startTime", item["startTime"]);
            return getRes(jo);
        }
        private string getProjFundTotal(string projId)
        {
            var findStr = new JObject { { "projId", projId }, { "$or", new JArray { new JObject { { "event", "ProcessProposal" } }, new JObject { { "event", "Withdrawal" } } } } }.ToString();
            //var fieldStr = new JObject { { "tokenTribute", 1 }, { "event", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, "molonotifyinfos", findStr); //, fieldStr);
            if (queryRes.Count == 0) return "0";

            return queryRes.Sum(p => {

                if (p["event"].ToString() == "Withdrawal") {
                    return -1*decimal.Parse(p["amount"].ToString());
                }
                return decimal.Parse(p["tokenTribute"].ToString());
            }).ToString();

        }
        // 提案
        public JArray getProjProposalList(string projId, int pageNum, int pageSize, string address = "")
        {
            var findStr = new JObject { { "projId", projId} }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var sortStr = "{'blockTime':-1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, findStr, sortStr, (pageNum-1)*pageSize, pageSize);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var symbol = getProjFundSymbol(projId);
            var rr = queryRes.Select(p => {
                var jo = new JObject();
                jo.Add("projId", p["projId"]);
                jo.Add("proposalIndex", p["proposalIndex"]);
                jo.Add("proposalTitle", p["proposalName"]);
                jo.Add("sharesRequested", p["sharesRequested"]);
                jo.Add("tokenTribute", p["tokenTribute"]);
                jo.Add("tokenTributeSymbol", symbol);
                jo.Add("timestamp", p["blockTime"]);
                jo.Add("voteYesCount", p["voteYesCount"]);
                jo.Add("voteNotCount", p["voteNotCount"]);
                //jo.Add("hasVote", p["proposer"].ToString() == address);
                jo.Add("hasVote", isVote(p["projId"].ToString(), p["proposalIndex"].ToString(), address));
                jo.Add("proposalState", p["proposalState"]);
                jo.Add("handleState", p["handleState"]);
                return jo;
            });
            return getRes(new JObject { { "count", count }, { "list", new JArray { rr } } });
        }
        private string getProjFundSymbol(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "fundSymbol", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return "eth";

            var item = queryRes[0];
            return item["fundSymbol"].ToString();
        }
        private bool isVote(string projId, string proposalIndex, string address)
        {
            var findStr = new JObject { { "projId", projId }, { "proposalIndex", proposalIndex }, { "address", address } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr) > 0;
        }
        public JArray getProjProposalDetail(string projId, string proposalIndex)
        {
            var findStr = new JObject { {"projId", projId},{ "proposalIndex", proposalIndex } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, findStr);
            if (queryRes.Count == 0) getRes();

            var item = queryRes[0];
            var jo = new JObject();
            jo["projId"] = projId;
            jo["proposalIndex"] = proposalIndex;
            jo["proposalTitle"] = item["proposalName"];
            jo["proposalDetail"] = item["proposalDetail"];
            jo["proposer"] = item["proposer"];
            var username = getUsername(item["proposer"].ToString(), out string headIconUrl);
            jo["username"] = username;
            jo["headIconUrl"] = headIconUrl;
            jo.Add("sharesRequested", item["sharesRequested"]);
            jo.Add("tokenTribute", item["tokenTribute"]);
            jo.Add("tokenTributeSymbol", getProjFundSymbol(projId));
            jo.Add("applicant", item["applicant"]);
            username = getUsername(item["applicant"].ToString(), out headIconUrl);
            jo.Add("applicantUsername", username);
            jo.Add("applicantHeadIconUrl", headIconUrl);
            return getRes(jo);
        }
        public JArray getVoteInfo(string projId, string proposalIndex, string address)
        {
            var voteCount = "0";
            var voteType = "";
            var balance = "0";
            var findStr = new JObject { { "projId", projId }, { "proposalIndex", proposalIndex }, { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if(queryRes.Count > 0)
            {
                voteCount = queryRes[0]["balance"].ToString();
                voteType = queryRes[0]["type"].ToString();
            }
            findStr = new JObject { { "projId", projId }, { "proposalIndex", "" }, { "address", address } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if (queryRes.Count > 0)
            {
                balance = queryRes[0]["balance"].ToString();
            }
            var res = new JObject { { "voteCount", voteCount }, { "voteType", voteType }, { "balance", balance } };
            return getRes(res);
        }
        private string getUsername(string address, out string headIconUrl)
        {
            headIconUrl = "";
            var findStr = new JObject { { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr);
            if (queryRes.Count() == 0) return "";

            var item = queryRes[0];
            headIconUrl = item["headIconUrl"].ToString();
            return item["username"].ToString();
        }
        // 成员
        public JArray getProjMemberList(string projId, int pageNum, int pageSize)
        {
            var findStr = new JObject { { "projId", projId }, { "proposalIndex", "" } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            //
            var match = new JObject { { "$match", new JObject { { "projId", projId }, { "proposalIndex", "" } } } }.ToString();
            var lookup = new JObject { { "$lookup", new JObject {
                { "from", userInfoCol},
                {"localField", "address" },
                {"foreignField", "address" },
                {"as", "us" },
            } } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "_id", -1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var list = new List<string> { match, lookup, sort, skip, limit };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var rr =
            queryRes.Select(p => {
                var jo = new JObject();
                var username = "";
                var headIconUrl = "";
                var us = (JArray)p["us"];
                if(us.Count > 0)
                {
                    username = us[0]["username"].ToString();
                    headIconUrl = us[0]["headIconUrl"].ToString();
                }
                jo.Add("username", username);
                jo.Add("headIconUrl", headIconUrl);
                jo.Add("address", p["address"]);
                jo.Add("shares", p["balance"]);
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { rr } } });
        }

        //molo.discuss
        private string getRootId(string preDiscussId, string discussId = "", bool isProposal=false)
        {
            if (preDiscussId == "") return discussId;

            string findStr = new JObject { { "discussId", preDiscussId } }.ToString();
            string fieldStr = new JObject { { "rootId", 1 } }.ToString();
            var coll = projMoloDiscussInfoCol;
            if (isProposal) coll = projMoloProposalDiscussInfoCol;
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, coll, findStr, fieldStr);
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
            var findStr = new JObject { { "discussId", discussId }, { "userId", userId } }.ToString();
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
        public JArray getMoloDiscuss(string curId)
        {
            var findStr = new JObject { { "discussId", curId } }.ToString();
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
        private Dictionary<string, bool> getIsZanDict(string[] discussIdArr, string userId, bool isProposal = false)
        {
            var joArr = discussIdArr.Select(p => new JObject { { "discussId", p }, { "userId", userId } }).ToArray();
            var findStr = new JObject { { "$or", new JArray { joArr } } }.ToString();
            var fieldStr = new JObject { { "discussId", 1 } }.ToString();
            var coll = projMoloDiscussZanInfoCol;
            if (isProposal) coll = projMoloProposalDiscussZanInfoCol;
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, coll, findStr, fieldStr);
            if (queryRes.Count == 0) return new Dictionary<string, bool>();
            return queryRes.ToDictionary(k => k["discussId"].ToString(), v => true);
        }
        private Dictionary<string, long> getSubSizeDict(string[] rooIdArr, bool isProposal= false)
        {
            var match = new JObject { { "$match", MongoFieldHelper.toFilter(rooIdArr, "rootId") } }.ToString();
            var group = new JObject { { "$group", new JObject { { "_id", "$rootId" }, { "sum", new JObject { { "$sum", 1 } } } } } }.ToString();
            var list = new List<string> { match, group };
            var coll = projMoloDiscussInfoCol;
            if (isProposal) coll = projMoloProposalDiscussInfoCol;
            var subRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, coll, list);
            if (subRes.Count == 0) return new Dictionary<string, long>();
            return subRes.ToDictionary(k => k["_id"].ToString(), v => long.Parse(v["sum"].ToString()));
        }

        // molo.proposal.discuss
        // 添加评论
        public JArray addMoloPropDiscuss(Controller controller, string projId, string proposalIndex, string preDiscussId, string discussContent)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var discussId = DaoInfoHelper.genProjMoloProposalDiscussId(projId, proposalIndex, preDiscussId, discussContent, userId);
            var rootId = getRootId(preDiscussId, discussId, true);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                {"projId", projId },
                {"proposalIndex", proposalIndex},
                {"proposalId", DaoInfoHelper.genProjMoloProposalId(projId, proposalIndex)},
                {"preDiscussId", preDiscussId },
                {"discussId", discussId },
                {"discussContent", discussContent },
                {"userId", userId },
                {"zanCount", 0 },
                {"rootId", rootId },
                {"time", now },
                {"lastUpdateTime", now },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussInfoCol, newdata);
            return getRes(new JObject { { "discussId", discussId } });
        }
        // 点赞评论
        public JArray zanMoloPropDiscuss(Controller controller, string projId, string proposalIndex, string discussId)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "discussId", discussId },{ "userId", userId} }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussZanInfoCol, findStr) == 0)
            {
                var newdata = new JObject {
                    {"projId", projId },
                    {"proposalIndex", proposalIndex },
                    {"discussId", discussId },
                    {"userId", userId },
                    {"time", TimeHelper.GetTimeStamp() },
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussZanInfoCol, newdata);
            }
            return getRes();
        }
        // 查看单条评论
        public JArray getMoloPropDiscuss(string curId)
        {
            var findStr = new JObject {{ "discussId", curId } }.ToString();
            var fieldStr = new JObject { { "preDiscussId", 1 }, { "discussContent", 1 }, { "zanCount", 1 }, { "time", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            return getRes(item);
        }
        // 一级评论列表
        public JArray getMoloPropDiscussList(Controller controller, string projId, string proposalIndex, int pageNum = 1, int pageSize = 10)
        {
            //
            var findJo = new JObject { { "projId", projId }, { "proposalIndex", proposalIndex },{ "preDiscussId", "" } };
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussInfoCol, findJo.ToString());
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
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            // 是否已点赞
            var userId = getUserId(controller);
            var idArr = queryRes.Select(p => p["discussId"].ToString()).Distinct().ToArray();
            var zanDict = getIsZanDict(idArr, userId, true);

            // 子评论数量
            var rootIdArr = queryRes.Select(p => p["rootId"].ToString()).Distinct().ToArray();
            var rootIdDict = getSubSizeDict(rootIdArr, true);

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
        public JArray getMoloPropSubDiscussList(Controller controller, string rootId, int pageNum = 1, int pageSize = 10)
        {
            var findJo = new JObject { { "rootId", rootId }, { "preDiscussId", new JObject { { "$ne", "" } } } };
            var findStr = findJo.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussInfoCol, findStr);
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
                {"from", projMoloProposalDiscussInfoCol },
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

            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalDiscussInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            // 是否已点赞
            var userId = getUserId(controller);
            var idArr = queryRes.Select(p => p["discussId"].ToString()).Distinct().ToArray();
            var zanDict = getIsZanDict(idArr, userId, true);

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
        public JArray saveContractInfo(Controller controller,
            string projVersion, string projName, string projBrief, string projDetail, string projCoverUrl, string officailWeb,
            string fundHash, string fundSymbol, long fundDecimls,
            long periodDuration, /* 单位:秒 */
            long votingPeriodLength, long notingPeriodLength, long cancelPeriodLength, /* 单位:个 */
            string proposalDeposit, string proposalReward, string summonerAddress, JArray contractHashs
            )
        {
            if(!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "officailWeb", officailWeb } }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }

            var projId = DaoInfoHelper.genProjId(projName, projVersion);
            var now = TimeHelper.GetTimeStamp();
            var date = DateTime.Now;
            foreach(var item in contractHashs)
            {
                findStr = new JObject { { "contractHash", item["hash"] } }.ToString();
                if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, findStr) == 0)
                {
                    var data = new JObject {
                        { "projId", projId},
                        { "contractName", item["name"]},
                        { "contractHash", item["hash"]},
                        { "fundDecimls", fundDecimls},
                        { "type", "0"},
                        { "createdAt", date},
                        { "updatedAt", date},
                    }.ToString();
                    mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, data);
                } else
                {
                    var updateStr = new JObject { { "$set", new JObject {
                        { "projId", projId},
                        { "contractName", item["name"]},
                        { "fundDecimls", fundDecimls},
                        { "type", "0"},
                        { "updatedAt", date},
                    } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, updateStr, findStr);
                }
            }
            //
            var newdata = new JObject {
                {"projId", projId},
                {"projName", projName},
                {"projType", "moloch"},
                {"projBrief", projBrief},
                {"projDetail", projDetail},
                {"projCoverUrl", projCoverUrl},
                {"projVersion", projVersion},
                {"officailWeb", officailWeb},
                {"fundHash", fundHash},
                {"fundSymbol", fundSymbol},
                {"fundDecimals", fundDecimls},
                {"periodDuration", periodDuration},
                {"votingPeriodLength", votingPeriodLength},
                {"notingPeriodLength", notingPeriodLength},
                {"cancelPeriodLength", cancelPeriodLength},
                {"votePeriod", periodDuration * votingPeriodLength},
                {"notePeriod", periodDuration * notingPeriodLength},
                {"cancelPeriod", periodDuration * cancelPeriodLength},
                {"proposalDeposit", proposalDeposit},
                {"proposalReward", proposalReward},
                {"summonerAddress", summonerAddress.ToLower()},
                {"contractHashs", contractHashs},
                {"userId", userId},
                {"fundTotal", "0"},
                {"tokenTotal", 0},
                {"hasTokenCount", 0},
                {"discussCount", 0},
                {"startTime", 0},
                {"time", now},
                {"lastUpdateTime", now}
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, newdata);
            return getRes(new JObject { { "projId", projId } });
        }

        public JArray queryContractInfo(Controller controller, string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject {
                { "periodDuration",item["periodDuration"]},
                { "votingPeriodLength",item["votingPeriodLength"]},
                { "notingPeriodLength",item["notingPeriodLength"]},
                { "cancelPeriodLength",item["cancelPeriodLength"]},
                { "contractHashs",item["contractHashs"]}
            };
            return getRes(res);
        }
        public JArray saveProposal(Controller controller, string projId, string proposalName, string proposalDetail, string sharesRequested, string tokenTribute)
        {
            if(!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // TODO: 检查其他
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                {"projId", projId},
                {"proposalIndex", ""},
                {"proposalName", proposalName},
                {"proposalDetail", proposalDetail},
                {"sharesRequested", sharesRequested},
                {"proposalSate", tokenTribute},
                {"voteYesCount", 0},
                {"voteNotCount", 0},
                {"proposer", ""},
                {"delegateKey", ""},
                {"applicant", ""},
                {"contractHash", ""},
                {"userId",userId },
                {"discussCount", 0},
                {"time", now},
                {"lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, newdata);
            return getRes();
        }
        
        //
        public JArray getTokenBalance(Controller controller, string projId, string address)
        {
            address = address.ToLower();
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId },{ "proposalIndex",""}, { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);

            var balance = 0L;
            var newDelegateKey = "";
            if (queryRes.Count > 0)
            {
                balance = long.Parse(queryRes[0]["balance"].ToString());
                newDelegateKey = queryRes[0]["newDelegateKey"].ToString();
            }
            var res = new JObject { { "balance", balance }, { "newDelegateKey", newDelegateKey } };
            return getRes(res);
        }
        public JArray getProjFundInfo(Controller controller, string projId)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr);
            var fundHash = "";
            var fundSymbol = "";
            if(queryRes.Count > 0)
            {
                fundHash = queryRes[0]["fundHash"].ToString();
                fundSymbol = queryRes[0]["fundSymbol"].ToString();
            } 
            return getRes(new JObject { { "fundHash", fundHash },{ "fundSymbol", fundSymbol } });
        }
    }
    
}
