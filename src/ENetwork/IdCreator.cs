
namespace ENetwork
{
    public class IdCreator
    {
        private static int RpcId;
        private static int NetworkId;
        private static int NextId;

        public static int CreateRpcId()
        {
            System.Threading.Interlocked.Increment(ref RpcId);
            System.Threading.Interlocked.CompareExchange(ref RpcId, 1, int.MaxValue);
            return RpcId;
        }

        public static int CreateNetworkId()
        {
            System.Threading.Interlocked.Increment(ref NetworkId);
            System.Threading.Interlocked.CompareExchange(ref NetworkId, 1, int.MaxValue);
            return NetworkId;
        }

        public static int CreateNextId()
        {
            System.Threading.Interlocked.Increment(ref NextId);
            System.Threading.Interlocked.CompareExchange(ref NextId, 1, int.MaxValue);
            return NextId;
        }
    }
}
