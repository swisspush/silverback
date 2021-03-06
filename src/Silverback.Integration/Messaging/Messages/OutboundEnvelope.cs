﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class OutboundEnvelope : RawOutboundEnvelope, IOutboundEnvelopeInternal
    {
        public OutboundEnvelope(
            object? message,
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            Type? outboundConnectorType = null,
            bool autoUnwrap = false,
            IOffset? offset = null)
            : base(headers, endpoint, offset)
        {
            Message = message;
            OutboundConnectorType = outboundConnectorType;
            AutoUnwrap = autoUnwrap;
        }

        public object? Message { get; set; }

        public bool AutoUnwrap { get; }

        public Type? OutboundConnectorType { get; }
    }
}