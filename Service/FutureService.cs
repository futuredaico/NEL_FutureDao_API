using Microsoft.AspNetCore.Mvc;
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
    public class FutureService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr { get; set; }
        public string dao_mongodbDatabase { get; set; }
        public string projInfoCol { get; set; } = "moloprojinfos";
        public string projTeamInfoCol { get; set; } = "moloprojteaminfos";
        public string userInfoCol { get; set; } = "daouserinfos";
        public string projUpdateInfoCol { get; set; } = "daoprojupdateinfos";
        public string projUpdateStarInfoCol { get; set; } = "daoprojupdatestarinfos";
        public string projStarInfoCol { get; set; } = "daoprojstarinfos";
        public string projSupportInfoCol { get; set; } = "daoprojsupportinfos";

        public string projDiscussInfoCol { get; set; } = "daoprojdiscussinfos";
        public string projUpdateDiscussInfoCol { get; set; } = "daoprojupdatediscussinfos";
        public string projDiscussZanInfoCol { get; set; } = "daoprojdiscusszaninfos";
        public string projUpdateDiscussZanInfoCol { get; set; } = "daoprojupdatediscusszaninfos";
        //
        public string projFinanceCol { get; set; } = "daoprojfinanceinfos";
        public string projFinanceFundPoolCol { get; set; } = "daoprojfinancefundpoolinfos";

        public string projBalanceInfoCol { get; set; } = "";

        public string tokenUrl { get; set; } = "";
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }
        public UserServiceV3 us { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);

        //1. create + delete + modify + query
        public JArray createProj(Controller controller, string projName, string projTitle, string projBrief, string officialWeb, string projCoverUrl, string projVideoUrl, string projDetail)
        {
            if (!checkProjNameLen(projName)
                || !checkProjTitleLen(projTitle)
                || !checkProjBriefLen(projBrief))
            {
                return getErrorRes(DaoReturnCode.lenExceedingThreshold);
            }
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string address))
            {
                return getErrorRes(code);
            }
            // 去重
            if (hasRepeatProj(projName, projTitle))
            {
                return getErrorRes(DaoReturnCode.T_RepeatProjNameOrProjTitle);
            }
            // 封面
            if (!DaoInfoHelper.StoreFile(oss, bucketName, "", projCoverUrl, out string newProjCoverUrl))
            {
                return getErrorRes(DaoReturnCode.headIconNotUpload);
            }
            projCoverUrl = newProjCoverUrl;
            // 视频
            if (!DaoInfoHelper.StoreFile(oss, bucketName, "", projCoverUrl, out string newProjVideoUrl))
            {
                return getErrorRes(DaoReturnCode.headIconNotUpload);
            }
            projVideoUrl = newProjVideoUrl;
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

            // ->项目成员
            string projId = DaoInfoHelper.genProjId(projName, projTitle);
            long now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "userId", userId},
                { "address", address},
                { "role", TeamRole.Admin.name},
                { "roleLevel", TeamRole.Admin.level},
                { "invitorId","" },
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            // ->项目成员
            newdata = new JObject {
                { "projId", projId},
                { "projName", projName},
                { "projTitle", projTitle},
                { "projType", ProjType.Future},
                { "projBrief", projBrief},
                { "officialWeb", officialWeb},
                { "projCoverUrl", projCoverUrl},
                { "projVideoUrl", projVideoUrl},
                { "projDetail", projDetail},
                { "platform", "eth"}, // 前端已去掉,后台暂保留
                { "projState", ProjState.IdeaPub}, // 新创建项目直接进入创意发布状态
                { "activeState",  ActiveState.DisplayYes}, // 默认可显示在首页
                { "userId", userId},
                { "lastUpdatorId", userId},
                { "hasTokenCount",0}, //
                { "starCount", 0},
                { "discussCount", 0},
                { "updateCount", 0},
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, newdata);
            return getRes(new JObject { { "projId", projId } });
        }
        public JArray deleteProj(Controller controller, string projId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 管理员权限
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionDeleteProj);
            }
            // TODO ...
            if (hasStartFinance(projId))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
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
        public JArray modifyProj(Controller controller, string projId, string projBrief, string officialWeb, string projCoverUrl, string projVideoUrl, string projDetail)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 项目成员权限
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }

            var findStr = new JObject { { "projId", projId  } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if(queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.C_InvalidProjInfo);
            }
            var item = queryRes[0];

            var isUpdate = false;
            var updateJo = new JObject();
            // 
            var oldprojBrief = item["projBrief"].ToString();
            if(oldprojBrief != projBrief)
            {
                updateJo["projBrief"] = projBrief;
                isUpdate = true;
            }
            var oldofficialWeb = item["officialWeb"].ToString();
            if (oldofficialWeb != officialWeb)
            {
                updateJo["officialWeb"] = officialWeb;
                isUpdate = true;
            }
            var oldprojCoverUrl = item["projCoverUrl"].ToString();
            if (oldprojCoverUrl != projCoverUrl && oldprojCoverUrl.toTemp() != projCoverUrl)
            {
                if (!DaoInfoHelper.StoreFile(oss, bucketName, oldprojCoverUrl, projCoverUrl, out string newProjCoverUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                if (oldprojCoverUrl != newProjCoverUrl)
                {
                    updateJo.Add("projCoverUrl", newProjCoverUrl);
                    isUpdate = true;
                }
            }
            var oldprojVideoUrl = item["projVideoUrl"].ToString();
            if (oldprojVideoUrl != projVideoUrl && oldprojVideoUrl.toTemp() != projVideoUrl)
            {
                if (!DaoInfoHelper.StoreFile(oss, bucketName, oldprojVideoUrl, projVideoUrl, out string newProjVideoUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                if (oldprojVideoUrl != newProjVideoUrl)
                {
                    updateJo.Add("projVideoUrl", newProjVideoUrl);
                    isUpdate = true;
                }
            }
            var oldprojDetail = item["projDetail"].ToString();
            if (oldprojDetail != projDetail)// && projDetail.Trim().Length > 0)
            {
                var olist = oldprojDetail.catchFileUrl();
                var nlist = projDetail.catchFileUrl();
                // 添加新的
                var ll = nlist.Except(olist).ToList();
                foreach (var ii in ll)
                {
                    if (ii.Trim().Length == 0) continue;
                    if (!DaoInfoHelper.StoreFile(oss, bucketName, "", ii, out string newUrl))
                    {
                        return getErrorRes(DaoReturnCode.headIconNotUpload);
                    }
                    projDetail = projDetail.Replace(ii, newUrl);
                }
                // 删除老的
                olist.Except(nlist).ToList().ForEach(p => {
                    DaoInfoHelper.StoreFile(oss, bucketName, p, "", out string newUrl);
                });
                if(oldprojDetail != projDetail)
                {
                    updateJo.Add("projDetail", projDetail);
                    isUpdate = true;
                }
            }
            if (isUpdate)
            {
                long now = TimeHelper.GetTimeStamp();
                findStr = new JObject { { "projId", projId } }.ToString();
                updateJo.Add("lastUpdatorId", userId);
                updateJo.Add("lastUpdateTime", now);
                var updateStr = new JObject { { "$set", updateJo } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray queryProj(Controller controller, string projId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 项目成员权限
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            //
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.C_InvalidProjInfo);
            }
            var item = queryRes[0];
            item["role"] = item["userId"].ToString() == userId ? TeamRoleType.Admin : TeamRoleType.Member;
            return getRes(item);
        }
        //
        // 查询项目(管理中/关注中/参与中)
        public JArray queryProjListAtManage(Controller controller, int pageNum = 1, int pageSize = 10)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            {
                return getErrorRes(code);
            }
            var findJo = new JObject { { "address", userAddress/*不用userId原因是被邀请后id未及时更新*/ } };
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findJo.ToString());
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            //
            var match = new JObject { { "$match", findJo } }.ToString();
            var lookup = new JObject{{"$lookup", new JObject {
                {"from", projInfoCol },
                {"localField", "projId" },
                {"foreignField", "projId" },
                { "as", "ps"}
            } } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "lastUpdatTime", 1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var list = new List<string> { match, sort, skip, limit, lookup };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, list);
            if (queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", new JArray { } } });
            }
            var res = queryRes.Select(p =>
            {
                var ps = ((JArray)p["ps"])[0];
                return new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projBrief", ps["projBrief"]},
                    { "projCoverUrl", ps["projCoverUrl"]},
                    { "tokenTotal", ps["tokenTotal"] == null ? 0:int.Parse(ps["tokenTotal"].ToString())},
                    { "hasTokenCount", ps["hasTokenCount"]}
                };
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        public JArray queryProjListAtStar(Controller controller, int pageNum = 1, int pageSize = 10)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            //
            var findJo = new JObject { { "userId", userId }, { "starState", StarState.StarYes } };
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findJo.ToString());
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
            var project = new JObject { { "$project", new JObject { { "ps", 1 } } } }.ToString();
            var list = new List<string> { match, sort, skip, limit, lookup, project };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, list);

            var res = queryRes.Select(p => {
                var ps = ((JArray)p["ps"])[0];
                return new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projBrief", ps["projBrief"]},
                    { "projCoverUrl", ps["projCoverUrl"]},
                    { "tokenTotal", ps["tokenTotal"] == null ? 0:int.Parse(ps["tokenTotal"].ToString())},
                    { "hasTokenCount", ps["hasTokenCount"]}
                };
            }).ToArray();

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        public JArray queryProjListAtJoin(Controller controller, int pageNum = 1, int pageSize = 10)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            {
                return getErrorRes(code);
            }

            var findJo = new JObject { { "address", userAddress }, { "balance", new JObject { { "$gt", 0 } } } };
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, findJo.ToString());
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
            var project = new JObject { { "$project", new JObject { { "ps", 1 } } } }.ToString();
            var list = new List<string> { match, sort, skip, limit, lookup, project };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, list);

            var res = queryRes.Select(p => {
                var ps = ((JArray)p["ps"])[0];
                return new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projBrief", ps["projBrief"]},
                    { "projCoverUrl", ps["projCoverUrl"]},
                    { "tokenTotal", ps["tokenTotal"] == null ? 0:int.Parse(ps["tokenTotal"].ToString())},
                    { "hasTokenCount", ps["hasTokenCount"]}
                };
            }).ToArray();

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }

        private bool checkProjNameLen(string name) => name.Length <= 30;
        private bool checkProjTitleLen(string title) => title.Length <= 60;
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
            var roleLevel = isNeedAdmin ? TeamRole.Admin.level : TeamRole.Member.level;
            var findStr = new JObject {
                { "projId", projId },
                { "userId", userId },
                { "roleLevel", new JObject { { "$lte", roleLevel} } } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            return count > 0;
        }
        private bool hasStartFinance(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr) > 0;
        }
        private string getProjIdFilter(string projId)
        {
            return MongoFieldHelper.toFilter(new string[] { projId, projId.toTemp() }, "projId").ToString();
        }
        
        //2. invite + delete + modify + query
        public JArray inviteMember(Controller controller, string address, string role, string projId)
        {
            address = address.ToLower();
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 管理权限
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }

            var findStr = new JObject { { "projId", projId }, { "address", address} }.ToString();
            if(mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            var rInfo = (role == TeamRole.Admin.name) ? TeamRole.Admin : TeamRole.Member;
            long now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId },
                { "userId", DaoInfoHelper.genUserId(address, address, address)},
                { "address", address },
                { "role", rInfo.name},
                { "roleLevel", rInfo.level},
                { "invitorId", userId },
                { "time", now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
            return getRes();
        }
        public JArray deleteMember(Controller controller, string address, string projId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            {
                return getErrorRes(code);
            }
            // 管理权限
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }

            var findStr = new JObject { { "projId", projId }, { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var roleLevel = int.Parse(item["roleLevel"].ToString());
            if(roleLevel == TeamRole.Admin.level && userAddress != address)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            
            return getRes();
        }
        public JArray modifyMember() { return null; }
        public JArray queryMember() { return null; }
        public JArray queryMemberList(Controller controller, string projId, int pageNum, int pageSize)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            {
                return getErrorRes(code);
            }
            // 成员权限
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyProj);
            }
            //
            var findStr = new JObject { { "projId", projId } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            var list = new List<string>();
            var match = new JObject { { "$match", new JObject { { "projId", projId }} } }.ToString();
            var lookup = new JObject{{"$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "address" },
                {"foreignField", "address" },
                { "as", "us"}
            } } }.ToString();
            var project = new JObject { { "$project",
                    MongoFieldHelper.toReturn(new string[] { "userId","address", "role", "us" })
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
                var jo = new JObject();
                jo["userId"] = p["userId"];
                jo["address"] = p["address"];
                jo["role"] = p["role"];
                jo["username"] = "";
                jo["headIconUrl"] = "";
                if (((JArray)p["us"]).Count > 0)
                {
                    jo["username"] = p["us"][0]["username"];
                    jo["headIconUrl"] = p["us"][0]["headIconUrl"];
                }
                jo["isMine"] = p["address"].ToString() == userAddress;
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        //
        
        //3. create + delete + modify + query
        public JArray createUpdate(Controller controller, string projId, string updateTitle, string updateDetail)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 成员权限
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionCreateUpdate);
            }
            //
            var nlist = updateDetail.catchFileUrl();
            foreach (var ii in nlist)
            {
                if (ii.Trim().Length == 0) continue;
                if (!DaoInfoHelper.StoreFile(oss, bucketName, "", ii, out string newUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                updateDetail = updateDetail.Replace(ii, newUrl);
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
                { "userId",  userId},
                { "lastUpdatorId",  userId},
                { "time",  now},
                { "lastUpdateTime", now},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, newdata);
            return getRes(new JObject { { "updateId", updateId } });
        }
        public JArray deleteUpdate(Controller controller, string projId, string updateId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 成员权限
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
        public JArray modifyUpdate(Controller controller, string projId, string updateId, string updateTitle, string updateDetail)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 成员权限
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyUpdate);
            }
            var updateJo = new JObject();
            if (updateTitle.Trim() != "")
            {
                updateJo.Add("updateTitle", updateTitle);
            }
            var nlist = updateDetail.catchFileUrl();
            foreach (var ii in nlist)
            {
                if (ii.Trim().Length == 0) continue;
                if (!DaoInfoHelper.StoreFile(oss, bucketName, "", ii, out string newUrl))
                {
                    return getErrorRes(DaoReturnCode.headIconNotUpload);
                }
                updateDetail = updateDetail.Replace(ii, newUrl);
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
        public JArray queryUpdate(Controller controller, string projId, string updateId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // TODO: 成员权限(管理中) + 关注中 + 参与中
            var isMember = isProjMember(projId, userId);
            if (!isMember || false || false)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyUpdate);
            }
            //
            var findStr = new JObject {{ "updateId", updateId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.S_InvalidUpdateId);
            }

            var item = queryRes[0];
            var updateTitle = item["updateTitle"].ToString();
            var updateDetail = item["updateDetail"].ToString();
            var lastUpdateTime = item["lastUpdateTime"].ToString();
            getUserInfo(userId, out string username, out string headIconUrl);

            var res = new JObject {
                { "updateTitle",updateTitle},
                { "updateDetail",updateDetail},
                { "lastUpdateTime",lastUpdateTime},
                { "username",username},
                { "headIconUrl",headIconUrl},
                { "isMember",isMember},
            };
            return getRes(res); ;
        }
        public JArray queryUpdateList(Controller controller, string projId, int pageNum, int pageSize)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // TODO: 成员权限(管理中) + 关注中 + 参与中
            var isMember = isProjMember(projId, userId);
            if (!isMember || false || false)
            {
                return getErrorRes(DaoReturnCode.T_HaveNotPermissionModifyUpdate);
            }

            var findStr = new JObject { { "projId", projId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            var sortStr = new JObject { { "lastUpdateTime", -1 } }.ToString();
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, sortStr, skip, limit);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray { } } });

            var list = queryRes.Select(p => {
                return new JObject {
                    {"updateTitle", p["updateTitle"]},
                    { "updateDetail",p["updateDetail"]},
                    { "lastUpdateTime",p["lastUpdateTime"]}
                };
            }).ToArray();
            var res = new JObject { { "count", count }, { "list", new JArray { list } } };
            return getRes(res) ;
        }


        // ---------------->
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
        public JArray queryUpdateForAll(string projId, string updateId, string userId/*游客为空串*/)
        {
            var match1 = new JObject { { "$match", new JObject { { "projId", projId }, { "updateId", updateId } } } }.ToString();
            var match = JsonConvert.SerializeObject(new JObject { { "$match", new JObject { { "projId", projId }, { "updateId", updateId } } } });
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
            var list = new List<string> { match, lookup, project };
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
            string findStr = new JObject { { "updateId", updateId }, { "userId", userId } }.ToString();
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
            string findStr = new JObject { { "projId", projId.toTemp() } }.ToString();
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


        //
        public JArray queryProjList(int pageNum = 1, int pageSize = 10)
        {
            var findStr = "{}";
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (count == 0)
            {
                return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            }

            var sortStr = "{'hasTokenCount':-1}";
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, sortStr, skip, limit);
            if(queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });
            }

            var list = queryRes.Select(p => {
                var jo = new JObject();
                jo["projId"] = p["projId"];
                jo["projName"] = p["projName"];
                jo["projBrief"] = p["projBrief"];
                jo["projType"] = p["projType"];

                var projState = p["projState"];
                if(projState != null && projState.ToString() == ProjState.IdeaPub)
                {
                    jo["projState"] = p["projState"];
                } else
                {
                    jo["tokenTotal"] = p["tokenTotal"];
                    jo["hasTokenCount"] = p["hasTokenCount"];
                }
                return jo;
            });

            return getRes(new JObject { { "count", count }, { "list", queryRes } });
        }
        public JArray queryProjDetail(Controller controller, string projId)
        {
            // TODO: 
            //if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            //{
            //}
            var userId = "";
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            getStarState(projId, userId, out bool isStar, out bool isSupport);
            item["isSupport"] = isSupport;
            item["isStar"] = isStar;
            item["hasIssueAmt"] = "0";
            item["hasSellAmt"] = "0";
            item["hasSupport"] = "0";
            item["fundReservePoolTotal"] = "0";
            if (getProjFinanceInfo(projId, out string hasIssueAmt, out string hasSellAmt, out string hasSupport, out string fundReservePoolTotal))
            {
                item["hasIssueAmt"] = hasIssueAmt;
                item["hasSellAmt"] = hasSellAmt;
                item["hasSupport"] = hasSupport;
                item["fundReservePoolTotal"] = fundReservePoolTotal;
            }
            item["type"] = "";
            item["platform"] = "";
            item["fundName"] = "";
            if (getProjFinanceType(projId, out string type, out string platform, out string fundName))
            {
                item["type"] = type;
                item["platform"] = platform;
                item["fundName"] = fundName;
            }
            return getRes(item);
        }
        //
        public JArray queryProjUpdateList(Controller controller, string projId)
        {
            return null;
        }
        public JArray queryProjUpdateDetail(Controller controller, string projId, string updateId)
        {
            return null;
        }


        //
        public JArray manualAddProj(Controller controller, string projName, string projTitle, string projType, string projVersion, string projDetail, string projCoverUrl, string minimumTribute, string approved, string email, string summoner, string molochDaoAddress, JArray contractHash)
        {
            /*
             * 
查子合约哈希
查支持资产哈希
查资产 哈希/精度/名称
查 时间段/投票期长度/公示期长度/取消期长度/押金/奖励
             */
            //

            return getRes();
        }


        // ------------------------------------------------------------------------------->
        private bool getProjFinanceInfo(string projId, out string hasIssueAmt, out string hasSellAmt, out string hasSupport, out string fundReservePoolTotal)
        {
            hasIssueAmt = "";
            hasSellAmt = "";
            hasSupport = "";
            fundReservePoolTotal = "";

            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "hasOnBuyFundTotal", 1 }, { "hasIssueTokenTotal", 1 }, { "hasSupport", 1 }, { "fundReservePoolTotal", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceFundPoolCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            hasIssueAmt = queryRes[0]["hasIssueTokenTotal"].ToString().formatDecimal();
            hasSellAmt = queryRes[0]["hasOnBuyFundTotal"].ToString().formatDecimal().formatEth();
            hasSupport = queryRes[0]["hasSupport"].ToString();
            fundReservePoolTotal = queryRes[0]["fundReservePoolTotal"].ToString().formatDecimal().formatEth();
            return true;
        }
        private bool getProjFinanceType(string projId, out string type, out string platform, out string tokenName)
        {
            type = "";
            platform = "";
            tokenName = "";
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "type", 1 }, { "platform", 1 }, { "fundName", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            type = queryRes[0]["type"].ToString();
            platform = queryRes[0]["platform"].ToString();
            tokenName = queryRes[0]["fundName"].ToString();
            return true;
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
            findStr = new JObject { { "userId", userId }, { "emailVerifyState", EmailState.hasVerifyAtInvitedYes } }.ToString();
            long manageCount = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);

            return getRes(new JObject { { "starCount", starCount }, { "manageCount", manageCount } });
        }
    }

    class TeamRole
    {
        public static RoleType Admin = new RoleType { name = "admin", level = 1};
        public static RoleType Member = new RoleType { name = "member", level = 2};
    }
    class RoleType
    {
        public string name { get; set; }
        public int level { get; set; }
    }
    class ActiveState
    {
        // 
        public const string DisplayYes = "1";
        public const string DisplayNot = "0";
    }
}
