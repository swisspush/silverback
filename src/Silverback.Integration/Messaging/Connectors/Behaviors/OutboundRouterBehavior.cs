﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Behaviors
{
    /// <summary>
    ///     Routes the messages to the outbound endpoint by wrapping them in an
    ///     <see cref="IOutboundEnvelope{TMessage}" /> that is republished to the bus.
    /// </summary>
    public class OutboundRouterBehavior : IBehavior, ISorted
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly IOutboundRoutingConfiguration _routing;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundRouterBehavior" /> class.
        /// </summary>
        /// <param name="serviceProvider"> The <see cref="IServiceProvider" />. </param>
        public OutboundRouterBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _routing = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
        }

        /// <inheritdoc />
        public int SortIndex { get; } = IntegrationBehaviorsSortIndexes.OutboundRouter;

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<object?>> Handle(
            IReadOnlyCollection<object> messages,
            MessagesHandler next)
        {
            Check.NotNull(next, nameof(next));

            var routedMessages = await WrapAndRepublishRoutedMessages(messages);

            // The routed messages are discarded because they have been republished
            // as OutboundEnvelope and they will be normally subscribable
            // (if PublishOutboundMessagesToInternalBus is true).
            messages = messages.Where(m => !routedMessages.Contains(m)).ToList();

            return await next(messages);
        }

        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private async Task<IReadOnlyCollection<object>> WrapAndRepublishRoutedMessages(IEnumerable<object> messages)
        {
            var envelopesToRepublish = messages
                .Where(message => !(message is IOutboundEnvelope))
                .SelectMany(
                    message =>
                        _routing
                            .GetRoutesForMessage(message)
                            .SelectMany(
                                route =>
                                    CreateOutboundEnvelope(message, route)))
                .ToList();

            if (envelopesToRepublish.Any())
            {
                await _serviceProvider
                    .GetRequiredService<IPublisher>()
                    .PublishAsync(envelopesToRepublish);
            }

            return envelopesToRepublish.Select(m => m.Message).ToList()!;
        }

        private IEnumerable<IOutboundEnvelope> CreateOutboundEnvelope(object message, IOutboundRoute route)
        {
            var headers = new MessageHeaderCollection();
            var endpoints = route.Router.GetDestinationEndpoints(message, headers);

            foreach (var endpoint in endpoints)
            {
                yield return (IOutboundEnvelope)Activator.CreateInstance(
                    typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                    message,
                    headers,
                    endpoint,
                    route.OutboundConnectorType,
                    _routing.PublishOutboundMessagesToInternalBus);
            }
        }
    }
}
