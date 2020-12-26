﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetDaemon.Infrastructure.Extensions
{
    internal static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetTypesWhereSubclassOf<T>(this Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsGenericType && !type.IsAbstract && type.IsSubclassOf(typeof(T)));
        }
    }
}