using System;
using System.Threading.Tasks;

namespace ENetwork.CallCotext
{
    public interface ICallContext
    {
        Type MessageType { get; set; }
        void SetResult(object result);
        Task<TResult> GetResult<TResult>();
    }
}
