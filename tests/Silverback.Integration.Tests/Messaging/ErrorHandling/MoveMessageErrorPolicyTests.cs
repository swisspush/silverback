﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class MoveMessageErrorPolicyTests
    {
        private readonly IErrorPolicyBuilder _errorPolicyBuilder;

        private readonly IBroker _broker;

        public MoveMessageErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services.AddNullLogger();

            services.AddSilverback().WithConnectionToMessageBroker(
                options => options
                    .AddBroker<TestBroker>());

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);

            _broker = serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();
        }

        [Fact]
        public void HandleError_InboundMessage_MessageMoved()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault());
            var envelope = new InboundEnvelope(
                new byte[1],
                null,
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name)
            {
                Message = "hey oh!"
            };

            policy.HandleError(
                new[]
                {
                    envelope
                },
                new InvalidOperationException("test"));
            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.ProducedMessages.Count.Should().Be(1);
        }

        [Fact]
        public void HandleError_InboundMessage_MessagePreserved()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault());

            var message = new TestEventOne { Content = "hey oh!" };
            var headers = new MessageHeaderCollection();
            var rawContent = TestConsumerEndpoint.GetDefault().Serializer
                .Serialize(message, headers, MessageSerializationContext.Empty);
            var envelope = new InboundEnvelope(
                rawContent,
                headers,
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name)
            {
                Message = message,
                Headers =
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };
            policy.HandleError(new[] { envelope }, new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            var producedMessage = producer.ProducedMessages.Last();
            var (deserializedMessage, _) =
                producedMessage.Endpoint.Serializer.Deserialize(
                    producedMessage.Message,
                    producedMessage.Headers,
                    MessageSerializationContext.Empty);
            deserializedMessage.Should().BeEquivalentTo(envelope.Message);
        }

        [Fact]
        public void HandleError_NotDeserializedInboundMessage_MessagePreserved()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault());

            var envelope = new InboundEnvelope(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name)
            {
                Message = null,
                Headers =
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };

            policy.HandleError(new[] { envelope }, new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());
            var producedMessage = producer.ProducedMessages.Last();

            producedMessage.Message.Should().Equal(producedMessage.Message);
        }

        [Fact]
        public void HandleError_InboundMessage_HeadersPreserved()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault());

            var envelope = new InboundEnvelope(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name)
            {
                Message = "hey oh!",
                Headers =
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };
            policy.HandleError(new[] { envelope }, new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.ProducedMessages.Last().Headers.Should().Contain(envelope.Headers);
        }

        [Fact]
        public void Transform_InboundMessage_MessageTranslated()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault())
                .Transform((envelope, ex) => { envelope.Message = new TestEventTwo(); });

            byte[] rawMessage = Encoding.UTF8.GetBytes("hey oh!");
            var headers = new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(string).AssemblyQualifiedName)
            };
            var rawInboundEnvelopes = new[]
            {
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    TestConsumerEndpoint.GetDefault().Name),
            };

            policy.HandleError(rawInboundEnvelopes, new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());
            var (producedMessage, _) = producer.Endpoint.Serializer.Deserialize(
                producer.ProducedMessages[0].Message,
                producer.ProducedMessages[0].Headers,
                MessageSerializationContext.Empty);
            producedMessage.Should().BeOfType<TestEventTwo>();
        }

        [Fact]
        public void Transform_InboundMessage_HeadersProperlyModified()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault())
                .Transform(
                    (outboundEnvelope, ex) =>
                    {
                        outboundEnvelope.Headers.Add("error", ex.GetType().Name);
                    });

            var envelope = new InboundEnvelope(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null,
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);
            envelope.Headers.Add("key", "value");
            policy.HandleError(new[] { envelope }, new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());
            var newHeaders = producer.ProducedMessages[0].Headers;
            newHeaders.Count.Should().Be(6); // message-id, message-type, key, traceid, error, source-endpoint
        }

        [Fact]
        public void HandleError_InboundMessage_SourceEndpointHeaderIsSet()
        {
            var policy = _errorPolicyBuilder.Move(TestProducerEndpoint.GetDefault());

            var message = new InboundEnvelope(
                Encoding.UTF8.GetBytes("hey oh!"),
                null,
                null,
                new TestConsumerEndpoint("source-endpoint"),
                "source-endpoint")
            {
                Message = "hey oh!",
                Headers =
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
            };
            policy.HandleError(new[] { message }, new InvalidOperationException("test"));

            var producer = (TestProducer)_broker.GetProducer(TestProducerEndpoint.GetDefault());

            producer.ProducedMessages.Last()
                .Headers
                .Should().ContainEquivalentOf(
                    new MessageHeader(
                        DefaultMessageHeaders.SourceEndpoint,
                        "source-endpoint"));
        }
    }
}
