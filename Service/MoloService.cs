using Microsoft.AspNetCore.Mvc;
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
        public string projMoloVersionInfoCol { get; set; } = "moloprojversioninfos";
        public string projMoloBalanceInfoCol { get; set; } = "moloprojbalanceinfos";
        public string projMoloFundInfoCol { get; set; } = "moloprojfundinfos";
        public string projMoloTokenInfoCol { get; set; } = "moloprojtokeninfos";
        public string projMoloDiscussInfoCol { get; set; } = "moloprojdiscussinfos";
        public string projMoloDiscussZanInfoCol { get; set; } = "moloprojdiscusszaninfos";
        public string projMoloProposalInfoCol { get; set; } = "moloproposalinfos";
        public string projMoloProposalDiscussInfoCol { get; set; } = "moloproposaldiscussinfos";
        public string projMoloProposalDiscussZanInfoCol { get; set; } = "moloproposaldiscusszaninfos";
        public string userInfoCol { get; set; } = "daouserinfos";
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }

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
                //var members = getProjTeamCount(p["projId"].ToString(), out long shares);// 后面会废弃
                var jo = new JObject();
                jo.Add("projId", p["projId"]);
                jo.Add("projName", p["projName"]);
                jo.Add("projType", p["projType"]);
                jo.Add("projBrief", p["projBrief"]);
                jo.Add("projCoverUrl", p["projCoverUrl"]);
                jo.Add("shares", p["tokenTotal"]);
                jo.Add("members", p["hasTokenCount"]);
                return jo;
            }).ToArray();

            var res = new JObject { { "count", count }, { "list", new JArray { rr } } };
            return getRes(res);
        }
        private long getProjTeamCount(string projId, out long shares)
        {
            shares = 0;
            var findStr = new JObject { { "projId", projId },{ "proposalQueueIndex",""} }.ToString();
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

            //var members = getProjTeamCount(item["projId"].ToString(), out long shares); // 后面会废弃
            var jo = new JObject();
            jo.Add("projId", item["projId"]);
            jo.Add("projName", item["projName"]);
            jo.Add("projType", item["projType"]);
            jo.Add("projVersion", item["projVersion"]);
            jo.Add("projBrief", item["projBrief"]);
            jo.Add("projDetail", item["projDetail"]);
            jo.Add("projCoverUrl", item["projCoverUrl"]);
            jo.Add("officailWeb", item["officailWeb"]);
            var fundTotal = "0";// getProjFundTotal(projId);// 后面会废弃
            jo.Add("fundTotal", fundTotal);
            jo.Add("fundSymbol", item["fundSymbol"]);
            jo.Add("shares", item["tokenTotal"]);
            jo.Add("member", item["hasTokenCount"]);
            //
            //var val = shares == 0 ? 0 : decimal.Parse(fundTotal) / new decimal(shares);
            //var valStr = val.ToString();
            //if (valStr.Contains(".")) valStr = val.ToString("0.0000");
            //jo.Add("valuePerShare", valStr); ;
            jo.Add("valuePerShare", "0"); ;
            jo.Add("discussCount", item["discussCount"]);
            jo.Add("votePeriod", item["votePeriod"]);
            jo.Add("notePeriod", item["notePeriod"]);
            jo.Add("cancelPeriod", item["cancelPeriod"]);
            jo.Add("startTime", item["startTime"]);
            //
            jo.Add("contractHash", "");
            jo.Add("contractName", "");
            var hashArr = (JArray)item["contractHashs"];
            var info = hashArr.Where(p => p["name"].ToString().ToLower() == "moloch").ToArray();
            if(info != null && info.Length > 0)
            {
                jo["contractHash"] = info[0]["hash"];
                jo["contractName"] = info[0]["name"];
            }
            
            jo.Add("summonerAddress", item["summonerAddress"]);
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
                if(p["didPass"].ToString() == "1")
                {
                    return decimal.Parse(p["tokenTribute"].ToString());
                }
                return 0;
            }).ToString();

        }
        public JArray getProjFundTotal(string projId, int pageNum, int pageSize)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloFundInfoCol, findStr);
            if(count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projMoloFundInfoCol, findStr, 
                "{}", (pageNum-1)*pageSize, pageSize);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var list = queryRes.Select(p => new JObject {
                { "fundTotal", p["fundTotal"]},
                { "fundHash", p["fundHash"]},
                { "fundSymbol", p["fundSymbol"]}
            }).ToArray();

            var res = new JObject {
                {"count", count },
                {"list", new JArray{ list } }
            };
            return getRes(res);
        }
        public JArray getProjBidPrice(string pair="ETH-USDT")
        {
            //var res = new JObject { { "price", "136.71" } };
            var res = new JObject { { "price", "168.14" } };
            return getRes(res);
        }
        // 提案
        public JArray getProjProposalList(string projId, int pageNum, int pageSize, string address = "", string type="1")
        {
            var findJo = new JObject { { "projId", projId } };
            if(type == "0")
            {
                findJo.Add("proposalState", "10150");
            } else
            {
                //findJo.Add("proposalState", new JObject { { "$ne", "10150" } });
                var findJA = new JArray {
                    new JObject{{ "proposalState", new JObject { { "$ne", "10150" } } } },
                    new JObject{{ "proposalState", new JObject { { "$ne", "10151" } } } },
                };
                findJo.Add("$and", findJA);
            }
            var findStr = findJo.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var sortStr = "{'blockTime':-1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, findStr, sortStr, (pageNum-1)*pageSize, pageSize);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            //var symbol = getProjFundSymbol(projId);
            var rr = queryRes.Select(p => {
                var jo = new JObject();
                jo.Add("projId", p["projId"]);
                jo.Add("proposalIndex", p["proposalIndex"]);
                jo.Add("proposalQueueIndex", p["proposalQueueIndex"]);
                jo.Add("proposalTitle", p["proposalName"]);
                //jo.Add("sharesRequested", p["sharesRequested"]);
                //jo.Add("tokenTribute", p["tokenTribute"]);
                //jo.Add("tokenTributeSymbol", symbol);
                if (p["lootRequested"] != null)
                {
                    jo.Add("version", "2.0");
                    jo.Add("sharesRequested", p["sharesRequested"]);
                    jo.Add("lootRequested", p["lootRequested"]);
                    jo.Add("tributeToken", p["tributeToken"]);
                    jo.Add("tributeOffered", p["tributeOffered"]);
                    jo.Add("tributeOfferedSymbol", p["tributeOfferedSymbol"]);
                    jo.Add("paymentRequested", p["paymentRequested"]);
                    jo.Add("paymentRequestedSymbol", p["paymentRequestedSymbol"]);
                    jo.Add("startingPeriod", p["startingPeriod"]);
                    jo.Add("proposalType", getProposalType(p));
                } else
                {
                    jo.Add("version", "1.0");
                    jo.Add("sharesRequested", p["sharesRequested"]);
                    jo.Add("lootRequested", 0);
                    jo.Add("tributeToken", "");
                    jo.Add("tributeOffered", p["tokenTribute"]);
                    jo.Add("tributeOfferedSymbol", p["tokenTributeSymbol"]);
                    jo.Add("paymentRequested", 0);
                    jo.Add("paymentRequestedSymbol", "");
                    jo.Add("startingPeriod", -1);
                    jo.Add("proposalType", ProposalType.ApplyShare);
                }
                jo.Add("applicant", p["applicant"].ToString());

                jo.Add("timestamp", p["voteStartTime"]);
                jo.Add("voteYesCount", p["voteYesCount"]);
                jo.Add("voteNotCount", p["voteNotCount"]);
                jo.Add("isMine", p["proposer"].ToString().ToLower() == address.ToLower());
                jo.Add("hasVote", isVote(p["projId"].ToString(), p["proposalQueueIndex"].ToString(), address));
                jo.Add("proposalState", p["proposalState"]);
                jo.Add("handleState", p["handleState"]);
                return jo;
            });
            return getRes(new JObject { { "count", count }, { "list", new JArray { rr } } });
        }
        private string getProposalType(JToken jt)
        {
            var zeroAddr = "0x0000000000000000000000000000000000000000";
            var tributeToken = jt["tributeToken"].ToString();
            var paymentToken = jt["paymentToken"].ToString();
            if(tributeToken == zeroAddr && paymentToken == zeroAddr)
            {
                return ProposalType.PickOutMember;
            }
            if(tributeToken != zeroAddr && paymentToken == zeroAddr)
            {
                return ProposalType.AddSupportToken;
            }
            if (tributeToken != zeroAddr && paymentToken != zeroAddr)
            {
                return ProposalType.ApplyShare;
            }
            return "notKwon";
        }
        private string getProposalTypeOld(JToken jt)
        {
            var sharesRequested = jt["sharesRequested"].ToString();
            var lootRequested = jt["lootRequested"].ToString();
            var paymentRequested = jt["paymentRequested"].ToString();
            var tributeOffered = jt["tributeOffered"].ToString();
            var tributeToken = jt["tributeToken"].ToString();

            var proposalType = ProposalType.PickOutMember;
            var sum = decimal.Parse(sharesRequested) + decimal.Parse(lootRequested) + decimal.Parse(paymentRequested);
            if (sum > 0)
            {
                proposalType = ProposalType.ApplyShare;
            }
            else
            {
                if (tributeToken != null && tributeToken.ToString() != "0x0000000000000000000000000000000000000000")
                {
                    proposalType = ProposalType.AddSupportToken;
                }
                else
                {
                    proposalType = ProposalType.PickOutMember;
                }
            }
            return proposalType;
        }
        private bool isVote(string projId, string proposalIndex, string address)
        {
            var findStr = new JObject { { "projId", projId }, { "proposalQueueIndex", proposalIndex }, { "address", address } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr) > 0;
        }
        public JArray getProjProposalDetail(string projId, string proposalIndex)
        {
            var findStr = new JObject { {"projId", projId},{ "proposalIndex", proposalIndex } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloProposalInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var jo = new JObject();
            jo["projId"] = projId;
            jo["proposalIndex"] = proposalIndex;
            jo["proposalQueueIndex"] = item["proposalQueueIndex"];
            jo["proposalTitle"] = item["proposalName"];
            jo["proposalDetail"] = item["proposalDetail"];
            jo["proposer"] = item["proposer"];
            var username = getUsername(item["proposer"].ToString(), out string headIconUrl);
            jo["username"] = username;
            jo["headIconUrl"] = headIconUrl;
            //jo.Add("sharesRequested", item["sharesRequested"]);
            //jo.Add("tokenTribute", item["tokenTribute"]);
            //jo.Add("tokenTributeSymbol", getProjFundSymbol(projId));
            if(item["lootRequested"] != null)
            {
                jo.Add("version", "2.0");
                jo.Add("sharesRequested", item["sharesRequested"]);
                jo.Add("lootRequested", item["lootRequested"]);
                jo.Add("tributeToken", item["tributeToken"]);
                jo.Add("tributeOffered", item["tributeOffered"]);
                jo.Add("tributeOfferedSymbol", item["tributeOfferedSymbol"]);
                jo.Add("paymentRequested", item["paymentRequested"]);
                jo.Add("paymentRequestedSymbol", item["paymentRequestedSymbol"]);
                jo.Add("startingPeriod", item["startingPeriod"]);
                jo.Add("proposalType", getProposalType(item));
            } else
            {
                jo.Add("version", "1.0");
                jo.Add("sharesRequested", item["sharesRequested"]);
                jo.Add("lootRequested", 0);
                jo.Add("tributeToken", "");
                jo.Add("tributeOffered", item["tokenTribute"]);
                jo.Add("tributeOfferedSymbol", item["tokenTributeSymbol"]);
                jo.Add("paymentRequested", 0);
                jo.Add("paymentRequestedSymbol", "");
                jo.Add("startingPeriod", -1);
                jo.Add("proposalType", ProposalType.ApplyShare);
            }
            //

            jo.Add("applicant", item["applicant"]);
            username = getUsername(item["applicant"].ToString(), out headIconUrl);
            jo.Add("applicantUsername", username);
            jo.Add("applicantHeadIconUrl", headIconUrl);
            return getRes(jo);
        }
        
        public JArray getVoteInfo(string projId, string proposalIndex, string address)
        {
            address = address.ToLower();
            address = getDelegateOrSlf(address);
            var voteCount = "0";
            var voteType = "";
            var balance = "0";
            var findStr = new JObject { { "projId", projId }, { "proposalQueueIndex", proposalIndex }, { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if(queryRes.Count > 0)
            {
                voteCount = queryRes[0]["balance"].ToString();
                voteType = queryRes[0]["type"].ToString();
            }
            findStr = new JObject { { "projId", projId }, { "proposalQueueIndex", "" }, { "address", address } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if (queryRes.Count > 0)
            {
                balance = queryRes[0]["balance"].ToString();
            }
            var res = new JObject { { "voteCount", voteCount }, { "voteType", voteType }, { "balance", balance } };
            return getRes(res);
        }
        private string getDelegateOrSlf(string address)
        {
            var findStr = new JObject { { "newDelegateKey", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if(queryRes.Count > 0)
            {
                return queryRes[0]["address"].ToString();
            }
            return address;
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
        public JArray getProjMemberList(string projId, int pageNum, int pageSize, string role="0")
        {
            var key = "balance";
            if (role == "1") key = "sharesBalance";
            if (role == "2") key = "lootBalance";
            var findJo = new JObject {
                { "projId", projId },
                { "proposalQueueIndex", "" },
                //{ "balance", new JObject { { "$gt", 0 } } }
                { key, new JObject { { "$gt", 0 } } }
            };
            var findStr = findJo.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            //
            var match = new JObject { { "$match", findJo } }.ToString();
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
                jo.Add("shares", p[key]);
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { rr } } });
        }

        #region 评论接口
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
        #endregion
        
        //
        public JArray querySupportVersion(Controller controller)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject{ {"activeState",  1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloVersionInfoCol, findStr);
            if(queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", 0},{ "list", new JArray()} });
            }
            var arr = queryRes.Select(p => new JObject {
                {"type",p["type"] },
                {"version",p["version"] }
            }).ToArray();
            return getRes(new JObject { { "count", arr.Length }, { "list", new JArray { arr } } });
        }
        public JArray saveContractInfo(Controller controller,
            string projVersion, string projName, string projBrief, string projDetail, string projCoverUrl, string officailWeb,
            string fundHash, string fundSymbol, long fundDecimals,
            long periodDuration, /* 单位:秒 */
            long votingPeriodLength, long notingPeriodLength, long cancelPeriodLength, /* 单位:个 */
            string proposalDeposit, string proposalReward, string summonerAddress, JArray contractHashs,
            long emergencyExitWait/* 提案处理期限 */, 
            long bailoutWait/* 剔除成员执行期限 */, long startBlockTime, JArray fundInfoArr,
            string txid=""
            )
        {
            if(!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            if (fundHash.ToLower().Trim().Length == 0
                && fundInfoArr.Count == 0)
            {
                return getErrorRes(DaoReturnCode.C_InvalidParamFmt);
            }
            // 兼容v1 和 v2 版本
            if (fundHash.ToLower().Length == 0)
            {
                fundHash = fundInfoArr[0]["hash"].ToString();
                fundSymbol = fundInfoArr[0]["symbol"].ToString();
                fundDecimals = long.Parse(fundInfoArr[0]["decimals"].ToString());
            } else
            {
                if(fundInfoArr.All(p => p["hash"].ToString().ToLower() != fundHash.ToLower()))
                {
                    fundInfoArr.Add(new JObject {
                        {"hash", fundHash },
                        {"symbol", fundSymbol },
                        {"decimals", fundDecimals },
                    });
                }
            }
            // 封面
            if (!DaoInfoHelper.StoreFile(oss, bucketName, "", projCoverUrl, out string newHeadIconUrl))
            {
                return getErrorRes(DaoReturnCode.headIconNotUpload);
            }
            projCoverUrl = newHeadIconUrl;
            // 详情中url处理
            var nlist = projDetail.catchFileUrl();
            foreach (var ii in nlist)
            {
                if (ii.Trim().Length == 0) continue;
                if (!DaoInfoHelper.StoreFile(oss, bucketName, "", ii, out string newUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                projDetail = projDetail.Replace(ii, newUrl);
            }

            var projId = DaoInfoHelper.genProjId(projName, projVersion);
            var now = TimeHelper.GetTimeStamp();
            var date = DateTime.Now;
            foreach(var item in contractHashs)
            {
                var findStr = new JObject { { "contractHash", item["hash"].ToString().ToLower() } }.ToString();
                if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, findStr) == 0)
                {
                    var data = new JObject {
                        { "projId", projId},
                        { "contractName", item["name"]},
                        { "contractHash", item["hash"].ToString().ToLower()},
                        { "fundDecimals", fundDecimals},
                        { "type", "1"},
                        { "createdAt", date},
                        { "updatedAt", date},
                    }.ToString();
                    mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, data);
                } else
                {
                    var updateStr = new JObject { { "$set", new JObject {
                        { "projId", projId},
                        { "contractName", item["name"]},
                        { "fundDecimals", fundDecimals},
                        { "type", "1"},
                        { "updatedAt", date},
                    } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, updateStr, findStr);
                }
            }
            foreach(var item in fundInfoArr)
            {
                var findStr = new JObject { { "fundHash", item["hash"] } }.ToString();
                if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloTokenInfoCol, findStr) == 0)
                {
                    var data = new JObject {
                        { "fundHash", item["hash"] },
                        { "fundSymbol", item["symbol"] },
                        { "fundDecimals", item["decimals"] }
                    }.ToString();
                    mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloTokenInfoCol, data);
                }

                findStr = new JObject { { "projId", projId }, { "fundHash", item["hash"] } }.ToString();
                if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloFundInfoCol, findStr) == 0)
                {
                    var data = new JObject {
                        { "projId", projId},
                        { "fundHash", item["hash"] },
                        { "fundSymbol", item["symbol"] },
                        { "fundDecimals", item["decimals"] },
                        { "fundTotal", "0" },
                        { "fundTotalTp", "0" },
                    }.ToString();
                    mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloFundInfoCol, data);
                }

            }
            
            //
            processSummonerEventBalance(projId, summonerAddress.ToLower());
            //

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
                {"fundDecimals", fundDecimals},
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
                {"fundInfoArr", fundInfoArr},
                {"txid", txid},
                {"userId", userId},
                {"lastUpdatorId", userId},
                {"tokenTotal", 1},
                {"hasTokenCount", 1},
                {"discussCount", 0},
                {"emergencyExitWait",emergencyExitWait},
                {"bailoutWait", bailoutWait},
                {"startTime", startBlockTime},
                {"time", now},
                {"lastUpdateTime", now}
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, newdata);
            return getRes(new JObject { { "projId", projId } });
        }
        private void processSummonerEventBalance(string projId, string address)
        {
            var sharesBalance = 1L;
            var now = TimeHelper.GetTimeStamp();
            var findStr = new JObject { { "projId", projId},{ "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            if(queryRes.Count == 0)
            {
                var newdata = new JObject {
                    { "projId", projId},
                    { "proposalQueueIndex", ""},
                    { "type", "0"},
                    { "address", address},
                    { "balance", sharesBalance},
                    { "sharesBalance", sharesBalance},
                    { "sharesBalanceTp", 0},
                    { "lootBalance", 0},
                    { "lootBalanceTp", 0},
                    { "newDelegateKey", ""},
                    { "creator","1" },
                    { "time", now},
                    { "lastUpdateTime", now},
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, newdata);
                return;
            }
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
                { "emergencyExitWait",item["emergencyExitWait"]},
                { "contractHashs",item["contractHashs"]}
            };
            return getRes(res);
        }
        // 
        public JArray getTokenBalance(Controller controller, string projId, string address)
        {
            address = address.ToLower();
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId },{ "proposalQueueIndex", ""}, { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);

            var balance = 0L;
            var sharesBalance = 0L;
            var lootBalance = 0L;
            var newDelegateKey = "";
            if (queryRes.Count > 0)
            {
                var item = queryRes[0];
                balance = long.Parse(item["balance"].ToString());
                sharesBalance = long.Parse(item["sharesBalance"].ToString());
                lootBalance = long.Parse(item["lootBalance"].ToString());
                if(item["newDelegateKey"] != null)
                {
                    newDelegateKey = item["newDelegateKey"].ToString();
                }
            }
            var res = new JObject {
                { "balance", balance },
                { "sharesBalance", sharesBalance },
                { "lootBalance", lootBalance },
                { "newDelegateKey", newDelegateKey }
            };
            return getRes(res);
        }
        public JArray getTokenBalanceFromUpStream(Controller controller, string projId, string address)
        {
            address = address.ToLower();
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId }, { "proposalQueueIndex", "" }, { "newDelegateKey", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr);
            var upAddress = "";
            var upBalance = 0L;
            if(queryRes.Count > 0)
            {
                upAddress = queryRes[0]["address"].ToString();
                upBalance = long.Parse(queryRes[0]["balance"].ToString());
            }
            var res = new JObject { { "upAddress", upAddress }, { "upBalance", upBalance } };
            return getRes(res);
        }
        //
        public JArray getProjDepositInfo(Controller controller, string projId)
        {
            //if (!us.getUserInfo(controller, out string code, out string userId))
            //{
            //    return getErrorRes(code);
            //}
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr);
            var fundHash = "";
            var fundSymbol = "";
            var fundDecimals = 0L;
            var proposalDeposit = "";
            if (queryRes.Count > 0)
            {
                var item = queryRes[0];
                fundHash = item["fundHash"].ToString();
                fundSymbol = item["fundSymbol"].ToString();
                fundDecimals = long.Parse(item["fundDecimals"].ToString());
                proposalDeposit = item["proposalDeposit"].ToString();
            }
            var res = new JObject {
                { "fundHash", fundHash },
                { "fundSymbol", fundSymbol },
                { "fundDecimals", fundDecimals },
                { "proposalDeposit", proposalDeposit }
            };
            return getRes(res);
        }
        public JArray getProjFundList(Controller controller, string projId, int pageNum, int pageSize)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId} }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloFundInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var sortStr = "{'fundHash':1}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projMoloFundInfoCol, findStr, sortStr, pageSize*(pageNum-1), pageSize);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var arr = queryRes.Select(p => new JObject {
                {"fundHash", p["fundHash"] },
                {"fundSymbol", p["fundSymbol"] },
                {"fundDecimals", p["fundDecimals"] }
            });
            var res = new JObject { { "count", count }, { "list", new JArray { arr } } };
            return getRes(res) ;
        }

        //
        private string getUserAddress(string userId)
        {
            var findStr = new JObject { { "userId", userId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr);
            if (queryRes.Count == 0) return "";

            return queryRes[0]["address"].ToString();
        }
        private bool isMember(string projId, string userId)
        {
            var addr = getUserAddress(userId);
            if (addr == "") return false;

            var findStr = new JObject { { "projId", projId }, { "proposalQueueIndex", "" }, { "type", "0" }, { "address", addr },{ "balance", new JObject { { "$gt", 0} } } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloBalanceInfoCol, findStr) > 0;
        }
        public JArray modifyProjInfo(Controller controller, string projId, string projBrief, string projDetail, string projCoverUrl, string officailWeb)
        {
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            if(!isMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr); 
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.C_InvalidProjInfo);
            
            
            // 详情中url处理
            var nlist = projDetail.catchFileUrl();
            foreach (var ii in nlist)
            {
                if (ii.Trim().Length == 0) continue;
                if (!DaoInfoHelper.StoreFile(oss, bucketName, "", ii, out string newUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                projDetail = projDetail.Replace(ii, newUrl);
            }
            // 封面
            if (!DaoInfoHelper.StoreFile(oss, bucketName, "", projCoverUrl, out string newHeadIconUrl))
            {
                return getErrorRes(DaoReturnCode.headIconNotUpload);
            }
            projCoverUrl = newHeadIconUrl;
            //
            var item = queryRes[0];
            if(item["projBrief"].ToString() != projBrief
                || item["projDetail"].ToString() != projDetail
                || item["projCoverUrl"].ToString() != projCoverUrl
                || item["officailWeb"].ToString() != officailWeb)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "projBrief", projBrief},
                    { "projDetail", projDetail},
                    { "projCoverUrl", projCoverUrl},
                    { "officailWeb", officailWeb},
                    { "lastUpdatorId", userId},
                    { "lastUpdateTime", TimeHelper.GetTimeStamp()},
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray getLastUpdatorInfo(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloInfoCol, findStr);
            if(queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var id = item["userId"].ToString();
            if(item["lastUpdatorId"] != null)
            {
                id = item["lastUpdatorId"].ToString();
            }
            var addr = getUserAddress(id);
            var res = new JObject {
                {"lastUpdatorAddress",  addr},
                {"lastUpdateTime", item["lastUpdateTime"] }
            };
            return getRes(res);
        }

    }

    class ProposalType
    {
        public const string ApplyShare = "0";           // 申请股份
        public const string AddSupportToken = "1";      // 添加代币
        public const string PickOutMember = "2";        // 剔除成员
    }
}
