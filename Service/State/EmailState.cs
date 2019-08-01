
namespace NEL_FutureDao_API.Service.State
{
    public class EmailState
    {
        public const string sendBeforeState = "10100";
        public const string sendAfterState = "10101";
        public const string hasVerify = "10102";
        public const string sendBeforeStateAtResetPassword = "10103";
        public const string sendAfterStateAtResetPassword = "10104";
        public const string hasVerifyAtResetPassword = "10105";
        public const string sendBeforeStateAtChangeEmail = "10106";
        public const string sendAfterStateAtChangeEmail = "10107";
        public const string hasVerifyAtChangeEmail = "10108";
        public const string sendBeforeStateAtInvited = "10109";
        public const string sendAfterStateAtInvited = "10110";
        public const string hasVerifyAtInvitedYes = "10111";
        public const string hasVerifyAtInvitedNot = "10112";
    }
}
