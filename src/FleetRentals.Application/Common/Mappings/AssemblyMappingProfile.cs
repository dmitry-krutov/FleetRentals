using System.Reflection;
using AutoMapper;

namespace FleetRentals.Application.Common.Mappings;

public sealed class AssemblyMappingProfile : Profile
{
    public AssemblyMappingProfile()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic /* && a.FullName!.StartsWith("AntypSite.") */)
            .Concat(new[] { Assembly.GetExecutingAssembly() })
            .Distinct()
            .ToArray();

        foreach (var asm in assemblies)
            ApplyMappingsFromAssembly(asm);
    }

    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null)!.ToArray()!;
        }

        var iMapTo = typeof(IMapTo<>);
        var iMapFrom = typeof(IMapFrom<>);

        foreach (var type in types)
        {
            if (type is not { IsClass: true, IsAbstract: false })
                continue;

            var declaredMapping = type.GetMethod(
                name: "Mapping",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                binder: null,
                types: new[] { typeof(Profile) },
                modifiers: null);

            var mapToIfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == iMapTo)
                .ToArray();
            var mapFromIfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == iMapFrom)
                .ToArray();

            if (mapToIfaces.Length == 0 && mapFromIfaces.Length == 0)
                continue;

            if (declaredMapping != null)
            {
                var instance = Activator.CreateInstance(type);
                declaredMapping.Invoke(instance, new object[] { this });
            }
            else
            {
                foreach (var itf in mapToIfaces)
                {
                    var dest = itf.GetGenericArguments()[0];
                    CreateMap(type, dest);
                }

                foreach (var itf in mapFromIfaces)
                {
                    var src = itf.GetGenericArguments()[0]; // T
                    CreateMap(src, type); // T -> type
                }
            }
        }
    }
}