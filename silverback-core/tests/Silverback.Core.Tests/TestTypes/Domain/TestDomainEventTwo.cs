﻿// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestDomainEventTwo : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}