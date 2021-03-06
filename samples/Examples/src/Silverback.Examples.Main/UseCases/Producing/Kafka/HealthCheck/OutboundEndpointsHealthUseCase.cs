﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.HealthChecks;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.HealthCheck
{
    public class OutboundEndpointsHealthUseCase : UseCase
    {
        public OutboundEndpointsHealthUseCase()
        {
            Title = "Check outbound endpoints";
            Description = "A ping message is sent to all configured outbound endpoints to ensure that they are " +
                          "all reachable.";
            ExecutionsCount = 1;
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionToMessageBroker(options => options
                    .AddKafka()
                    .AddDbOutboundConnector());
        }

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        MessageTimeoutMs = 1000
                    }
                })
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events-2")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        MessageTimeoutMs = 1000
                    }
                })
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-failure")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://somwhere:1000",
                        MessageTimeoutMs = 1000
                    }
                }));
        }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine("Pinging all endpoints...");
            ConsoleHelper.ResetColor();

            var result = await new OutboundEndpointsHealthCheckService(
                serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                serviceProvider.GetRequiredService<IBrokerCollection>()).PingAllEndpoints();

            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            ConsoleHelper.ResetColor();
        }
    }
}