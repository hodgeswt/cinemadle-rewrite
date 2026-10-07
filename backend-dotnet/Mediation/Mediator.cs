
namespace Cinemadle.Mediation;

public interface IHandlersProvider
{
  public T GetHandler<T>() where T: notnull;
}

public class ServicesHandlersProvider(IServiceProvider serviceProvider) : IHandlersProvider
{
  public T GetHandler<T>() where T : notnull
  {
    return serviceProvider.GetRequiredService<T>();
  }
}

public class Mediator(IHandlersProvider handlersProvider)
{
  public async Task<IMediatorResponse<TResponse>> Dispatch<TRequest, TResponse>(TRequest request)
  {
    try {
      var handler = handlersProvider.GetHandler<IRequestHandler<TRequest, TResponse>>();
      if (await handler.Validate(request) is RequestValidationError validationError)
      {
        return new InvalidMediatorResponse<TResponse>(validationError);
      }

      if (await handler.PreProcess(request) is ProcessingError preProcessingError)
      {
        return new ProcessingErrorMediatorResponse<TResponse>(preProcessingError);
      }

      return new ValidMediatorResponse<TResponse>(await handler.Handle(request));
    }
    catch (Exception ex)
    {
      return new ExceptionThrownDuringProcessing<TResponse>(ex);
    }
  }
}