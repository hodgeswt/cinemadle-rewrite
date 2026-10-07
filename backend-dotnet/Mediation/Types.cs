namespace Cinemadle.Mediation;

public interface IRequestHandler<TRequest, TResponse>
{
  Task<TResponse> Handle(TRequest request);

  Task<ProcessingError?> PreProcess(TRequest request)
  {
    return Task.FromResult<ProcessingError?>(null);
  }

  Task<ProcessingError?> PreValidate(TRequest request)
  {
    return Task.FromResult<ProcessingError?>(null);
  }

  Task<RequestValidationError?> Validate(TRequest request)
  {
    return Task.FromResult<RequestValidationError?>(null);
  }
}

public class MediatorException() : Exception("Unable to determine type of mediator response");
public record class RequestValidationError(string Message, int StatusCode = 400);
public record class ProcessingError(string Message, int StatusCode = 404);
public interface IMediatorResponse<TResponse>;
public record struct InvalidMediatorResponse<TResponse>(RequestValidationError Error) : IMediatorResponse<TResponse>;
public record struct ExceptionThrownDuringProcessing<TResponse>(Exception InnerException) : IMediatorResponse<TResponse>;
public record struct ValidMediatorResponse<TResponse>(TResponse Value) : IMediatorResponse<TResponse>;
public record struct ProcessingErrorMediatorResponse<TResponse>(ProcessingError Value) : IMediatorResponse<TResponse>;