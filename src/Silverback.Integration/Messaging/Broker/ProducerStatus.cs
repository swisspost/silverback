// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The possible states of the <see cref="IProducer" /> as exposed in the
    ///     <see cref="IProducerStatusInfo" />.
    /// </summary>
    public enum ProducerStatus
    {
        /// <summary>
        ///     The producer is not connected to the message broker.
        /// </summary>
        Disconnected = 0,

        /// <summary>
        ///     The producer has successfully initialized the connection to the message broker.
        /// </summary>
        /// <remarks>
        ///     This doesn't necessary mean that it is connected and ready to produce. The underlying library might
        ///     handle the connection process asynchronously in the background or the protocol might require extra steps.
        /// </remarks>
        Connected = 1,

        /// <summary>
        ///     The producer is completely initialized and is ready to produce.
        /// </summary>
        /// <remarks>
        ///     This includes all extra steps that might be required by the underlying library or the protocol.
        /// </remarks>
        Ready = 2,

        /// <summary>
        ///     The producer is connected and has produced some messages.
        /// </summary>
        Producing = 3
    }
}
