﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyChainTests
    {
        private readonly IErrorPolicyBuilder _errorPolicyBuilder;

        private readonly IServiceProvider _fakeServiceProvider = Substitute.For<IServiceProvider>();

        public ErrorPolicyChainTests()
        {
            var services = new ServiceCollection();

            services.AddSilverback().WithConnectionToMessageBroker(
                options => options
                    .AddBroker<TestBroker>());

            services.AddNullLogger();

            _errorPolicyBuilder =
                new ErrorPolicyBuilder(
                    services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }),
                    NullLoggerFactory.Instance);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        public void HandleError_RetryWithMaxFailedAttempts_AppliedAccordingToMaxFailedAttempts(int failedAttempts)
        {
            var rawMessage = new byte[1];
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };

            var testPolicy = new TestErrorPolicy(_fakeServiceProvider);

            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
                testPolicy);

            chain.HandleError(
                new[]
                {
                    new InboundEnvelope(
                        rawMessage,
                        headers,
                        null,
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)
                },
                new InvalidOperationException("test"));

            testPolicy.Applied.Should().Be(failedAttempts > 3);
        }

        [Theory]
        [InlineData(1, ErrorAction.Retry)]
        [InlineData(2, ErrorAction.Retry)]
        [InlineData(3, ErrorAction.Skip)]
        [InlineData(4, ErrorAction.Skip)]
        public async Task HandleError_RetryTwiceThenSkip_CorrectPolicyApplied(
            int failedAttempts,
            ErrorAction expectedAction)
        {
            var rawMessage = new byte[1];
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };

            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(2),
                _errorPolicyBuilder.Skip());

            var action = await chain.HandleError(
                new[]
                {
                    new InboundEnvelope(
                        rawMessage,
                        headers,
                        null,
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)
                },
                new InvalidOperationException("test"));

            action.Should().Be(expectedAction);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(5, 2)]
        public void HandleError_MultiplePoliciesWithSetMaxFailedAttempts_CorrectPolicyApplied(
            int failedAttempts,
            int expectedAppliedPolicy)
        {
            var rawMessage = new byte[1];
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };

            var policies = new[]
            {
                new TestErrorPolicy(_fakeServiceProvider).MaxFailedAttempts(2),
                new TestErrorPolicy(_fakeServiceProvider).MaxFailedAttempts(2),
                new TestErrorPolicy(_fakeServiceProvider).MaxFailedAttempts(2)
            };

            var chain = _errorPolicyBuilder.Chain(policies);

            chain.HandleError(
                new[]
                {
                    new InboundEnvelope(
                        rawMessage,
                        headers,
                        null,
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)
                },
                new InvalidOperationException("test"));

            for (var i = 0; i < policies.Length; i++)
            {
                policies[i].As<TestErrorPolicy>().Applied.Should().Be(i == expectedAppliedPolicy);
            }
        }

        // TODO: Test with multiple messages (batch)
    }
}
