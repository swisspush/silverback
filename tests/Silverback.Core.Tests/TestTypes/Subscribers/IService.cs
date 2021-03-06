﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface IService : ISubscriber
    {
    }
}