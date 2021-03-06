﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc />
    public interface IInboundEnvelope<out TMessage> : IInboundEnvelope
        where TMessage : class
    {
        /// <summary>
        ///     Gets the deserialized message body.
        /// </summary>
        new TMessage? Message { get; }
    }
}
