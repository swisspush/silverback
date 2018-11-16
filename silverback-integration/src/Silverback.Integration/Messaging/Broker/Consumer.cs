﻿using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly ILogger<Consumer> _logger;

        public event EventHandler<IEnvelope> Received;

        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger)
           : base(broker, endpoint)
        {
            _logger = logger;
        }

        protected void HandleMessage(byte[] buffer)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is attached to the Received event.");

            var envelope = Serializer.Deserialize(buffer);

            _logger.LogTrace($"Received message '{envelope.Message.Id}' on endpoint '{Endpoint.Name}' (source: '{envelope.Source}').");

            Received(this, envelope);
        }
    }
}