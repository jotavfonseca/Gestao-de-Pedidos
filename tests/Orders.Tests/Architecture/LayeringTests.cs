using System.Reflection;
using Orders.Core.Domain.Orders;

namespace Orders.Tests.Architecture;

/// <summary>Garante a regra de dependências do DDD tático (constitution.md §4).</summary>
public class LayeringTests
{
    private static readonly Assembly Core = typeof(Order).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_infrastructure()
    {
        var violations = ReferencesFrom("Orders.Core.Domain", "Microsoft.EntityFrameworkCore", "Npgsql", "Azure");
        Assert.True(violations.Count == 0, "Domínio depende de infraestrutura:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void Application_does_not_depend_on_service_bus()
    {
        var violations = ReferencesFrom("Orders.Core.Application", "Azure");
        Assert.True(violations.Count == 0, "Aplicação depende do Service Bus:\n" + string.Join("\n", violations));
    }

    private static List<string> ReferencesFrom(string @namespace, params string[] forbiddenAssemblyPrefixes)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                 BindingFlags.Static | BindingFlags.DeclaredOnly;

        return Core.GetTypes()
            .Where(type => type.Namespace?.StartsWith(@namespace, StringComparison.Ordinal) == true)
            .SelectMany(type =>
            {
                var used = new List<Type?> { type.BaseType };
                used.AddRange(type.GetInterfaces());
                used.AddRange(type.GetFields(all).Select(f => f.FieldType));
                used.AddRange(type.GetProperties(all).Select(p => p.PropertyType));
                foreach (var method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
                {
                    used.AddRange(method.GetParameters().Select(p => p.ParameterType));
                    if (method is MethodInfo info) used.Add(info.ReturnType);
                }

                return used
                    .Where(t => t is not null)
                    .SelectMany(Flatten)
                    .Where(t => forbiddenAssemblyPrefixes.Any(prefix =>
                        t.Assembly.GetName().Name!.StartsWith(prefix, StringComparison.Ordinal)))
                    .Select(t => $"{type.FullName} → {t.FullName}");
            })
            .Distinct()
            .ToList();
    }

    private static IEnumerable<Type> Flatten(Type? type)
    {
        if (type is null) yield break;
        yield return type;
        if (type.HasElementType)
        {
            foreach (var inner in Flatten(type.GetElementType())) yield return inner;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments().SelectMany(Flatten)) yield return argument;
        }
    }
}
