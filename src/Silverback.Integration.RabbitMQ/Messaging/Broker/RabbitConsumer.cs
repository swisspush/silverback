﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer{TBroker,TEndpoint,TOffset}" />
    public class RabbitConsumer : Consumer<RabbitBroker, RabbitConsumerEndpoint, RabbitOffset>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly ILogger<RabbitConsumer> _logger;

        private readonly object _pendingOffsetLock = new object();

        private IModel _channel;

        private string _queueName;

        private AsyncEventingBasicConsumer _consumer;

        private string _consumerTag;

        private bool _disconnecting;

        private int _pendingOffsetsCount;

        private RabbitOffset? _pendingOffset;

        public RabbitConsumer(
            RabbitBroker broker,
            RabbitConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            IServiceProvider serviceProvider,
            ILogger<RabbitConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)

        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }


        /// <inheritdoc />
        public override void Connect()
        {
            if (_consumer != null)
                return;

            (_channel, _queueName) = _connectionFactory.GetChannel(Endpoint);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += TryHandleMessage;

            _consumerTag = _channel.BasicConsume(
                _queueName,
                false,
                _consumer);
        }

        /// <inheritdoc />
        public override void Disconnect()
        {
            if (_consumer == null)
                return;

            _disconnecting = true;

            CommitPendingOffset();

            _channel.BasicCancel(_consumerTag);
            _channel?.Dispose();
            _channel = null;
            _queueName = null;
            _consumerTag = null;
            _consumer = null;

            _disconnecting = false;
        }

        /// <inheritdoc />
        protected override Task Commit(IReadOnlyCollection<RabbitOffset> offsets)
        {
            CommitOrStoreOffset(offsets.OrderBy(offset => offset.DeliveryTag).Last());
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task Rollback(IReadOnlyCollection<RabbitOffset> offsets)
        {
            BasicNack(offsets.Max(offset => offset.DeliveryTag));
            return Task.CompletedTask;
        }

        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task TryHandleMessage(object sender, BasicDeliverEventArgs deliverEventArgs)
        {
            RabbitOffset? offset = null;

            try
            {
                offset = new RabbitOffset(deliverEventArgs.ConsumerTag, deliverEventArgs.DeliveryTag);

                _logger.LogDebug(
                    "Consuming message {offset} from endpoint {endpointName}.",
                    offset.Value,
                    Endpoint.Name);

                if (_disconnecting)
                    return;

                await HandleMessage(
                    deliverEventArgs.Body.ToArray(),
                    deliverEventArgs.BasicProperties.Headers.ToSilverbackHeaders(),
                    Endpoint.Name,
                    offset);
            }
            catch (Exception ex)
            {
                const string errorMessage =
                    "Fatal error occurred consuming the message {offset} from endpoint {endpointName}. " +
                    "The consumer will be stopped.";
                _logger.LogCritical(ex, errorMessage, offset?.Value, Endpoint.Name);

                Disconnect();
            }
        }

        private void CommitOrStoreOffset(RabbitOffset offset)
        {
            lock (_pendingOffsetLock)
            {
                if (Endpoint.AcknowledgeEach == 1)
                {
                    BasicAck(offset.DeliveryTag);
                    return;
                }

                _pendingOffset = offset;
                _pendingOffsetsCount++;
            }

            if (Endpoint.AcknowledgeEach <= _pendingOffsetsCount)
                CommitPendingOffset();
        }

        private void CommitPendingOffset()
        {
            lock (_pendingOffsetLock)
            {
                if (_pendingOffset == null)
                    return;

                BasicAck(_pendingOffset.DeliveryTag);
                _pendingOffset = null;
                _pendingOffsetsCount = 0;
            }
        }

        private void BasicAck(ulong deliveryTag)
        {
            try
            {
                _channel.BasicAck(deliveryTag, true);

                _logger.LogDebug(
                    "Successfully committed (basic.ack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred committing (basic.ack) the delivery tag {deliveryTag}.",
                    deliveryTag);

                throw;
            }
        }

        private void BasicNack(ulong deliveryTag)
        {
            try
            {
                _channel.BasicNack(deliveryTag, true, true);

                _logger.LogDebug(
                    "Successfully rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred rolled back (basic.nack) the delivery tag {deliveryTag}.",
                    deliveryTag);

                throw;
            }
        }
    }
}
