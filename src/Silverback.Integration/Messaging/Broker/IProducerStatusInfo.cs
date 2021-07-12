// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Encapsulates the status details and basic statistics of an <see cref="IProducer" />.
    /// </summary>
    public interface IProducerStatusInfo
    {
        /// <summary>
        ///     Gets the current producer status.
        /// </summary>
        ProducerStatus Status { get; }

        /// <summary>
        ///     Gets the collection of <see cref="IProducerStatusChange" /> recording all state transitions.
        /// </summary>
        IReadOnlyCollection<IProducerStatusChange> History { get; }

        /// <summary>
        ///     Gets the total number of messages that have been produced by the consumer instance.
        /// </summary>
        int ProducedMessagesCount { get; }

        /// <summary>
        ///     Gets the timestamp at which the latest message has been consumed.
        /// </summary>
        DateTime? LatestProducedMessageTimestamp { get; }

        /// <summary>
        ///     Gets the message identifier of the latest consumed message.
        /// </summary>
        IBrokerMessageIdentifier? LatestProducedMessageIdentifier { get; }
    }
}
