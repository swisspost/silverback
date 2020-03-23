// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherProducer : Producer<TestOtherBroker, TestOtherProducerEndpoint>
    {
        public List<ProducedMessage> ProducedMessages { get; }

        public TestOtherProducer(
            TestOtherBroker broker,
            TestOtherProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors)
            : base(
                broker,
                endpoint,
                new MessageIdProvider(new[] { new DefaultPropertiesMessageIdProvider() }),
                behaviors,
                new NullLogger<TestOtherProducer>(),
                new MessageLogger())
        {
            ProducedMessages = broker.ProducedMessages;
        }

        protected override IOffset Produce(RawBrokerEnvelope envelope)
        {
            ProducedMessages.Add(new ProducedMessage(envelope.RawMessage, envelope.Headers, Endpoint));
            return null;
        }

        protected override Task<IOffset> ProduceAsync(RawBrokerEnvelope envelope)
        {
            Produce(envelope.RawMessage, envelope.Headers);
            return Task.FromResult<IOffset>(null);
        }
    }
}