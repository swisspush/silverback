// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class SpyBrokerBehavior : IProducerBehavior, IConsumerBehavior, ISorted
    {
        private readonly List<IOutboundEnvelope> _outboundEnvelopes = new List<IOutboundEnvelope>();

        private readonly List<IInboundEnvelope> _inboundEnvelopes = new List<IInboundEnvelope>();

        public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();

        public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public int SortIndex { get; } = int.MaxValue;

        public Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            _outboundEnvelopes.Add(context.Envelope);

            return next(context);
        }

        public Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            context.Envelopes.ForEach(envelope => _inboundEnvelopes.Add((IInboundEnvelope)envelope));

            return next(context, serviceProvider);
        }
    }
}
