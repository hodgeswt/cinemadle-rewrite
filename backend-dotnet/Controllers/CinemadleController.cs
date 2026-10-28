using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Cinemadle.Mediation;
using Cinemadle.Controllers.Handlers;

namespace Cinemadle.Controllers;

[Route("api/cinemadle")]
[ApiController]
// TODO: remove unused arguments. It breaks unit test constructors right now
public class CinemadleController(
    ILogger<CinemadleController> logger,
    IOptions<CinemadleConfig> configRepository,
    ITmdbRepository tmdbRepository,
    IGuessRepository guessRepository,
    IHintRepository hintRepository,
    Mediator mediator,
    DatabaseContext db)
    : CinemadleControllerBase
{
  private readonly CinemadleConfig _config = configRepository.Value;

  [Authorize]
  [HttpGet("validate")]
  public ActionResult<bool> Validate()
  {
    return true;
  }

  [HttpGet("anonUserId")]
  public async Task<ActionResult> GetAnonUserId()
  {
    var o = await mediator.Dispatch<CreateAnonUserIdRequest, string>(new CreateAnonUserIdRequest());
    return new MediatorResponseHandler<string>(o).Handle();
  }

  [HttpGet("gameSummary/anon")]
  public async Task<ActionResult> GetGameSummaryAnon(
      [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
      [FromQuery, Required] Guid userId
  )
  {
    var o = await mediator.Dispatch<GetGameSummaryRequest, GameSummaryDto>(
      new GetGameSummaryRequest(true, userId.ToString(), date, _config.GameLength, false)
    );
    return new MediatorResponseHandler<GameSummaryDto>(o).Handle();
  }


  [Authorize]
  [HttpGet("gameSummary")]
  public async Task<ActionResult> GetGameSummary(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    var o = await mediator.Dispatch<GetGameSummaryRequest, GameSummaryDto>(
      new GetGameSummaryRequest(false, GetUserId(), date, _config.GameLength, false)
    );
    return new MediatorResponseHandler<GameSummaryDto>(o).Handle();
  }

  [Authorize]
  [HttpGet("target/image")]
  public async Task<ActionResult> GetMovieImage(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    var o = await mediator.Dispatch<GetMovieImageRequest, ImageDto?>(
      new GetMovieImageRequest(false, GetUserId(), date, false)
    );
    return new MediatorResponseHandler<ImageDto?>(o).NullSuccessIs404().Handle();
  }

  [HttpGet("target")]
  public async Task<ActionResult> GetTargetMovie(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    var o = await mediator.Dispatch<GetTargetMovieRequest, MovieDto?>(
      new GetTargetMovieRequest(string.Empty, date, false)
    );

    return new MediatorResponseHandler<MovieDto?>(o).NullSuccessIs404().Handle();
  }

  [HttpGet("movielist")]
  public async Task<ActionResult> GetMovieList()
  {
    logger.LogDebug("+GetMovieList");

    try
    {
      Dictionary<string, int> movies = await tmdbRepository.GetMovieList();
      return new OkObjectResult(movies);
    }
    catch (Exception ex)
    {
      logger.LogError("GetMovieList Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);

      logger.LogDebug("-GetMovieList");
      return new StatusCodeResult(500);
    }
  }

  [HttpGet("guesses/anon")]
  public async Task<ActionResult> GetPastGuessesAnon(
      [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
      [FromQuery, Required] Guid userId
  )
  {
    var o = await mediator.Dispatch<GetPastGuessesRequest, GetPastGuessesResponse>(
      new GetPastGuessesRequest(true, userId.ToString(), date, false)
    );

    var handler =
      new MediatorResponseHandler<GetPastGuessesResponse>(o)
      .OnSuccess((x) => new OkObjectResult(x.PastGuessMediaIds));

    return handler.Handle();
  }

  [Authorize]
  [HttpGet("guesses")]
  public async Task<ActionResult> GetPastGuesses(
      [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    var o = await mediator.Dispatch<GetPastGuessesRequest, GetPastGuessesResponse>(
      new GetPastGuessesRequest(false, GetUserId(), date, false)
    );

    var handler =
      new MediatorResponseHandler<GetPastGuessesResponse>(o)
      .OnSuccess((x) => new OkObjectResult(x.PastGuessMediaIds));

    return handler.Handle();
  }

  [HttpGet("guess/anon/{id}")]
  public async Task<ActionResult> GuessMovieAnon(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
    [FromQuery, Required] Guid userId,
    int id
  )
  {
    var o = await mediator.Dispatch<GuessMovieRequest, GuessDto?>(
      new GuessMovieRequest(true, userId.ToString(), date, id, _config.GameLength, false)
    );

    return new MediatorResponseHandler<GuessDto?>(o).Handle();
  }

  [Authorize]
  [HttpGet("guess/{id}")]
  public async Task<ActionResult> GuessMovie(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
    int id
  )
  {
    var o = await mediator.Dispatch<GuessMovieRequest, GuessDto?>(
      new GuessMovieRequest(false, GetUserId(), date, id, _config.GameLength, false)
    );

    return new MediatorResponseHandler<GuessDto?>(o).Handle();
  }

  [Authorize]
  [HttpGet("hints")]
  public async Task<ActionResult> GetHints(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    var o = await mediator.Dispatch<GetHintsRequest, Dictionary<string, HintsDto>>(
      new GetHintsRequest(false, GetUserId(), date, false)
    );

    return new MediatorResponseHandler<Dictionary<string, HintsDto>>(o).Handle();
  }

  [HttpGet("hints/anon")]
  public async Task<ActionResult> GetHintsAnon(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
    [FromQuery, Required] Guid userId
  )
  {
    var o = await mediator.Dispatch<GetHintsRequest, Dictionary<string, HintsDto>>(
      new GetHintsRequest(true, userId.ToString(), date, false)
    );

    return new MediatorResponseHandler<Dictionary<string, HintsDto>>(o).Handle();
  }

  [Authorize]
  [HttpGet("hints/custom/{customGameId}")]
  public async Task<ActionResult> GetHintsCustomGame(string customGameId)
  {
    var o = await mediator.Dispatch<GetHintsRequest, Dictionary<string, HintsDto>>(
      new GetHintsRequest(false, GetUserId(), customGameId, true)
    );

    return new MediatorResponseHandler<Dictionary<string, HintsDto>>(o).Handle();
  }
}
