﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Connectors
{
    public class OutboundConnector : IOutboundConnector
    {
        private readonly IBroker _broker;

        public OutboundConnector(IBroker broker)
        {
            _broker = broker;
        }

        public Task RelayMessage(object message, IEndpoint destinationEndpoint) =>
            _broker.GetProducer(destinationEndpoint).ProduceAsync(message);
    }
}