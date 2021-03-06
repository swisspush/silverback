﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tests.Integration.TestTypes.Domain
{
    [SuppressMessage("", "CA1040", Justification = Justifications.MarkerInterface)]
    public interface IIntegrationEvent : IIntegrationMessage
    {
    }
}