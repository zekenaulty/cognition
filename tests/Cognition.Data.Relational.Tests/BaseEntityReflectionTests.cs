using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Newtonsoft.Json.Linq;

namespace Cognition.Data.Relational.Tests;

public class BaseEntityReflectionTests
{
    /// <summary>
    /// Reflection-driven regression safeguard across every BaseEntity POCO. Assigns representative values to
    /// writable properties (including primitives, collections, dictionaries, and navigation references) to make
    /// sure properties still accept data after model refactors.
    /// </summary>
    [Fact]
    public void BaseEntities_ShouldSupportBasicReadWriteViaReflection()
    {
        var assembly = typeof(CognitionDbContext).Assembly;
        var entityTypes = assembly.GetTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.FullName)
            .ToList();

        entityTypes.Should().NotBeEmpty("expected at least one BaseEntity implementation");

        foreach (var entityType in entityTypes)
        {
            object instance;
            try
            {
                instance = Activator.CreateInstance(entityType)
                    ?? throw new InvalidOperationException($"Activator returned null for {entityType.FullName}.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to create instance of {entityType.FullName}. Ensure a public parameterless constructor exists.", ex);
            }

            foreach (var property in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite || property.SetMethod?.IsPublic != true)
                {
                    continue; // skip read-only or non-public setters
                }

                if (!SampleValueProvider.TryCreate(property.PropertyType, out var sample))
                {
                    continue; // unsupported exotic type; skip but keep iterating
                }

                property.SetValue(instance, sample);
                var roundTrip = property.GetValue(instance);
                SampleValueProvider.AssertRoundTrip(sample, roundTrip, property.PropertyType, $"{entityType.Name}.{property.Name}");
            }
        }
    }

    private static class SampleValueProvider
    {
        private static readonly Dictionary<Type, object?> PrimitiveSamples = new()
        {
            [typeof(string)] = "sample-text",
            [typeof(bool)] = true,
            [typeof(byte)] = (byte)42,
            [typeof(sbyte)] = (sbyte)7,
            [typeof(char)] = 'Z',
            [typeof(short)] = (short)123,
            [typeof(ushort)] = (ushort)456,
            [typeof(int)] = 789,
            [typeof(uint)] = (uint)987,
            [typeof(long)] = 13579L,
            [typeof(ulong)] = 24680UL,
            [typeof(float)] = 1.23f,
            [typeof(double)] = 4.56d,
            [typeof(decimal)] = 7.89m,
            [typeof(DateTime)] = new DateTime(2025, 10, 05, 12, 34, 56, DateTimeKind.Utc),
            [typeof(DateTimeOffset)] = new DateTimeOffset(2025, 10, 05, 12, 34, 56, TimeSpan.Zero),
            [typeof(TimeSpan)] = TimeSpan.FromMinutes(42),
            [typeof(Guid)] = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            [typeof(Uri)] = new Uri("https://example.com"),
            [typeof(Version)] = new Version(1, 2, 3, 4),
            [typeof(CultureInfo)] = CultureInfo.InvariantCulture,
            [typeof(TimeOnly)] = new TimeOnly(12, 34, 56),
            [typeof(DateOnly)] = new DateOnly(2025, 10, 05)
        };

        public static bool TryCreate(Type declaredType, out object? value)
        {
            return TryCreateInternal(declaredType, new HashSet<Type>(), 0, out value);
        }

        private static bool TryCreateInternal(Type declaredType, HashSet<Type> visited, int depth, out object? value)
        {
            var underlying = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            var trackReference = !underlying.IsValueType && underlying != typeof(string);
            var removeVisited = false;

            try
            {
                if (trackReference)
                {
                    if (!visited.Add(underlying))
                    {
                        value = GetDefault(declaredType);
                        return true;
                    }

                    removeVisited = true;
                }

                if (PrimitiveSamples.TryGetValue(underlying, out var primitive))
                {
                    value = ConvertForNullable(declaredType, primitive);
                    return true;
                }

                if (underlying.IsEnum)
                {
                    var names = Enum.GetNames(underlying);
                    if (names.Length > 0)
                    {
                        value = Enum.Parse(underlying, names[0]);
                        value = ConvertForNullable(declaredType, value);
                        return true;
                    }
                }

                if (underlying == typeof(object))
                {
                    value = new object();
                    return true;
                }

                if (underlying == typeof(byte[]))
                {
                    value = new byte[] { 1, 2, 3 };
                    return true;
                }

                if (underlying == typeof(JsonElement))
                {
                    value = JsonDocument.Parse("{\"sample\":123}").RootElement.Clone();
                    return true;
                }

                if (typeof(JToken).IsAssignableFrom(underlying))
                {
                    value = underlying switch
                    {
                        var t when t == typeof(JObject) => new JObject { ["sample"] = 123 },
                        var t when t == typeof(JArray) => new JArray { 1, 2, 3 },
                        var t when t == typeof(JValue) => new JValue("sample"),
                        _ => (JToken)new JObject { ["sample"] = 123 }
                    };
                    return true;
                }

                if (underlying.IsGenericType && underlying.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var args = underlying.GetGenericArguments();
                    if (args.Length == 2 && TryCreateInternal(args[0], visited, depth + 1, out var key) && TryCreateInternal(args[1], visited, depth + 1, out var val))
                    {
                        value = Activator.CreateInstance(underlying, key, val);
                        value = ConvertForNullable(declaredType, value);
                        return true;
                    }
                }

                if (underlying.IsArray)
                {
                    var elementType = underlying.GetElementType();
                    if (elementType != null && TryCreateInternal(elementType, visited, depth + 1, out var elementValue))
                    {
                        var array = Array.CreateInstance(elementType, 1);
                        array.SetValue(elementValue, 0);
                        value = array;
                        return true;
                    }
                }

                if (IsDictionaryType(declaredType, out var keyType, out var valueType))
                {
                    if (TryCreateInternal(keyType, visited, depth + 1, out var dictKey) && TryCreateInternal(valueType, visited, depth + 1, out var dictValue))
                    {
                        var concrete = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                        var dictionary = Activator.CreateInstance(concrete);
                        concrete.GetMethod("Add")!.Invoke(dictionary, new[] { dictKey, dictValue });
                        value = dictionary;
                        return true;
                    }
                }

                if (IsCollectionType(declaredType, out var itemType))
                {
                    if (TryCreateInternal(itemType, visited, depth + 1, out var itemValue))
                    {
                        var listType = typeof(List<>).MakeGenericType(itemType);
                        var list = (IList)Activator.CreateInstance(listType)!;
                        list.Add(itemValue);
                        value = list;
                        return true;
                    }
                }

                if (typeof(BaseEntity).IsAssignableFrom(underlying))
                {
                    if (depth > 2)
                    {
                        value = null;
                        return true; // break cycles in navigation graphs
                    }

                    try
                    {
                        var entity = Activator.CreateInstance(underlying);
                        var idProperty = underlying.GetProperty("Id");
                        idProperty?.SetValue(entity, Guid.NewGuid());
                        value = entity;
                        return true;
                    }
                    catch
                    {
                        value = null;
                        return true;
                    }
                }

                if (!underlying.IsAbstract && underlying.GetConstructor(Type.EmptyTypes) != null && depth < 2)
                {
                    value = Activator.CreateInstance(underlying);
                    value = ConvertForNullable(declaredType, value);
                    return true;
                }

                value = null;
                return false;
            }
            finally
            {
                if (removeVisited)
                {
                    visited.Remove(underlying);
                }
            }
        }

        public static void AssertRoundTrip(object? expected, object? actual, Type propertyType, string propertyName)
        {
            if (expected is null || actual is null)
            {
                actual.Should().Be(expected, propertyName + " should accept null values");
                return;
            }

            if (expected is JsonElement expectedJson && actual is JsonElement actualJson)
            {
                actualJson.GetRawText().Should().Be(expectedJson.GetRawText(), propertyName + " should round-trip JSON");
                return;
            }

            if (expected is IDictionary expectedDict && actual is IDictionary actualDict)
            {
                actualDict.Should().BeEquivalentTo(expectedDict, propertyName + " should round-trip dictionaries");
                return;
            }

            if (expected is IEnumerable expectedEnumerable && expected is not string && actual is IEnumerable actualEnumerable)
            {
                actualEnumerable.Should().BeEquivalentTo(expectedEnumerable, propertyName + " should round-trip collections");
                return;
            }

            actual.Should().Be(expected, propertyName + " should round-trip scalar values");
        }

        private static bool IsDictionaryType(Type type, out Type keyType, out Type valueType)
        {
            var iface = GetGenericInterface(type, typeof(IDictionary<,>));
            if (iface != null)
            {
                var args = iface.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }

            keyType = typeof(object);
            valueType = typeof(object);
            return false;
        }

        private static bool IsCollectionType(Type type, out Type itemType)
        {
            var iface = GetGenericInterface(type, typeof(IEnumerable<>));
            if (iface != null)
            {
                var arg = iface.GetGenericArguments()[0];
                if (arg != typeof(char))
                {
                    itemType = arg;
                    return true;
                }
            }

            itemType = typeof(object);
            return false;
        }

        private static Type? GetGenericInterface(Type type, Type openGeneric)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
            {
                return type;
            }

            return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
        }

        private static object? ConvertForNullable(Type declaredType, object? value)
        {
            return Nullable.GetUnderlyingType(declaredType) != null ? value : value;
        }

        private static object? GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
