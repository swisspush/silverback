﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class ServiceCollectionAddSilverbackExtensionsTests
    {
        [Fact]
        public void AddSilverback_PublisherIsRegisteredAndWorking()
        {
            var servicesProvider = GetServiceProvider(
                services => services
                    .AddSilverback());

            var publisher = servicesProvider.GetRequiredService<IPublisher>();

            publisher.Should().NotBeNull();

            Action act = () => publisher.Publish(new TestEventOne());

            act.Should().NotThrow();
        }

        private static IServiceProvider GetServiceProvider(Action<IServiceCollection> configAction)
        {
            var services = new ServiceCollection()
                .AddNullLogger();

            configAction(services);

            return services.BuildServiceProvider();
        }
    }
}
