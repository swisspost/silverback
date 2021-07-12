// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    internal class ProducerStatusInfo : IProducerStatusInfo
    {
        private readonly List<IProducerStatusChange> _history = new();

        public IReadOnlyCollection<IProducerStatusChange> History => _history;

        public ProducerStatus Status { get; private set; }

        public int ProducedMessagesCount { get; private set; }

        public DateTime? LatestProducedMessageTimestamp { get; private set; }

        public IBrokerMessageIdentifier? LatestProducedMessageIdentifier { get; private set; }

        public void SetDisconnected() => ChangeStatus(ProducerStatus.Disconnected);

        public void SetConnected(bool allowStepBack = false)
        {
            if (allowStepBack || Status < ProducerStatus.Connected)
                ChangeStatus(ProducerStatus.Connected);
        }

        public void SetReady() => ChangeStatus(ProducerStatus.Ready);

        public void RecordProducedMessage(IBrokerMessageIdentifier? brokerMessageIdentifier)
        {
            if (Status is ProducerStatus.Connected or ProducerStatus.Ready)
                ChangeStatus(ProducerStatus.Producing);

            ProducedMessagesCount++;
            LatestProducedMessageTimestamp = DateTime.UtcNow;
            LatestProducedMessageIdentifier = brokerMessageIdentifier;
        }

        private void ChangeStatus(ProducerStatus status)
        {
            Status = status;
            _history.Add(new ProducerStatusChange(status));
        }
    }
}
