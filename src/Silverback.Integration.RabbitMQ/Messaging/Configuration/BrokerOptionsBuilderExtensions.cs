// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary> Adds the <c> AddRabbit </c> method to the <see cref="IBrokerOptionsBuilder" />. </summary>
    public static class BrokerOptionsBuilderExtensions
    {
        /// <summary> Registers RabbitMQ as message broker. </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddRabbit(this IBrokerOptionsBuilder brokerOptionsBuilder) =>
            brokerOptionsBuilder.AddBroker<RabbitBroker>();
    }
}