// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    internal class ProducerStatusChange : IProducerStatusChange
    {
        public ProducerStatusChange(ProducerStatus status)
            : this(status, DateTime.UtcNow)
        {
        }

        public ProducerStatusChange(ProducerStatus status, DateTime timestamp)
        {
            Status = status;
            Timestamp = timestamp;
        }

        public ProducerStatus Status { get; }

        public DateTime? Timestamp { get; }
    }
}
