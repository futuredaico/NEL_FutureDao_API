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
        public string projTeamInfoCol { get; set; } = "daoprojteaminfos";
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
        public string projFinanceInfoCol { get; set; } = "daoprojfinanceinfos";
        public string projFinanceRewardInfoCol { get; set; } = "daoprojfinancerewardinfos";
        public string projFinanceFundPoolCol { get; set; } = "daoprojfinancefundpoolinfos";

        public string projBalanceInfoCol { get; set; } = "moloprojbalanceinfos";

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
            // 启动融资后不能删项目
            if (hasStartFinance(projId))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var findStr = new JObject { { "projId", projId } }.ToString();
            mh.DeleteData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
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
            var jo = new JObject();
            jo["projId"] = item["projId"];
            jo["projName"] = item["projName"];
            jo["projTitle"] = item["projTitle"];
            jo["projBrief"] = item["projBrief"];
            jo["officialWeb"] = item["officialWeb"];
            jo["projCoverUrl"] = item["projCoverUrl"];
            jo["projVideoUrl"] = item["projVideoUrl"];
            jo["projDetail"] = item["projDetail"];
            jo["role"] = item["userId"].ToString() == userId ? TeamRoleType.Admin : TeamRoleType.Member;
            jo["startFinanceFlag"] = 0;
            if (item["startFinanceFlag"] != null)
            {
                jo["startFinanceFlag"] = (int)item["startFinanceFlag"];
            }
            return getRes(jo);
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
            var sort = new JObject { { "$sort", new JObject { { "lastUpdateTime", -1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var list = new List<string> { match, sort, skip, limit, lookup };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, list);
            if (queryRes.Count == 0)
            {
                return getRes(new JObject { { "count", count }, { "list", new JArray { } } });
            }
            var res = queryRes.Select(p => {
                var ps = ((JArray)p["ps"])[0];
                //
                var jo = new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projType", ps["projType"] },
                    { "projBrief", ps["projBrief"]},
                    { "projCoverUrl", ps["projCoverUrl"]},
                    //{ "tokenTotal", ps["tokenTotal"] == null ? 0:int.Parse(ps["tokenTotal"].ToString())},
                    //{ "hasTokenCount", ps["hasTokenCount"]}
                };
                jo["projState"] = "";
                if (ps["projState"] != null)
                {
                    jo["projState"] = ps["projState"];
                }
                jo["shares"] = 0;
                if (ps["tokenTotal"] != null)
                {
                    jo["shares"] = ps["tokenTotal"];
                }
                jo["members"] = ps["hasTokenCount"];
                return jo;
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
            var sort = new JObject { { "$sort", new JObject { { "lastUpdateTime", -1 } } } }.ToString();
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
                //
                var jo = new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projType", ps["projType"] },
                    { "projBrief", ps["projBrief"]},
                    { "projCoverUrl", ps["projCoverUrl"]},
                    //{ "tokenTotal", ps["tokenTotal"] == null ? 0:int.Parse(ps["tokenTotal"].ToString())},
                    //{ "hasTokenCount", ps["hasTokenCount"]}
                };
                jo["projState"] = "";
                if (ps["projState"] != null)
                {
                    jo["projState"] = ps["projState"];
                }
                jo["shares"] = 0;
                if (ps["tokenTotal"] != null)
                {
                    jo["shares"] = ps["tokenTotal"];
                }
                jo["members"] = ps["hasTokenCount"];
                return jo;
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

            var findJo = new JObject { { "address", userAddress }, { "type", "0"}, { "balance", new JObject { { "$gt", 0 } } } };
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, findJo.ToString());
            if (count == 0) return getRes(new JObject { { "count", 0 }, { "list", new JArray() } });

            var match = new JObject { { "$match", findJo } }.ToString();
            var sort = new JObject { { "$sort", new JObject { { "lastUpdateTime", -1 } } } }.ToString();
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
                //
                var jo =  new JObject {
                    { "projId", ps["projId"]},
                    { "projName", ps["projName"]},
                    { "projType", ps["projType"] },
                    { "projBrief", ps["projBrief"]},
                    { "projCoverUrl", ps["projCoverUrl"]},
                    //{ "tokenTotal", ps["tokenTotal"] == null ? 0:int.Parse(ps["tokenTotal"].ToString())},
                    //{ "hasTokenCount", ps["hasTokenCount"]}
                };
                jo["projState"] = "";
                if (ps["projState"] != null)
                {
                    jo["projState"] = ps["projState"];
                }
                jo["shares"] = 0;
                if (ps["tokenTotal"] != null)
                {
                    jo["shares"] = ps["tokenTotal"];
                }
                jo["members"] = ps["hasTokenCount"];
                return jo;
            }).ToArray();

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        public JArray queryProjCount(Controller controller)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "userId", userId } }.ToString();
            var manageCount = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            var starCount = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr);
            findStr = new JObject { { "address", userAddress }, { "type", "0" }, { "balance", new JObject { { "$gt", 0} } } }.ToString();
            var joinCount = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, findStr);

            var res = new JObject {
                { "joinCount", joinCount},
                { "starCount", starCount},
                { "manageCount", manageCount},
            };
            return getRes(res);
        }

        private bool checkProjNameLen(string name) => name.Length <= 30;
        private bool checkProjTitleLen(string title) => title.Length <= 60;
        private bool checkProjBriefLen(string brief) => brief.Length <= 400;
        
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
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr) > 0;
        }
        
        //
        public void processProjTeam(string projId, string address, long time, string invitorId="", bool isAdmin=true)
        {
            var userId = DaoInfoHelper.genUserId(address, address, address);
            var rInfo = isAdmin ? TeamRole.Admin : TeamRole.Member;
            var newdata = new JObject {
                { "projId", projId },
                { "userId", userId},
                { "address", address },
                { "role", rInfo.name},
                { "roleLevel", rInfo.level},
                { "invitorId", invitorId },
                { "time", time},
                { "lastUpdateTime", time},
            }.ToString();
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, newdata);
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
                updateJo.Add("lastUpdatorId", userId);
                updateJo.Add("lastUpdateTime", TimeHelper.GetTimeStamp());
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

            var match = new JObject { { "$match", new JObject { { "projId", projId }, { "updateId", updateId } } } }.ToString();
            var lookup = new JObject { { "$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "lastUpdatorId"},
                {"foreignField", "userId" },
                {"as","us" }
            } } }.ToString();
            var list = new List<string> { match, lookup };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, list);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.S_InvalidUpdateId);

            var item = (JObject)queryRes[0];
            var data = new JObject();
            data["projId"] = item["projId"];
            data["updateId"] = item["updateId"];
            data["updateTitle"] = item["updateTitle"];
            data["updateDetail"] = item["updateDetail"];
            data["lastUpdateTime"] = item["lastUpdateTime"];
            data["username"] = ((JArray)item["us"])[0]["username"].ToString();
            data["headIconUrl"] = ((JArray)item["us"])[0]["headIconUrl"].ToString();
            data["isMember"] = isMember;

            return getRes(data);
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
                    {"projId", p["projId"]},
                    {"updateId", p["updateId"]},
                    {"updateTitle", p["updateTitle"]},
                    { "updateDetail",p["updateDetail"]},
                    { "lastUpdateTime",p["lastUpdateTime"]}
                };
            }).ToArray();
            var res = new JObject { { "count", count }, { "list", new JArray { list } } };
            return getRes(res) ;
        }
        private bool checkUpdateTitleLen(string ss) => ss.Length <= 40;
        private bool checkUpdateDetailLen(string ss) => ss.Length <= 500;

        // 4.首页展示
        public JArray queryProjList() { return null; /*与moloch共用*/}
        public JArray queryProjDetail(Controller controller, string projId)
        {
            // 无需权限, 游客亦可查看
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            us.getUserInfo(controller, out string code, out string userId);
            getStarState(projId, userId, out bool isStar, out bool isJoin);

            var res = new JObject();
            res["projName"] = item["projName"];
            res["projType"] = item["projType"];
            res["projVersion"] = "";
            if(item["projVersion"] != null)
            {
                res["projVersion"] = item["projVersion"];
            }
            res["projBrief"] = item["projBrief"];
            res["projDetail"] = item["projDetail"];
            res["officialWeb"] = item["officialWeb"];
            res["isStar"] = isStar;
            res["contractAddress"] = "";
            res["creatorAddress"] = "";
            
            return getRes(res);
        }
        public JArray queryProjTeam(string projId, int pageNum=1, int pageSize=10)
        {
            // 无需权限, 游客亦可查看
            var findJo = new JObject { { "projId", projId} };
            var findStr = findJo.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var match = new JObject { { "$match", findJo } }.ToString();
            var sortStr = new JObject { { "$sort", new JObject { { "time", 1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            //
            var lookup = new JObject { { "$lookup", new JObject {
                { "from", userInfoCol},
                { "localField", "userId"},
                { "foreignField", "userId" },
                { "as", "ps"}
            } } }.ToString();
            var project = new JObject { { "$project", new JObject { { "ps", 1 } } } }.ToString();
            var list = new List<string> { match, sortStr, skip, limit, lookup, project };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projTeamInfoCol, list);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });

            var res = queryRes.Select(p =>
            {
                var ps = ((JArray)p["ps"])[0];
                //
                var jo = new JObject();
                jo["username"] = ps["username"];
                jo["address"] = ps["address"];
                jo["headIconUrl"] = ps["headIconUrl"];
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count},{ "list", new JArray { res } } });
        }
        public JArray queryProjUpdateList(string projId, int pageNum=1, int pageSize=10)
        {
            // 无需权限, 游客亦可查看
            var findStr = new JObject { { "projId", projId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count},{ "list", new JArray()} });

            var sortStr = new JObject { { "time", -1 } }.ToString();
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr, sortStr, skip, limit);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var list = queryRes.Select(p => {
                var data = new JObject();
                data["projId"] = p["projId"];
                data["updateId"] = p["updateId"];
                data["updateTitle"] = p["updateTitle"];
                data["updateDetail"] = p["updateDetail"];
                data["lastUpdateTime"] = p["lastUpdateTime"];
                data["discussCount"] = p["discussCount"];
                data["zanCount"] = p["zanCount"];
                return data;
            }).ToArray();
            var res = new JObject { { "count", count }, { "list", new JArray { list } } };
            return getRes(res);
        }
        public JArray queryProjUpdateDetail(Controller controller, string projId, string updateId)
        {
            // 无需权限, 游客亦可查看
            var match = new JObject { { "$match", new JObject { { "projId", projId }, { "updateId", updateId } } } }.ToString();
            var lookup = new JObject { { "$lookup", new JObject {
                {"from", userInfoCol },
                {"localField", "lastUpdatorId"},
                {"foreignField", "userId" },
                {"as","us" }
            } } }.ToString();
            var list = new List<string> { match, lookup};
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, list);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.S_InvalidUpdateId);

            var item = (JObject)queryRes[0];
            var data = new JObject();
            data["projId"] = item["projId"];
            data["updateId"] = item["updateId"];
            data["updateTitle"] = item["updateTitle"];
            data["updateDetail"] = item["updateDetail"];
            data["lastUpdateTime"] = item["lastUpdateTime"];
            data["discussCount"] = item["discussCount"];
            data["zanCount"] = item["zanCount"];
            data["username"] = ((JArray)item["us"])[0]["username"].ToString();
            data["headIconUrl"] = ((JArray)item["us"])[0]["headIconUrl"].ToString();

            us.getUserInfo(controller, out string code, out string userId);
            bool isMember = isProjMember(projId, userId);
            bool isZan = isZanUpdate(updateId, userId);
            long rank = getUpdateRank(projId, long.Parse(item["lastUpdateTime"].ToString()));
            data["isMember"] = isMember;
            data["isZan"] = isZan;
            data["rank"] = rank;
            return getRes(data);
        }
        
        private bool isZanUpdate(string updateId, string userId)
        {
            if (userId == "") return false;
            string findStr = new JObject { { "updateId", updateId }, { "userId", userId } }.ToString();
            return mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateStarInfoCol, findStr) > 0;
        }
        private long getUpdateRank(string projId, long time)
        {
            string findStr = new JObject { { "projId", projId }, { "time", new JObject { { "$lt", time } } } }.ToString();
            long count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projUpdateInfoCol, findStr);
            return count + 1;
        }



        // ------------------------------------------------------------------------------->
        public JArray queryJoinOrgAddressList(Controller controller, int pageNum=1, int pageSize=10)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId, out string userAddress))
            {
                return getErrorRes(code);
            }
            // 
            var findJo = new JObject { { "address", userAddress }, { "type", "0" }, { "balance", new JObject { { "$gt", 0 } } } };
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, findJo.ToString());

            var match = new JObject { { "$match", findJo } }.ToString();
            var sortStr = new JObject { { "$sort", new JObject { { "balance", -1 } } } }.ToString();
            var skip = new JObject { { "$skip", pageSize * (pageNum - 1) } }.ToString();
            var limit = new JObject { { "$limit", pageSize } }.ToString();
            var lookup = new JObject { { "$lookup", new JObject {
                { "from", projInfoCol},
                { "localField", "projId"},
                { "foreignField", "projId" },
                { "as", "ps" }
            } } }.ToString();
            var project = new JObject { { "$project", new JObject { { "ps", 1 } } } }.ToString();
            var list = new List<string> { match, sortStr, skip, limit, lookup, project };
            var queryRes = mh.Aggregate(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, list);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count},{ "list", queryRes} });

            var res = queryRes.Select(p => {
                var ps = ((JArray)p["ps"])[0];
                var jo = new JObject();
                jo["projName"] = ps["projName"];
                jo["molochHash"] = "";
                if(ps["contractHashs"] != null)
                {
                    var hashArr = ((JArray)ps["contractHashs"]).Where(ph => ph["name"].ToString() == "moloch").ToArray();
                    if(hashArr != null && hashArr.Count() > 0)
                    {
                        jo["molochHash"] = hashArr[0]["hash"].ToString();
                    }
                }
                return jo;
            });

            return getRes(new JObject { { "count", count }, { "list", new JArray { res } } });
        }
        public JArray saveContractInfo(Controller controller,
            string projId, string recvAddress, 
            string fundHash/*融资代币*/, string fundSymbol/*融资符号*/,
            string tokenName/*项目代币名称*/, string tokenSymbol/*项目代币符号*/,
            string reserveRundRatio/*储备比例*/, JArray faucetJA/*水龙头列表*/, 
            string txid
            )
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 
            if(hasStartFinance(projId))
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            var now = TimeHelper.GetTimeStamp();
            var data = new JObject();
            data["projId"] = projId;
            data["recvAddress"] = recvAddress;
            data["fundHash"] = fundHash;
            data["fundSymbol"] = fundSymbol;
            data["fundDecimals"] = 0;
            data["tokenName"] = tokenName;
            data["tokenSymbol"] = tokenSymbol;
            data["reserveRundRatio"] = reserveRundRatio;
            data["faucetJA"] = faucetJA;
            data["txid"] = txid;
            data["time"] = now;
            data["lastUpdateTime"] = now;
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, data.ToString());

            //
            var findStr = new JObject { { "projId", projId } }.ToString();
            var updateStr = new JObject { { "$set", new JObject { { "startFinanceFlag", 1 },{ "projState", ProjState.DAICO } } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
            return getRes();
        }
        public JArray getContractInfo(Controller controller, string projId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 成员权限
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject();
            res["projId"] = item["projId"];
            res["recvAddress"] = item["recvAddress"];
            res["fundHash"] = item["fundHash"];
            res["fundSymbol"] = item["fundSymbol"];
            res["fundDecimals"] = item["fundDecimals"];
            res["tokenName"] = item["tokenName"];
            res["tokenSymbol"] = item["tokenSymbol"];
            res["reserveRundRatio"] = item["reserveRundRatio"];
            res["faucetJA"] = item["faucetJA"];
            return getRes(res);
        }
        public JArray saveRewardInfo(Controller controller, 
            string projId, string connectorName, string connectorTel, JObject info)
        {
            // 参数检查
            if (!checkConnectorName(connectorName) || !checkConnectorTel(connectorTel))
            {
                return getErrorRes(DaoReturnCode.C_InvalidParamLen);
            }
            var infoJA = info["info"] as JArray;
            if (infoJA != null && infoJA.Count > 0)
            {
                if (connectorName.Length == 0 || connectorTel.Length == 0) return getErrorRes(DaoReturnCode.C_InvalidParamLen);
                if (!infoJA.All(p => {
                    if (p["rewardId"] == null
                        || p["rewardName"] == null
                        || p["rewardDesc"] == null
                        || p["price"] == null
                        || p["priceUnits"] == null
                        || p["limitFlag"] == null
                        || p["limitMax"] == null
                        || p["distributeTimeFlag"] == null
                        || p["distributeTimeFixYes"] == null
                        || p["distributeTimeFixNot"] == null
                        || p["distributeWay"] == null
                        || p["tokenSymbol"] == null
                        || p["note"] == null)
                    {
                        return false;
                    }
                    var len = p["rewardName"].ToString().Length;
                    if (len == 0 || len > 40) return false;
                    len = p["rewardDesc"].ToString().Length;
                    if (len == 0 || len > 500) return false;
                    if (!checkIntFmt(p["price"])) return false;
                    var tp = p["limitFlag"].ToString();
                    if (tp == SelectKey.Yes)
                    {
                        if (p["limitMax"] == null || !checkIntFmt(p["limitMax"])) return false;
                    }
                    len = p["note"].ToString().Length;
                    if (len > 100) return false;
                    return true;
                })) return getErrorRes(DaoReturnCode.C_InvalidParamFmt);
            }

            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 成员权限
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            // 未启动融资前可以设置回报
            var findStr = new JObject { { "projId", projId } }.ToString();
            var updateStr = new JObject { { "$set", new JObject {
                { "connectorName", connectorName },
                { "connectorTel", connectorTel }
            } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);

            // 增删改
            var nlist = new List<JToken>();
            findStr = new JObject { { "projId", projId }, { "activeState", RewardActiveState.Valid_Yes } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                nlist = infoJA.ToList();
            }
            else
            {
                foreach (var item in infoJA)
                {
                    // 增
                    var id = item["rewardId"].ToString();
                    if (id.Trim().Length == 0)
                    {
                        nlist.Add(item);
                        continue;
                    }
                    var tItems = queryRes.Where(p => p["rewardId"].ToString() == id).ToArray();
                    if (tItems.Count() == 0) continue;
                    // 改
                    var tItem = tItems[0];
                    bool eq = item["rewardName"].ToString() == tItem["rewardName"].ToString()
                        && item["rewardDesc"].ToString() == tItem["rewardDesc"].ToString()
                        && item["price"].ToString() == tItem["price"].ToString()
                        && item["limitFlag"].ToString() == tItem["limitFlag"].ToString()
                        && item["limitMax"].ToString() == tItem["limitMax"].ToString()
                        && item["distributeTimeFlag"].ToString() == tItem["distributeTimeFlag"].ToString()
                        && item["distributeTimeFixYes"].ToString() == tItem["distributeTimeFixYes"].ToString()
                        && item["distributeTimeFixNot"].ToString() == tItem["distributeTimeFixNot"].ToString()
                        && item["distributeWay"].ToString() == tItem["distributeWay"].ToString()
                        && item["tokenSymbol"].ToString() == tItem["tokenSymbol"].ToString()
                        ;
                    if (eq) continue;
                    findStr = new JObject { { "rewardId", item["rewardId"] } }.ToString();
                    updateStr = new JObject { { "$set", new JObject { { "activeState", RewardActiveState.Valid_Not } } } }.ToString();
                    mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, updateStr, findStr);
                    nlist.Add(item);
                }
                // 删
                var oIds = queryRes.Select(p => p["rewardId"].ToString()).ToArray();
                foreach (var id in oIds)
                {
                    if (infoJA.All(p => p["rewardId"].ToString() != id))
                    {
                        findStr = new JObject { { "projId", projId }, { "rewardId", id } }.ToString();
                        updateStr = new JObject { { "$set", new JObject { { "activeState", RewardActiveState.Valid_Not } } } }.ToString();
                        mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, updateStr, findStr);
                    }
                }
            }
            //
            var res = nlist.Select(p =>
            {
                p["projId"] = projId;
                p["rewardId"] = DaoInfoHelper.genProjRewardId(projId, p["rewardName"].ToString());
                p["activeState"] = RewardActiveState.Valid_Yes;
                p["hasSellCount"] = 0;
                p["hasSellCountTp"] = 0;
                return p;
            }).ToArray();
            if (res.Count() > 0)
            {
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, new JArray { res });
            }
            return getRes();
        }
        public JArray getRewardInfo(Controller controller, string projId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 成员权限
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var res = new JObject();
            var item = queryRes[0];
            res["connectorName"] = item["connectorName"];
            res["connectorTel"] = item["connectorTel"];
            //
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                res["info"] = new JArray();
                return getRes(res);
            }
            var list = 
            queryRes.Select(p => {
                var jo = new JObject();
                jo["rewardId"] = p["rewardId"];
                jo["rewardName"] = p["rewardName"];
                jo["rewardDesc"] = p["rewardDesc"];
                jo["price"] = p["price"];
                jo["priceUnits"] = p["priceUnits"];
                jo["limitFlag"] = p["limitFlag"];
                jo["limitMax"] = p["limitMax"];
                jo["distributeTimeFlag"] = p["distributeTimeFlag"];
                jo["distributeTimeFixYes"] = p["distributeTimeFixYes"];
                jo["distributeTimeFixNot"] = p["distributeTimeFixNot"];
                jo["distributeWay"] = p["distributeWay"];
                jo["tokenSymbol"] = p["tokenSymbol"];
                jo["note"] = p["note"];
                return jo;
            }).ToArray();
            res["info"] = new JArray { list };
            return getRes(res);
        }
        public JArray queryProjTokenPrice(string projId)
        {
            return null;
        }
        public JArray queryRewardList(string projId, int pageNum = 1, int pageSize = 10)
        {
            var findStr = new JObject { { "projId", projId }, { "activeState", RewardActiveState.Valid_Yes } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var sortStr = new JObject { { "hasSellCount", -1 } }.ToString();
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, findStr, sortStr, skip, limit);
            if(queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });

            var list =
            queryRes.Select(p => {
                var jo = new JObject();
                jo["rewardId"] = p["rewardId"];
                jo["rewardName"] = p["rewardName"];
                jo["rewardDesc"] = p["rewardDesc"];
                jo["price"] = p["price"];
                jo["priceUnits"] = p["priceUnits"];
                jo["limitFlag"] = p["limitFlag"];
                jo["limitMax"] = p["limitMax"];
                jo["distributeTimeFlag"] = p["distributeTimeFlag"];
                jo["distributeTimeFixYes"] = p["distributeTimeFixYes"];
                jo["distributeTimeFixNot"] = p["distributeTimeFixNot"];
                jo["distributeWay"] = p["distributeWay"];
                jo["note"] = p["note"];
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { list } } });
        }
        public JArray queryRewardDetail(string rewardId)
        {
            var findStr = new JObject { { "rewardId", rewardId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, findStr);
            if(queryRes.Count == 0) return getRes(queryRes);

            var item = queryRes[0];
            var res = new JObject();
            res["rewardId"] = item["rewardId"];
            res["rewardName"] = item["rewardName"];
            res["rewardDesc"] = item["rewardDesc"];
            res["price"] = item["price"];
            res["priceUnits"] = item["priceUnits"];
            res["limitFlag"] = item["limitFlag"];
            res["limitMax"] = item["limitMax"];
            res["distributeTimeFlag"] = item["distributeTimeFlag"];
            res["distributeTimeFixYes"] = item["distributeTimeFixYes"];
            res["distributeTimeFixNot"] = item["distributeTimeFixNot"];
            res["distributeWay"] = item["distributeWay"];
            res["note"] = item["note"];
            return getRes(res);
        }

        private bool checkConnectorName(string name) => name.Length <= 40;
        private bool checkConnectorTel(string tel) => tel.Length <= 40;
        private bool checkIntFmt(JToken tp)
        {
            if (tp == null) return false;
            try
            {
                decimal.Parse(tp.ToString());
                var rr = tp.ToString().Split(".");
                if (rr.Length == 1) return rr[0].Length <= 9;
                else if (rr.Length == 2) return rr[0].Length <= 9 && rr[1].Length <= 4;
                else return false;
            }
            catch
            {
                return false;
            }
        }
        

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
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            type = queryRes[0]["type"].ToString();
            platform = queryRes[0]["platform"].ToString();
            tokenName = queryRes[0]["fundName"].ToString();
            return true;
        }
        private void getStarState(string projId, string userId, out bool isStar, out bool isJoin)
        {
            isStar = false;
            isJoin = false;
            if (userId == "") return;

            string findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            string fieldStr = new JObject { { "starState", 1 }, { "joinState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return;

            isStar = queryRes[0]["starState"].ToString() == StarState.StarYes;
            isJoin = queryRes[0]["joinState"].ToString() == StarState.SupportYes;
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
