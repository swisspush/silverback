﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback
{
    internal static class Justifications
    {
        public const string MarkerInterface =
            "Intentionally a marker interface";

        public const string NullableTypesSpacingFalsePositive =
            "[]?, []!, ()? and ()! are recognized as wrongly spaced (probably a small bug in the analyzer)";

        public const string CalledBySilverback =
            "The method is called by Silverback internals and don't need to check for null";

        public const string ExceptionLogged =
            "The exception is logged, it is not swallowed";

        public const string MutableProperties =
            "Using the object hashcode since the properties are actually mutable";

        public const string CanExposeByteArray =
            "It doesn't really cause any issue and it would feel unnatural to expose a collection instead of " +
            "a byte array";

        public const string AllowedForConstants =
            "Nested types are allowed in constant classes: they are used to create a structure";

        public const string Subscriber =
            "Subscriber method called by the IPublisher";
    }
}
