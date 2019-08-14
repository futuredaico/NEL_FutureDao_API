using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace NEL_FutureDao_API.Service
{
    public class DiscussService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string userInfoCol { get; set; } = "daoUserInfo";
        public string projInfoCol { get; set; } = "daoProjInfo";
        public string projStarInfoCol { get; set; } = "daoProjStarInfo";
        public string projTeamInfoCol { get; set; } = "daoProjTeamInfo";
        public string projUpdateInfoCol { get; set; } = "daoProjUpdateInfo";
        public string projDiscussInfoCol { get; set; } = "daoProjDiscussInfo";
        public string projUpdateDiscussInfoCol { get; set; } = "daoProjUpdateDiscussInfo";
        public string projDiscussZanInfoCol { get; set; } = "daoProjDiscussZanInfo";
        public string projUpdateDiscussZanInfoCol { get; set; } = "daoProjUpdateDiscussZanInfo";
        public string tokenUrl { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        private bool checkToken(string userId, string accessToken, out string code)
            => TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out code);

        //
        private bool checkUserId(string userId, out string code)
        {
            code = "";
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 
                || (queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerify
                    && queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtResetPassword
                    && queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtChangeEmail))
            {
                code = DaoReturnCode.S_NoPermissionAddDiscuss;
                return false;
            }
            return true;
        }

        
        private bool checkProjPreDiscussId(string projId, string preDiscussId, out string code)
        {
            code = "";
            if (preDiscussId == "")
            {
                if (!checkProjExist(projId))
                {
                    code = DaoReturnCode.S_InvalidProjId;
                    return false;
                }
            }
            else
            {
                if (!checkProjDiscussExist(projId, preDiscussId, true))
                {
                    code = DaoReturnCode.S_InvalidProjIdOrDiscussId;
                    return false;
                }
            }
            return true;
        }
        private bool checkProjExist(string projId)
        {
            string findStr = new JObject { { "projId", projId } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr) > 0;
        }
        private bool checkProjDiscussExist(string projId, string discussId, bool checkPre=false)
        {
            string key = checkPre ? "preDiscussId" : "discussId";
            string findStr = new JObject { { "projId", projId }, { key, discussId } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, findStr) > 0;
        }

        private bool checkUpdatePreDiscussId(string projId, string updateId, string preDiscussId, out string code)
        {
            code = "";
            if(preDiscussId == "")
            {
                if(!checkUpdateExist(projId, updateId))
                {
                    code = DaoReturnCode.S_InvalidUpdateIdOrProjId;
                    return false;
                }
            } else
            {
                if(!checkUpdateDiscussExist(projId, updateId, preDiscussId, true))
                {
                    code = DaoReturnCode.S_InvalidUpdateIdOrDiscussId;
                    return false;
                }
            }
            return true;
        }
        private bool checkUpdateExist(string projId, string updateId)
        {
            string findStr = new JObject { { "projId", projId},{ "updateId", updateId } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr) > 0;
        }
        private bool checkUpdateDiscussExist(string projId, string updateId, string discussId, bool checkPre=false)
        {
            string key = checkPre ? "preDiscussId" : "discussId";
            string findStr = new JObject { { "projId", projId},{ "updateId", updateId }, { key, discussId } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findStr) > 0;
        }

        private bool checkIsAdmin(string projId, string userId)
        {
            string findStr = new JObject { { "projId", projId},{ "userId", userId},{ "role", TeamRoleType.Admin} }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) > 0;
        }
        private bool checkLen(string content)
        {
            return content.Length <= 400;
        }
        private bool checkSupport(string projId, string userId)
        {
            string findStr = new JObject { { "projId", projId }, { "userId", userId }, { "supportState", StarState.SupportYes } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr) > 0;
        }

        //
        public JArray addProjDiscuss(string userId, string accessToken, string projId, string preDiscussId, string discussContent)
        {
            if(!checkLen(discussContent))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            if(!checkSupport(projId, userId))
            {
                return getErrorRes(DaoReturnCode.S_NoPermissionAddDiscuss);
            }
            // 
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            //
            if (!checkUserId(userId, out code))
            {
                return getErrorRes(code);
            }
            //
            if (!checkProjPreDiscussId(projId, preDiscussId, out code))
            {
                return getErrorRes(code);
            }
            //
            string discussId = DaoInfoHelper.genProjDiscussId(projId, preDiscussId, discussContent, userId);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                {"projId", projId },
                {"preDiscussId", preDiscussId },
                {"discussId", discussId },
                {"discussContent", discussContent },
                {"userId", userId },
                {"zanCount", 0 },
                {"time", now },
                {"lastUpdateTime", now },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, newdata);
            return getRes(new JObject { { "discussId", discussId } });
        }
        public JArray delProjDiscuss(string userId, string accessToken, string discussId, string projId=""/*admin删除时使用到*/)
        {
            // 
            if (!checkToken(userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { {"discussId", discussId } }.ToString();
            string fieldStr = new JObject { { "userId",1},{ "projId", 1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, findStr, fieldStr);
            if(queryRes.Count > 0)
            {
                var item = queryRes[0];
                if ((item["projId"].ToString() != projId) &&
                    (item["userId"].ToString() != userId || checkIsAdmin(projId, userId)))
                {
                    return getErrorRes(DaoReturnCode.S_NoPermissionDelDiscuss);
                }
                mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, findStr);
            }
            return getRes();
        }
        public JArray getProjDiscuss(string projId, string curId, string userId = ""/*显示[修改/删除]时使用到*/)
        {
            string findStr = new JObject { {"projId", projId },{ "discussId", curId} }.ToString();
            string fieldStr = new JObject { { "preDiscussId", 1 }, { "discussContent", 1 }, { "zanCount", 1 }, { "time", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            item["canModify"] = isProjMember(projId, userId);
            return getRes(queryRes[0]);
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
        public JArray getProjSubDiscussList(string projId, string curId, int pageNum = 1, int pageSize = 10)
        {
            var findJo = new JObject { { "projId", projId }, { "preDiscussId", curId } };
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, findJo.ToString());
            if (count == 0) return getRes(new JObject { { "count",count},{ "list",new JArray { } } });

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
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });


            var discussSubSizeDict = new Dictionary<string, long>();
            var discussIdArr = queryRes.Select(p => p["discussId"].ToString()).Distinct().ToArray();
            match = new JObject { { "$match", MongoFieldHelper.toFilter(discussIdArr, "preDiscussId") } }.ToString();
            var group = new JObject { { "$group", new JObject { { "_id", "$preDiscussId" }, { "sum", new JObject { { "$sum", 1 } } } } } }.ToString();
            list = new List<string> { match, group };
            var subRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, list);
            if(queryRes.Count > 0)
            {
                discussSubSizeDict = subRes.ToDictionary(k => k["_id"].ToString(), v => long.Parse(v["sum"].ToString()));
            }
            var res = queryRes.Select(p => {
                var jo = (JObject)p;
                jo["username"] = ((JArray)jo["us"])[0]["username"].ToString();
                jo["headIconUrl"] = ((JArray)jo["us"])[0]["headIconUrl"].ToString();
                jo.Remove("us");
                jo["subSize"] = discussSubSizeDict.GetValueOrDefault(jo["discussId"].ToString(), 0);
                return jo;
            }).ToArray();

            return getRes(new JObject { { "count", count},{ "list", new JArray { res } } });
        }
        //
        public JArray addUpdateDiscuss(string userId, string accessToken, string projId, string updateId, string preDiscussId, string discussContent)
        {
            if(!checkLen(discussContent))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            if(!checkSupport(projId, userId))
            {
                return getErrorRes(DaoReturnCode.S_NoPermissionAddDiscuss);
            }

            // 
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            //
            if (!checkUserId(userId, out code))
            {
                return getErrorRes(code);
            }
            if(!checkUpdatePreDiscussId(projId, updateId, preDiscussId, out code))
            {
                return getErrorRes(code);
            }
            
            string discussId = DaoInfoHelper.genProjUpdateDiscussId(updateId, preDiscussId, discussContent, userId);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                {"projId", projId },
                {"updateId", updateId },
                {"preDiscussId", preDiscussId },
                {"discussId", discussId },
                {"discussContent", discussContent },
                {"userId", userId },
                {"zanCount", 0 },
                {"time", now },
                {"lastUpdateTime", now },
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, newdata);
            return getRes(new JObject { { "discussId", discussId } });
        }
        public JArray delUpdateDiscuss(string userId, string accessToken, string discussId, string projId = ""/*admin删除时使用到*/)
        {
            // 
            if (!checkToken(userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "discussId", discussId } }.ToString();
            string fieldStr = new JObject { { "userId", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findStr, fieldStr);
            if (queryRes.Count > 0)
            {
                var item = queryRes[0];
                if ((item["projId"].ToString() != projId) &&
                    (item["userId"].ToString() != userId || checkIsAdmin(projId, userId)))
                {
                    return getErrorRes(DaoReturnCode.S_NoPermissionDelDiscuss);
                }
                mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findStr);
            }
            return getRes();
        }
        public JArray getUpdateDiscuss(string projId, string updateId, string curId, string userId= ""/*显示[修改/删除]时使用到*/)
        {
            string findStr = new JObject { { "updateId", updateId }, { "discussId", curId } }.ToString();
            string fieldStr = new JObject { { "preDiscussId", 1 }, { "discussContent", 1 }, { "zanCount", 1 }, { "time", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            item["canModify"] = isProjMember(projId, userId);
            return getRes(queryRes[0]);
        }
        public JArray getUpdateSubDiscussList(string updateId, string curId, int pageNum = 1, int pageSize = 10)
        {
            var findJo = new JObject { { "updateId", updateId }, { "preDiscussId", curId } };
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findJo.ToString());
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

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
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });


            var discussSubSizeDict = new Dictionary<string, long>();
            var discussIdArr = queryRes.Select(p => p["discussId"].ToString()).Distinct().ToArray();
            match = new JObject { { "$match", MongoFieldHelper.toFilter(discussIdArr, "preDiscussId") } }.ToString();
            var group = new JObject { { "$group", new JObject { { "_id", "$preDiscussId" }, { "sum", new JObject { { "$sum", 1 } } } } } }.ToString();
            list = new List<string> { match, group };
            var subRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, list);
            if (queryRes.Count > 0)
            {
                discussSubSizeDict = subRes.ToDictionary(k => k["_id"].ToString(), v => long.Parse(v["sum"].ToString()));
            }
            var res = queryRes.Select(p => {
                var jo = (JObject)p;
                jo["username"] = ((JArray)jo["us"])[0]["username"].ToString();
                jo["headIconUrl"] = ((JArray)jo["us"])[0]["headIconUrl"].ToString();
                jo.Remove("us");
                jo["subSize"] = discussSubSizeDict.GetValueOrDefault(jo["discussId"].ToString(), 0);
                return jo;
            }).ToArray();

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }

        public JArray zanProjDiscuss(string userId, string accessToken, string projId, string discussId)
        {
            // 
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            //
            if (!checkUserId(userId, out code))
            {
                return getErrorRes(code);
            }
            if (!checkProjDiscussExist(projId, discussId))
            {
                return getErrorRes(DaoReturnCode.S_InvalidProjIdOrDiscussId);
            }
            string findStr = new JObject { { "discussId", discussId} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussZanInfoCol, findStr) == 0)
            {
                var newdata = new JObject {
                    {"projId", projId },
                    {"discussId", discussId },
                    {"userId", userId },
                    {"time", TimeHelper.GetTimeStamp() },
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussZanInfoCol, newdata);
            }
            return getRes() ;
        }
        public JArray zanUpdateDiscuss(string userId, string accessToken, string projId, string updateId, string discussId)
        {
            // 
            string code;
            if (!checkToken(userId, accessToken, out code))
            {
                return getErrorRes(code);
            }
            //
            if (!checkUserId(userId, out code))
            {
                return getErrorRes(code);
            }
            if (!checkUpdateDiscussExist(projId, updateId, discussId))
            {
                return getErrorRes(DaoReturnCode.S_InvalidUpdateIdOrDiscussId);
            }
            string findStr = new JObject { { "discussId", discussId } }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussZanInfoCol, findStr) == 0)
            {
                var newdata = new JObject {
                    {"projId", projId },
                    {"discussId", discussId },
                    {"userId", userId },
                    {"time", TimeHelper.GetTimeStamp() },
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussZanInfoCol, newdata);
            }
            return getRes();
        }
    }
}
