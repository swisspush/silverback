﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     The base class for the InboundConnector checking that each message is processed only once.
    /// </summary>
    public abstract class ExactlyOnceInboundConnector : InboundConnector
    {
        private readonly ILogger _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExactlyOnceInboundConnector" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        /// <param name="serviceProvider"> The <see cref="IServiceProvider" />. </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        protected ExactlyOnceInboundConnector(
            IBrokerCollection brokerCollection,
            IServiceProvider serviceProvider,
            ILogger<ExactlyOnceInboundConnector> logger)
            : base(brokerCollection, serviceProvider, logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task RelayMessages(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider)
        {
            envelopes = (await EnsureExactlyOnce(envelopes, serviceProvider)).ToList();

            await base.RelayMessages(envelopes, serviceProvider);
        }

        /// <summary>
        ///     Checks whether the message contained in the specified envelope must be processed. It ensures
        ///     that each message is processed exactly once.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IRawInboundEnvelope" /> containing the message to be processed.
        /// </param>
        /// <param name="serviceProvider"> The <see cref="IServiceProvider" />. </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the result of the asynchronous operation. The task
        ///     result contains a value indicating whether the message must be processed.
        /// </returns>
        protected abstract Task<bool> MustProcess(IRawInboundEnvelope envelope, IServiceProvider serviceProvider);

        private async Task<IEnumerable<IRawInboundEnvelope>> EnsureExactlyOnce(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider) =>
            await envelopes.WhereAsync(
                async envelope =>
                {
                    if (await MustProcess(envelope, serviceProvider))
                        return true;

                    _logger.LogDebug(
                        EventIds.ExactlyOnceInboundConnectorMessageAlreadyProcessed,
                        "Message is being skipped since it was already processed.",
                        envelope);
                    return false;
                });
    }
}
