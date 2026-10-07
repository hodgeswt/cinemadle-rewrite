using Cinemadle.Mediation;

namespace Cinemadle.ServiceExtensions;

public static class AddMediatorExtension
{
  public static IServiceCollection AddMediator(this IServiceCollection services)
  {
    services.AddSingleton<IHandlersProvider, ServicesHandlersProvider>();
    services.AddSingleton<Mediator>();

    var handlers = typeof(Mediator).Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(IRequestHandler<,>))) ?? [];

    foreach (Type handler in handlers)
    {
      foreach (var serviceType in handler.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
      {
        services.AddScoped(serviceType, handler);
      }
    }

    return services;
  }
}