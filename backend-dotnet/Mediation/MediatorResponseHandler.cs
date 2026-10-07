using Microsoft.AspNetCore.Mvc;

namespace Cinemadle.Mediation; 

public class MediatorResponseHandler<TResponse>(IMediatorResponse<TResponse> response)
{
  private Func<Exception, ActionResult> _onException = (_) => { return new StatusCodeResult(500); };
  private Func<RequestValidationError, ActionResult> _onValidationError = (error) => { return new StatusCodeResult(error.StatusCode); };
  private Func<ProcessingError, ActionResult> _onProcessingError = (error) => { return new StatusCodeResult(error.StatusCode); };
  private Func<TResponse, ActionResult> _onSuccess = (data) => { return new OkObjectResult(data); };

  public MediatorResponseHandler<TResponse> OnValidationError(Func<RequestValidationError, ActionResult> handler)
  {
    _onValidationError = handler;
    return this;
  }
  public MediatorResponseHandler<TResponse> OnProcessingError(Func<ProcessingError, ActionResult> handler)
  {
    _onProcessingError = handler;
    return this;
  }
  public MediatorResponseHandler<TResponse> OnException(Func<Exception, ActionResult> handler)
  {
    _onException = handler;
    return this;
  }
  public MediatorResponseHandler<TResponse> OnSuccess(Func<TResponse, ActionResult> handler)
  {
    _onSuccess = handler;
    return this;
  }

  public ActionResult Handle()
  {
    if (response is InvalidMediatorResponse<TResponse> invalid)
    {
      return _onValidationError(invalid.Error);
    }

    if (response is ExceptionThrownDuringProcessing<TResponse> exception)
    {
      return _onException(exception.InnerException);
    }

    if (response is ProcessingErrorMediatorResponse<TResponse> processingError)
    {
      return _onProcessingError(processingError.Value);
    }

    if (response is ValidMediatorResponse<TResponse> valid)
    {
      return _onSuccess(valid.Value);
    }

    return _onException(new MediatorException());
  }
}