﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Subscribers
{
    public class SubscribedMethodInvokerTests
    {
        private static readonly IServiceProvider ServiceProvider = Substitute.For<IServiceProvider>();

        [Fact]
        public async Task Invoke_MethodWithSingleMessageParameter_MethodInvokedForMatchingMessages()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod = new DelegateSubscription((Action<TestEventOne>)Method1, new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            calls.Should().Be(2);

            void Method1(TestEventOne eventOne) => calls++;
        }

        [Fact]
        public async Task Invoke_MethodWithSingleMessageParameterOfAncestorType_MethodInvoked()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod = new DelegateSubscription((Action<IEvent>)Method1, new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            calls.Should().Be(3);

            void Method1(IEvent @event) => calls++;
        }

        [Fact]
        public async Task Invoke_MethodWithSingleMessageParameterOfNotMatchingType_MethodNotInvoked()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod =
                new DelegateSubscription((Action<TestCommandOne>)Method1, new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            calls.Should().Be(0);

            void Method1(TestCommandOne command) => calls++;
        }

        [Fact]
        public async Task Invoke_MethodWithEnumerableParameter_MethodInvokedForMatchingMessages()
        {
            var receivedMessages = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod =
                new DelegateSubscription((Action<IEnumerable<TestEventOne>>)Method1, new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            receivedMessages.Should().Be(2);

            void Method1(IEnumerable<TestEventOne> events) => receivedMessages += events.Count();
        }

        [Fact]
        public async Task Invoke_MethodWithEnumerableParameterOfAncestorType_MethodInvoked()
        {
            var receivedMessages = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod =
                new DelegateSubscription((Action<IEnumerable<IEvent>>)Method1, new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            receivedMessages.Should().Be(3);

            void Method1(IEnumerable<IEvent> events) => receivedMessages += events.Count();
        }

        [Fact]
        public async Task Invoke_NoMessagesMatchingEnumerableType_MethodNotInvoked()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod = new DelegateSubscription(
                (Action<IEnumerable<TestCommandOne>>)Method1,
                new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            calls.Should().Be(0);

            void Method1(IEnumerable<TestCommandOne> events) => calls++;
        }

        [Fact]
        public async Task Invoke_MethodWithReadOnlyCollectionParameter_MethodInvokedForMatchingMessages()
        {
            var receivedMessages = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod =
                new DelegateSubscription(
                    (Action<IReadOnlyCollection<TestEventOne>>)Method1,
                    new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            receivedMessages.Should().Be(2);

            void Method1(IReadOnlyCollection<TestEventOne> events) => receivedMessages += events.Count;
        }

        [Fact]
        public async Task Invoke_MethodWithReadOnlyCollectionParameterOfAncestorType_MethodInvoked()
        {
            var receivedMessages = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod =
                new DelegateSubscription((Action<IReadOnlyCollection<IEvent>>)Method1, new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            receivedMessages.Should().Be(3);

            void Method1(IReadOnlyCollection<IEvent> events) => receivedMessages += events.Count;
        }

        [Fact]
        public async Task Invoke_NoMessagesMatchingReadOnlyCollectionType_MethodNotInvoked()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod = new DelegateSubscription(
                (Action<IReadOnlyCollection<TestCommandOne>>)Method1,
                new SubscriptionOptions());

            var messages = new object[] { new TestEventOne(), new TestEventTwo(), new TestEventOne() };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    messages,
                    true);

            calls.Should().Be(0);

            void Method1(IReadOnlyCollection<TestCommandOne> events) => calls++;
        }

        [Fact]
        public async Task Invoke_EnvelopesToUnwrap_MethodInvokedWithPureMessage()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod = new DelegateSubscription((Action<IEvent>)Method1, new SubscriptionOptions());

            var envelopes = new object[]
            {
                new TestEnvelope(new TestEventOne()),
                new TestEnvelope(new TestEventTwo()),
                new TestEnvelope(new TestEventOne())
            };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    envelopes,
                    true);

            calls.Should().Be(3);

            void Method1(IEvent @event)
            {
                if (@event != null)
                    calls++;
            }
        }

        [Fact]
        public async Task Invoke_Envelopes_MethodInvoked()
        {
            var calls = 0;

            var (resolver, handler) = GetDefaultResolverAndHandler();
            var subscribedMethod = new DelegateSubscription((Action<TestEnvelope>)Method1, new SubscriptionOptions());

            var envelopes = new object[]
            {
                new TestEnvelope(new TestEventOne()),
                new TestEnvelope(new TestEventTwo()),
                new TestEnvelope(new TestEventOne())
            };

            await new SubscribedMethodInvoker(resolver, handler, ServiceProvider)
                .Invoke(
                    subscribedMethod.GetSubscribedMethods(ServiceProvider).First(),
                    envelopes,
                    true);

            calls.Should().Be(3);

            void Method1(TestEnvelope envelope)
            {
                if (envelope != null)
                    calls++;
            }
        }

        private static (ArgumentsResolverService, ReturnValueHandlerService) GetDefaultResolverAndHandler()
        {
            var argumentsResolverService = new ArgumentsResolverService(
                new IArgumentResolver[]
                {
                    new SingleMessageArgumentResolver(),
                    new ReadOnlyCollectionMessageArgumentResolver(),
                    new EnumerableMessageArgumentResolver()
                });

            var returnValueHandlerService = new ReturnValueHandlerService(Array.Empty<IReturnValueHandler>());

            return (argumentsResolverService, returnValueHandlerService);
        }
    }
}
