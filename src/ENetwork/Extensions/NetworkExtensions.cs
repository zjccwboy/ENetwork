using DotNetty.Transport.Channels;
using ENetwork.Pool;
using System.Collections.Generic;

namespace ENetwork
{
    public static class NetworkExtensions
    {
        public static NettyNetwork ListenAsync(this NettyNetwork network)
        {
            network.ListenAsync();
            return network;
        }

        public static NettyNetwork ConnectAsync(this NettyNetwork network)
        {
            network.ConnectAsync();
            return network;
        }

        public static List<ChannelContext> GetContextList(this NettyNetwork network)
        {
            return ConnectionContextPool.GetContextList(network);
        }

        public static ChannelContext GetNextContext(this NettyNetwork network)
        {
            return ConnectionContextPool.GetNextContext(network);
        }

        public static ChannelContext GetSingleContext(this NettyNetwork network)
        {
            return ConnectionContextPool.GetSingleContext(network);
        }

        public static void InsertPool(this NettyNetwork network, ChannelContext context)
        {
            ConnectionContextPool.Insert(network, context);
        }

        public static bool DeletePool(this NettyNetwork network, IChannelId channelId)
        {
            return ConnectionContextPool.Delete(network, channelId);
        }
    }
}
