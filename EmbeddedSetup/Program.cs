using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using EventStore.Core.Data;
using EventStore.Projections.Core.Services.Http;
using ExpectedVersion = EventStore.ClientAPI.ExpectedVersion;

namespace EmbeddedSetup
{
    class Program
    {
        private static Task<int> _eventStoreNodeStarted;

        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static async Task Run()
        {
            var eventStore = CreateEventStore();

            eventStore.Start();

            await _eventStoreNodeStarted;

            using (var connection = EmbeddedEventStoreConnection.Create(eventStore))
            {
                await connection.ConnectAsync();

                await WriteEvents(connection);

                Console.WriteLine("Done writing events. Hit enter to continue.");
                Console.ReadLine();

                await ReadEvents(connection);

                Console.ReadLine();
            }
        }

        private static async Task ReadEvents(IEventStoreConnection connection)
        {
            var slice = await connection.ReadStreamEventsForwardAsync("my-stream", 0, 128, false);

            foreach (var @event in slice.Events)
            {
                Console.WriteLine(Encoding.UTF8.GetString(@event.Event.Data));
            }
        }

        private static async Task WriteEvents(IEventStoreConnection connection)
        {
            var events = Enumerable.Range(0, 128)
                .Select(CreateEvent);

            await connection.AppendToStreamAsync(
                "my-stream",
                ExpectedVersion.Any,
                events);
        }

        private static EventData CreateEvent(int count)
        {
            var data = Encoding.UTF8.GetBytes(string.Format(@"{{""count"": {0}}}", count));

            return new EventData(Guid.NewGuid(), "my-event", true, data, null);
        }

        private static ClusterVNode CreateEventStore()
        {
            var notListening = new IPEndPoint(IPAddress.None, 0);

            ClusterVNode eventStoreNode = EmbeddedVNodeBuilder.AsSingleNode()
                .WithExternalHttpOn(notListening)
                .WithInternalHttpOn(notListening)
                .WithExternalTcpOn(notListening)
                .WithInternalTcpOn(notListening)
                .RunInMemory();

            var eventStoreNodeStartedSource = new TaskCompletionSource<int>();

            _eventStoreNodeStarted = eventStoreNodeStartedSource.Task;

            eventStoreNode.NodeStatusChanged += (_, e) =>
            {
                if (e.NewVNodeState != VNodeState.Master)
                {
                    return;
                }

                eventStoreNodeStartedSource.SetResult(0);
            };

            return eventStoreNode;
        }
    }
}
