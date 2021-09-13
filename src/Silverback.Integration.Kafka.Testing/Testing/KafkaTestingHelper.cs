// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="IKafkaTestingHelper" />
    public class KafkaTestingHelper : TestingHelper<KafkaBroker>, IKafkaTestingHelper
    {
        private readonly IInMemoryTopicCollection? _topics;

        private readonly IMockedConsumerGroupsCollection? _groups;

        private readonly KafkaBroker _kafkaBroker;

        private readonly ILogger<KafkaTestingHelper> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaTestingHelper" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public KafkaTestingHelper(
            IServiceProvider serviceProvider,
            ILogger<KafkaTestingHelper> logger)
            : base(serviceProvider, logger)
        {
            _topics = serviceProvider.GetService<IInMemoryTopicCollection>();
            _groups = serviceProvider.GetService<IMockedConsumerGroupsCollection>();
            _kafkaBroker = serviceProvider.GetRequiredService<KafkaBroker>();
            _logger = logger;
        }

        /// <inheritdoc cref="IKafkaTestingHelper.GetConsumerGroup(string)" />
        public IMockedConsumerGroup GetConsumerGroup(string groupId)
        {
            if (_groups == null)
                throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

            return _groups.First(group => group.GroupId == groupId);
        }

        /// <inheritdoc cref="IKafkaTestingHelper.GetConsumerGroup(string,string)" />
        public IMockedConsumerGroup GetConsumerGroup(string groupId, string bootstrapServers)
        {
            if (_groups == null)
                throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

            return _groups.First(group => group.GroupId == groupId && group.BootstrapServers == bootstrapServers);
        }

        /// <inheritdoc cref="IKafkaTestingHelper.GetTopic(string)" />
        public IInMemoryTopic GetTopic(string name) =>
            GetTopics(name).First();

        /// <inheritdoc cref="IKafkaTestingHelper.GetTopic(string,string)" />
        public IInMemoryTopic GetTopic(string name, string bootstrapServers) =>
            GetTopics(name, bootstrapServers).First();

        /// <inheritdoc cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(CancellationToken)" />
        public override async Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken)
        {
            if (_groups == null)
                return;

            try
            {
                // Loop until the outbox is empty since the consumers may produce new messages
                do
                {
                    await WaitUntilOutboxIsEmptyAsync(cancellationToken).ConfigureAwait(false);

                    await Task.WhenAll(
                            _groups.Select(
                                group =>
                                    group.WaitUntilAllMessagesAreConsumedAsync(cancellationToken)))
                        .ConfigureAwait(false);
                }
                while (!await IsOutboxEmptyAsync().ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The timeout elapsed before all messages could be consumed and processed.");
            }
        }

        private IReadOnlyCollection<IInMemoryTopic> GetTopics(string name, string? bootstrapServers = null)
        {
            if (_topics == null)
                throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

            List<IInMemoryTopic> topics = _topics.Where(
                    topic =>
                        topic.Name == name &&
                        (bootstrapServers == null || string.Equals(
                            bootstrapServers,
                            topic.BootstrapServers,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (topics.Count != 0)
                return topics;

            if (bootstrapServers != null)
                return new[] { _topics.Get(name, bootstrapServers) };

            // If the topic wasn't created yet, just create one per each broker
            return _kafkaBroker
                .Producers.Select(producer => ((KafkaProducerEndpoint)producer.Endpoint).Configuration.BootstrapServers)
                .Union(
                    _kafkaBroker.Consumers.Select(
                        producer =>
                            ((KafkaConsumerEndpoint)producer.Endpoint).Configuration.BootstrapServers))
                .Select(servers => servers.ToUpperInvariant())
                .Distinct()
                .Select(servers => _topics.Get(name, servers))
                .ToList();
        }
    }
}
