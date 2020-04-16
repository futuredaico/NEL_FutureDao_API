using Microsoft.AspNetCore.Mvc;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using NEL_FutureDao_API.Service.Help;
using NEL_FutureDao_API.Service.State;
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
        public string projFinanceOrderInfoCol { get; set; } = "daoprojfinanceorderinfos";
        public string projFinanceProposalInfoCol { get; set; } = "daoprojfinanceproposalinfos";
        public string projFinanceProposalVoteInfoCol { get; set; } = "daoprojfinanceproposalvoteinfos";

        public string projBalanceInfoCol { get; set; } = "moloprojbalanceinfos";
        public string projMoloHashInfoCol { get; set; } = "moloprojhashinfos";
        public string pendingInfoCol = "contractNeeds";//"pendingapprovalprojs";
        //
        public UserServiceV3 us { get; set; }
        public OssHelper oss { get; set; }
        public string bucketName { get; set; }

        private JArray getErrorRes(string code) => RespHelper.getErrorRes(code);
        private JArray getRes(JToken res = null) => RespHelper.getRes(res);

        #region 项目/团队/更新模块
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
            if (!DaoInfoHelper.StoreFile(oss, bucketName, "", projVideoUrl, out string newProjVideoUrl))
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
            findStr = new JObject { { "userId", userId },{ "starState", StarState.StarYes } }.ToString();
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
            getStarState(projId, userId, out bool isStar);

            var res = new JObject();
            res["projId"] = item["projId"];
            res["projName"] = item["projName"];
            res["projTitle"] = item["projTitle"];
            res["projType"] = item["projType"];
            res["projVersion"] = "";
            if(item["projVersion"] != null)
            {
                res["projVersion"] = item["projVersion"];
            }
            res["projState"] = item["projState"];
            res["projBrief"] = item["projBrief"];
            res["projDetail"] = item["projDetail"];
            res["projCoverUrl"] = item["projCoverUrl"];
            res["projVideoUrl"] = item["projVideoUrl"];
            res["officialWeb"] = item["officialWeb"];
            res["isStar"] = isStar;
            res["contractAddress"] = "";
            res["creatorAddress"] = ""; // 融资后新增
            if(getProjContractInfo(projId, out string creatorAddress, out string contractAddress))
            {
                res["contractAddress"] = contractAddress;
                res["creatorAddress"] = creatorAddress;
            }
            res["discussCount"] = item["discussCount"];
            res["updateCount"] = item["updateCount"];
            res["hasIssueAmt"] = 0; // 新增已发送token数量
            
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

        private void getStarState(string projId, string userId, out bool isStar)
        {
            isStar = false;
            if (userId == "") return;

            var findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            var fieldStr = new JObject { { "starState", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return;

            isStar = queryRes[0]["starState"].ToString() == StarState.StarYes;
            return;
        }
        private bool getProjContractInfo(string projId, out string creatorAddress, out string contractAddress)
        {
            creatorAddress = "";
            contractAddress = "";
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr);
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            creatorAddress = item["creatorAddress"]?.ToString();

            var rr = ((JArray)item["contractHashArr"]).Where(p => p["name"].ToString() == "TradeFundPool").Select(p => p["hash"].ToString()).ToArray();
            if (rr.Length > 0) contractAddress = rr[0];
            return true;
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
        #endregion

        #region 融资模块
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
                jo["projId"] = ps["projId"];
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
            string fundHash/*融资代币*/, string fundSymbol/*融资符号*/, long fundDecimals/*融资精度*/,
            string tokenName/*项目代币名称*/, string tokenSymbol/*项目代币符号*/,
            string reserveRundRatio/*储备比例*/, JArray faucetJA/*水龙头列表*/, 
            string creatorAddress, JArray contractHashs
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
            data["recvAddressName"] = getMoloProjName(recvAddress);
            data["fundHash"] = fundHash;
            data["fundSymbol"] = fundSymbol;
            data["fundDecimals"] = fundDecimals;
            data["tokenName"] = tokenName;
            data["tokenSymbol"] = tokenSymbol;
            data["reserveRundRatio"] = reserveRundRatio;
            data["percent"] = faucetJA[0]["percent"];
            data["min"] = faucetJA[0]["min"];
            data["max"] = faucetJA[0]["max"];
            data["faucetJA"] = faucetJA;
            data["creatorAddress"] = creatorAddress;
            data["contractHashArr"] = contractHashs;
            data["time"] = now;
            data["lastUpdateTime"] = now;
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, data.ToString());

            processProjHash(projId, contractHashs);
            //
            processProjFinanceFlag(projId);
            //
            processPendings(projId, contractHashs);

            return getRes();
        }
        private string getMoloProjName(string recvAddr)
        {
            var findStr = new JObject { { "contractHashs.hash", recvAddr } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (queryRes.Count == 0) return "";

            return queryRes[0]["projName"].ToString();
        }
        private void processProjHash(string projId, JArray contractHashs, string owner="futuredao")
        {
            foreach (var item in contractHashs)
            {
                var findStr = new JObject { { "contractHash", item["hash"].ToString().ToLower() } }.ToString();
                if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, findStr) == 0)
                {
                    var data = new JObject {
                        { "projId", projId},
                        { "contractName", item["name"]},
                        { "contractHash", item["hash"].ToString().ToLower()},
                        { "owner", owner },
                        { "txid", item["txid"]},
                    }.ToString();
                    mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projMoloHashInfoCol, data);
                }
            }
        }
        private void processProjFinanceFlag(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var updateStr = new JObject { { "$set", new JObject { { "startFinanceFlag", 1 }, { "projState", ProjState.DAICO } } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, updateStr, findStr);
        }
        private void processPendings(string projId, JArray contractHashs, bool waitRunAfter = false)
        {
            var approved = waitRunAfter ? ApprovedState.runAfterFlag : ApprovedState.continueFlag;
            foreach (var item in contractHashs)
            {
                var findStr = new JObject { { "projId", projId }, { "contractHash", item["hash"] } }.ToString();
                if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, pendingInfoCol, findStr) == 0)
                {
                    var data = new JObject {
                        { "projId", projId},
                        { "contractHash", item["hash"].ToString().ToLower()},
                        { "contractName", item["name"]},
                        { "type", approved},
                    }.ToString();
                    mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, pendingInfoCol, data);
                }
            }
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
            res["fundTotal"] = "0";
            res["fundReserveTotal"] = "0";
            return getRes(res);
        }
        public JArray getContractHash(Controller controller, string projId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }

            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];

            var hashArr = 
            ((JArray)item["contractHashArr"]).Select(p => {
                var jo = (JObject)p;
                jo.Remove("txid");
                return jo;
            }).ToArray();
            var res = new JObject();
            res["hashArr"] = new JArray { hashArr };
            res["hashLen"] = hashArr.Length;
            return getRes(res);
        }

        public JArray getProjFundAndTokenInfo(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject();
            res["fundSymbol"] = item["fundSymbol"];
            res["fundHash"] = item["fundHash"];
            res["tokenSymbol"] = item["tokenSymbol"];
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
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, updateStr, findStr);

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
            var findStr = new JObject { { "projId", projId },{ "activeState", RewardActiveState.Valid_Yes} }.ToString();
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
                jo["hasSellCount"] = p["hasSellCount"];
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
            res["activeState"] = item["activeState"];
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
        //
        public JArray saveSettlementPerMonthInfo(Controller controller, string projId, string txid)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 
            if (!hasStartFinance(projId))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            // 管理权限
            if (!isProjMember(projId, userId))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var findStr = new JObject { { "lastSettlementTxid", txid } }.ToString();
            if (mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr) > 0)
            {
                return getErrorRes(DaoReturnCode.RepeatOperate);
            }
            findStr = new JObject { { "projId", projId } }.ToString();
            var updateStr = new JObject { { "$set", new JObject {
                { "lastSettlementTxid", txid},
                { "lastSettlementTime", TimeHelper.GetTimeStamp() },
                { "lastSettlementRes", "" }
            } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, updateStr, findStr);
            return getRes();
        }
        #endregion

        #region 关注模块
        public JArray startStarProj(Controller controller, string projId)
        {
            return starYesOrNot(controller, projId);
        }
        public JArray cancelStarProj(Controller controller, string projId)
        {
            return starYesOrNot(controller, projId, true);
        }
        private JArray starYesOrNot(Controller controller, string projId, bool isCancel = false)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                return getErrorRes(DaoReturnCode.S_InvalidProjId);
            }
            var starState = isCancel ? StarState.StarNot : StarState.StarYes;
            findStr = new JObject { { "projId", projId }, { "userId", userId } }.ToString();
            queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, findStr);
            if (queryRes.Count == 0)
            {
                if (isCancel) return getRes();
                var now = TimeHelper.GetTimeStamp();
                var newdata = new JObject {
                    {"projId", projId },
                    {"userId", userId },
                    {"starState", starState },
                    {"time", now},
                    {"lastUpdateTime", now},
                }.ToString();
                mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, newdata);
                return getRes();
            }

            var item = queryRes[0];
            if (item["starState"].ToString() != starState)
            {
                var now = TimeHelper.GetTimeStamp();
                var updateStr = new JObject { { "$set", new JObject {
                    { "starState", starState},
                    { "lastUpdateTime", now},
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projStarInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        #endregion

        #region 订单模块
        public JArray initBuyOrder(Controller controller,
            string projId,
            string rewardId,
            string amount,
            string rewardAmount,
            string connectorName,
            string connectorTel,
            string connectorAddress,
            string connectorEmail,
            string connectorMessage
            )
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "rewardId", rewardId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceRewardInfoCol, findStr);
            if (queryRes.Count == 0
                || queryRes[0]["activeState"].ToString() != RewardActiveState.Valid_Yes
                || queryRes[0]["projId"].ToString() != projId
                )
            {
                // 无效回报id
                return getErrorRes(DaoReturnCode.Invalid_RewardId);
            }
            var item = queryRes[0];

            if (!getProjTokenName(projId, out string tokenSymbol, out string fundSymbol))
            {
                return getErrorRes(DaoReturnCode.S_InvalidProjId);
            }
            string projName = getProjName(projId);
            var orderId = DaoInfoHelper.genProjRewardOrderId(projId, rewardId, userId);
            var now = TimeHelper.GetTimeStamp();
            var newdata = new JObject {
                { "projId", projId},
                { "projName", projName},
                { "rewardId", rewardId},
                { "orderId", orderId},
                { "orderState", OrderState.WaitingPay},
                { "rewardName", item["rewardName"]},
                { "price", item["price"]},
                { "priceUnit", fundSymbol },
                { "amount", amount},
                { "totalCost", (decimal.Parse(item["price"].ToString()) * decimal.Parse(amount)).ToString()},
                { "totalCostUnit", fundSymbol },
                { "rewardAmount", rewardAmount },
                { "rewardAmountUnit", tokenSymbol },
                { "senderNote", ""},
                { "connectorName", connectorName},
                { "connectorTel", connectorTel},
                { "connectorEmail", connectorEmail},
                { "connectorAddress", connectorAddress},
                { "connectorMessage", connectorMessage},
                { "distributeWay", item["distributeWay"]},
                { "txid", ""},
                { "userId", userId},
                { "originInfo", item},
                { "markTime",  now},
                { "time",  now},
                { "lastUpdateTime", now}
            };
            mh.PutData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, newdata);
            return getRes(new JObject { { "orderId", orderId }, { "time", now } });
        }
        public JArray confirmBuyOrder(Controller controller, string orderId, string txid)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }

            var findStr = new JObject { { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr);
            if (queryRes.Count == 0
                || queryRes[0]["userId"].ToString() != userId)
            {
                // 无效订单id
                return getErrorRes(DaoReturnCode.Invalid_OrderId);
            }

            var item = queryRes[0];
            if (item["orderState"].ToString() != OrderState.WaitingPay)
            {
                // 无效操作
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            if (item["txid"].ToString() != txid)
            {
                var updateStr = new JObject { { "$set", new JObject {
                    { "orderState", OrderState.WaitingConfirm},
                    { "txid", txid},
                } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        public JArray cancelBuyOrder(Controller controller, string orderId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr);
            if (queryRes.Count == 0
                || queryRes[0]["userId"].ToString() != userId)
            {
                // 无效订单id
                return getErrorRes(DaoReturnCode.Invalid_OrderId);
            }

            var item = queryRes[0];
            if (item["orderState"].ToString() != OrderState.WaitingPay)
            {
                // 无效操作
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var updateStr = new JObject { { "$set", new JObject { { "orderState", OrderState.Canceled }, { "markTime", TimeHelper.GetTimeStamp() } } } }.ToString();
            mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, updateStr, findStr);
            return getRes();
        }
        public JArray confirmDeliverBuyOrder(Controller controller, string projId, string orderId, string note)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var findStr = new JObject { { "projId", projId }, { "orderId", orderId } }.ToString();
            var fieldStr = new JObject { { "orderState", 1 }, { "senderNote", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return getErrorRes(DaoReturnCode.Invalid_OrderId);

            var item = queryRes[0];
            var orderState = item["orderState"].ToString();
            if (orderState != OrderState.WaitingDeliverGoods
                && orderState != OrderState.hasDeliverGoods)
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            if (item["senderNote"].ToString() != note)
            {
                var updateStr = new JObject { { "$set", new JObject { { "orderState", OrderState.hasDeliverGoods }, { "senderNote", note } } } }.ToString();
                mh.UpdateData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, updateStr, findStr);
            }
            return getRes();
        }
        // 买家
        public JArray queryBuyOrderList(Controller controller, int pageNum, int pageSize)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "userId", userId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var sortStr = new JObject { { "time", -1 } }.ToString();
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr, sortStr, skip, limit);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });

            var rlist = queryRes.Select(p => {
                var jo = new JObject();
                jo["projId"] = p["projId"];
                jo["projName"] = p["projName"];
                jo["rewardId"] = p["rewardId"];
                jo["rewardName"] = p["rewardName"];
                jo["orderId"] = p["orderId"];
                jo["price"] = p["price"];
                jo["priceUnit"] = p["priceUnit"];
                jo["amount"] = p["amount"];
                jo["totalCost"] = p["totalCost"];
                jo["totalCostUnit"] = p["totalCostUnit"];
                jo["orderState"] = p["orderState"];
                jo["time"] = p["time"];
                jo["connectorName"] = p["connectorName"];
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { rlist } } });
        }
        public JArray queryBuyOrder(Controller controller, string projId, string orderId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            var findStr = new JObject { { "projId", projId }, { "orderId", orderId },{ "userId", userId} }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            getProjRewardConnector(projId, out string senderName, out string senderTel);

            var res = new JObject {
                {"projId", projId},
                {"projName", item["projName"]},
                {"rewardId", item["rewardId"]},
                {"rewardName", item["rewardName"]},
                {"orderId", item["orderId"]},
                {"orderState", item["orderState"]},
                {"price", item["price"]},
                {"priceUnit", item["priceUnit"]},
                {"amount", item["amount"]},
                {"totalCost", item["totalCost"]},
                {"totalCostUnit", item["totalCostUnit"]},
                {"time", item["time"]},
                { "connectorName", item["connectorName"]},
                { "connectorTel", item["connectorTel"]},
                { "connectorAddress", item["connectorAddress"]},
                { "connectorEmail", item["connectorEmail"]},
                { "connectorMessage", item["connectorMessage"]},
                { "senderName", senderName},
                { "senderTel", senderTel},
                { "senderNote", item["senderNote"]},
            };
            return getRes(res);
        }
        // 卖家
        public JArray queryProjBuyOrderList(Controller controller, string projId, int pageNum, int pageSize, int isDelivery, string buyerName, string orderId, int orderType)
        {
            //卖家角度
            //待发货/已发货
            //收件人名称 + 买家名称 + 订单编号 + 订单类型(实物/虚拟)
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 管理员
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }
            var findJo = new JObject { { "projId", projId } };
            // 待发货/已发货(没有则默认待发货)
            if (isDelivery == 0)
            {
                findJo.Add("orderState", OrderState.WaitingDeliverGoods);
            }
            else if (isDelivery == 1)
            {
                findJo.Add("orderState", OrderState.hasDeliverGoods);
            }
            else
            {
                findJo.Add("orderState", OrderState.WaitingDeliverGoods);
            }

            // 买价姓名(没有则为全部)
            if (buyerName != "")
            {
                findJo.Add("connectorName", buyerName);
            }

            // 订单编号(已优先处理)
            if(orderId.Trim().Length != 0)
            {
                findJo.Add("orderId", orderId);
            }

            // 订单发放类型(没有则为全部)
            if (orderType == 0)
            {
                findJo.Add("distributeWay", SelectKey.Not);
            }
            else if (orderType == 1)
            {
                findJo.Add("distributeWay", SelectKey.Yes);
            }
            //
            var findStr = findJo.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr);
            if (count == 0) return getRes(new JObject { { "count", count }, { "list", new JArray() } });

            var sortStr = new JObject { { "time", -1 } }.ToString();
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr, sortStr, skip, limit);
            if (queryRes.Count == 0) return getRes(new JObject { { "count", count }, { "list", queryRes } });

            var rlist = queryRes.Select(p => {
                var jo = new JObject();
                jo["projId"] = p["projId"];
                jo["projName"] = p["projName"];
                jo["rewardId"] = p["rewardId"];
                jo["rewardName"] = p["rewardName"];
                jo["orderId"] = p["orderId"];
                jo["price"] = p["price"];
                jo["priceUnit"] = p["priceUnit"];
                jo["amount"] = p["amount"];
                jo["totalCost"] = p["totalCost"];
                jo["totalCostUnit"] = p["totalCostUnit"];
                jo["orderState"] = p["orderState"];
                jo["time"] = p["time"];
                jo["connectorName"] = p["connectorName"];
                return jo;
            }).ToArray();
            return getRes(new JObject { { "count", count }, { "list", new JArray { rlist } } });
        }
        public JArray queryProjBuyOrder(Controller controller, string projId, string orderId)
        {
            // 权限
            if (!us.getUserInfo(controller, out string code, out string userId))
            {
                return getErrorRes(code);
            }
            // 管理员
            if (!isProjMember(projId, userId, true))
            {
                return getErrorRes(DaoReturnCode.InvalidOperate);
            }

            var findStr = new JObject { { "projId", projId }, { "orderId", orderId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceOrderInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            getProjRewardConnector(projId, out string senderName, out string senderTel);

            var res = new JObject {
                {"projId", projId},
                {"projName", item["projName"]},
                {"rewardId", item["rewardId"]},
                {"rewardName", item["rewardName"]},
                {"orderId", item["orderId"]},
                {"orderState", item["orderState"]},
                {"price", item["price"]},
                {"priceUnit", item["priceUnit"]},
                {"amount", item["amount"]},
                {"totalCost", item["totalCost"]},
                {"totalCostUnit", item["totalCostUnit"]},
                {"time", item["time"]},
                { "connectorName", item["connectorName"]},
                { "connectorTel", item["connectorTel"]},
                { "connectorAddress", item["connectorAddress"]},
                { "connectorEmail", item["connectorEmail"]},
                { "connectorMessage", item["connectorMessage"]},
                { "senderName", senderName},
                { "senderTel", senderTel},
                { "senderNote", item["senderNote"]}
            };
            return getRes(res);
        }

        private string getProjName(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "projName", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return "";
            return queryRes[0]["projName"].ToString();
        }
        private bool getProjTokenName(string projId, out string tokenSymbol, out string fundSymbol)
        {
            tokenSymbol = "";
            fundSymbol = "";
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "tokenSymbol", 1 }, { "fundSymbol", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            tokenSymbol = item["tokenSymbol"].ToString();
            fundSymbol = item["fundSymbol"].ToString();
            return true;
        }
        private bool getProjRewardConnector(string projId, out string connectorName, out string connectorTel)
        {
            connectorName = "";
            connectorTel = "";
            var findStr = new JObject { { "projId", projId } }.ToString();
            var fieldStr = new JObject { { "connectorName", 1 }, { "connectorTel", 1 } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr, fieldStr);
            if (queryRes.Count == 0) return false;

            var item = queryRes[0];
            connectorName = item["connectorName"].ToString();
            connectorTel = item["connectorTel"].ToString();
            return true;
        }
        #endregion

        #region 评论模块
        // 与molo共用
        #endregion

        #region 交易模块
        public JArray queryShareBalance(string projId, string address)
        {
            var findStr = new JObject { { "projId", projId }, { "type", "6" }, { "address", address } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projBalanceInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject {
                {"balance", item["balance"] },
                {"balanceCanUse", item["balanceLockNot"] },
                {"balanceLock", item["balanceLockYes"] },
            };
            return getRes(res);
        }
        #endregion


        #region 治理模块
        public JArray queryProjFinanceInfo(string projId)
        {
            var findStr = new JObject { { "projId", projId } }.ToString();
            var queryRes = mh.GetData(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceInfoCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject();
            res["recvAddress"] = item["recvAddress"];
            res["recvAddressName"] = item["recvAddressName"];
            res["fundSymbol"] = item["fundSymbol"];
            res["percent"] = item["percent"];
            res["min"] = item["min"];
            res["max"] = item["max"];
            res["reserveRundRatio"] = item["reserveRundRatio"];
            res["fundPoolTotal"] = "0";
            if(item["fundPoolTotal"] != null)
            {
                res["fundPoolTotal"] = item["fundPoolTotal"].ToString().formatDecimal();
            }
            res["fundReservePoolTotal"] = "0";
            if (item["fundReservePoolTotal"] != null)
            {
                res["fundReservePoolTotal"] = item["fundReservePoolTotal"].ToString().formatDecimal();
            }
            return getRes(res);
        }
        public JArray queryProjProposalList(Controller controller, string projId, int pageNum, int pageSize)
        {
            us.getUserInfo(controller, out string code, out string userId, out string address);
            //
            var findStr = new JObject { { "projId", projId } }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceProposalInfoCol, findStr);
            if (count == 0) return getRes();

            var sortStr = new JObject { { "blockTime", -1 } }.ToString();
            var skip = pageSize * (pageNum - 1);
            var limit = pageSize;
            var queryRes = mh.GetDataPages(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceProposalInfoCol, findStr, sortStr, skip, limit);
            if (queryRes.Count == 0) return getRes();

            var rr = queryRes.Select(p =>
            {
                var jo = new JObject();
                jo["index"] = p["index"];
                jo["proposalType"] = p["proposalType"];
                jo["ratio"] = "0";
                jo["minValue"] = "0";
                jo["maxValue"] = "0";
                jo["startTime"] = p["votingStartTime"];
                jo["proposalState"] = p["proposalState"];
                jo["voteYesCount"] = p["voteYesCount"];
                jo["voteNotCount"] = p["voteNotCount"];
                jo["hasVote"] = hasVote(address, projId, jo["index"].ToString());
                var pType = jo["proposalType"].ToString();
                if(pType == ProposalTypeF.ChangeMonthlyAllocation)
                {
                    jo["ratio"] = p["ratio"];
                    jo["minValue"] = p["minValue"];
                    jo["maxValue"] = p["maxValue"];
                }
                return jo;
            }).ToArray();

            var res = new JObject { { "count", count }, { "list", new JArray { rr } } };
            return getRes(res);
        }
        private bool hasVote(string address, string projId, string index)
        {
            if (address == "") return false;

            var findStr = new JObject { { "projId", projId},{ "index",index},{ "address", address} }.ToString();
            var count = mh.GetDataCount(dao_mongodbConnStr, dao_mongodbDatabase, projFinanceProposalVoteInfoCol, findStr);
            return count > 0;
        }
        #endregion
    }
    class ProposalTypeF
    {
        public const string ChangeMonthlyAllocation = "ChangeM";
        public const string ApplyClearingProposal = "ApplyC";
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
