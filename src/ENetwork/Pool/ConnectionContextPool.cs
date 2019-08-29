using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ENetwork.Pool
{
    public static class ConnectionContextPool
    {
        private static ConcurrentDictionary<int, ConcurrentDictionary<IChannelId, ChannelContext>> NetworkConnections { get; } = new ConcurrentDictionary<int, ConcurrentDictionary<IChannelId, ChannelContext>>();

        internal static void Insert(NettyNetwork network, ChannelContext context)
        {
            if(!NetworkConnections.TryGetValue(network.Id, out ConcurrentDictionary<IChannelId, ChannelContext> connections))
            {
                connections = new ConcurrentDictionary<IChannelId, ChannelContext>();
                NetworkConnections.AddOrUpdate(network.Id, connections, (k, v) => connections);
            }
            connections.AddOrUpdate(context.ChannelId, context, (k, v) => context);
        }

        internal static bool Delete(NettyNetwork network, IChannelId channelId)
        {
            if (!NetworkConnections.TryGetValue(network.Id, out ConcurrentDictionary<IChannelId, ChannelContext> connections))
                return false;

            return connections.TryRemove(channelId, out ChannelContext value);
        }

        internal static List<ChannelContext> GetContextList(NettyNetwork network)
        {
            return GetContexts(network.Id);
        }

        internal static ChannelContext GetNextContext(NettyNetwork network)
        {
            var contexts = GetContexts(network.Id).OrderBy(c => c.ChannelId).ToList();
            if (!contexts.Any())
                return null;

            ChannelContext context = null;
            while (true)
            {
                if (contexts.Count == 0)
                    break;

                var index = IdCreator.CreateNextId() % contexts.Count;
                context = contexts[index];
                if (context.Handler.IsClient)
                    throw new NotSupportedException("This method does not supported a client network search, you can using GetSingleContext.");

                if (context.ChannelHanderContext.Channel.Active)
                    break;
                else
                {
                    context = null;
                    contexts.RemoveAt(index);
                }
            }
            return context;
        }

        internal static ChannelContext GetSingleContext(NettyNetwork network)
        {
            if (!NetworkConnections.TryGetValue(network.Id, out ConcurrentDictionary<IChannelId, ChannelContext> connections))
                return null;

            var context = connections.Values.FirstOrDefault();
            if(!context.Handler.IsClient)
                throw new NotSupportedException("This method does not supported a server network search, you can using GetNextContext.");

            return context;
        }

        private static List<ChannelContext> GetContexts(int networkId)
        {
            List<ChannelContext> channels = new List<ChannelContext>();
            if (NetworkConnections.TryGetValue(networkId, out ConcurrentDictionary<IChannelId, ChannelContext> connections))
                channels.AddRange(connections.Values);
            return channels;
        }
    }
}
