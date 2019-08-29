using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using ENetwork.CallCotext;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ENetwork
{
    public class NetworkHandler : ChannelHandlerAdapter
    {
        public bool IsClient { get; }
        public int NetworkId => Network.Id;
        public NettyNetwork Network { get; }
        private ConcurrentDictionary<IChannelId, ConcurrentDictionary<int, ICallContext>> CallContexts { get; } = new ConcurrentDictionary<IChannelId, ConcurrentDictionary<int, ICallContext>>();
        private ConcurrentQueue<ChannelContext> ChannelContextQueue { get; } = new ConcurrentQueue<ChannelContext>();

        public NetworkHandler(bool isClient, NettyNetwork network)
        {
            IsClient = isClient;
            Network = network;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            var channelContext = DequeueChannelContext();
            channelContext.SetContext(this, context);

            Network.InsertPool(channelContext);

            if (IsClient)
            {
                Console.WriteLine($"{context.Channel.LocalAddress} --> {context.Channel.RemoteAddress} Connect success.");
            }
            else
            {
                Console.WriteLine($"{context.Channel.LocalAddress} <-- {context.Channel.RemoteAddress} Connect success.");
            }

            this.Network.OnActive?.Invoke(channelContext);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var byteBuffer = message as IByteBuffer;
            if (byteBuffer == null)
                return;

            try
            {
                var messageId = byteBuffer.GetInt(0);
                var rpcId = byteBuffer.GetInt(4);
                var senderType = (SenderType)byteBuffer.GetByte(8);

                const int headerSize = sizeof(int) * 2 + sizeof(byte);
                var messageStr = byteBuffer.ToString(byteBuffer.ReaderIndex + headerSize, byteBuffer.ReadableBytes - headerSize, Encoding.UTF8);

                if (senderType == SenderType.FeedbackSender)
                {
                    var callContext = RemoveCallAwaiter(context, rpcId);
                    if (callContext == null)
                        return;

                    var messageType = callContext.MessageType;
                    if (messageType == typeof(string))
                    {
                        var messageObj = messageStr;
                        callContext.SetResult(messageObj);
                    }
                    else
                    {
                        try
                        {
                            var messageObj = JsonConvert.DeserializeObject(messageStr, messageType);
                            callContext.SetResult(messageObj);
                        }
                        catch(Exception ex)
                        {
                            callContext.SetResult(null);
                            Console.WriteLine($"MessageId:{messageId} MessageType:{messageType.Name} ReceiveMessage:{messageStr} Exception:{ex}");
                        }
                    }
                }
                else
                {
                    var channelContext = DequeueChannelContext();
                    channelContext.SetContext(this, context, messageId, rpcId, senderType);

                    var messageType = MessageBinder.GetMessageType(messageId);
                    var adapter = MessageBinder.GetAdapter(messageId);
                    if (messageType == typeof(string))
                    {
                        var messageObj = messageStr;
                        adapter.DispatchAdapter(channelContext, messageObj);
                    }
                    else
                    {
                        try
                        {
                            var messageObj = JsonConvert.DeserializeObject(messageStr, messageType);
                            adapter.DispatchAdapter(channelContext, messageObj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"MessageId:{messageId} MessageType:{messageType.Name} ReceiveMessage:{messageStr} Exception:{ex}");
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                DisconnectAsync(context).Wait();
            }
            finally
            {
                try
                {
                    ReferenceCountUtil.Release(byteBuffer);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public override Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress)
        {
            return base.ConnectAsync(context, remoteAddress, localAddress);
        }

        public override async Task CloseAsync(IChannelHandlerContext context)
        {
            await base.CloseAsync(context);
            HandleClosing(context);
        }

        public override async Task DisconnectAsync(IChannelHandlerContext context)
        {
            await base.DisconnectAsync(context);
            HandleClosing(context);
            Console.WriteLine($"{context.Channel.RemoteAddress} Connection closed.");
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception.ToString());
            context.CloseAsync();
            HandleClosing(context);
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            if (evt is IdleStateEvent)
            {
                IdleState state = (evt as IdleStateEvent).State;
                if (state == IdleState.WriterIdle)
                {
                    context.WriteAndFlushAsync(evt);
                }
            }
            else
            {
                base.UserEventTriggered(context, evt);
            }
        }

        private void HandleClosing(IChannelHandlerContext context)
        {
            if (TryRemoveChannelCallContexts(context, out List<ICallContext> callContexts))
            {
                foreach (var callContext in callContexts)
                    callContext.SetResult(null);
            }

            if (Network.DeletePool(context.Channel.Id))
            {
                var channelContext = DequeueChannelContext();
                channelContext.SetContext(this, context);

                this.Network.OnClose?.Invoke(channelContext);

                if (IsClient)
                    this.Network.ReconnectAsync();
            }
        }

        private ICallContext RemoveCallAwaiter(IChannelHandlerContext context, int rpcId)
        {
            if (!CallContexts.TryGetValue(context.Channel.Id, out ConcurrentDictionary<int, ICallContext> callContexts))
                return null;

            callContexts.TryRemove(rpcId, out ICallContext callContext);
            return callContext;
        }

        internal ICallContext CreateAwaiterCallContext<TResult>(IChannelId channelId, int rpcId, Type messageType)
        {
            var callContext = new CallContext<TResult>()
            {
                MessageType = messageType,
                Tcs = new TaskCompletionSource<TResult>(),
            };

            AddCallContextAwaiter(channelId, rpcId, callContext);

            return callContext;
        }

        private void AddCallContextAwaiter(IChannelId channelId, int rpcId, ICallContext callContext)
        {
            if (!CallContexts.TryGetValue(channelId, out ConcurrentDictionary<int, ICallContext> callContexts))
            {
                callContexts = new ConcurrentDictionary<int, ICallContext>();
                CallContexts.AddOrUpdate(channelId, callContexts, (k, v) => callContexts);
            }
            callContexts.AddOrUpdate(rpcId, callContext, (k, v) => callContext);
        }

        internal void EnqueueChannelContext(ChannelContext context)
        {
            context.Flush();
            ChannelContextQueue.Enqueue(context);
        }

        private ChannelContext DequeueChannelContext()
        {
            if (!ChannelContextQueue.TryDequeue(out ChannelContext context))
                context = new ChannelContext();

            return context;
        }

        private bool TryRemoveChannelCallContexts(IChannelHandlerContext context, out List<ICallContext> callContexts)
        {
            callContexts = null;
            if (CallContexts.TryRemove(context.Channel.Id, out ConcurrentDictionary<int, ICallContext> values))
            {
                callContexts = values.Values.ToList();
                return true;
            }
            return false;
        }
    }
}
