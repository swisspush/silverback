﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Consumer.Behaviors
{
    public class LogHeadersBehavior : IBehavior
    {
        private readonly ILogger<LogHeadersBehavior> _logger;

        public LogHeadersBehavior(ILogger<LogHeadersBehavior> logger)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<object>> Handle(
            IReadOnlyCollection<object> messages,
            MessagesHandler next)
        {
            foreach (var message in messages.OfType<IInboundEnvelope>())
            {
                if (message.Headers != null && message.Headers.Any())
                {
                    _logger.LogInformation("Headers: {@headers}", message.Headers);
                }
            }

            return await next(messages);
        }
    }
}