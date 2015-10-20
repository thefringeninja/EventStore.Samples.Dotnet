using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using Xunit;

namespace EmbeddedTests
{
    public class get_event_store_should : IDisposable
    {
        private readonly Task _nodeStarted;
        private readonly ClusterVNode _node;
        private readonly IEventStoreConnection _connection;

        public get_event_store_should()
        {
            _node = GetEventStoreNode.Create(out _nodeStarted);
            _node.Start();
            _connection = EmbeddedEventStoreConnection.Create(_node);
        }

        [Fact]
        public async Task write_events()
        {
            await _nodeStarted;

            await _connection.ConnectAsync();

            await _connection.AppendToStreamAsync("my-stream", ExpectedVersion.NoStream, CreateEvent(0));

            var slice = await _connection.ReadStreamEventsForwardAsync("my-stream", 0, 512, false);

            Assert.Equal(1, slice.Events.Length);
        }

        private static EventData CreateEvent(int count)
        {
            var data = Encoding.UTF8.GetBytes(string.Format(@"{{""count"": {0}}}", count));

            return new EventData(Guid.NewGuid(), "my-event", true, data, null);
        }


        public void Dispose()
        {
            _connection.Dispose();
            _node.Stop();
        }
    }
}
