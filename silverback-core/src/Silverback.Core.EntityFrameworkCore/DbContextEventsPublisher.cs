﻿// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Domain;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.EntityFrameworkCore
{
    /// <summary>
    /// Exposes some extension methods for the <see cref="DbContext"/> that handle domain events as part 
    /// of the SaveChanges transaction.
    /// </summary>
    public static class DbContextEventsPublisher
    {
        /// <summary>
        /// Publishes the domain events generated by the tracked entities and then executes the provided save changes procedure.
        /// </summary>
        public static int ExecuteSaveTransaction(DbContext dbContext, Func<int> saveChanges, IEventPublisher eventPublisher) =>
            ExecuteSaveTransaction(dbContext, () => Task.FromResult(saveChanges()), eventPublisher, false).Result;

        /// <summary>
        /// Publishes the domain events generated by the tracked entities and then executes the provided save changes procedure.
        /// </summary>
        public static Task<int> ExecuteSaveTransactionAsync(DbContext dbContext, Func<Task<int>> saveChangesAsync, IEventPublisher eventPublisher) =>
            ExecuteSaveTransaction(dbContext, saveChangesAsync, eventPublisher, true);

        private static async Task<int> ExecuteSaveTransaction(DbContext dbContext, Func<Task<int>> saveChanges, IEventPublisher eventPublisher,  bool async)
        {
            await PublishDomainEvents(dbContext, eventPublisher, async);

            var saved = false;
            try
            {
                var result = await saveChanges();

                saved = true;

                await PublishEvent<TransactionCommitEvent>(eventPublisher, async);

                return result;
            }
            catch (Exception)
            {
                if (!saved)
                    await PublishEvent<TransactionRollbackEvent>(eventPublisher, async);

                throw;
            }
        }

        private static async Task PublishDomainEvents(DbContext dbContext, IEventPublisher eventPublisher, bool async)
        {
            var events = GetDomainEvents(dbContext);

            // Keep publishing events fired inside the event handlers
            while (events.Any())
            {
                if (async)
                    await events.ForEachAsync(eventPublisher.PublishAsync);
                else
                    events.ForEach(eventPublisher.Publish);

                events = GetDomainEvents(dbContext);
            }
        }

        private static List<IDomainEvent<IDomainEntity>> GetDomainEvents(DbContext dbContext)
        {
            var events = dbContext.ChangeTracker.Entries<IDomainEntity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            // Clear all events to avoid firing the same event multiple times during the recursion
            events.ForEach(e => e.Source.ClearEvents());

            return events;
        }

        private static Task<int> SaveChanges(DbContext dbContext, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken, bool async) =>
            async
            ? dbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
            : Task.FromResult(dbContext.SaveChanges(acceptAllChangesOnSuccess));

        private static Task PublishEvent<TEvent>(IEventPublisher eventPublisher, bool async)
            where TEvent : IEvent, new()
        {
            if (async)
                return eventPublisher.PublishAsync(new TEvent());

            eventPublisher.Publish(new TEvent());

            return Task.CompletedTask;
        }
    }
}