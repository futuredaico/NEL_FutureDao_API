
namespace NEL_FutureDao_API.Service.State
{
    public class DaoReturnCode
    {
        public const string success = "00000";
        public const string invalidUsername = "10200";      // 不合法用户名
        public const string usernameHasRegisted = "10201";  // 用户名已注册
        public const string invalidEmail = "10202";         // 不合法的邮箱
        public const string emailHasRegisted = "10203";     // 邮箱已注册
        public const string invalidPasswordLen = "10204";   // 不合法的密码
        public const string passwordError = "10205";        // 密码错误
        public const string invalidVerifyCode = "10206";    // 不合法的验证码
        public const string invalidLoginInfo = "10207";     // 无效的登录信息(即用户名/邮箱/密码错误)
        public const string notFindUserInfo = "10208";      // 没有找到用户信息
        public const string invalidAccessToken = "10209";      // 无效token
        public const string expireAccessToken = "10210";       // token过期
        public const string headIconNotUpload = "10211";       // 头像未上传
        public const string projBriefNotUpload = "10212";      // 项目封面未上传
        public const string projVideoNotUpload = "10213";      // 项目视频未上传
        public const string projRequiredFieldIsEmpty = "10214";// 项目必选项为空
        public const string projNotSupportOp = "10215";    // 项目不支持此操作

        public const string EmailNotVerify = "10219";   // 邮箱未验证
        public const string EmailVerifying = "10220";   // 邮箱验证中
        public const string EmailVerifySucc = "10221"; // 邮箱验证成功 
        public const string EmailVerifyFail = "10222"; // 邮箱验证失败

        public const string T_RepeatProjNameOrProjTitle = "10230";      // 重复的项目名称或项目标题
        public const string T_HaveNotPermissionCreateProj = "10231";    // 没有权限[创建]项目
        public const string T_HaveNotPermissionDeleteProj = "10232";    // 没有权限[删除]项目
        public const string T_HaveNotPermissionModifyProj = "10233";    // 没有权限[修改]项目
        public const string T_HaveNotPermissionQueryProj = "10234";     // 没有权限[查询]项目
        public const string T_HaveNotPermissionCreateUpdate = "10235";  // 没有权限[创建]项目更新
        public const string T_HaveNotPermissionDeleteUpdate = "10236";  // 没有权限[删除]项目更新
        public const string T_HaveNotPermissionModifyUpdate = "10237";  // 没有权限[修改]项目更新
        public const string T_HaveNotPermissionQueryUpdate = "10238";   // 没有权限[查询]项目更新
        public const string T_InvalidTargetUserId = "10239";
        public const string T_HaveNotPermissionInviteTeamMember = "102341"; // 没有权限[创建/邀请]项目成员
        public const string T_HaveNotPermissionDeleteTeamMember = "102342"; // 没有权限[删除]项目成员
        public const string T_HaveNotPermissionModifyTeamMember = "102343"; // 没有权限[修改(角色)]项目成员
        public const string T_HaveNotPermissionQueryTeamMember = "102344";  // 没有权限[查询]项目成员
        public const string T_HaveNotPermissionDeleteTeamAdmin = "102345";  // 没有权限删除项目团队管理员
        public const string T_HaveNotPermissionDeleteYourSelf = "102346";   // 没有权限删除项目团队成员自己

        public const string S_NoPermissionAddDiscuss = "10251";
        public const string S_NoPermissionDelDiscuss = "10252";
        public const string S_InvalidProjId = "10253";
        public const string S_InvalidProjIdOrDiscussId = "10254";
        public const string S_InvalidUpdateId = "10255";
        public const string S_InvalidUpdateIdOrDiscussId = "10256";
        public const string S_InvalidUpdateIdOrProjId = "10257";


    }
}
