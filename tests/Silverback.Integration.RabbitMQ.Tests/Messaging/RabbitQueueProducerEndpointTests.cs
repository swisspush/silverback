﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging
{
    public class RabbitQueueProducerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new RabbitQueueProducerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DeserializedEndpoint_TrueIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };

            var json = JsonConvert.SerializeObject(
                endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<RabbitQueueProducerEndpoint>(
                json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentName_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint");
            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint2");

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueConsumerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                }
            };
            var endpoint2 = new RabbitQueueConsumerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = true,
                    IsAutoDeleteEnabled = false,
                    IsExclusive = false
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameSerializerSettings_TrueIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSerializerSettings_FalseIsReturned()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                }
            };

            var endpoint2 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 8
                    }
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void SerializationAndDeserialization_NoInformationIsLost()
        {
            var endpoint1 = new RabbitQueueProducerEndpoint("endpoint")
            {
                Queue = new RabbitQueueConfig
                {
                    IsDurable = false,
                    IsAutoDeleteEnabled = true,
                    IsExclusive = true
                },
                Serializer = new JsonMessageSerializer
                {
                    Settings =
                    {
                        MaxDepth = 100
                    }
                },
                Chunk = new ChunkSettings
                {
                    Size = 3000
                }
            };

            var json = JsonConvert.SerializeObject(
                endpoint1,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var endpoint2 = JsonConvert.DeserializeObject<RabbitQueueProducerEndpoint>(
                json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            endpoint2.Should().BeEquivalentTo(endpoint1);
        }
    }
}
