// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using RabbitMQ.Client;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The factory that creates and stores the connections to Rabbit in order to create a single connection
    ///     per each <see cref="RabbitConnectionConfig" />.
    /// </summary>
    public interface IRabbitConnectionFactory : IDisposable
    {
        /// <summary> Returns a channel to produce to the specified endpoint. </summary>
        /// <param name="endpoint"> The endpoint to be produced to. </param>
        /// <returns> The <see cref="IModel" /> representing the channel. </returns>
        IModel GetChannel(RabbitProducerEndpoint endpoint);

        /// <summary> Returns a channel to consume from the specified endpoint. </summary>
        /// <param name="endpoint"> The endpoint to be consumed from. </param>
        /// <returns> The <see cref="IModel" /> representing the channel. </returns>
        (IModel channel, string queueName) GetChannel(RabbitConsumerEndpoint endpoint);
    }
}
