﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class StreamingTests : E2ETestFixture
    {
        [Fact]
        public async Task Streaming_UnboundedEnumerable_MessagesReceivedAndCommitted()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                await foreach (var message in eventsStream)
                                {
                                    DefaultTopic.GetCommittedOffsetsCount("consumer1")
                                        .Should().Be(receivedMessages.Count);

                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 15).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(15);
            var receivedContents = receivedMessages.Select(message => message.Content);
            receivedContents.Should().BeEquivalentTo(
                Enumerable.Range(1, 15).Select(i => i.ToString(CultureInfo.InvariantCulture)));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
        }


        [Fact]
        public async Task Streaming_UnboundedObservable_MessagesReceived()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamObservable<TestEventOne> observable) =>
                                observable.Subscribe(
                                    message =>
                                    {
                                        // AsyncTestingUtil.Wait(
                                        //     () => DefaultTopic.GetCommittedOffsetsCount("consumer1") >=
                                        //           receivedMessages.Count,
                                        //     1000);
                                        //
                                        // DefaultTopic.GetCommittedOffsetsCount("consumer1")
                                        //     .Should().Be(receivedMessages.Count);

                                        receivedMessages.Add(message);
                                    })))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 3).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromMinutes(1));

            receivedMessages.Should().HaveCount(3);
            var receivedContents = receivedMessages.Select(message => message.Content);
            receivedContents.Should().BeEquivalentTo(
                Enumerable.Range(1, 3).Select(i => i.ToString(CultureInfo.InvariantCulture)));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task Streaming_DisconnectWhileEnumerating_EnumerationAborted()
        {
            bool aborted = false;
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                try
                                {
                                    foreach (var message in eventsStream)
                                    {
                                        receivedMessages.Add(message);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    aborted = true;
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(2);

            Broker.Disconnect();

            aborted.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Streaming_DisconnectWhileObserving_ObserverCompleted()
        {
            bool completed = false;
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamObservable<TestEventOne> observable) =>
                                observable.Subscribe(
                                    message => receivedMessages.Add(message),
                                    () => completed = true)))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

            receivedMessages.Should().HaveCount(2);

            Broker.Disconnect();

            completed.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Streaming_UnboundedEnumerableProcessingFailed_ConsumerStopped()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                            {
                                await foreach (var message in enumerable)
                                {
                                    receivedMessages.Add(message);
                                    if (receivedMessages.Count == 2)
                                        throw new InvalidOperationException("Test");
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

            receivedMessages.Should().HaveCount(2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Streaming_UnboundedObservableProcessingFailed_ConsumerStopped()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamObservable<TestEventOne> observable) =>
                                observable.Subscribe(
                                    message =>
                                    {
                                        receivedMessages.Add(message);

                                        if (receivedMessages.Count == 2)
                                            throw new InvalidOperationException("Test");
                                    })))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

            receivedMessages.Should().HaveCount(2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }
    }
}
