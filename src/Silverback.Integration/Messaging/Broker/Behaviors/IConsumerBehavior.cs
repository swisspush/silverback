﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build a custom pipeline, plugging some functionality into the
    ///     <see cref="IConsumer" />.
    /// </summary>
    public interface IConsumerBehavior : IBrokerBehavior
    {
        /// <summary>
        ///     Process, handles or transforms the message being consumed.
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the behaviors pipeline.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services in the current
        ///     pipeline.
        /// </param>
        /// <param name="next"> The next behavior in the pipeline. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next);
    }
}
