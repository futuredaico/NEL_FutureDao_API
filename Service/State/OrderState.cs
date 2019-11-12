
namespace NEL_FutureDao_API.Service.State
{
    public class OrderState
    {
        // 等待付款 + 等待确认 + 等待发货 + 已发货 + 取消订单 + 付款超时 + 交易失败
        public const string WaitingPay = "10141";
        public const string WaitingConfirm = "10142";
        public const string WaitingDeliverGoods = "10143";
        public const string hasDeliverGoods = "10144";
        public const string Canceled = "10145";
        public const string PayTimeout = "10146";
        public const string TxFailed = "10147";
    }
}
