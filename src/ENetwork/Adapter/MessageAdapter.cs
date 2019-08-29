

namespace ENetwork
{
    public abstract class MessageAdapter<TMessage> : IMessageAdapter<TMessage> where TMessage : class
    {
        public abstract void Input(ChannelContext context, TMessage input);

        public void DispatchAdapter(ChannelContext context, object input)
        {
            this.Input(context, input as TMessage);
        }
    }
}
