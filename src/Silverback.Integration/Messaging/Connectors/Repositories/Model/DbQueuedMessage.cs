﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories.Model
{
    /// <summary>
    ///     Extends the <see cref="QueuedMessage" /> adding the specific information related to a message stored
    ///     in the outbox table.
    /// </summary>
    public class DbQueuedMessage : QueuedMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbQueuedMessage" /> class.
        /// </summary>
        /// <param name="id">
        ///     The primary key of the database record.
        /// </param>
        /// <param name="messageType">
        ///     The type of the message.
        /// </param>
        /// <param name="content">
        ///     The message raw binary content (body).
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpointName">
        ///     The name of the target endpoint.
        /// </param>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public DbQueuedMessage(
            int id,
            Type? messageType,
            byte[]? content,
            IEnumerable<MessageHeader> headers,
            string endpointName)
            : base(messageType, content, headers, endpointName)
        {
            Id = id;
        }

        /// <summary>
        ///     Gets the value of the primary key of the related database record.
        /// </summary>
        public int Id { get; }
    }
}
