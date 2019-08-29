using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ENetwork
{
    public class MessageBinder
    {
        private static Dictionary<int, IMessageAdapter> HandlerAdapter { get; } = new Dictionary<int, IMessageAdapter>();
        private static Dictionary<int, Type> MessageTypes { get; } = new Dictionary<int, Type>();

        public static void Bind<TAdapter>(int messageId) where TAdapter : IMessageAdapter, new()
        {
            Bind(messageId, typeof(TAdapter));
        }

        public static void Bind<TAdapter>(Enum messageId) where TAdapter : IMessageAdapter, new()
        {
            var id = Convert.ToInt32(messageId);
            Bind<TAdapter>(id);
        }

        public static void Bind(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(t => typeof(IMessageAdapter).IsAssignableFrom(t));
            if (!types.Any())
                return;

            foreach(var type in types)
            {
                var attributes = type.GetCustomAttributes<AdapterBinderAttribute>();
                if (attributes == null || !attributes.Any())
                    continue;

                foreach(var attribute in attributes)
                    Bind(attribute.MessageId, type);
            }
        }

        private static void Bind(int messageId, Type adapterType)
        {
            if (HandlerAdapter.ContainsKey(messageId))
                return;

            var adapter = (IMessageAdapter)Activator.CreateInstance(adapterType);
            HandlerAdapter.Add(messageId, adapter);
            var messageType = adapterType.BaseType.GetGenericArguments().FirstOrDefault();
            MessageTypes.Add(messageId, messageType);
        }

        public static IMessageAdapter GetAdapter(int messageId)
        {
            HandlerAdapter.TryGetValue(messageId, out IMessageAdapter adapter);
            return adapter;
        }

        public static Type GetMessageType(int messageId)
        {
            MessageTypes.TryGetValue(messageId, out Type type);
            return type;
        }
    }
}
