using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;

namespace ENetwork
{
    public class NettyNetwork
    {
        private const int HEART_BEAT_RPC_FLAG = -1;

        public int Id { get;}
        public Action<ChannelContext> OnActive { get; set; }
        public Action<ChannelContext> OnClose { get; set; }
        public int HeartbeatMilisecond { get; set; } = 8000;
        public bool EnableHeartbeat { get; set; } = true;

        private string Address { get; set; }
        private int Port { get; set; }
        private Bootstrap ClientBootstrap { get; set; }
        private ServerBootstrap ServerBootstrap { get; set; }
        private IEventLoopGroup EventLoopGroup { get; set; }
        private IEventLoopGroup WorkerGroup { get; set; }

        private volatile bool IsReconnectStarted;

        public NettyNetwork(int port)
        {
            this.Address = "0.0.0.0";
            this.Port = port;
            this.Id = IdCreator.CreateNetworkId();
            MessageBinder.Bind<HeartbeatAdapter>(HEART_BEAT_RPC_FLAG);
        }

        public NettyNetwork(string address, int port)
        {
            this.Address = address;
            this.Port = port;
            this.Id = IdCreator.CreateNetworkId();
            MessageBinder.Bind<HeartbeatAdapter>(HEART_BEAT_RPC_FLAG);
        }

        public Task ConnectAsync()
        {
            if(this.EventLoopGroup == null)
            {
                EventLoopGroup = new MultithreadEventLoopGroup();
                ClientBootstrap = new Bootstrap();
                ClientBootstrap
                    .Group(this.EventLoopGroup)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.ConnectTimeout, TimeSpan.FromMilliseconds(60000))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new LoggingHandler());
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
                        pipeline.AddLast("network-handler", new NetworkHandler(true, this));
                    }));

                if (EnableHeartbeat) Ping(true);
            }

            try
            {
                return this.ClientBootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(this.Address), this.Port));
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                ReconnectAsync();
                return Task.FromResult(0);
            }
        }

        public Task ListenAsync()
        {
            if (EventLoopGroup == null)
            {
                var EventLoopGroup = new DispatcherEventLoopGroup();
                WorkerGroup = new WorkerEventLoopGroup(EventLoopGroup);
                ServerBootstrap = new ServerBootstrap();
                ServerBootstrap.Group(EventLoopGroup, WorkerGroup);
                ServerBootstrap.Channel<TcpServerChannel>();
                ServerBootstrap
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(4));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(int.MaxValue, 0, 4, 0, 4));
                        pipeline.AddLast("network-handler", new NetworkHandler(true, this));
                    }));

                if (EnableHeartbeat) Ping(false);
            }
            try
            {
                Console.WriteLine($"Listen ip {this.Address} port {this.Port}");
                return ServerBootstrap.BindAsync(new IPEndPoint(IPAddress.Parse(this.Address), this.Port));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Task.FromResult(0);
            }
        }

        internal async void ReconnectAsync()
        {
            if (IsReconnectStarted)
                return;

            IsReconnectStarted = true;
            while (true)
            {
                await Task.Delay(3000);
                try
                {
                    await this.ClientBootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(this.Address), this.Port));
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            IsReconnectStarted = false;
        }

        private async void Ping(bool isClient)
        {
            while (true)
            {
                try
                {
                    await Task.Delay(HeartbeatMilisecond);
                    if (isClient)
                    {
                        var context = this.GetSingleContext();
                        await SendHeartbeat(context);
                    }
                    else
                    {
                        var contexts = this.GetContextList();
                        foreach (var context in contexts)
                            await SendHeartbeat(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private async Task SendHeartbeat(ChannelContext context)
        {
            if (context == null)
                return;

            const string heartbeatMsg = "ping";
            var heartbeat = await context.CallAsync<string>(HEART_BEAT_RPC_FLAG, heartbeatMsg);
            if (heartbeat != heartbeatMsg)
            {
                await context.Handler.DisconnectAsync(context.ChannelHanderContext);
                return;
            }
        }
    }
}
