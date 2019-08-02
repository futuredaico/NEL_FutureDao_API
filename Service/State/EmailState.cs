
namespace NEL_FutureDao_API.Service.State
{
    public class EmailState
    {
        public const string sendBeforeState = "10101";
        public const string sendAfterState = "10102";
        public const string hasVerify = "10103";
        public const string hasVerifyFailed = "10104";
        public const string sendBeforeStateAtResetPassword = "10105";
        public const string sendAfterStateAtResetPassword = "10106";
        public const string hasVerifyAtResetPassword = "10107";
        public const string hasVerifyAtResetPasswordFailed = "10108";
        public const string sendBeforeStateAtChangeEmail = "10109";
        public const string sendAfterStateAtChangeEmail = "10110";
        public const string hasVerifyAtChangeEmail = "10111";
        public const string hasVerifyAtChangeEmailFailed = "10112";
        public const string sendBeforeStateAtInvited = "10113";
        public const string sendAfterStateAtInvited = "10114";
        public const string hasVerifyAtInvitedYes = "10115";
        public const string hasVerifyAtInvitedNot = "10116";
    }
}
