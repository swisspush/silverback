﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.InMemory.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.InMemory.Messaging.Broker
{
    public class InMemoryBrokerTests
    {
        private static readonly MessagesReceivedAsyncCallback VoidCallback = args => Task.CompletedTask;

        private readonly IServiceProvider _serviceProvider;

        public InMemoryBrokerTests()
        {
            var services = new ServiceCollection();

            services.AddNullLogger();

            services.AddSilverback().WithConnectionToMessageBroker(
                options => options
                    .AddInMemoryBroker());

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        [Fact]
        public void GetProducer_ReturnsNewInMemoryProducer()
        {
            var endpoint = new KafkaProducerEndpoint("test");

            var producer = _serviceProvider.GetRequiredService<IBroker>().GetProducer(endpoint);

            producer.Should().NotBeNull();
            producer.Should().BeOfType<InMemoryProducer>();
        }

        [Fact]
        public void AddConsumer_ReturnsNewInMemoryConsumer()
        {
            var endpoint = new KafkaConsumerEndpoint("test");

            var consumer = _serviceProvider.GetRequiredService<IBroker>().AddConsumer(endpoint, VoidCallback);

            consumer.Should().NotBeNull();
            consumer.Should().BeOfType<InMemoryConsumer>();
        }

        [Fact]
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public void InMemoryBroker_ProduceMessage_MessageConsumed()
        {
            var endpointName = "test";
            var receivedMessages = new List<object>();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            broker.AddConsumer(
                new KafkaConsumerEndpoint(endpointName),
                args =>
                    args.Envelopes.ForEach(
                        envelope =>
                        {
                            var (deserialized, _) = envelope.Endpoint.Serializer.Deserialize(
                                envelope.RawMessage,
                                new MessageHeaderCollection(envelope.Headers),
                                MessageSerializationContext.Empty);

                            if (deserialized != null)
                                receivedMessages.Add(deserialized);
                        }));

            producer.Produce(new TestMessage { Content = "hello!" });
            producer.Produce(new TestMessage { Content = "hello 2!" });

            receivedMessages.Count.Should().Be(2);
            receivedMessages.Should().AllBeOfType<TestMessage>();
        }

        [Fact]
        public void InMemoryBroker_ProduceMessage_MessageReceived()
        {
            var endpointName = "test";
            var receivedMessages = new List<object>();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            broker.AddConsumer(
                new KafkaConsumerEndpoint(endpointName),
                args =>
                    args.Envelopes.ForEach(
                        envelope =>
                        {
                            var (deserialized, _) = envelope.Endpoint.Serializer.Deserialize(
                                envelope.RawMessage,
                                new MessageHeaderCollection(envelope.Headers),
                                MessageSerializationContext.Empty);

                            if (deserialized != null)
                                receivedMessages.Add(deserialized);
                        }));

            producer.Produce(new TestMessage { Content = "hello!" });

            receivedMessages.First().Should().BeOfType<TestMessage>();
            receivedMessages.OfType<TestMessage>().First().Content.Should().Be("hello!");
        }

        [Fact]
        public void InMemoryBroker_ProduceMessage_MessageHeadersReceived()
        {
            var endpointName = "test";
            var receivedHeaders = new List<IEnumerable<MessageHeader>>();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            broker.AddConsumer(
                new KafkaConsumerEndpoint(endpointName),
                args =>
                    args.Envelopes.ForEach(envelope => receivedHeaders.Add(envelope.Headers)));

            producer.Produce(
                new TestMessage { Content = "hello!" },
                new[] { new MessageHeader("a", "b"), new MessageHeader("c", "d") });

            receivedHeaders.First().Should().ContainEquivalentOf(new MessageHeader("a", "b"));
            receivedHeaders.First().Should().ContainEquivalentOf(new MessageHeader("c", "d"));
        }

        [Fact]
        public void InMemoryBroker_PublishMessageThroughConnector_MessageConsumed()
        {
            var endpointName = "test";
            var receivedEnvelopes = new List<object>();

            _serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe((IInboundEnvelope<TestMessage> envelope) => receivedEnvelopes.Add(envelope))
                .Connect(
                    endpoints => endpoints
                        .AddInbound(new KafkaConsumerEndpoint(endpointName))
                        .AddOutbound<TestMessage>(new KafkaProducerEndpoint(endpointName)));

            using (var scope = _serviceProvider.CreateScope())
            {
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                publisher.Publish(new TestMessage { Content = "hello!" });
                publisher.Publish(new TestMessage { Content = "hello 2!" });
            }

            receivedEnvelopes.Count.Should().Be(2);
            receivedEnvelopes.OfType<IInboundEnvelope>().Select(x => x.Message).Should().AllBeOfType<TestMessage>();
        }

        [Fact]
        public void InMemoryBroker_ConnectAndDispose_NoExceptionIsThrown()
        {
            var broker = (IDisposable)_serviceProvider.GetRequiredService<IBusConfigurator>().Connect().First();

            broker.Dispose();
        }
    }
}
