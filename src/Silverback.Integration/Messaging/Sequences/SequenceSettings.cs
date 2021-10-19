﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Sequences.Batch;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     The sequence handling settings.
    /// </summary>
    public sealed record SequenceSettings : IValidatableEndpointSettings
    {
        /// <summary>
        ///     Gets the timeout after which an incomplete sequence that isn't pushed with new messages will
        ///     be aborted and discarded. The default is a conservative 30 minutes.
        /// </summary>
        /// <remarks>
        ///     This setting is ignored for batches (<see cref="BatchSequence" />), use the
        ///     <see cref="BatchSettings.MaxWaitTime" /> instead.
        /// </remarks>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(30);

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public void Validate()
        {
            if (Timeout <= TimeSpan.Zero)
                throw new EndpointConfigurationException("Sequence.Timeout must be greater than 0.");

            if (Timeout.TotalMilliseconds > int.MaxValue)
            {
                throw new EndpointConfigurationException(
                    "Sequence.Timeout.TotalMilliseconds must be lower or equal to Int32.MaxValue.");
            }
        }
    }
}
