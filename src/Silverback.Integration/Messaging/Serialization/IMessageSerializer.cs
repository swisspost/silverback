﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages sent through the broker.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        ///     Serializes the specified message object into a byte array.
        /// </summary>
        /// <param name="message">
        ///     The message object to be serialized.
        /// </param>
        /// <param name="messageHeaders">
        ///     The message headers collection.
        /// </param>
        /// <param name="context">
        ///     The context information.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="Stream" /> with the serialized message.
        /// </returns>
        ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);

        /// <summary>
        ///     Deserializes the byte array back into a message object.
        /// </summary>
        /// <param name="message">
        ///     The <see cref="Stream" /> containing the message to be deserialized.
        /// </param>
        /// <param name="messageHeaders">
        ///     The message headers collection.
        /// </param>
        /// <param name="context">
        ///     The context information.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the
        ///     deserialized message (or <c>null</c> when the input is null or empty) and the type of the message.
        /// </returns>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        ValueTask<(object?, Type)> DeserializeAsync(
            Stream? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);
    }
}
