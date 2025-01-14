﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks
{
    internal sealed class ClientSession : IDisposable, IClientSession
    {
        private readonly IMqttApplicationMessageReceivedHandler _messageHandler;

        private readonly Channel<MqttApplicationMessage> _channel =
            Channel.CreateUnbounded<MqttApplicationMessage>();

        private readonly List<Subscription> _subscriptions = new();

        private CancellationTokenSource _readCancellationTokenSource = new();

        private int _pendingMessagesCount;

        public ClientSession(
            IMqttClientOptions clientOptions,
            IMqttApplicationMessageReceivedHandler messageHandler)
        {
            ClientOptions = Check.NotNull(clientOptions, nameof(clientOptions));
            _messageHandler = Check.NotNull(messageHandler, nameof(messageHandler));
        }

        public IMqttClientOptions ClientOptions { get; }

        public int PendingMessagesCount => _pendingMessagesCount;

        public bool IsConnected { get; private set; }

        public bool IsConsumerDisconnected =>
            !((_messageHandler as MockedMqttClient)?.Consumer?.IsConnected ?? true);

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public void Connect()
        {
            if (IsConnected)
                return;

            IsConnected = true;

            if (_readCancellationTokenSource.IsCancellationRequested)
                _readCancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => ReadChannelAsync(_readCancellationTokenSource.Token));
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            IsConnected = false;

            _readCancellationTokenSource.Cancel();
        }

        public void Subscribe(IReadOnlyCollection<string> topics)
        {
            lock (_subscriptions)
            {
                foreach (var topic in topics)
                {
                    if (_subscriptions.Any(subscription => subscription.Topic == topic))
                        continue;

                    _subscriptions.Add(new Subscription(ClientOptions, topic));
                }
            }
        }

        public void Unsubscribe(IReadOnlyCollection<string> topics)
        {
            lock (_subscriptions)
            {
                _subscriptions.RemoveAll(subscription => topics.Contains(subscription.Topic));
            }
        }

        public async ValueTask PushAsync(MqttApplicationMessage message, IMqttClientOptions clientOptions)
        {
            lock (_subscriptions)
            {
                if (_subscriptions.All(subscription => !subscription.IsMatch(message.Topic, clientOptions)))
                    return;
            }

            await _channel.Writer.WriteAsync(message).ConfigureAwait(false);

            Interlocked.Increment(ref _pendingMessagesCount);
        }

        public void Dispose()
        {
            Disconnect();

            _readCancellationTokenSource.Dispose();
        }

        private async Task ReadChannelAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MqttApplicationMessage message =
                    await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                var eventArgs = new MqttApplicationMessageReceivedEventArgs(
                    ClientOptions.ClientId,
                    message,
                    new MqttPublishPacket(),
                    (_, _) => Task.CompletedTask);

                Task messageHandlingTask =
                    _messageHandler.HandleApplicationMessageReceivedAsync(eventArgs)
                        .ContinueWith(
                            _ => Interlocked.Decrement(ref _pendingMessagesCount),
                            TaskScheduler.Default);

                if (message.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                    await messageHandlingTask.ConfigureAwait(false);
            }
        }

        private sealed class Subscription
        {
            public Subscription(IMqttClientOptions clientOptions, string topic)
            {
                Topic = topic;
                Regex = GetSubscriptionRegex(topic, clientOptions);
            }

            public string Topic { get; }

            public Regex Regex { get; }

            public bool IsMatch(string topic, IMqttClientOptions clientOptions) =>
                Regex.IsMatch(GetFullTopicName(topic, clientOptions));

            private static Regex GetSubscriptionRegex(string topic, IMqttClientOptions clientOptions)
            {
                var pattern = Regex.Escape(GetFullTopicName(topic, clientOptions))
                    .Replace("\\+", "[\\w]*", StringComparison.Ordinal)
                    .Replace("\\#", "[\\w\\/]*", StringComparison.Ordinal);

                return new Regex($"^{pattern}$", RegexOptions.Compiled);
            }

            private static string GetFullTopicName(string topic, IMqttClientOptions clientOptions) =>
                $"{GetBrokerIdentifier(clientOptions)}|{topic}";

            private static string GetBrokerIdentifier(IMqttClientOptions clientOptions) =>
                clientOptions.ChannelOptions switch
                {
                    MqttClientTcpOptions tcpOptions =>
                        $"{tcpOptions.Server.ToUpperInvariant()}-{tcpOptions.GetPort()}",
                    MqttClientWebSocketOptions socketOptions =>
                        socketOptions.Uri.ToUpperInvariant(),
                    _ => throw new InvalidOperationException(
                        "Expecting ChannelOptions to be of type " +
                        "MqttClientTcpOptions or MqttClientWebSocketOptions.")
                };
        }
    }
}
