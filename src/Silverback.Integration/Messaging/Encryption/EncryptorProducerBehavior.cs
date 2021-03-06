// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     Encrypts the message according to the <see cref="EncryptionSettings" />.
    /// </summary>
    public class EncryptorProducerBehavior : IProducerBehavior, ISorted
    {
        private readonly IMessageTransformerFactory _factory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EncryptorProducerBehavior" /> class.
        /// </summary>
        /// <param name="factory">
        ///     The <see cref="IMessageTransformerFactory" />.
        /// </param>
        public EncryptorProducerBehavior(IMessageTransformerFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Encryptor;

        /// <inheritdoc />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Endpoint.Encryption != null)
            {
                context.Envelope.RawMessage = await _factory
                    .GetEncryptor(context.Envelope.Endpoint.Encryption)
                    .TransformAsync(context.Envelope.RawMessage, context.Envelope.Headers);
            }

            await next(context);
        }
    }
}
