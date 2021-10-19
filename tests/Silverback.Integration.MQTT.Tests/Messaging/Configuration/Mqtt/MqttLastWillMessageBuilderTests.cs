﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttLastWillMessageBuilderTests
{
    [Fact]
    public void Build_WithoutTopicName_ExceptionThrown()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        Action act = () => builder.Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void ProduceTo_TopicName_TopicSet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder.ProduceTo("testaments");

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.Topic.Should().Be("testaments");
    }

    [Fact]
    public async Task Message_SomeMessage_PayloadSet()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .Message(message);

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.Payload.Should().NotBeNullOrEmpty();

        (object? deserializedMessage, Type _) = await new JsonMessageSerializer<TestEventOne>().DeserializeAsync(
            new MemoryStream(willMessage.Payload),
            new MessageHeaderCollection(),
            TestConsumerEndpoint.GetDefault());

        deserializedMessage.Should().BeEquivalentTo(message);
    }

    [Fact]
    public void WithDelay_TimeSpam_DelaySet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .WithDelay(TimeSpan.FromSeconds(42));

        builder.Delay.Should().Be(42);
    }

    [Fact]
    public void WithQualityOfServiceLevel_QosLevelSet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_QosLevelSet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .WithAtMostOnceQoS();

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_QosLevelSet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .WithAtLeastOnceQoS();

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithExactlyOnceQoS_QosLevelSet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .WithExactlyOnceQoS();

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void WithRetain_RetainFlagSet()
    {
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .Retain();

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.Retain.Should().BeTrue();
    }

    [Fact]
    public async Task SerializeUsing_NewtonsoftJsonSerializer_PayloadSerialized()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        NewtonsoftJsonMessageSerializer<TestEventOne> serializer = new()
        {
            Encoding = MessageEncoding.Unicode,
            Settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            }
        };
        byte[]? messageBytes = (await serializer.SerializeAsync(
            message,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault())).ReadAll();
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .SerializeUsing(serializer)
            .Message(message);

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.Payload.Should().NotBeNullOrEmpty();
        willMessage.Payload.Should().BeEquivalentTo(messageBytes);
    }

    [Fact]
    public async Task SerializeAsJson_Action_PayloadSerialized()
    {
        TestEventOne message = new() { Content = "Hello MQTT!" };
        JsonMessageSerializer<TestEventOne> serializer = new()
        {
            Options = new JsonSerializerOptions
            {
                WriteIndented = true
            }
        };
        byte[]? messageBytes = (await serializer.SerializeAsync(
            message,
            new MessageHeaderCollection(),
            TestProducerEndpoint.GetDefault())).ReadAll();
        MqttLastWillMessageBuilder<TestEventOne> builder = new();

        builder
            .ProduceTo("testaments")
            .SerializeAsJson(
                serializerBuilder => serializerBuilder
                    .WithOptions(
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }))
            .Message(message);

        MqttApplicationMessage willMessage = builder.Build();
        willMessage.Payload.Should().NotBeNullOrEmpty();
        willMessage.Payload.Should().BeEquivalentTo(messageBytes);
    }
}
