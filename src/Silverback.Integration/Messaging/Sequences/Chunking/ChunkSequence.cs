﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    /// <summary>
    ///     Represents a sequence of chunks that belong to the same message.
    /// </summary>
    public class ChunkSequence : Sequence<IRawInboundEnvelope>
    {
        private int? _lastIndex;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkSequence" /> class.
        /// </summary>
        /// <param name="sequenceId">
        ///     The identifier that is used to match the consumed messages with their belonging sequence.
        /// </param>
        /// <param name="totalLength">
        ///     The expected total length of the sequence.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
        ///     sequence gets published to the internal bus.
        /// </param>
        public ChunkSequence(string sequenceId, int? totalLength, ConsumerPipelineContext context)
            : base(sequenceId, context)
        {
            TotalLength = totalLength;
        }

        /// <inheritdoc cref="Sequence{TEnvelope}.AddCoreAsync" />
        protected override Task AddCoreAsync(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var chunkIndex = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                             throw new InvalidOperationException("Chunk index header not found.");

            if (!EnsureOrdering(chunkIndex))
                return Task.CompletedTask;

            return base.AddCoreAsync(envelope);
        }

        /// <inheritdoc cref="Sequence{TEnvelope}.IsLastMessage" />
        protected override bool IsLastMessage(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return envelope.Headers.GetValue<bool>(DefaultMessageHeaders.IsLastChunk) == true;
        }

        private bool EnsureOrdering(int index)
        {
            if (_lastIndex == null && index != 0)
            {
                throw new InvalidOperationException(
                    $"Sequence error. Received chunk with index {index} as first chunk for the sequence {SequenceId}, expected index 0. "); // TODO: Use custom exception type?
            }

            if (_lastIndex != null && index == _lastIndex)
                return false;

            if (_lastIndex != null && index != _lastIndex + 1)
            {
                throw new InvalidOperationException(
                    $"Sequence error. Received chunk with index {index} after index {_lastIndex}."); // TODO: Use custom exception type?
            }

            _lastIndex = index;

            return true;
        }
    }
}
