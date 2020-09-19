﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawOutboundEnvelope" />
    internal class RawOutboundEnvelope : RawBrokerEnvelope, IRawOutboundEnvelope
    {
        public RawOutboundEnvelope(
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            IOffset? offset = null,
            IDictionary<string, string>? additionalLogData = null)
            : this(null, headers, endpoint, offset, additionalLogData)
        {
        }

        public RawOutboundEnvelope(
            Stream? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            IOffset? offset = null,
            IDictionary<string, string>? additionalLogData = null)
            : base(rawMessage, headers, endpoint, offset, additionalLogData)
        {
        }

        public new IProducerEndpoint Endpoint => (IProducerEndpoint)base.Endpoint;
    }
}
