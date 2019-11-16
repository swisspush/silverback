﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Connectors.Behaviors
{
    public class OutboundRoutingBehavior : ISortedBehavior
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOutboundRoutingConfiguration _routing;
        private readonly MessageKeyProvider _messageKeyProvider;

        public OutboundRoutingBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _routing = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            _messageKeyProvider = serviceProvider.GetRequiredService<MessageKeyProvider>();
        }

        public int SortIndex { get; } = 100;

        public async Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            var routedMessages = await WrapAndRepublishRoutedMessages(messages);

            if (!_routing.PublishOutboundMessagesToInternalBus)
                messages = messages.Where(m => !routedMessages.Contains(m)).ToList();

            return await next(messages);
        }

        private async Task<IEnumerable<object>> WrapAndRepublishRoutedMessages(IEnumerable<object> messages)
        {
            var wrappedMessages = messages
                .Where(message => !(message is IOutboundMessageInternal))
                .SelectMany(message => _routing.GetRoutesForMessage(message)
                    .Select(route => CreateOutboundMessage(message, route)));

            if (wrappedMessages.Any())
                await _serviceProvider
                    .GetRequiredService<IPublisher>()
                    .PublishAsync(wrappedMessages);

            return wrappedMessages.Select(m => m.Content);
        }
        private IOutboundMessage CreateOutboundMessage(object message, IOutboundRoute route)
        {
            var wrapper = (IOutboundMessage)Activator.CreateInstance(
                typeof(OutboundMessage<>).MakeGenericType(message.GetType()),
                message, null, route);

            _messageKeyProvider.EnsureKeyIsInitialized(wrapper);

            return wrapper;
        }
    }
}