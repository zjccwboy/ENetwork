using System;

namespace ENetwork
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AdapterBinderAttribute : Attribute
    {
        public int MessageId { get; }
        public AdapterBinderAttribute(object messageId)
        {
            MessageId = Convert.ToInt32(messageId);
        }
    }
}
