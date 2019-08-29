using System;
using System.Collections.Generic;
using System.Text;
using ENetwork;
using Example.Common;

namespace Example.SayHelloServer
{
    [AdapterBinder(SayType.SayHello)]
    public class SayHelloAdapter : MessageAdapter<SayInput>
    {
        public override async void Input(ChannelContext context, SayInput input)
        {
            Console.WriteLine($"The client say:{input.TalkContent}");
            await context.OutputAsync(new SayOutput { TalkContent = $"I am server, hello client." });
        }
    }
}
