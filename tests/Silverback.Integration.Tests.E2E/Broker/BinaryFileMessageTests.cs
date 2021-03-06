// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E")]
    public class BinaryFileMessageTests
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        private readonly ServiceProvider _serviceProvider;

        private readonly IBusConfigurator _configurator;

        private readonly SpyBrokerBehavior _spyBehavior;

        public BinaryFileMessageTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddInMemoryBroker()
                        .AddInMemoryChunkStore())
                .AddSingletonBrokerBehavior<SpyBrokerBehavior>();

            _serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _configurator = _serviceProvider.GetRequiredService<IBusConfigurator>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
        }

        [Fact]
        public async Task DefaultSettings_ProducedAndConsumed()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
                ContentType = "application/pdf"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            _spyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
        }

        [Fact]
        public async Task InheritedBinaryFileMessage_ProducedAndConsumed()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
                ContentType = "application/pdf",
                CustomHeader = "hello!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            _spyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
            _spyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header", "hello!"));
        }

        [Fact]
        public async Task ForcedDefaultBinaryFileDeserializer_ProducedAndConsumed()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
                ContentType = "application/pdf"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Serializer = BinaryFileMessageSerializer.Default
                        }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            _spyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
        }

        [Fact]
        public async Task ForcedDefaultBinaryFileSerializerAndDeserializer_ProducedAndConsumed()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
                ContentType = "application/pdf"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IBinaryFileMessage>(
                        new KafkaProducerEndpoint("test-e2e")
                        {
                            Serializer = BinaryFileMessageSerializer.Default
                        })
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Serializer = BinaryFileMessageSerializer.Default
                        }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            _spyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
        }

        [Fact]
        public async Task BinaryFileMessageWithoutHeaders_ProducedAndConsumed()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            _configurator.Connect(
                endpoints => endpoints
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Serializer = BinaryFileMessageSerializer.Default
                        }));

            using var scope = _serviceProvider.CreateScope();
            var broker = scope.ServiceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawContent);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .BeEquivalentTo(rawContent);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeAssignableTo<IBinaryFileMessage>();
            _spyBehavior.InboundEnvelopes[0].Message.As<IBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(rawContent);
        }

        [Fact]
        public async Task BinaryFileMessageWithoutHeadersAndForcedDeserializer_ProducedAndConsumed()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            _configurator.Connect(
                endpoints => endpoints
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Serializer = BinaryFileMessageSerializer.Default
                        }));

            using var scope = _serviceProvider.CreateScope();
            var broker = scope.ServiceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawContent);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .BeEquivalentTo(rawContent);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeAssignableTo<IBinaryFileMessage>();
            _spyBehavior.InboundEnvelopes[0].Message.As<IBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(rawContent);
        }

        [Fact]
        public async Task InheritedBinaryWithoutTypeHeader_ProducedAndConsumed()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new[]
            {
                new MessageHeader("x-custom-header", "hello!")
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
                        }));

            using var scope = _serviceProvider.CreateScope();
            var broker = scope.ServiceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawContent, headers);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .BeEquivalentTo(rawContent);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<InheritedBinaryFileMessage>();
            _spyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(rawContent);
            _spyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().CustomHeader.Should()
                .Be("hello!");
        }

        [Fact]
        public async Task EncryptionAndChunking_EncryptedAndChunkedThenAggregatedAndDecrypted()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[]
                {
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                    0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                    0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                },
                ContentType = "application/pdf"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IBinaryFileMessage>(
                        new KafkaProducerEndpoint("test-e2e")
                        {
                            Chunk = new ChunkSettings
                            {
                                Size = 10
                            },
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        })
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(5);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .NotBeEquivalentTo(message.Content);
            _spyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(30);
                });
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task EncryptionAndChunkingOfInheritedBinary_EncryptedAndChunkedThenAggregatedAndDecrypted()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new byte[]
                {
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                    0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                    0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                },
                ContentType = "application/pdf",
                CustomHeader = "hello!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IBinaryFileMessage>(
                        new KafkaProducerEndpoint("test-e2e")
                        {
                            Chunk = new ChunkSettings
                            {
                                Size = 10
                            },
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        })
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")
                        {
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(5);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .NotBeEquivalentTo(message.Content);
            _spyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(30);
                });
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<InheritedBinaryFileMessage>();
            _spyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(message.Content);
            _spyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().CustomHeader.Should()
                .Be("hello!");
        }
    }
}
