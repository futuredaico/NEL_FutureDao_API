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
        public string projStarInfoCol { get; set; } = "daoProjStarInfo";
        public string projSupportInfoCol { get; set; } = "daoProjSupportInfo";
        public string tokenUrl { get; set; } = "";
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }

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
            if(item[0]["projSubState"].ToString() == ProjSubState.Auditing)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
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
            /*
            if (item["projConverUrl"].ToString() != projCoverUrl && projCoverUrl.Trim().Length > 0)
            {
                updateJo.Add("projCoverUrl", projCoverUrl);
                isUpdate = true;
            }*/
            var oldUrl = item["projConverUrl"].ToString();
            if(oldUrl != projCoverUrl && projCoverUrl.Trim().Length > 0)
            {

                if (!DaoInfoHelper.StoreFile(oss, bucketName, oldUrl, projCoverUrl, "defaultHeadIconUrl"))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                updateJo.Add("projCoverUrl", projCoverUrl);
                isUpdate = true;
            }
            if (item["projBrief"].ToString() != projBrief && projBrief.Trim().Length > 0)
            {
                updateJo.Add("projBrief", projBrief);
                isUpdate = true;
            }
            /*
            if (item["videoBriefUrl"].ToString() != videoBriefUrl && videoBriefUrl.Trim().Length > 0)
            {
                updateJo.Add("videoBriefUrl", videoBriefUrl);
                isUpdate = true;
            }*/
            oldUrl = item["videoBriefUrl"].ToString();
            if (oldUrl != videoBriefUrl && videoBriefUrl.Trim().Length > 0)
            {

                if (!DaoInfoHelper.StoreFile(oss, bucketName, oldUrl, videoBriefUrl, "defaultHeadIconUrl"))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
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
                { "discussCount",0 },
                { "zanCount",0 },
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
            fieldStr = new JObject { { "updateTitle", 1 }, { "updateDetail", 1 }, {"discussCount",1 },{"zanCount",1 },{ "_id", 1 } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getRes();
            }
            return getRes(queryRes[0]);
        }
        
        
        public JArray commitProjAudit(string userId, string accessToken, string projId)
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
            findStr = new JObject { { "projId", projId }}.ToString();
            fieldStr = MongoFieldHelper.toReturn(new string[] { "projName","projTitle", "projConverUrl", "projSubState", "projBrief", "projDetail","connectEmail"}).ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if(queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.projRequiredFieldIsEmpty);
            }
            var item = queryRes[0];
            if(item["projName"].ToString().Trim() == ""
                || item["projTitle"].ToString().Trim() == ""
                || item["projConverUrl"].ToString().Trim() == ""
                || item["projBrief"].ToString().Trim() == ""
                || item["projDetail"].ToString().Trim() == ""
                || item["connectEmail"].ToString().Trim() == "")
            {
                return getErrorRes(DaoReturnCode.projRequiredFieldIsEmpty);
            }
            //
            if(item["projSubState"].ToString() != ProjSubState.Auditing)
            {
                var updateStr = new JObject { { "$set", new JObject {
                {"projSubState", ProjSubState.Auditing},
                {"lastUpdatorId",  userId},
                {"lastUpdateTime", TimeHelper.GetTimeStamp() }
            } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            }
            return getRes();
        }


        // 查询项目(all/管理中/关注中/支持中)
        public JArray queryProjList(int pageNum=1, int pageSize=10)
        {
            return queryProjListPrivate(pageNum, pageSize);
        }
        public JArray queryProjListAtManage(string userId, string accessToken, int pageNum=1, int pageSize=10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            return queryProjListPrivate(pageNum, pageSize, userId, ProjMangeSortType.Managing);
        }
        public JArray queryProjListAtStar(string userId, string accessToken, int pageNum = 1, int pageSize = 10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            return queryProjListPrivate(pageNum, pageSize, userId, ProjMangeSortType.Staring);
        }
        public JArray queryProjListPrivate(int pageNum, int pageSize, string userId="", string manageOrStar="")
        {
            JArray queryRes = new JArray();
            if (!getListFilter(pageNum, pageSize, userId, manageOrStar, out string findStr, out long count))
            {
                return getRes(new JObject { {"count", count},{ "list", queryRes } });
            }
            //long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            //JArray queryRes = new JArray();
            //if(count > 0)
            {
                int skip = 0;
                if(findStr == "{}")
                {
                    skip = pageSize * (pageNum - 1);
                }
                string sortStr = "{'time':-1}";
                string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projState","projSubState","supportCount", "lastUpdateTime" }).ToString();
                queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, sortStr, skip, pageSize, fieldStr);
            }
            var res = new JObject { { "count", count }, { "list", queryRes } };
            return getRes(res);
        }
        private bool getListFilter(int pageNum, int pageSize, string userId, string manageOrStar, out string filter, out long count)
        {
            filter = "{}";
            count = 0;
            if (manageOrStar == ProjMangeSortType.Managing)
            {
                string findStr = new JObject { { "userId", userId },{ "emailVerifyState", EmailState.hasVerifyAtInvitedYes} }.ToString();
                count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
                if (count == 0) return false;
                string sortStr = "{'time':1}";
                string fieldStr = new JObject { { "projId",1},{ "_id",0} }.ToString();
                var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, sortStr, pageSize*(pageNum-1), pageSize, fieldStr);
                if (queryRes.Count == 0) return false;
                var arr = queryRes.Select(p => p["projId"].ToString()).ToArray();
                filter = MongoFieldHelper.toFilter(arr, "projId").ToString();
                return true;
            }
            else if(manageOrStar == ProjMangeSortType.Staring)
            {
                string findStr = new JObject { { "userId", userId }, { "starState", StarState.StarYes } }.ToString();
                count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr);
                if (count == 0) return false;
                string sortStr = "{'time':1}";
                string fieldStr = new JObject { { "projId", 1 }, { "_id", 0 } }.ToString();
                var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, sortStr, pageSize*(pageNum-1), pageSize, fieldStr);
                if (queryRes.Count == 0) return false;
                var arr = queryRes.Select(p => p["projId"].ToString()).ToArray();
                filter = MongoFieldHelper.toFilter(arr, "projId").ToString();
                return true;
            }
            else
            {
                string findStr = "{}";
                count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
                if (count == 0) return false;
                return true;
            }
        }
        public JArray queryProjDetail(string projId, string userId="")
        {
            string findStr = new JObject { {"projId", projId } }.ToString();
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projName","projTitle","projType", "projConverUrl","projBrief", "projDetail", "supportCount"}).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            getStarState(projId, userId, out bool isStar, out bool isSupport);
            item["isSupport"] = isStar;
            item["isStar"] = isSupport;
            return getRes(item);
        }
        private void getStarState(string projId, string userId, out bool isStar, out bool isSupport)
        {
            isStar = false;
            isSupport = false;
            if (userId == "") return;

            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "starState", 1 }, { "supportState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return;

            isStar = queryRes[0]["starState"].ToString() == StarState.StarYes;
            isSupport = queryRes[0]["supportState"].ToString() == StarState.SupportYes;
            return;
        }
        public JArray queryProjTeamBrief(string projId, int pageNum=1, int pageSize=10)
        {
            string findStr = new JObject { { "projId", projId},{ "emailVerifyState", EmailState.hasVerifyAtInvitedYes} }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (count == 0)
            {
                return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            }
            
            string sortStr = "{'role':1}";
            string fieldStr = new JObject { { "userId",1} }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, sortStr, pageSize*(pageNum-1), pageSize, fieldStr);
            if (queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", new JArray() } });
            }
            var arr = queryRes.Select(p => p["userId"].ToString()).Distinct().ToArray();
            findStr = MongoFieldHelper.toFilter(arr, "userId").ToString();
            fieldStr = new JObject { { "username",1},{ "headIconUrl",1},{ "brief",1},{ "_id",0} }.ToString();
            sortStr = "{}";
            queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, sortStr, 0, pageSize, fieldStr);

            return getRes(new JObject { { "count", count},{ "list", queryRes} }) ;
        }
        // 查询项目更新
        public JArray queryUpdateList(string projId, int pageNum=1, int pageSize=10)
        {
            string findStr = new JObject { { "projId", projId} }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            if(count == 0)
            {
                return getRes(new JObject { { "count",0},{ "list", new JArray()} });
            }
            string sortStr = "{'time':-1}";
            string fieldStr = new JObject { { "updateId",1},{ "updateTitle",1},{ "updateDetail",1},{ "discussCount",1},{"zanCount",1 },{ "lastUpdateTime",1},{ "_id",0} }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            return getRes(new JObject { { "count", count }, { "list", queryRes }});
        }
        
        // 
        public JArray startStarProj(string userId, string accessToken, string projId)
        {
            return starAndSupportProj(userId, accessToken, projId, StarState.StarYes); ;
        }
        public JArray cancelStarProj(string userId, string accessToken, string projId)
        {
            return starAndSupportProj(userId, accessToken, projId, StarState.StarNot); ;
        }
        public JArray startSupportProj(string userId, string accessToken, string projId)
        {
            return starAndSupportProj(userId, accessToken, projId, StarState.SupportYes);
        }
        private JArray starAndSupportProj(string userId, string accessToken, string projId, string starOrSupportState)
        {
            // [看好/关注]: 看好自动关注项目, 看好不能取消
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!StarState.toState(starOrSupportState, out string starState, out string supportState))
            {
                return getErrorRes(DaoReturnCode.projNotSupportOp);
            }
            // 关注 + 取关 + 看好[看好+关注]
            string findStr = new JObject { {"projId", projId},{"userId", userId} }.ToString();
            string fieldStr = new JObject { { "starState",1},{ "supportState",1} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, fieldStr);

            var now = TimeHelper.GetTimeStamp();
            if(queryRes.Count == 0)
            {
                var newdata = new JObject {
                    {"projId", projId},
                    {"userId", userId},
                    {"starState", starState},
                    {"supportState", supportState},
                    {"time", now},
                    {"lastUpdateTime", now},
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, newdata);
            }
            else
            {
                var item = queryRes[0];
                var updateJo = new JObject();
                if(starState != "" && starState != item["starState"].ToString())
                {
                    updateJo.Add("starState", starState);
                }
                if (supportState != "" && supportState != item["supportState"].ToString())
                {
                    updateJo.Add("supportState", supportState);
                }
                if(updateJo.Count > 0)
                {
                    updateJo.Add("lastUpdateTime", now);
                    var updateStr = new JObject { { "$set", updateJo } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, updateStr, findStr);
                }
            }
            return getRes();
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

    class StarState
    {
        public const string StarYes = "10131";
        public const string StarNot = "10132";
        public const string SupportYes = "10133";
        public const string SupportNot = "10134";

        public static bool toState(string state, out string starState, out string supportState)
        {
            if(state == StarYes)
            {
                starState = StarYes;
                supportState = "";
                return true;
            }
            if (state == StarNot)
            {
                starState = StarNot;
                supportState = "";
                return true;
            }
            if (state == SupportYes)
            {
                starState = StarYes;
                supportState = SupportYes;
                return true;
            }
            if (state == SupportNot)
            {
                // 不支持该操作
                starState = StarNot;
                supportState = SupportNot;
                return false;
            }
            starState = "";
            supportState = "";
            return false;

        }
    }
    class ProjMangeSortType
    {
        public const string Managing = "10137";
        public const string Staring = "10138";
    }
}
