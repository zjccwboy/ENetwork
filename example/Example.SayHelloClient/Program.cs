using ENetwork;
using Example.Common;
using System;

namespace Example.SayHelloClient
{
    class Program
    {
        private static NettyNetwork network = new NettyNetwork("127.0.0.1", 6666);

        static void Main(string[] args)
        {
            network.ConnectAsync().Wait();

            Say();

            Console.ReadKey();
        }

        static void Say()
        {
            var helloOutput = network.GetSingleContext().Call<SayOutput>(SayType.SayHello, new SayInput { TalkContent = "I am client, hello server." });
            Console.WriteLine($"The server say:{helloOutput.TalkContent}");

            var hiOutput = network.GetSingleContext().Call<SayOutput>(SayType.SayHi, new SayInput { TalkContent = "I am client, hi server." });
            Console.WriteLine($"The server say:{hiOutput.TalkContent}");
        }
    }
}
