using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
        public string projUpdateStarInfoCol { get; set; } = "daoProjUpdateStarInfo";
        public string projStarInfoCol { get; set; } = "daoProjStarInfo";
        public string projSupportInfoCol { get; set; } = "daoProjSupportInfo";

        public string projDiscussInfoCol { get; set; } = "daoProjDiscussInfo";
        public string projUpdateDiscussInfoCol { get; set; } = "daoProjUpdateDiscussInfo";
        public string projDiscussZanInfoCol { get; set; } = "daoProjDiscussZanInfo";
        public string projUpdateDiscussZanInfoCol { get; set; } = "daoProjUpdateDiscussZanInfo";

        public string tokenUrl { get; set; } = "";
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);

        public JArray createProj(string userId, string accessToken, string projName, string projTitle, string projType, string projCoverUrl, string projBrief)
        {
            if(!checkProjNameLen(projName) 
                || !checkProjTitleLen(projTitle)
                || !checkProjBriefLen(projBrief))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (hasRepeatProj(projName, projTitle))
            {
                return getErrorRes(DaoReturnCode.T_RepeatProjNameOrProjTitle);
            }
            if (!isValidUser(userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionCreateProj);
            }
            // TODO
            if (!DaoInfoHelper.StoreFile(oss, bucketName, "", projCoverUrl, out string newProjCoverUrl))
            {
                return getErrorRes(DaoReturnCode.projBriefNotUpload);
            }
            string projId = DaoInfoHelper.genProjId(projName, projTitle);
            long now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "userId", userId},
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
                { "projId", projId.toTemp()},
                { "projName", projName},
                { "projTitle", projTitle},
                { "projType", ProjType.to(projType)},
                { "projConverUrl", newProjCoverUrl},
                { "projBrief", projBrief},
                { "platform", "neo"},
                { "projVideoUrl", ""},
                { "projDetail", ""},
                { "projState", ProjState.Readying},
                { "projSubState", ProjSubState.Init},
                { "connectEmail", ""},
                { "officialWeb", ""},
                { "community", ""},
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
            return getRes(new JObject { { "projId", projId } });
        }
        public JArray deleteProj(string userId, string accessToken, string projId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteProj);
            }
            string findStr = getProjIdFilter(projId);
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            findStr = new JObject { { "projId", projId } }.ToString();
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateStarInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projDiscussZanInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussZanInfoCol, findStr);
            mh.DeleteDataMany(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            return getRes();
        }
        public JArray modifyProjVideo(string userId, string accessToken, string projId, string projVideoUrl, string projDetail)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            if (!canModify(projId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            string findStr = new JObject { { "projId", projId.toTemp() } }.ToString();
            string fieldStr = new JObject { { "projVideoUrl", 1 }, { "projDetail", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            var item = queryRes[0];

            var isUpdate = false;
            var updateJo = new JObject();
            var oldprojVideoUrl = item["projVideoUrl"].ToString();
            if (!DaoInfoHelper.StoreFile(oss, bucketName, oldprojVideoUrl, projVideoUrl, out string newProjVideoUrl))
            {
                return getErrorRes(DaoReturnCode.headIconNotUpload);
            }
            if (oldprojVideoUrl != newProjVideoUrl)
            {
                updateJo.Add("projVideoUrl", newProjVideoUrl);
                isUpdate = true;
            }

            var oldprojDetail = item["projDetail"].ToString();
            if (oldprojDetail != projDetail && projDetail.Trim().Length > 0)
            {
                updateJo.Add("projDetail", projDetail);
                isUpdate = true;
            }
            if (isUpdate)
            {
                updateJo.Add("lastUpdatorId", userId);
                updateJo.Add("lastUpdateTime", TimeHelper.GetTimeStamp());
                var updateStr = new JObject { { "$set", updateJo } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray modifyProjEmail(string userId, string accessToken, string projId, string connectEmail, string officialWeb, string community)
        {
            if(!checkNormalLen(connectEmail)
                || !checkNormalLen(officialWeb)
                || !checkNormalLen(community))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            if (!canModify(projId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            string findStr = new JObject { { "projId", projId.toTemp() } }.ToString();
            string fieldStr = new JObject { { "connectEmail", 1 }, { "officialWeb", 1 }, { "community", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            var item = queryRes[0];

            var isUpdate = false;
            var updateJo = new JObject();
            var oldconnectEmail = item["connectEmail"].ToString();
            var oldofficialWeb = item["officialWeb"].ToString();
            var oldcommunity = item["community"].ToString();
            if (oldconnectEmail != connectEmail && connectEmail.Trim().Length > 0)
            {
                updateJo.Add("connectEmail", connectEmail);
                isUpdate = true;
            }
            if (oldofficialWeb != officialWeb && officialWeb.Trim().Length > 0)
            {
                updateJo.Add("officialWeb", officialWeb);
                isUpdate = true;
            }
            if (oldcommunity != community && community.Trim().Length > 0)
            {
                updateJo.Add("community", community);
                isUpdate = true;
            }
            if (isUpdate)
            {
                updateJo.Add("lastUpdatorId", userId);
                updateJo.Add("lastUpdateTime", TimeHelper.GetTimeStamp());
                var updateStr = new JObject { { "$set", updateJo } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray modifyProjName(string userId, string accessToken, string projId, string projName, string projTitle, string projType, string projConverUrl, string projBrief)
        {
            if (!checkProjNameLen(projName)
                || !checkProjTitleLen(projTitle)
                || !checkProjBriefLen(projBrief))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            string findStr = getProjIdFilter(projId);
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projBrief", "projSubState" }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Any(p => p["projSubState"].ToString() == ProjSubState.Auditing))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            var now = TimeHelper.GetTimeStamp();
            var rr = queryRes.Where(p => p["projId"].ToString() == projId.toTemp()).ToArray();
            if (rr.Count() == 0)
            {
                var newdata = new JObject {
                    { "projId", projId.toTemp()},
                    { "projName", projName},
                    { "projTitle", projTitle},
                    { "projType", ProjType.to(projType)},
                    { "projConverUrl", projConverUrl},
                    { "projBrief", projBrief},
                    { "platform", "neo"},
                    { "projVideoUrl", ""},
                    { "projDetail", ""},
                    { "projState", ProjState.Readying},
                    { "projSubState", ProjSubState.Init},
                    { "connectEmail", ""},
                    { "officialWeb", ""},
                    { "community", ""},
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
            }
            else
            {
                var item = rr[0];

                var isUpdate = false;
                var updateJo = new JObject();
                var oldprojName = item["projName"].ToString();
                if (oldprojName != projName && projName.Trim().Length > 0)
                {
                    updateJo.Add("projName", projName);
                    isUpdate = true;
                }
                var oldprojTitle = item["projTitle"].ToString();
                if (oldprojTitle != projTitle && projTitle.Trim().Length > 0)
                {
                    updateJo.Add("projTitle", projTitle);
                    isUpdate = true;
                }
                var oldprojType = item["projType"].ToString();
                if (oldprojType != projType && projType.Trim().Length > 0)
                {
                    updateJo.Add("projType", projType);
                    isUpdate = true;
                }
                var oldprojConverUrl = item["projConverUrl"].ToString();
                if (!DaoInfoHelper.StoreFile(oss, bucketName, oldprojConverUrl, projConverUrl, out string newProjConverUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                if (oldprojConverUrl != newProjConverUrl)
                {
                    updateJo.Add("projConverUrl", newProjConverUrl);
                    isUpdate = true;
                }
                var oldprojBrief = item["projBrief"].ToString();
                if (oldprojBrief != projBrief && projBrief.Trim().Length > 0)
                {
                    updateJo.Add("projBrief", projBrief);
                    isUpdate = true;
                }
                if (isUpdate)
                {
                    findStr = new JObject { { "projId", item["projId"].ToString() } }.ToString();
                    updateJo.Add("lastUpdatorId", userId);
                    updateJo.Add("lastUpdateTime", TimeHelper.GetTimeStamp());
                    var updateStr = new JObject { { "$set", updateJo } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
                }
            }
            return getRes();
        }
        public JArray queryProj(string userId, string accessToken, string projId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionQueryProj);
            }
            //
            string findStr = getProjIdFilter(projId);
            string sortStr = "{'time':-1}";
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projBrief", "projVideoUrl", "projDetail", "projState", "projSubState", "connectEmail", "officialWeb", "community", "creatorId" }).ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, sortStr, 0, 1, fieldStr);
            var item = queryRes[0];
            item["role"] = item["creatorId"].ToString() == userId ? TeamRoleType.Admin : TeamRoleType.Member;
            item["projId"] = item["projId"].ToString().toNormal();
            return getRes(item);
        }

        private bool checkProjNameLen(string name) => name.Length <= 20;
        private bool checkProjTitleLen(string title) => title.Length <= 40;
        private bool checkProjBriefLen(string brief) => brief.Length <= 400;
        private bool checkNormalLen(string ss) => ss.Length <= 40;
        private bool checkUpdateLen(string title) => title.Length <= 80;

        private bool isValidUser(string userId)
        {
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "emailVerifyState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            var state = queryRes[0]["emailVerifyState"].ToString();
            return state == EmailState.hasVerify
                    || state == EmailState.hasVerifyAtChangeEmail
                    || state == EmailState.hasVerifyAtResetPassword;
        }
        private bool hasRepeatProj(string projName, string projTitle)
        {
            string findStr = new JObject { { "$or", new JArray{
                new JObject{{ "projName", projName } },
                new JObject{{ "projTitle", projTitle } }
            } } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr) > 0;
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
        private string getProjIdFilter(string projId)
        {
            return MongoFieldHelper.toFilter(new string[] { projId, projId.toTemp() }, "projId").ToString();
        }
        private bool canModify(string projId)
        {
            string findStr = getProjIdFilter(projId);
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            return queryRes.All(p => p["projSubState"].ToString() != ProjSubState.Auditing);
        }

        
        //0. query + invite + send + verify
        public JArray queryMember(string userId, string accessToken, string targetEmail/*模糊匹配*/, int pageNum = 1, int pageSize = 10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            var findJo1 = MongoFieldHelper.newRegexFilter(targetEmail, "email");
            var findJo2 = MongoFieldHelper.toFilter(new string[] { EmailState.hasVerify, EmailState.hasVerifyAtChangeEmail, EmailState.hasVerifyAtResetPassword }, "emailVerifyState");
            string findStr = new JObject { { "$and", new JArray { findJo1, findJo2 } } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr);
            if (count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", new JArray { } } });
            }
            string fieldStr = new JObject { { "userId", 1 }, { "username", 1 }, { "email", 1 }, { "headIconUrl", 1 }, { "_id", 0 } }.ToString();
            string sortStr = "{}";
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }
        public JArray inviteMember(string userId, string accessToken, string targetUserId, string projId)
        {
            if (userId == targetUserId)
            {
                // TODO 可换成另一个错误码
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteYourSelf);
            }
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionInviteTeamMember);
            }
            if (!isValidUser(targetUserId))
            {
                // 无效目标用户id
                return getErrorRes(DaoReturnCode.T_InvalidTargetUserId);
            }
            var now = TimeHelper.GetTimeStamp();
            string findStr = new JObject { { "userId", targetUserId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                var newdata = new JObject {
                    { "projId", projId},
                    { "userId", targetUserId},
                    { "authenticationState", TeamAuthenticationState.Init},
                    { "role", TeamRoleType.Member},
                    { "emailVerifyState", EmailState.sendBeforeStateAtInvited},
                    { "emailVerifyCode","" },
                    { "invitorId", userId },
                    { "time", now},
                    { "lastUpdateTime", now}
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            }
            else
            {
                var oldState = queryRes[0]["emailVerifyState"].ToString();
                if (oldState == EmailState.hasVerifyAtInvitedNot)
                {
                    var updateStr = new JObject { { "$set", new JObject {
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
            string findStr = new JObject { { "email", email } }.ToString();
            string fieldStr = new JObject { { "userId", 1 }, { "username", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["username"].ToString() != username)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            var userId = queryRes[0]["userId"].ToString();

            findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            fieldStr = new JObject { { "emailVerifyState", 1 }, { "emailVerifyCode", 1 }, { "username", 1 } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0
                || queryRes[0]["emailVerifyCode"].ToString() != verifyCode)
            {
                return getErrorRes(DaoReturnCode.invalidVerifyCode);
            }
            string oldState = queryRes[0]["emailVerifyState"].ToString();
            if (oldState == EmailState.hasVerifyAtInvitedYes
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
            //
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionQueryTeamMember);
            }
            //
            string findStr = new JObject { { "projId", projId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            var list = new List<string>();
            var match = new JObject { { "$match", new JObject { { "projId", projId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } } } }.ToString();
            var lookup = new JObject{{"$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "userId" },
                {"foreignField", "userId" },
                { "as", "us"}
            } } }.ToString();
            var project = new JObject { { "$project",
                    MongoFieldHelper.toReturn(new string[] { "userId","authenticationState", "role", "us" })
            } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "role", 1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            list.Add(match);
            list.Add(lookup);
            list.Add(project);
            list.Add(sort);
            list.Add(skip);
            list.Add(limit);
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, list);
            if (queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", new JArray { } } });
            }
            var res = queryRes.Select(p => {
                var jo = (JObject)p;
                jo["username"] = jo["us"][0]["username"];
                jo["headIconUrl"] = jo["us"][0]["headIconUrl"];
                jo.Remove("us");
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }

        public JArray createUpdate(string userId, string accessToken, string projId, string updateTitle, string updateDetail)
        {
            if(!checkUpdateLen(updateTitle))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionCreateUpdate);
            }
            // TODO 是否需要检查项目的二级状态: 增删改查

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
            return getRes(new JObject { { "updateId", updateId } });
        }
        public JArray deleteUpdate(string userId, string accessToken, string projId, string updateId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteUpdate);
            }
            string findStr = new JObject { { "projId", projId }, { "updateId", updateId } }.ToString();
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateStarInfoCol, findStr);
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussInfoCol, findStr);
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateDiscussZanInfoCol, findStr);
            return getRes();
        }
        public JArray modifyUpdate(string userId, string accessToken, string projId, string updateId, string updateTitle, string updateDetail)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyUpdate);
            }
            var updateJo = new JObject();
            if (updateTitle.Trim() != "")
            {
                updateJo.Add("updateTitle", updateTitle);
            }
            if (updateDetail.Trim() != "")
            {
                updateJo.Add("updateDetail", updateDetail);
            }
            if (updateJo.Count > 0)
            {
                string findStr = new JObject { { "projId", projId }, { "updateId", updateId } }.ToString();
                var updateStr = new JObject { { "$set", updateJo } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray queryUpdateOld(string projId, string updateId, string userId/*游客为空串*/)
        {
            string findStr = new JObject { { "projId", projId }, { "updateId", updateId } }.ToString();
            string fieldStr = new JObject { { "updateTitle", 1 }, { "updateDetail", 1 }, { "lastUpdatorId", 1 }, { "lastUpdateTime", 1 }, { "time", 1 }, { "discussCount", 1 }, { "zanCount", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getRes();
            }

            var item = queryRes[0];
            var lastUpdatorId = item["lastUpdatorId"].ToString();
            getUserInfo(lastUpdatorId, out string username, out string headIconUrl);
            bool isMember = isProjMember(projId, userId);
            long rank = getUpdateRank(projId, long.Parse(item["time"].ToString()));
            item["username"] = username;
            item["headIconUrl"] = headIconUrl;
            item["isMember"] = isMember;
            item["rank"] = rank;
            var res = (JObject)item;
            res.Remove("time");
            return getRes(res);
        }
        public JArray queryUpdate(string projId, string updateId, string userId/*游客为空串*/)
        {
            var match1 = new JObject { {"$match", new JObject { { "projId", projId }, { "updateId", updateId } } } }.ToString();
            var match = JsonConvert.SerializeObject(new JObject { {"$match", new JObject { { "projId", projId }, { "updateId", updateId } } } });
            var lookup = new JObject { { "$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "lastUpdatorId"},
                {"foreignField", "userId" },
                {"as","us" }
            } } }.ToString();
            var project = new JObject { { "$project", new JObject {
                { "updateTitle", 1 },
                { "updateDetail", 1 },
                { "lastUpdatorId", 1 },
                { "lastUpdateTime", 1 },
                { "time", 1 },
                { "discussCount", 1 },
                { "zanCount", 1 },
                { "us.username",1},
                { "us.headIconUrl",1},
                { "_id", 0 }
            } } }.ToString();
            var list = new List<string> { match ,lookup , project};
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, list);
            if (queryRes.Count == 0)
            {
                return getRes();
            }

            var item = (JObject)queryRes[0];
            item["username"] = ((JArray)item["us"])[0]["username"].ToString();
            item["headIconUrl"] = ((JArray)item["us"])[0]["headIconUrl"].ToString();
            item.Remove("us");

            bool isMember = isProjMember(projId, userId);
            long rank = getUpdateRank(projId, long.Parse(item["time"].ToString()));
            bool isZan = isZanUpdate(updateId, userId);
            item["isMember"] = isMember;
            item["rank"] = rank;
            item["isZan"] = isZan;
            item.Remove("time");
            return getRes(item);
        }
        private bool isZanUpdate(string updateId, string userId)
        {
            if (userId == "") return false;
            string findStr = new JObject { { "updateId", updateId},{ "userId",userId} }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateStarInfoCol, findStr) > 0;
        }

        private bool getUserInfo(string userId, out string username, out string headIconUrl)
        {
            username = "";
            headIconUrl = "";
            string findStr = new JObject { { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "username", 1 }, { "headIconUrl", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            username = item["username"].ToString();
            headIconUrl = item["headIconUrl"].ToString();
            return true;
        }
        private long getUpdateRank(string projId, long time)
        {
            string findStr = new JObject { { "projId", projId }, { "time", new JObject { { "$lt", time } } } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            return count + 1;
        }


        public JArray commitProjAudit(string userId, string accessToken, string projId)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projName", "projTitle", "projConverUrl", "projSubState", "projBrief", "projDetail", "connectEmail" }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.projRequiredFieldIsEmpty);
            }
            var item = queryRes[0];
            if (item["projName"].ToString().Trim() == ""
                || item["projTitle"].ToString().Trim() == ""
                || item["projConverUrl"].ToString().Trim() == ""
                || item["projBrief"].ToString().Trim() == ""
                || item["projDetail"].ToString().Trim() == ""
                || item["connectEmail"].ToString().Trim() == "")
            {
                return getErrorRes(DaoReturnCode.projRequiredFieldIsEmpty);
            }
            //
            if (item["projSubState"].ToString() != ProjSubState.Auditing)
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
        public JArray queryProjList(int pageNum = 1, int pageSize = 10)
        {
            //return queryProjListPrivate(pageNum, pageSize);
            string findStr = new JObject { { "projState", ProjState.IdeaPub }, { "projSubState", ProjSubState.Init } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            string sortStr = "{'time':-1}";
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projState", "projSubState", "supportCount", "lastUpdateTime" }).ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            return getRes(new JObject { { "count", count},{ "list", queryRes} });
        }
        public JArray queryProjListAtManage(string userId, string accessToken, int pageNum = 1, int pageSize = 10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            //return queryProjListPrivate(pageNum, pageSize, userId, ProjMangeSortType.Managing);
            string findStr = new JObject { { "userId", userId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            string sortStr = "{'time':1}";
            string fieldStr = new JObject { { "projId", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            var arr = queryRes.Select(p => p["projId"].ToString()).Distinct().ToArray();
            arr = getProjTempAndOther(arr);

            findStr = MongoFieldHelper.toFilter(arr, "projId").ToString();
            fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projState", "projSubState", "supportCount", "lastUpdateTime" }).ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            return getRes(new JObject { { "count", count},{ "list", toFormat(queryRes)} });
        }
        public JArray queryProjListAtStar(string userId, string accessToken, int pageNum = 1, int pageSize = 10)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            //return queryProjListPrivate(pageNum, pageSize, userId, ProjMangeSortType.Staring);
            var findJo = new JObject { { "userId", userId }, { "starState", StarState.StarYes } };
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findJo.ToString());
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var match = new JObject { { "$match", findJo } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "time", 1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var lookup = new JObject { { "$lookup", new JObject {
                { "from", projInfoCol},
                { "localField", "projId"},
                { "foreignField", "projId" },
                { "as", "ps"}
            } } }.ToString();
            var project = new JObject { { "$project", new JObject { { "ps",1} } } }.ToString();
            var list = new List<string> { match, sort, skip, limit, lookup, project};
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, list);

            var res = queryRes.Select(p => {
                var ps = ((JArray)p["ps"])[0];
                return new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projTitle", ps["projTitle"]},
                    { "projType", ps["projType"]},
                    { "projConverUrl", ps["projConverUrl"]},
                    { "projState", ps["projState"]},
                    { "projSubState", ps["projSubState"]},
                    { "supportCount", ps["supportCount"]},
                    { "lastUpdateTime", ps["lastUpdateTime"]}
                };
            }).ToArray();

            return getRes(new JObject { { "count", count},{ "list", new JArray { res } } });
        }
        public JArray queryProjListPrivate(int pageNum, int pageSize, string userId = "", string manageOrStar = "")
        {
            JArray queryRes = new JArray();
            if (!getListFilter(pageNum, pageSize, userId, manageOrStar, out string findStr, out long count))
            {
                return getRes(new JObject { { "count", count }, { "list", queryRes } });
            }
            //long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            //JArray queryRes = new JArray();
            //if(count > 0)
            {
                int skip = 0;
                if (findStr == "{}")
                {
                    skip = pageSize * (pageNum - 1);
                }
                string sortStr = "{'time':-1}";
                string fieldStr = MongoFieldHelper.toReturn(new string[] { "projId", "projName", "projTitle", "projType", "projConverUrl", "projState", "projSubState", "supportCount", "lastUpdateTime" }).ToString();
                queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, sortStr, skip, pageSize, fieldStr);
            }
            
            var res = new JObject { { "count", count }, { "list", toFormat(queryRes) } };
            return getRes(res);
        }
        private JArray toFormat(JArray queryRes)
        {
            var res = queryRes.Select(p => {
                p["projId"] = p["projId"].ToString().toNormalId();
                return p;
            }).ToArray();
            return new JArray { res };
        }
        private bool getListFilter(int pageNum, int pageSize, string userId, string manageOrStar, out string filter, out long count)
        {
            filter = "{}";
            count = 0;
            if (manageOrStar == ProjMangeSortType.Managing)
            {
                string findStr = new JObject { { "userId", userId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } }.ToString();
                count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
                if (count == 0) return false;
                string sortStr = "{'time':1}";
                string fieldStr = new JObject { { "projId", 1 }, { "_id", 0 } }.ToString();
                var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
                if (queryRes.Count == 0) return false;
                var arr = queryRes.Select(p => p["projId"].ToString()).ToArray();
                arr = getProjTempAndOther(arr);
                
                filter = MongoFieldHelper.toFilter(arr, "projId").ToString();
                return true;
            }
            else if (manageOrStar == ProjMangeSortType.Staring)
            {
                string findStr = new JObject { { "userId", userId }, { "starState", StarState.StarYes } }.ToString();
                count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr);
                if (count == 0) return false;
                string sortStr = "{'time':1}";
                string fieldStr = new JObject { { "projId", 1 }, { "_id", 0 } }.ToString();
                var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
                if (queryRes.Count == 0) return false;
                var arr = queryRes.Select(p => p["projId"].ToString()).ToArray();
                filter = MongoFieldHelper.toFilter(arr, "projId").ToString();
                return true;
            }
            else
            {
                string findStr = new JObject { { "projState", ProjState.IdeaPub},{ "projSubState", ProjSubState.Init} }.ToString();
                count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
                if (count == 0) return false;
                return true;
            }
        }
        private string[] getProjTempAndOther(string[] arr)
        {
            var arrDict = getProjTemp(arr);
            if (arrDict.Count > 0)
            {
                arr = arr.Select(p => {
                    if (arrDict.GetValueOrDefault(p, false))
                    {
                        return "temp_" + p;
                    }
                    return p;
                }).ToArray();
            }
            return arr;
        }
        private Dictionary<string, bool> getProjTemp(string[] arr)
        {
            string[] arrTemp = arr.Select(p => "temp_" + p).ToArray();
            string findStr = MongoFieldHelper.toFilter(arrTemp, "projId").ToString();
            string fieldStr = new JObject { { "projId", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return new Dictionary<string, bool>();

            return queryRes.ToDictionary(k => k["projId"].ToString().Substring(5), v => true);
        }
        public JArray queryProjDetail(string projId, string userId = "")
        {
            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projName", "projTitle", "projType", "projConverUrl", "projVideoUrl", "projBrief", "projDetail", "supportCount","discussCount", "updateCount","time" }).ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            getStarState(projId, userId, out bool isStar, out bool isSupport);
            item["isSupport"] = isSupport;
            item["isStar"] = isStar;
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
        public JArray getProjInfo(string projId)
        {
            // TODO 可优化
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
        public JArray queryProjTeamBrief(string projId, int pageNum = 1, int pageSize = 10)
        {
            string findStr = new JObject { { "projId", projId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (count == 0)
            {
                return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            }

            string sortStr = "{'role':1}";
            string fieldStr = new JObject { { "userId", 1 } }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            if (queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", new JArray() } });
            }
            var arr = queryRes.Select(p => p["userId"].ToString()).Distinct().ToArray();
            findStr = MongoFieldHelper.toFilter(arr, "userId").ToString();
            fieldStr = new JObject { { "username", 1 }, { "headIconUrl", 1 }, { "brief", 1 }, { "_id", 0 } }.ToString();
            sortStr = "{}";
            queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, userInfoCol, findStr, sortStr, 0, pageSize, fieldStr);

            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }
        public JArray queryUpdateList(string projId, int pageNum = 1, int pageSize = 10)
        {
            string findStr = new JObject { { "projId", projId } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            if (count == 0)
            {
                return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            }
            string sortStr = "{'time':-1}";
            string fieldStr = new JObject { { "updateId", 1 }, { "updateTitle", 1 }, { "updateDetail", 1 }, { "discussCount", 1 }, { "zanCount", 1 }, { "lastUpdateTime", 1 }, { "_id", 0 } }.ToString();
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, sortStr, pageSize * (pageNum - 1), pageSize, fieldStr);
            return getRes(new JObject { { "count", count }, { "list", queryRes } });
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
            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "starState", 1 }, { "supportState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, fieldStr);

            var now = TimeHelper.GetTimeStamp();
            if (queryRes.Count == 0)
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
                if (starState != "" && starState != item["starState"].ToString())
                {
                    updateJo.Add("starState", starState);
                }
                if (supportState != "" && supportState != item["supportState"].ToString())
                {
                    updateJo.Add("supportState", supportState);
                }
                if (updateJo.Count > 0)
                {
                    updateJo.Add("lastUpdateTime", now);
                    var updateStr = new JObject { { "$set", updateJo } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, updateStr, findStr);
                }
            }
            return getRes();
        }

        // 
        public JArray getStarMangeProjCount(string userId, string accessToken)
        {
            if (!TokenHelper.checkAccessToken(tokenUrl, userId, accessToken, out string code))
            {
                return getErrorRes(code);
            }
            string findStr = new JObject { { "userId", userId }, { "starState", StarState.StarYes } }.ToString();
            long starCount = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr);
            findStr = new JObject { { "userId", userId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes} }.ToString();
            long manageCount = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);

            return getRes(new JObject { { "starCount", starCount},{ "manageCount",manageCount} });
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
