using System;
using System.Threading.Tasks;

namespace ENetwork.CallCotext
{
    public struct CallContext<TResult> : ICallContext
    {
        public TaskCompletionSource<TResult> Tcs { get; set; }

        public Type MessageType { get; set; }

        public Task<TResult1> GetResult<TResult1>()
        {
            var tcs = this.Tcs;
            return tcs.Task as Task<TResult1>;
        }

        public void SetResult(object result)
        {
            if (Tcs == null)
                return;

            if (result == null)
                Tcs.TrySetResult(default);
            else
                Tcs.TrySetResult((TResult)result);
        }
    }
}