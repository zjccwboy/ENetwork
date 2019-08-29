using DotNetty.Transport.Channels;

namespace ENetwork
{
    public class ChannelContext
    {
        public IChannelId ChannelId => ChannelHanderContext.Channel.Id;
        public IChannel Channel => ChannelHanderContext.Channel;
        public NetworkHandler Handler { get; private set; }
        public IChannelHandlerContext ChannelHanderContext { get; private set; }
        public int MessageId { get; private set; }
        internal int RpcId { get; private set; }
        internal SenderType SenderType { get; private set; }

        internal void Flush()
        {
            this.Handler = null;
            this.ChannelHanderContext = null;
            this.MessageId = 0;
            this.RpcId = 0;
            this.SenderType = SenderType.NormalSender;
        }

        internal void SetContext(NetworkHandler handler, IChannelHandlerContext handerContext, int messageId = 0
            , int rpcId = 0, SenderType senderType = SenderType.NormalSender)
        {
            this.Handler = handler;
            this.ChannelHanderContext = handerContext;
            this.MessageId = messageId;
            this.RpcId = rpcId;
            this.SenderType = senderType;
        }
    }
}
