﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Uses an <see cref="IOffsetStore" /> to keep track of the last processed offsets and guarantee that
    ///     each message is processed only once.
    /// </summary>
    public class OffsetStoredInboundConnector : ExactlyOnceInboundConnector
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OffsetStoredInboundConnector" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public OffsetStoredInboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ISilverbackLogger<OffsetStoredInboundConnector> logger)
            : base(brokerCollection, serviceProvider, logger)
        {
        }

        /// <inheritdoc cref="ExactlyOnceInboundConnector.MustProcess" />
        protected override async Task<bool> MustProcess(IRawInboundEnvelope envelope, IServiceProvider serviceProvider)
        {
            Check.NotNull(envelope, nameof(envelope));

            if (!(envelope.Offset is IComparableOffset comparableOffset))
            {
                throw new InvalidOperationException(
                    "The message broker implementation doesn't seem to support comparable offsets. " +
                    "The OffsetStoredInboundConnector cannot be used, please resort to LoggedInboundConnector " +
                    "to ensure exactly-once delivery.");
            }

            var offsetStore = serviceProvider.GetRequiredService<IOffsetStore>();

            var latest = await offsetStore.GetLatestValue(envelope.Offset.Key, envelope.Endpoint).ConfigureAwait(false);
            if (latest != null && latest.CompareTo(comparableOffset) >= 0)
                return false;

            serviceProvider.GetRequiredService<ConsumerTransactionManager>().Enlist(offsetStore);

            await offsetStore.Store(comparableOffset, envelope.Endpoint).ConfigureAwait(false);
            return true;
        }
    }
}
