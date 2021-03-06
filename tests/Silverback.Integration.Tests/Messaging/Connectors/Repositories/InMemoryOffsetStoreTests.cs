// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    public class InMemoryOffsetStoreTests
    {
        [Fact]
        public async Task Store_ForDifferentEndpoints_AllOffsetsStored()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint2") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint3") { GroupId = "group1" });
            await store.Commit();

            store.CommittedItemsCount.Should().Be(3);
        }

        [Fact]
        public async Task Store_ForDifferentConsumerGroups_AllOffsetsStored()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group2" });
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group3" });
            await store.Commit();

            store.CommittedItemsCount.Should().Be(3);
        }

        [Fact]
        public async Task Store_ForDifferentPartitions_AllOffsetsStored()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key2", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key3", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Commit();

            store.CommittedItemsCount.Should().Be(3);
        }

        [Fact]
        public async Task Store_SameTopicPartitionAndGroup_OffsetIsReplaced()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "2"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "3"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Commit();

            store.CommittedItemsCount.Should().Be(1);
        }

        [Fact]
        public async Task GetLatestValue_WithMultipleOffsetsStored_CorrectOffsetIsReturned()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "2"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "3"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key2", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint2") { GroupId = "group1" });
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group2" });
            await store.Commit();

            var result = await store.GetLatestValue(
                "key1",
                new TestConsumerEndpoint("endpoint1")
                {
                    GroupId = "group1"
                });

            result.Should().NotBeNull();
            result!.Value.Should().Be("3");
        }

        [Fact]
        public async Task GetLatestValue_NotStoredOffsets_NullIsReturned()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Commit();

            var result = await store.GetLatestValue(
                "key2",
                new TestConsumerEndpoint("endpoint1")
                {
                    GroupId = "group1"
                });

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLatestValue_CommittedOffsetsFromMultipleInstances_LastCommittedValueReturned()
        {
            var sharedList = new TransactionalDictionarySharedItems<string, IComparableOffset>();

            var store = new InMemoryOffsetStore(sharedList);
            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Commit();

            store = new InMemoryOffsetStore(sharedList);
            await store.Store(
                new TestOffset("key1", "2"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Commit();

            store = new InMemoryOffsetStore(sharedList);

            var result = await store.GetLatestValue(
                "key1",
                new TestConsumerEndpoint("endpoint1")
                {
                    GroupId = "group1"
                });

            result.Should().NotBeNull();
            result!.Value.Should().Be("2");
        }

        [Fact]
        public async Task Rollback_Store_Reverted()
        {
            var store = new InMemoryOffsetStore(new TransactionalDictionarySharedItems<string, IComparableOffset>());

            await store.Store(
                new TestOffset("key1", "1"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Commit();

            store.CommittedItemsCount.Should().Be(1);

            await store.Store(
                new TestOffset("key1", "2"),
                new TestConsumerEndpoint("endpoint1") { GroupId = "group1" });
            await store.Rollback();

            store.CommittedItemsCount.Should().Be(1);

            var result = await store.GetLatestValue(
                "key1",
                new TestConsumerEndpoint("endpoint1")
                {
                    GroupId = "group1"
                });

            result.Should().NotBeNull();
            result!.Value.Should().Be("1");
        }
    }
}
