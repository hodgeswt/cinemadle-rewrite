using Cinemadle.Controllers.Handlers;
using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Cinemadle.Mediation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinemadle.Controllers;

[Route("api/custom")]
public class CustomGamesController(
    ILogger<CustomGamesController> logger,
    IOptions<CinemadleConfig> options,
    ITmdbRepository tmdbRepo,
    IGuessRepository guessRepo,
    IHintRepository hintRepo,
    IFeatureFlagRepository flagRepo,
    DatabaseContext db,
    Mediator mediator
)
    : CinemadleControllerBase
{

    private readonly CinemadleConfig _config = options.Value;

    [Authorize]
    [HttpPost("create")]
    public async Task<ActionResult> CreateCustomGame([FromBody] CustomGameCreateDto customGame)
    {
        var o = await mediator.Dispatch<CreateCustomGameRequest, CustomGameDto>(
            new CreateCustomGameRequest(false, GetUserId(), customGame.Id)
        );
        return new MediatorResponseHandler<CustomGameDto>(o).Handle();
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult> GetCustomGame(string id)
    {
        var o = await mediator.Dispatch<GetCustomGameRequest, CustomGameDto?>(
            new GetCustomGameRequest(false, GetUserId(), id)
        );
        return new MediatorResponseHandler<CustomGameDto?>(o).NullSuccessIs404().Handle();
    }

    [Authorize]
    [HttpGet("{customGameId}/target")]
    public async Task<ActionResult> GetCustomGameTarget(string customGameId)
    {
        var o = await mediator.Dispatch<GetTargetMovieRequest, MovieDto?>(
            new GetTargetMovieRequest(GetUserId(), customGameId, true)
        );

        return new MediatorResponseHandler<MovieDto?>(o).NullSuccessIs404().Handle();
    }

    [Authorize]
    [HttpGet("{customGameId}/target/image")]
    public async Task<ActionResult> GetCustomGameImage(string customGameId)
    {
        var o = await mediator.Dispatch<GetMovieImageRequest, ImageDto?>(
            new GetMovieImageRequest(false, GetUserId(), customGameId, true)
        );
        return new MediatorResponseHandler<ImageDto?>(o).NullSuccessIs404().Handle();
    }

    [Authorize]
    [HttpGet("{customGameId}/guesses")]
    public ActionResult GetPastGuessesCustomGame(string customGameId)
    {
        logger.LogDebug("+GetPastGuessesCustomGame({customGameId})", customGameId);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetPastGuessesCustomGame({customGameId})", customGameId);
            return new UnauthorizedResult();
        }

        try
        {
            // Verify custom game exists
            CustomGame? customGame = db.CustomGames.FirstOrDefault(x => x.Id == customGameId);
            if (customGame is null)
            {
                logger.LogDebug("-GetPastGuessesCustomGame({customGameId}): Custom game not found", customGameId);
                return new NotFoundResult();
            }

            IEnumerable<UserGuess> guesses = db.Guesses.Where(
                x => x.GameId == customGameId && x.UserId == userId
            )
            .OrderBy(x => x.SequenceId);

            logger.LogDebug("GetPastGuessesCustomGame({customGameId}): {data}", customGameId, guesses.Count());

            logger.LogDebug("-GetPastGuessesCustomGame({customGameId})", customGameId);
            return new OkObjectResult(guesses.Select(x => x.GuessMediaId));
        }
        catch (Exception ex)
        {
            logger.LogError("GetPastGuessesCustomGame Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            logger.LogDebug("-GetPastGuessesCustomGame({customGameId})", customGameId);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("{customGameId}/guess/{movieId}")]
    public async Task<ActionResult> GuessMovieCustomGame(string customGameId, int movieId)
    {
        var o = await mediator.Dispatch<GuessMovieRequest, GuessDto?>(
            new GuessMovieRequest(false, GetUserId(), customGameId, movieId, _config.GameLength, true)
        );

        return new MediatorResponseHandler<GuessDto?>(o).Handle();
    }

    [Authorize]
    [HttpGet("{customGameId}/gameSummary")]
    public async Task<ActionResult> GetGameSummaryCustomGame(string customGameId)
    {
        var o = await mediator.Dispatch<GetGameSummaryRequest, GameSummaryDto>(
            new GetGameSummaryRequest(false, GetUserId(), customGameId, _config.GameLength, true)
        );
        
        return new MediatorResponseHandler<GameSummaryDto>(o).Handle();
    }
}