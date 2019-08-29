

namespace ENetwork
{
    /// <summary>
    /// 消息发送者类型，区分消息是否是RPC消息，协议设定RPC消息是支持双向的，需要标识出RPC消息是否为调用消息与回馈消息
    /// </summary>
    public enum SenderType : byte
    {
        /// <summary>
        /// 普通消息发送者
        /// </summary>
        NormalSender = 0,
        /// <summary>
        /// RPC调用消息发送者
        /// </summary>
        CallSender = 1,
        /// <summary>
        /// RPC回馈消息发送者
        /// </summary>
        FeedbackSender = 2,
    }
}
