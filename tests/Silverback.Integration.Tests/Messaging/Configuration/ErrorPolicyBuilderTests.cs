﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class ErrorPolicyBuilderTests
{
    [Fact]
    public void Stop_Default_StopPolicyCreated()
    {
        ErrorPolicyBuilder builder = new();

        builder.Stop();
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<StopConsumerErrorPolicy>();
    }

    [Fact]
    public void Stop_WithConfiguration_StopPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();

        builder.Stop(stopPolicy => stopPolicy.Exclude<InvalidOperationException>());
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<StopConsumerErrorPolicy>();
        policy.As<StopConsumerErrorPolicy>().ExcludedExceptions.Should()
            .BeEquivalentTo(new[] { typeof(InvalidOperationException) });
    }

    [Fact]
    public void Skip_Default_SkipPolicyCreated()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip();
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<SkipMessageErrorPolicy>();
    }

    [Fact]
    public void Skip_WithConfiguration_SkipPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();

        builder.Skip(skipPolicy => skipPolicy.ApplyTo<TimeoutException>());
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<SkipMessageErrorPolicy>();
        policy.As<SkipMessageErrorPolicy>().IncludedExceptions.Should()
            .BeEquivalentTo(new[] { typeof(TimeoutException) });
    }

    [Fact]
    public void Retry_Default_RetryPolicyCreated()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry();
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
    }

    [Fact]
    public void Retry_WithConfiguration_RetryPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(retryPolicy => retryPolicy.MaxFailedAttempts(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
    }

    [Fact]
    public void Retry_WithRetriesCountAndConfiguration_RetryPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(
            21,
            retryPolicy => retryPolicy.MaxFailedAttempts(retryPolicy.MaxFailedAttemptsCount * 2));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
    }

    [Fact]
    public void Retry_WithRetriesCountAndDelayAndConfiguration_RetryPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(
            21,
            TimeSpan.FromSeconds(42),
            retryPolicy => retryPolicy.MaxFailedAttempts(retryPolicy.MaxFailedAttemptsCount * 2));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
        policy.As<RetryErrorPolicy>().InitialDelay.Should().Be(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void
        Retry_WithRetriesCountAndDelayAndIncrementAndConfiguration_RetryPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();

        builder.Retry(
            21,
            TimeSpan.FromSeconds(42),
            TimeSpan.FromMinutes(42),
            retryPolicy => retryPolicy.MaxFailedAttempts(retryPolicy.MaxFailedAttemptsCount * 2));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<RetryErrorPolicy>();
        policy.As<RetryErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
        policy.As<RetryErrorPolicy>().InitialDelay.Should().Be(TimeSpan.FromSeconds(42));
        policy.As<RetryErrorPolicy>().DelayIncrement.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void Move_Endpoint_MovePolicyCreated()
    {
        ErrorPolicyBuilder builder = new();
        TestProducerConfiguration endpoint = TestProducerConfiguration.GetDefault();

        builder.Move(endpoint);
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.Should().BeSameAs(endpoint);
    }

    [Fact]
    public void Move_EndpointWithConfiguration_SkipPolicyCreatedAndConfigurationApplied()
    {
        ErrorPolicyBuilder builder = new();
        TestProducerConfiguration endpoint = TestProducerConfiguration.GetDefault();

        builder.Move(endpoint, movePolicy => movePolicy.MaxFailedAttempts(42));
        IErrorPolicy policy = builder.Build();

        policy.Should().BeOfType<MoveMessageErrorPolicy>();
        policy.As<MoveMessageErrorPolicy>().ProducerConfiguration.Should().BeSameAs(endpoint);
        policy.As<MoveMessageErrorPolicy>().MaxFailedAttemptsCount.Should().Be(42);
    }
}
