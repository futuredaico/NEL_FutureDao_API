
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

        public const string RepeatProjNameOrProjTitle = "10212";     // 重复的项目名称或项目标题
        public const string HaveNotPermissionModifyProj = "10213";   // 没有权限修改项目
        public const string HaveNotPermissionInviteMember = "10214"; // 没有权限邀请成员
        public const string InvalidTargetUserId = "10215";           // 不合法的用户id
        public const string HaveNotPermissionCreateUpdate = "10216"; // 没有权限创建项目更新
        public const string HaveNotPermissionQueryProjInfo = "10217"; // 没有权限查看项目信息
        public const string HaveNotPermissionModifyTeamRole = "10218"; // 没有权限修改成员角色

        public const string EmailNotVerify = "10219";   // 邮箱未验证
        public const string EmailVerifying = "10220";   // 邮箱验证中
        public const string EmailVerifySucc = "10221"; // 邮箱验证成功 
        public const string EmailVerifyFail = "10222"; // 邮箱验证失败
    }
}
