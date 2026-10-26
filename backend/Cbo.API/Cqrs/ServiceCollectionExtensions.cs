using System.Reflection;

namespace Cbo.API.Cqrs;

public static class ServiceCollectionExtensions
{
    private static readonly Type[] s_handlerInterfaces =
        [typeof(ICommandHandler<>), typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    /// <summary>
    /// Registers every concrete command and query handler in <paramref name="assembly"/> as Scoped.
    /// Throws when one message has two handlers.
    /// </summary>
    public static IServiceCollection AddCqrsHandlers(this IServiceCollection services, Assembly assembly)
    {
        Dictionary<Type, Type> handlerByService = [];

        IEnumerable<Type> handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false });

        foreach (Type handlerType in handlerTypes)
        {
            IEnumerable<Type> serviceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && s_handlerInterfaces.Contains(i.GetGenericTypeDefinition()));

            foreach (Type serviceType in serviceTypes)
            {
                if (!handlerByService.TryAdd(serviceType, handlerType))
                    throw new InvalidOperationException(
                        $"Both {handlerByService[serviceType]} and {handlerType} handle {serviceType}.");

                services.AddScoped(serviceType, handlerType);
            }
        }

        return services;
    }
}
