// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration
{
    public class SilverbackBuilderExtensionsTests
    {
        [Fact]
        public void WithConnectionTo_GenericAndSpecificVersions_BehavesTheSame()
        {
            var servicesGeneric = new ServiceCollection();
            var servicesSpecific = new ServiceCollection();

            new SilverbackBuilder(servicesGeneric).WithConnectionTo<KafkaBroker>();
            new SilverbackBuilder(servicesSpecific).WithConnectionToKafka();

            servicesGeneric.Count.Should().Be(servicesSpecific.Count);
        }
    }
}