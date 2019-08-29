using ENetwork;
using System;
using System.Diagnostics;

namespace Test.Benckmark
{
    class Program
    {
        private static NettyNetwork Client = new NettyNetwork("127.0.0.1", 6666);
        private static NettyNetwork Server = new NettyNetwork(6666);

        static void Main(string[] args)
        {
            MessageBinder.Bind(typeof(PingPongAdapter).Assembly);

            Server.ListenAsync().Wait();
            Client.ConnectAsync().Wait();

            PingPong();

            Console.ReadKey();
        }

        private static int CompleteCount;

        static async void PingPong()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (true)
            {
                var context = Client.GetSingleContext();
                if(context == null)
                {
                    Console.WriteLine("Not socket connection.");
                    return;
                }

                var output = await Client.GetSingleContext().CallAsync<Pong>(100, new Ping { PingInfo = "Ping info." });
                if(output == null)
                {
                    Console.WriteLine($"Response data is null.");
                    return;
                }

                System.Threading.Interlocked.Increment(ref CompleteCount);

                if(CompleteCount % 10000 == 0)
                {
                    Console.WriteLine($"Completed {CompleteCount} ElapsedMilliseconds:{stopwatch.ElapsedMilliseconds}");
                    stopwatch.Restart();
                }
            }
        }
    }
}
