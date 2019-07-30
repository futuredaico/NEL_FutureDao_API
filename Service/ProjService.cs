using NEL.NNS.lib;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace NEL_FutureDao_API.Service
{
    public class ProjService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string projInfoCol { get; set; } = "daoProjInfo";
        public string projTeamInfoCol { get; set; } = "daoProjTeamInfo";
        public string userInfoCol { get; set; } = "daoUserInfo";
        public string projUpdateInfoCol { get; set; } = "daoProjUpdateInfo";
        public string tokenUrl { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);
        private bool checkResCode(JArray res) => RespHelper.checkResCode(res);

        public JArray createProj(string userId, string accessToken, string projName, string projTitle, string projType, string projCoverUrl, string projBrief, string videoBriefUrl, string projDetail, string connectEmail, string officialWeb, string community)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "$or", new JArray{
                new JObject{{ "projName", projName } },
                new JObject{{ "projTitle", projTitle } }
            } } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr) > 0)
            {
                // 重复的项目名称/项目标题
                return getErrorRes(ProjReturnCode.RepeatProjNameOrProjTitle);
            }
            string projId = DaoInfoHelper.genProjId(projName, projTitle);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "projName", projName},
                { "projTitle", projTitle},
                { "projType", projType.ToLower()},
                { "projConverUrl", projCoverUrl},
                { "projBrief", projBrief},
                { "platform", projType},
                { "videoBriefUrl", videoBriefUrl},
                { "projDetail", projDetail},
                { "projState", ProjState.Readying},
                { "projSubState", ProjSubState.Init},
                { "connectEmail", connectEmail},
                { "officialWeb", officialWeb},
                { "community", community},
                { "creatorId", userId},
                { "lastUpdatorId", userId},
                { "supportCount", 0},
                { "starCount", 0},
                { "discussCount", 0},
                { "updateCount", 0},
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, newdata);
            //
            findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "email", 1 }, { "headIconUrl", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            var item = queryRes[0];

            newdata = new JObject {
                { "projId", projId},
                { "userId", userId},
                { "username", item["username"]},
                { "email", item["email"]},
                { "headIconUrl", item["headIconUrl"]},
                { "role", TeamRoleType.Admin},
                { "state", EmailState.hasVerifyAtInvitedYes},
                { "verifyCode","" },
                { "invitorId","" },
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            return getRes(new JObject { {"projId", projId} });
        }
        public JArray modifyProj(string userId, string accessToken, string projId, string projName, string projTitle, string projType, string projCoverUrl, string projBrief, string videoBriefUrl, string projDetail, string connectEmail, string officialWeb, string community)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject {{"projId",projId },{ "userId", userId} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                // 非项目成员不能修改项目
                return getErrorRes(ProjReturnCode.HaveNotPermissionModifyProj);
            }
            findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            var item = queryRes[0];
            var isUpdate = false;
            var updateJo = new JObject();
            if (item["projName"].ToString() != projName && projName.Trim().Length > 0)
            {
                updateJo.Add("projName", projName);
                isUpdate = true;
            }
            if (item["projTitle"].ToString() != projTitle && projTitle.Trim().Length > 0)
            {
                updateJo.Add("projTitle", projTitle);
                isUpdate = true;
            }
            if (item["projType"].ToString() != projType && projType.Trim().Length > 0)
            {
                updateJo.Add("projType", projType);
                isUpdate = true;
            }
            if (item["projConverUrl"].ToString() != projCoverUrl && projCoverUrl.Trim().Length > 0)
            {
                updateJo.Add("projCoverUrl", projCoverUrl);
                isUpdate = true;
            }
            if (item["projBrief"].ToString() != projBrief && projBrief.Trim().Length > 0)
            {
                updateJo.Add("projBrief", projBrief);
                isUpdate = true;
            }
            if (item["videoBriefUrl"].ToString() != videoBriefUrl && videoBriefUrl.Trim().Length > 0)
            {
                updateJo.Add("videoBriefUrl", videoBriefUrl);
                isUpdate = true;
            }
            if (item["projDetail"].ToString() != projDetail && projDetail.Trim().Length > 0)
            {
                updateJo.Add("projDetail", projDetail);
                isUpdate = true;
            }
            if (item["connectEmail"].ToString() != connectEmail && connectEmail.Trim().Length > 0)
            {
                updateJo.Add("connectEmail", connectEmail);
                isUpdate = true;
            }
            if (item["officialWeb"].ToString() != officialWeb && officialWeb.Trim().Length > 0)
            {
                updateJo.Add("officialWeb", officialWeb);
                isUpdate = true;
            }
            if (item["community"].ToString() != community && community.Trim().Length > 0)
            {
                updateJo.Add("community", community);
                isUpdate = true;
            }
            // projState
            // projSubState
            if (isUpdate)
            {
                updateJo.Add("lastUpdatorId", userId);
                updateJo.Add("lastUpdateTime", TimeHelper.GetTimeStamp());
                var updateStr = new JObject { { "$set", updateJo } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        //0. query
        //a.
        //b.send.invite
        //c.
        public JArray queryMember(string userId, string accessToken, string targetEmail/*模糊匹配*/, int pageNum=1, int pageSize=10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = MongoFieldHelper.newRegexFilter(targetEmail, "email").ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr);
            if(count == 0)
            {
                return getRes(new JObject { { "count", count},{ "list", new JArray { } } });
            }
            string fieldStr = new JObject { {"userId",1 },{ "username", 1},{ "email", 1 },{ "headIconUrl",1},{ "_id",0} }.ToString();
            string sortStr = "{}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, sortStr, pageSize*(pageNum-1), pageSize, fieldStr);
            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }
        public JArray inviteMember(string userId, string accessToken, string targetUserId, string projId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId }}.ToString();
            string fieldStr = new JObject { { "role", 1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0 || queryRes[0]["role"].ToString() != TeamRoleType.Admin)
            {
                // 用户无操作该项目的权限
                return getErrorRes(ProjReturnCode.HaveNotPermissionInviteMember);
            }
            findStr = new JObject { { "userId", targetUserId } }.ToString();
            fieldStr = new JObject { { "email", 1 }, { "username", 1 }, { "headIconUrl", 1 } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0)
            {
                // 无效目标用户id
                return getErrorRes(ProjReturnCode.InvalidTargetUserId);
            }
            string nEmail = queryRes[0]["email"].ToString();
            string nUsername = queryRes[0]["username"].ToString();
            string nIconUrl = queryRes[0]["headIconUrl"].ToString();
            var now = TimeHelper.GetTimeStamp();

            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if(queryRes.Count == 0)
            {
                var newdata = new JObject {
                    { "projId", projId},
                    { "userId", targetUserId},
                    { "email", nEmail},
                    { "username", nUsername},
                    { "headIconUrl", nIconUrl},
                    { "role", TeamRoleType.Member},
                    { "state", EmailState.sendBeforeStateAtInvited},
                    { "verifyCode","" },
                    { "invitorId", userId },
                    { "time", now},
                    { "lastUpdateTime", now}
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            } else
            {
                var oldState = queryRes[0]["state"].ToString();
                if ( oldState == EmailState.hasVerifyAtInvitedNot)
                {
                    var updateStr = new JObject { { "$set", new JObject {
                        {"email", nEmail },
                        {"username", nUsername },
                        {"headIconUrl", nIconUrl },
                        { "state", EmailState.sendBeforeStateAtInvited},
                        { "verifyCode","" },
                        { "invitorId", userId },
                        { "lastUpdateTime", now}
                    } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, updateStr, findStr);
                }
            }
            return getRes();
        }
        public JArray verifyInvite(string username, string email, string projId, string verifyCode, string agreeOrNot)
        {
            string findStr = new JObject { { "projId", projId }, { "email", email } }.ToString();
            string fieldStr = new JObject { { "state",1 },{ "verifyCode", 1},{ "username",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0
                || queryRes[0]["verifyCode"].ToString() != verifyCode
                || queryRes[0]["username"].ToString() != username)
            {
                return getErrorRes(UserReturnCode.invalidVerifyCode);
            }
            string oldState = queryRes[0]["state"].ToString();
            if(oldState == EmailState.hasVerifyAtInvitedYes
                || oldState == EmailState.hasVerifyAtInvitedNot)
            {
                return getErrorRes(UserReturnCode.invalidVerifyCode);
            }

            string state = agreeOrNot == "1" ? EmailState.hasVerifyAtInvitedYes : EmailState.hasVerifyAtInvitedNot;
            var updateStr = new JObject { { "$set", new JObject {
                    { "state", state },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, updateStr, findStr);
            return getRes();
        }

        public JArray createUpdate(string userId, string accessToken, string projId, string updateTitle, string updateDetail)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { {"projId", projId },{ "userId", userId} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) == 0)
            {
                // 用户无操作该项目的创建更新权限
                return getErrorRes(ProjReturnCode.HaveNotPermissionCreateUpdate);
            }

            var updateId = DaoInfoHelper.genProjUpdateId(projId, updateTitle);
            var now = TimeHelper.GetTimeStamp(); 
            var newdata = new JObject {
                { "projId", projId },
                { "updateId", updateId},
                { "updateTitle", updateTitle},
                { "updateDetail", updateDetail},
                { "creatorId",  userId},
                { "lastUpdatorId",  userId},
                { "time",  now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, newdata);
            return getRes();
        }


        // 查询项目(all/管理中/关注中/支持中)
        public JArray queryProj()
        {
            return null;
        }
        public JArray queryProjDetail()
        {
            return null;
        }
        public JArray queryUpdateDetail()
        {
            return null;
        }

    }

    class ProjType
    {
        public const string GAME = "game";  // 游戏
        public const string COMIC = "comic"; // 动漫
        public const string MOVIE = "movie"; // 电影
        public const string OTHER = "other"; // 其他
    }
    class ProjState
    {
        public const string Readying = "reading";  // 准备中
        public const string IdeaPub = "ideapub";   // 创意发布
        public const string CrowdFunding = "crowdfunding";  // 众筹中
        public const string Trading = "trading";       // 交易中
        public const string ClearUp = "clearup";       // 清退
    }
    class ProjSubState
    {
        // 一级/二级关系
        // * 准备中: 无状态/审核中/审核失败
        // * 众筹中: 无状态/预热中 
        public const string Init = "init";          // 初始状态: 无状态
        public const string Auditing = "auditing";      // 审核中
        public const string AuditFailed = "auditfailed";   // 审核失败
        public const string Preheating = "preheating";    // 预热中
    }
    class PlatformType
    {
        public const string NEO = "neo";
        public const string ETH = "eth";
    }
    class TeamRoleType
    {
        public const string Admin = "admin";
        public const string Member = "member";
    }
    class ProjReturnCode
    {
        public const string RepeatProjNameOrProjTitle = "10212";     // 重复的项目名称或项目标题
        public const string HaveNotPermissionModifyProj = "10213";   // 没有权限修改项目
        public const string HaveNotPermissionInviteMember = "10214"; // 没有权限邀请成员
        public const string InvalidTargetUserId = "10215";           // 不合法的用户id
        public const string HaveNotPermissionCreateUpdate = "10216"; // 没有权限创建项目更新
    }

}
