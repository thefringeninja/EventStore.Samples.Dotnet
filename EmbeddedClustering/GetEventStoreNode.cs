using System;
using System.Linq;
using System.Net;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using EventStore.Core.Data;

namespace EmbeddedClustering
{
    public static class GetEventStoreNode
    {
        public static ClusterVNode Create(int nodeId, int clusterSize, Action startProcessing, Action stopProcessing)
        {
            const int i = 1000;

            var gossipSeeds = Enumerable.Range(0, clusterSize)
                .Except(new[]{nodeId})
                .Select(otherNodeId => new IPEndPoint(IPAddress.Loopback, 1113 + otherNodeId * i))
                .ToArray();

            var builder = EmbeddedVNodeBuilder
                .AsClusterMember(clusterSize)
                .WithWorkerThreads(1)
                .WithInternalHeartbeatInterval(TimeSpan.FromSeconds(5))
                .WithGossipSeeds(
                    gossipSeeds)
                .RunInMemory()
                
                .WithInternalTcpOn(new IPEndPoint(IPAddress.Loopback, 1111 + nodeId * i))
                .WithExternalTcpOn(new IPEndPoint(IPAddress.Loopback, 1112 + nodeId * i))
                .WithInternalHttpOn(new IPEndPoint(IPAddress.Loopback, 1113 + nodeId * i))
                .AddInternalHttpPrefix("http://*:" + 1113 + nodeId * i + "/")
                .WithExternalHttpOn(new IPEndPoint(IPAddress.Loopback, 1114 + nodeId * i))
                .AddExternalHttpPrefix("http://*:" + 1114 + nodeId * i + "/")
                .WithTfChunkSize(1024 * 1024);

            ClusterVNode node = builder;

            node.NodeStatusChanged += (_, args) =>
            {
                if (args.NewVNodeState != VNodeState.Master)
                {
                    stopProcessing();
                }
                else
                {
                    startProcessing();
                }
            };

            return node;
        }
    }
}