﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using Silverback.Diagnostics;

namespace Silverback.Tools.LogEventsDocsGenerator;

internal static class DocsGenerator
{
    private static readonly HashSet<int> EventIdSet = new();

    public static void GenerateDocsTable(Type logEventsType)
    {
        Console.WriteLine("Id | Level | Message | Reference");
        Console.WriteLine(":-- | :-- | :-- | :--");

        foreach (PropertyInfo property in logEventsType.GetProperties())
        {
            LogEvent logEvent = (LogEvent)property.GetValue(null)!;

            EventIdSet.Add(logEvent.EventId.Id);

            string apiReferenceLink =
                $"[{property.Name}]" +
                $"(xref:{logEventsType.FullName}" +
                $"#{logEventsType.FullName!.Replace(".", "_", StringComparison.Ordinal)}" +
                $"_{property.Name})";

            string message = logEvent.Message.Replace("|", "&#124;", StringComparison.Ordinal);

            Console.WriteLine($"{logEvent.EventId.Id} | {logEvent.Level} | {message} | {apiReferenceLink}");
        }
    }
}
