

namespace ENetwork
{
    public interface IMessageAdapter<TMessage> : IMessageAdapter where TMessage : class
    {
        void Input(ChannelContext context, TMessage input);
    }

    public interface IMessageAdapter
    {
        void DispatchAdapter(ChannelContext context, object input);
    }
}