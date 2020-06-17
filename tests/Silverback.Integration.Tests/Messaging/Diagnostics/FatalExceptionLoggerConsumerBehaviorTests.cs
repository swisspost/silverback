// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class FatalExceptionLoggerConsumerBehaviorTests
    {
        [Fact]
        public async Task Handle_ExceptionThrown_ExceptionLogged()
        {
            var logger = Substitute.For<ILogger<FatalExceptionLoggerConsumerBehavior>>();
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            try
            {
                await new FatalExceptionLoggerConsumerBehavior(logger).Handle(
                    new ConsumerPipelineContext(new[] { rawEnvelope }, Substitute.For<IConsumer>()),
                    Substitute.For<IServiceProvider>(),
                    (_, __) => throw new InvalidCastException());
            }
            catch (Exception)
            {
                // Ignored
            }

            logger.ReceivedLog(LogLevel.Critical, typeof(InvalidCastException));
        }

        [Fact]
        public void Handle_ExceptionThrown_ExceptionRethrown()
        {
            var logger = Substitute.For<ILogger<FatalExceptionLoggerConsumerBehavior>>();
            var rawEnvelope = new RawInboundEnvelope(
                new byte[5],
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            Func<Task> act = () => new FatalExceptionLoggerConsumerBehavior(logger).Handle(
                new ConsumerPipelineContext(new[] { rawEnvelope }, Substitute.For<IConsumer>()),
                Substitute.For<IServiceProvider>(),
                (_, __) => throw new InvalidCastException());

            act.Should().ThrowExactly<InvalidCastException>();
        }
    }
}