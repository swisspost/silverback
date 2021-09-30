﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint, TIdentifier}" />
    public class MqttConsumer : Consumer<MqttBroker, MqttConsumerEndpoint, MqttMessageIdentifier>
    {
        private readonly MqttClientWrapper _clientWrapper;

        private readonly IInboundLogger<MqttConsumer> _logger;

        private readonly ConcurrentDictionary<string, ConsumedApplicationMessage> _inProcessingMessages =
            new();

        private ConsumerChannelManager? _channelManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttConsumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IInboundLogger{TCategoryName}" />.
        /// </param>
        public MqttConsumer(
            MqttBroker broker,
            MqttConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IInboundLogger<MqttConsumer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _clientWrapper = serviceProvider
                .GetRequiredService<IMqttClientsCache>()
                .GetClient(this);
            _logger = Check.NotNull(logger, nameof(logger));
        }

        internal async Task HandleMessageAsync(ConsumedApplicationMessage message)
        {
            var headers = Endpoint.Configuration.AreHeadersSupported
                ? new MessageHeaderCollection(message.ApplicationMessage.UserProperties.ToSilverbackHeaders())
                : new MessageHeaderCollection();

            headers.AddIfNotExists(DefaultMessageHeaders.MessageId, message.Id);

            // If another message is still pending, cancel it's task (might happen in case of timeout)
            if (!_inProcessingMessages.TryAdd(message.Id, message))
                throw new InvalidOperationException("The message has been processed already.");

            await HandleMessageAsync(
                    message.ApplicationMessage.Payload,
                    headers,
                    message.ApplicationMessage.Topic,
                    new MqttMessageIdentifier(Endpoint.Configuration.ClientId, message.Id))
                .ConfigureAwait(false);
        }

        internal async Task OnConnectionEstablishedAsync()
        {
            await _clientWrapper.SubscribeAsync(
                    Endpoint.Topics.Select(
                            topic =>
                                new MqttTopicFilterBuilder()
                                    .WithTopic(topic)
                                    .WithQualityOfServiceLevel(Endpoint.QualityOfServiceLevel)
                                    .Build())
                        .ToArray())
                .ConfigureAwait(false);

            if (IsConnected)
                await StartAsync().ConfigureAwait(false);

            SetReadyStatus();
        }

        internal async Task OnConnectionLostAsync()
        {
            await StopAsync().ConfigureAwait(false);

            RevertReadyStatus();
        }

        /// <inheritdoc cref="Consumer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync() => _clientWrapper.ConnectAsync(this);

        /// <inheritdoc cref="Consumer.DisconnectCoreAsync" />
        protected override async Task DisconnectCoreAsync()
        {
            await _clientWrapper.UnsubscribeAsync(Endpoint.Topics).ConfigureAwait(false);
            await _clientWrapper.DisconnectAsync(this).ConfigureAwait(false);
        }

        /// <inheritdoc cref="Consumer.StartCoreAsync" />
        protected override Task StartCoreAsync()
        {
            if (_clientWrapper == null)
                throw new InvalidOperationException("The consumer is not connected.");

            _channelManager = new ConsumerChannelManager(_clientWrapper, _logger);
            _channelManager.StartReading();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.StopCoreAsync" />
        protected override Task StopCoreAsync()
        {
            _channelManager?.StopReading();
            _channelManager?.Dispose();
            _channelManager = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.WaitUntilConsumingStoppedCoreAsync" />
        protected override Task WaitUntilConsumingStoppedCoreAsync() =>
            _channelManager?.Stopping ?? Task.CompletedTask;

        /// <inheritdoc cref="Consumer.CommitCoreAsync" />
        protected override Task CommitCoreAsync(
            IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers) =>
            SetProcessingCompletedAsync(brokerMessageIdentifiers, true);

        /// <inheritdoc cref="Consumer.RollbackCoreAsync" />
        protected override Task RollbackCoreAsync(
            IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers) =>
            SetProcessingCompletedAsync(brokerMessageIdentifiers, false);

        private Task SetProcessingCompletedAsync(
            IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers,
            bool isSuccess)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            string messageId = brokerMessageIdentifiers.Single().MessageId;

            if (!_inProcessingMessages.TryRemove(messageId, out var message))
                return Task.CompletedTask;

            message.TaskCompletionSource.SetResult(isSuccess);
            return Task.CompletedTask;
        }
    }
}
