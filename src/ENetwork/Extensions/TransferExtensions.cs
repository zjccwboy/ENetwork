using DotNetty.Buffers;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ENetwork
{
    public static class TransferExtensions
    {
        public static void Send(this ChannelContext context, int messageId, object message)
        {
            var rpcId = 0;
            context.SendAsync(messageId, rpcId, message);
        }

        public static void Send(this ChannelContext context, Enum messageId, object message)
        {
            var rpcId = 0;
            var integerId = Convert.ToInt32(messageId);
            context.SendAsync(integerId, rpcId, message);
        }

        public static Task SendAsync(this ChannelContext context, int messageId, object message)
        {
            var rpcId = 0;
            return context.SendAsync(messageId, rpcId, message);
        }

        public static Task SendAsync(this ChannelContext context, Enum messageId, object message)
        {
            var rpcId = 0;
            var integerId = Convert.ToInt32(messageId);
            return context.SendAsync(integerId, rpcId, message);
        }

        public static TResult Call<TResult>(this ChannelContext context, Enum messageId, object input)
        {
            var integerId = Convert.ToInt32(messageId);
            return context.Call<TResult>(integerId, input);
        }

        public static TResult Call<TResult>(this ChannelContext context, int messageId, object input)
        {
            if (!context.Channel.Active)
                return default;

            var rpcId = IdCreator.CreateRpcId();
            var callContext = context.Handler.CreateAwaiterCallContext<TResult>(context.ChannelId, rpcId, typeof(TResult));
            var task = callContext.GetResult<TResult>();

            context.SendAsync(messageId, rpcId, input, SenderType.CallSender);
            task.Wait();
            return task.Result;
        }

        public static Task<TResult> CallAsync<TResult>(this ChannelContext context, Enum messageId, object input)
        {
            var integerId = Convert.ToInt32(messageId);
            return context.CallAsync<TResult>(integerId, input);
        }

        public static Task<TResult> CallAsync<TResult>(this ChannelContext context, int messageId, object input)
        {
            if (!context.Channel.Active)
                return default;

            var rpcId = IdCreator.CreateRpcId();
            var callContext = context.Handler.CreateAwaiterCallContext<TResult>(context.ChannelId, rpcId, typeof(TResult));

            var task = callContext.GetResult<TResult>();
            context.SendAsync(messageId, rpcId, input, SenderType.CallSender);
            return task;
        }

        public static void Output(this ChannelContext context, object output)
        {
            context.OutputAsync(output);
        }

        public static Task OutputAsync(this ChannelContext context, object output)
        {
            try
            {
                var senderType = context.SenderType == SenderType.CallSender ? SenderType.FeedbackSender : context.SenderType;
                return context.SendAsync(context.MessageId, context.RpcId, output, senderType);
            }
            finally
            {
                context.Handler.EnqueueChannelContext(context);
            }
        }

        private static Task SendAsync(this ChannelContext context, int messageId, int rpcId, object message, SenderType senderType = SenderType.NormalSender)
        {
            if (!context.Channel.Active)
                return Task.FromResult(0);

            try
            {
                var buffer = Unpooled.Buffer();
                buffer.WriteInt(messageId);
                buffer.WriteInt(rpcId);
                buffer.WriteByte((byte)senderType);
                if (message != null)
                {
                    byte[] messageBytes = null;
                    if (message is string)
                        messageBytes = Encoding.UTF8.GetBytes(message.ToString());
                    else
                        messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                    buffer.WriteBytes(messageBytes);
                }
                return context.Channel.WriteAndFlushAsync(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Task.FromResult(0);
            }
        }
    }
}