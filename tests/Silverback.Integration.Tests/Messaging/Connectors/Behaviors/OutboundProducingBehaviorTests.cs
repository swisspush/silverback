﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Behaviors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Behaviors
{
    public class OutboundProducingBehaviorTests
    {
        private readonly OutboundProducerBehavior _behavior;

        private readonly InMemoryOutboundQueue _outboundQueue;

        private readonly TestBroker _broker;

        public OutboundProducingBehaviorTests()
        {
            var services = new ServiceCollection();

            _outboundQueue = new InMemoryOutboundQueue(new TransactionalListSharedItems<QueuedMessage>());

            services.AddSingleton<IOutboundQueueWriter>(_outboundQueue);

            services.AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddOutboundConnector()
                        .AddDeferredOutboundConnector());

            services.AddNullLogger();

            var serviceProvider = services.BuildServiceProvider();

            _behavior = (OutboundProducerBehavior)serviceProvider.GetServices<IBehavior>()
                .First(behavior => behavior is OutboundProducerBehavior);
            _broker = serviceProvider.GetRequiredService<TestBroker>();
        }

        [Fact]
        public async Task Handle_OutboundMessage_CorrectlyRelayed()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                Array.Empty<MessageHeader>(),
                TestProducerEndpoint.GetDefault(),
                typeof(OutboundConnector));

            await _behavior.Handle(new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope }, Task.FromResult!);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(10);
            queued.Count.Should().Be(0);
            _broker.ProducedMessages.Count.Should().Be(3);
        }

        [Fact]
        public async Task Handle_OutboundMessage_RelayedViaTheRightConnector()
        {
            var outboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne(),
                Array.Empty<MessageHeader>(),
                TestProducerEndpoint.GetDefault(),
                typeof(DeferredOutboundConnector));

            await _behavior.Handle(new[] { outboundEnvelope, outboundEnvelope, outboundEnvelope }, Task.FromResult!);
            await _outboundQueue.Commit();

            var queued = await _outboundQueue.Dequeue(10);
            queued.Count.Should().Be(3);
            _broker.ProducedMessages.Count.Should().Be(0);
        }
    }
}
