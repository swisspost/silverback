﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaBroker" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public KafkaBroker(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new KafkaProducer(
                this,
                endpoint,
                behaviorsProvider,
                serviceProvider.GetRequiredService<IConfluentProducersCache>(),
                serviceProvider,
                serviceProvider.GetRequiredService<IOutboundLogger<KafkaProducer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new KafkaConsumer(
                this,
                endpoint,
                behaviorsProvider,
                serviceProvider.GetRequiredService<IConfluentConsumerBuilder>(),
                serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>(),
                serviceProvider,
                serviceProvider.GetRequiredService<IInboundLogger<KafkaConsumer>>());
    }
}
