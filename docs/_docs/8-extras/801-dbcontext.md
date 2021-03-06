---
title: Sample DbContext (EF Core)
permalink: /docs/extra/dbcontext
toc: true
---

## Default Tables

Some features rely on data being stored in a persistent storage such as a database. This chapter highlights the `DbSet`'s that have to be added to your `DbContext` when using Silverback in combination with EF Core (via the `Silverback.Core.EntityFrameworkCore`).

Here a breakdown of the use cases that require a `DbSet`:
* Using an outbox table (see [Outbound Connector]({{ site.baseurl }}/docs/configuration/outbound)) will require a `DbSet<OutboundMessage>` and possibly a `DbSet<Lock>`, to enable horizontal scaling.
* Either a `DbSet<StoredOffset>` or a `DbSet<InboundMessage>` is necessary to ensure exactly-once processing (see [Inbound Connector]({{ site.baseurl }}/docs/configuration/inbound)).
* When consuming chunked messages (see [Chunking]({{ site.baseurl }}/docs/advanced/chunking)), you may want to temporary store the received chunks into a database table, until all chunks are received and the full message can be rebuilt and processed and you therefore need a `DbSet<TemporaryMessageChunk>` to be configured.

This is what a `DbContext` built to support all the aforementioned features will look like.

```csharp
using Microsoft.EntityFrameworkCore;
using Silverback.Database.Model;
using Silverback.EntityFrameworkCore;

namespace Sample
{
   public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<OutboundMessage> OutboundMessages { get; set; }
        public DbSet<InboundMessage> InboundMessages { get; set; }
        public DbSet<StoredOffset> StoredOffsets { get; set; }
        public DbSet<TemporaryMessageChunk> Chunks { get; set; }
        public DbSet<Lock> Locks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboundMessage>()
                .ToTable("Silverback_OutboundMessages");

            modelBuilder.Entity<InboundMessage>()
                .ToTable("Silverback_InboundMessages")
                .HasKey(t => new { t.MessageId, t.ConsumerGroupName });

            modelBuilder.Entity<StoredOffset>()
                .ToTable("Silverback_StoredOffsets");

            modelBuilder.Entity<TemporaryMessageChunk>()
                .ToTable("Silverback_MessageChunks")
                .HasKey(t => new { t.OriginalMessageId, t.ChunkId });
        }
    }
}
```

`InboundMessage` and `TemporaryMessageChunk` declare a composite primary key via annotation, thing that isn't supported yet by EF Core. It is therefore mandatory to explictly redeclare their primary key via the `HasKey` fluent API.
{: .notice--warning}

## DDD and Transactional Messages

Some additional changes are required in order for the events generated by the domain entities to be fired as part of the `SaveChanges` transaction. More details on this topic can be found in the [DDD and Domain Events]({{ site.baseurl }}/docs/quickstart/ddd) chapter of the quickstart.

```csharp
using Microsoft.EntityFrameworkCore;
using Silverback.EntityFrameworkCore;
using Silverback.Messaging.Publishing;

namespace Sample
{
   public class SampleDbContext : DbContext
    {
        private readonly DbContextEventsPublisher _eventsPublisher;

        public SampleDbContext(IPublisher publisher)
        {
            _eventsPublisher = new DbContextEventsPublisher(publisher, this);
        }

        public SampleDbContext(DbContextOptions options, IPublisher publisher)
            : base(options)
        {
            _eventsPublisher = new DbContextEventsPublisher(publisher, this);
        }

        // ...DbSet properties and OnModelCreating...

        public override int SaveChanges()
            => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => _eventsPublisher.ExecuteSaveTransaction(() => base.SaveChanges(acceptAllChangesOnSuccess));

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
            => _eventsPublisher.ExecuteSaveTransactionAsync(() =>
                base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));
    }
}
```