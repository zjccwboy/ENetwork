using ENetwork;
using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Benckmark
{
    [AdapterBinder(100)]
    public class PingPongAdapter : MessageAdapter<Ping>
    {
        public override async void Input(ChannelContext context, Ping input)
        {
            await context.OutputAsync(new Pong { PongInfo = "Pong info." });
        }
    }
}
