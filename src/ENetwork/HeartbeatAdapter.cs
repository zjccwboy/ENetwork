
namespace ENetwork
{
    public class HeartbeatAdapter : MessageAdapter<string>
    {
        public override async void Input(ChannelContext context, string input)
        {
            await context.OutputAsync(input);
        }
    }
}
