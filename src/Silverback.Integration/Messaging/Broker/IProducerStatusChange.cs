// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Encapsulates the information about the producer status transition.
    /// </summary>
    public interface IProducerStatusChange
    {
        /// <summary>
        ///     Gets the status into which the producer has transitioned.
        /// </summary>
        ProducerStatus Status { get; }

        /// <summary>
        ///     Gets the timestamp at which the producer transitioned to this status.
        /// </summary>
        DateTime? Timestamp { get; }
    }
}
