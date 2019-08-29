using System;
using ENetwork;
using Example.Common;

namespace Example.SayHelloServer
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageBinder.Bind(typeof(SayHiAdapter).Assembly);
            new NettyNetwork(6666).ListenAsync().Wait();

            Console.ReadKey();
        }
    }
}
