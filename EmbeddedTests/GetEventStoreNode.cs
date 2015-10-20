using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using EventStore.Core.Data;

namespace EmbeddedTests
{
    public static class GetEventStoreNode
    {
        public static ClusterVNode Create(out Task nodeStarted)
        {
            var source = new TaskCompletionSource<int>();

            var notListening = new IPEndPoint(IPAddress.None, 0);

            ClusterVNode node = EmbeddedVNodeBuilder.AsSingleNode()
                .RunInMemory()
                .WithExternalHttpOn(notListening)
                .WithExternalTcpOn(notListening)
                .WithInternalHttpOn(notListening)
                .WithInternalTcpOn(notListening)
                // set the chunk size to something small. 
                // This way you can run more tests in parallel and not run out of memory!
                .WithTfChunkSize(1024 * 1024);

            node.NodeStatusChanged += (_, args) =>
            {
                if (args.NewVNodeState != VNodeState.Master)
                {
                    return;
                }
                source.SetResult(0);
            };

            node.Start();

            nodeStarted = source.Task;

            return node;
        }
    }
}