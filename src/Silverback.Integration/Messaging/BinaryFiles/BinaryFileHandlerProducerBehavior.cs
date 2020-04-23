// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Switches to the <see cref="BinaryFileMessageSerializer" /> if the message being produced implements
    ///     the <see cref="IBinaryFileMessage" /> interface.
    /// </summary>
    public class BinaryFileHandlerProducerBehavior : IProducerBehavior, ISorted
    {
        private readonly BinaryFileMessageSerializer _binaryFileMessageSerializer = new BinaryFileMessageSerializer();

        /// <inheritdoc />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.BinaryFileHandler;

        /// <inheritdoc />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (context.Envelope.Message is IBinaryFileMessage &&
                !(context.Envelope.Endpoint.Serializer is BinaryFileMessageSerializer))
            {
                context.Envelope.RawMessage = await _binaryFileMessageSerializer.SerializeAsync(
                    context.Envelope.Message,
                    context.Envelope.Headers,
                    MessageSerializationContext.Empty);
            }

            await next(context);
        }
    }
}
