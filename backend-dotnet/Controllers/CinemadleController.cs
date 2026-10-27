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
      new GetGameSummaryRequest(true, userId.ToString(), date, _config.GameLength)
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
      new GetGameSummaryRequest(false, GetUserId(), date, _config.GameLength)
    );
    return new MediatorResponseHandler<GameSummaryDto>(o).Handle();
  }

  [Authorize]
  [HttpGet("target/image")]
  public async Task<ActionResult> GetMovieImage(
      [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    logger.LogDebug("+GetMovieImage({date})", date);
    string? userId = GetUserId();
    if (string.IsNullOrWhiteSpace(userId))
    {
      logger.LogDebug("-GetMovieImage({date})", date);
      return new UnauthorizedResult();
    }

    try
    {
      int userGuesses = db.Guesses.Where(x => x.GameId == date && x.UserId == userId).Count();

      MovieDto? target = await tmdbRepository.GetTargetMovie(date);

      if (target is null)
      {
        logger.LogDebug("-GetMovieImage({date})", date);
        return new StatusCodeResult(500);
      }

      bool win = db.Guesses.Where(x => x.GameId == date && x.GuessMediaId == target.Id).Any();

      if (!win && !_config.MovieImageBlurFactors.ContainsKey(userGuesses.ToString()))
      {
        logger.LogDebug("GetMovieImage({date}): User attempted to access image on guess {number}", date, userGuesses);
        logger.LogDebug("-GetMovieImage({date})", date);
        return new UnauthorizedResult();
      }

      float blurFactor = (userGuesses >= _config.GameLength || win) ? 0.0F : _config.MovieImageBlurFactors[userGuesses.ToString()];

      ImageDto? image = await GetBlurredImage(date, blurFactor, tmdbRepository);

      if (image is null)
      {
        logger.LogDebug("GetMovieImage({date}): No image found", date);
        logger.LogDebug("-GetMovieImage({date})", date);
        return new NotFoundResult();
      }

      Clue? clue = db.UserClues.Where(x => x.GameId == date && x.UserId == userId && x.ClueType == ClueType.Visual).FirstOrDefault();
      if (clue is null)
      {
        try
        {
          db.UserClues.Add(new Clue
          {
            UserId = userId,
            GameId = date,
            ClueType = ClueType.Visual,
            Inserted = DateTime.Now
          });
        }
        catch (Exception ex)
        {
          logger.LogError("GetMovieImage Unable to save to DB. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
          logger.LogDebug("-GetMovieImage({date})", date);

          return new StatusCodeResult(500);
        }

      }

      await db.SaveChangesAsync();

      logger.LogDebug("-GetMovieImage({date})", date);
      return new OkObjectResult(image);
    }
    catch (Exception ex)
    {
      logger.LogError("GetMovieImage Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
      logger.LogDebug("-GetMovieImage({date})", date);

      return new StatusCodeResult(500);
    }
  }

  [HttpGet("target")]
  public async Task<ActionResult> GetTargetMovie(
    [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
  )
  {
    var o = await mediator.Dispatch<GetTargetMovie, MovieDto?>(
      new GetTargetMovie(false, string.Empty, date, false)
    );

    var handler = new MediatorResponseHandler<MovieDto?>(o).OnSuccess(x =>
    {
      if (x is null)
      {
        return new NotFoundResult();
      }

      return new OkObjectResult(x);
    });

    return handler.Handle();
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
      new GetPastGuessesRequest(true, userId.ToString(), date)
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
      new GetPastGuessesRequest(false, GetUserId(), date)
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
      new GuessMovieRequest(true, userId.ToString(), date, id, _config.GameLength)
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
      new GuessMovieRequest(false, GetUserId(), date, id, _config.GameLength)
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
