using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
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
        public string tokenUrl { get; set; } = "";

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);

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
                return getErrorRes(DaoReturnCode.T_RepeatProjNameOrProjTitle);
            }
            findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "email", 1 }, { "headIconUrl", 1 },{ "emailVerifyState",1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0 
                || (queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerify
                    && queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtChangeEmail
                    && queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtResetPassword)
                )
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionCreateProj);
            }
            var item = queryRes[0];
            var username = item["username"];
            var email = item["email"];
            var headIconUrl = item["headIconUrl"];
            string projId = DaoInfoHelper.genProjId(projName, projTitle);
            long now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "userId", userId},
                { "username", username},
                { "email", email},
                { "headIconUrl", headIconUrl},
                { "authenticationState", TeamAuthenticationState.Init},
                { "role", TeamRoleType.Admin},
                { "emailVerifyState", EmailState.hasVerifyAtInvitedYes},
                { "emailVerifyCode","" },
                { "invitorId","" },
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            newdata = new JObject {
                { "projId", projId},
                { "projName", projName},
                { "projTitle", projTitle},
                { "projType", ProjType.to(projType)},
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
            return getRes(new JObject { {"projId", projId} });
        }
        public JArray deleteProj(string userId, string accessToken, string projId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 }, { "role", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 
                || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes
                || queryRes[0]["role"].ToString() != TeamRoleType.Admin)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteProj);
            }
            findStr = new JObject { { "projId", projId } }.ToString();
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            return getRes();
        }
        public JArray modifyProj(string userId, string accessToken, string projId, string projName, string projTitle, string projType, string projCoverUrl, string projBrief, string videoBriefUrl, string projDetail, string connectEmail, string officialWeb, string community)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            //
            findStr = new JObject { { "projId", projId } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
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
        public JArray queryProj(string userId, string accessToken, string projId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionQueryProj);
            }
            //
            findStr = new JObject { { "projId", projId } }.ToString();
            fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projBrief", "videoBriefUrl", "projDetail", "projState", "projSubState", "connectEmail", "officialWeb", "community", "creatorId" }).ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            var item = queryRes[0];
            item["role"] = item["creatorId"].ToString() == userId ? TeamRoleType.Admin : TeamRoleType.Member;
            return getRes(item);
        }
        public JArray getProjInfo(string projId)
        {
            //管理员头像、管理员名称、项目名称
            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = new JObject { { "creatorId", 1 }, { "projName", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getRes();
            }
            var creatorId = queryRes[0]["creatorId"].ToString();
            var projName = queryRes[0]["projName"].ToString();

            findStr = new JObject { { "userId", creatorId } }.ToString();
            fieldStr = new JObject { { "headIconUrl", 1 }, { "username", 1 } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            var adminHeadIconUrl = queryRes[0]["headIconUrl"].ToString();
            var adminUsername = queryRes[0]["username"].ToString();

            var res = new JObject { { "projName", projName }, { "adminHeadIconUrl", adminHeadIconUrl }, { "adminUsername", adminUsername } };
            return getRes(res);
        }


        //0. query + invite + send + verify
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
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 }, { "role", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes
                || queryRes[0]["role"].ToString() != TeamRoleType.Admin)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionInviteTeamMember);
            }
            findStr = new JObject { { "userId", targetUserId } }.ToString();
            fieldStr = new JObject { { "email", 1 }, { "username", 1 }, { "headIconUrl", 1 } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0)
            {
                // 无效目标用户id
                return getErrorRes(DaoReturnCode.T_InvalidTargetUserId);
            }
            var item = queryRes[0];
            string nEmail = item["email"].ToString();
            string nUsername = item["username"].ToString();
            string nIconUrl = item["headIconUrl"].ToString();
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
                    { "authenticationState", TeamAuthenticationState.Init},
                    { "role", TeamRoleType.Member},
                    { "emailVerifyState", EmailState.sendBeforeStateAtInvited},
                    { "emailVerifyCode","" },
                    { "invitorId", userId },
                    { "time", now},
                    { "lastUpdateTime", now}
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            } else
            {
                var oldState = queryRes[0]["emailVerifyState"].ToString();
                if ( oldState == EmailState.hasVerifyAtInvitedNot)
                {
                    var updateStr = new JObject { { "$set", new JObject {
                        {"email", nEmail },
                        {"username", nUsername },
                        {"headIconUrl", nIconUrl },
                        { "emailVerifyState", EmailState.sendBeforeStateAtInvited},
                        { "emailVerifyCode","" },
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
            string fieldStr = new JObject { { "emailVerifyState", 1 },{ "emailVerifyCode", 1},{ "username",1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0
                || queryRes[0]["emailVerifyCode"].ToString() != verifyCode
                || queryRes[0]["username"].ToString() != username)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            string oldState = queryRes[0]["emailVerifyState"].ToString();
            if(oldState == EmailState.hasVerifyAtInvitedYes
                || oldState == EmailState.hasVerifyAtInvitedNot)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }

            string state = agreeOrNot == "1" ? EmailState.hasVerifyAtInvitedYes : EmailState.hasVerifyAtInvitedNot;
            var updateStr = new JObject { { "$set", new JObject {
                    { "emailVerifyState", state },
                    { "lastUpdateTime", TimeHelper.GetTimeStamp() }
                } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, updateStr, findStr);
            return getRes();
        }

        public JArray deleteProjTeam(string userId, string accessToken, string projId, string targetUserId)
        {
            if (userId == targetUserId)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteYourSelf);
            }
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }

            string findStr = new JObject { {"$or", new JArray {
                new JObject{{"projId", projId},{ "userId", userId} },
                new JObject{{"projId", projId},{ "userId", targetUserId } }
            } } }.ToString();
            string fieldStr = new JObject { { "userId", 1 }, { "role", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteTeamMember);
            }

            bool isAdmin = queryRes.Any(p => p["userId"].ToString() == userId && p["role"].ToString() == TeamRoleType.Admin);
            if (!isAdmin)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteTeamMember);
            }
            bool hasTarget = queryRes.Any(p => p["userId"].ToString() == targetUserId);
            if (hasTarget)
            {
                isAdmin = queryRes.Any(p => p["userId"].ToString() == targetUserId && p["role"].ToString() == TeamRoleType.Admin);
                if (isAdmin)
                {
                    return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteTeamAdmin);
                }
                findStr = new JObject { { "projId", projId }, { "userId", targetUserId } }.ToString();
                mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            }
            return getRes();
        }
        public JArray modifyUserRole(string userId, string accessToken, string projId, string targetUserId, string roleType)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 }, { "role", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes
                || queryRes[0]["role"].ToString() != TeamRoleType.Admin)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyTeamMember);
            }

            findStr = new JObject { { "projId", projId }, { "userId", targetUserId } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (queryRes.Count > 0 && queryRes[0]["role"].ToString() != roleType)
            {
                string updateStr = new JObject { { "$set", new JObject {
                    { "role", roleType == TeamRoleType.Admin ? roleType:TeamRoleType.Member},
                    { "lastUpdateTime", TimeHelper.GetTimeStamp()}
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray queryProjTeam(string userId, string accessToken, string projId, int pageNum = 1, int pageSize = 10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionQueryTeamMember);
            }
            //
            findStr = new JObject { { "projId", projId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);

            fieldStr = MongoFieldHelper.toReturn(new string[] { "userId", "username", "headIconUrl", "authenticationState", "role" }).ToString();
            string sortStr = new JObject { { "role", 1 } }.ToString();
            queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }


        public JArray createUpdate(string userId, string accessToken, string projId, string updateTitle, string updateDetail)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionCreateUpdate);
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
            return getRes(new JObject { {"updateId", updateId} });
        }
        public JArray deleteUpdate(string userId, string accessToken, string projId, string updateId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteUpdate);
            }
            findStr = new JObject { { "projId", projId }, { "updateId", updateId } }.ToString();
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            return getRes();
        }
        public JArray modifyUpdate(string userId, string accessToken, string projId, string updateId, string updateTitle, string updateDetail)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyUpdate);
            }
            var updateJo = new JObject();
            if(updateTitle.Trim() != "")
            {
                updateJo.Add("updateTitel", updateTitle);
            }
            if(updateDetail.Trim() != "")
            {
                updateJo.Add("updateDetail", updateDetail);
            }
            if(updateJo.Count > 0)
            {
                findStr = new JObject { { "projId", projId }, { "updateId", updateId } }.ToString();
                var updateStr = new JObject { { "$set", updateJo } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray queryUpdate(string userId, string accessToken, string projId, string updateId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0 || queryRes[0]["emailVerifyState"].ToString() != EmailState.hasVerifyAtInvitedYes)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionQueryProj);
            }
            findStr = new JObject { { "projId", projId }, { "updateId", updateId } }.ToString();
            fieldStr = new JObject { { "updateTitle", 1 }, { "updateDetail", 1 }, { "_id", 1 } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getRes();
            }
            return getRes(queryRes[0]);
        }
        
        
        // 查询项目(all/管理中/关注中/支持中)
        public JArray queryProjList(int pageNum=1, int pageSize=10)
        {
            string findStr = "{}";
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);

            JArray queryRes = new JArray();
            if(count > 0)
            {
                string sortStr = "{'time':-1}";
                string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "supportCount" }).ToString();
                queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            }
            var res = new JObject { { "count", count }, { "list", queryRes } };
            return getRes(res);
        }
        public JArray queryProjDetail(string projId, string userId="")
        {
            string findStr = new JObject { {"projId", projId } }.ToString();
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projName","projTitle","projType", "projConverUrl","projBrief", "projDetail", "supportCount"}).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            item["isSupport"] = userId == null ? false: checkIsSupport(projId, userId);
            item["isStar"] = userId == null ? false : checkIsStar(projId, userId);
            return getRes(item);
        }
        private bool checkIsSupport(string projId, string userId)
        {
            return false;
        }
        private bool checkIsStar(string projId, string userId)
        {
            return false;
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

        public static string to(string type)
        {
            type = type.ToLower();
            if(GAME == type
                || COMIC == type
                || MOVIE == type
                || OTHER == type)
            {
                return type;
            }
            return OTHER;
        }
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
    class TeamAuthenticationState
    {
        public const string Init = "not"; // 初始状态，未认证
        public const string Person = "person"; // 个人认证
        public const string Company = "company"; // 企业认证
    }
}
