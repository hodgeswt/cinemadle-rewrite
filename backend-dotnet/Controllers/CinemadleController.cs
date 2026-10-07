using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
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
    IWebHostEnvironment env,
    IGuessRepository guessRepository,
    IHintRepository hintRepository,
    IFeatureFlagRepository flagRepo,
    Mediator mediator,
    DatabaseContext db)
    : CinemadleControllerBase
{
    private readonly CinemadleConfig _config = configRepository.Value;
    private readonly bool _isDevelopment = env.IsDevelopment();

    [Authorize]
    [HttpGet("validate")]
    public ActionResult<bool> Validate()
    {
        return true;
    }

    [HttpGet("anonUserId")]
    public async Task<ActionResult> GetAnonUserId()
    {
        logger.LogDebug("+GetAnonUserId");

        IEnumerable<AnonUser>? users = null;
        int count = 0;
        string userId = string.Empty;

        while ((users == null || users.Any()) && count < 5)
        {
            userId = Guid.NewGuid().ToString();

            users = db.AnonUsers
                .Where(x => x.UserId == userId);

            count++;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetAnonUserId");
            return new StatusCodeResult(500);
        }

        db.AnonUsers.Add(new()
        {
            UserId = userId
        });

        await db.SaveChangesAsync();

        logger.LogDebug("-GetAnonUserId");
        return new OkObjectResult(userId);
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
        var handler = new MediatorResponseHandler<GameSummaryDto>(o);
        return handler.Handle();
    }


    [Authorize]
    [HttpGet("gameSummary")]
    public async Task<ActionResult> GetGameSummary(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        var o = await mediator.Dispatch<GetGameSummaryRequest, GameSummaryDto>(
            new GetGameSummaryRequest(true, GetUserId(), date, _config.GameLength)
        );
        var handler = new MediatorResponseHandler<GameSummaryDto>(o);
        return handler.Handle();
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

            if (!win &&!_config.MovieImageBlurFactors.ContainsKey(userGuesses.ToString()))
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
        logger.LogDebug("+GetTargetMovie({date})", date);
        try
        {
            MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(date);

            if (targetMovie is null)
            {
                return new NotFoundResult();
            }

            logger.LogDebug("-GetTargetMovie({date})", date);
            return new OkObjectResult(targetMovie);
        }
        catch (Exception ex)
        {
            logger.LogError("GetTargetMovie Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            logger.LogDebug("-GetTargetMovie({date}", date);

            return new StatusCodeResult(500);

        }
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
    public ActionResult GetPastGuessesAnon(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
        [FromQuery, Required] Guid userId
    )
    {
        logger.LogDebug("+GetPastGuessesAnon({date}, {userId})", date, userId);

        string anonUserId = userId.ToString();
        AnonUser? user = db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            logger.LogWarning("GetPastGuessesAnon: attempted access by invalid user: {userId}", anonUserId);
            logger.LogDebug("-GetPastGuessesAnon({date}, {userId}", date, userId);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> guesses = db.AnonUserGuesses.Where(
                x => x.GameId == date && x.UserId == userId.ToString()
            )
            .OrderBy(x => x.SequenceId);

            logger.LogDebug("-GetPastGuessesAnon({date}, {userId})", date, userId);
            return new OkObjectResult(guesses.Select(x => x.GuessMediaId));
        }
        catch (Exception ex)
        {
            logger.LogError("GetPastGuesses Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            logger.LogDebug("-GetPastGuesses({date}, {userId})", date, userId);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("guesses")]
    public ActionResult GetPastGuesses(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        logger.LogDebug("+GetPastGuesses({date})", date);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetPastGuesses({date})", date);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> guesses = db.Guesses.Where(
                x => x.GameId == date && x.UserId == userId
            )
            .OrderBy(x => x.SequenceId);

            logger.LogDebug("GetPastGuesses({date}): {data}", date, guesses.Count());

            logger.LogDebug("-GetPastGuesses({date})", date);
            return new OkObjectResult(guesses.Select(x => x.GuessMediaId));
        }
        catch (Exception ex)
        {
            logger.LogError("GetPastGuesses Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            logger.LogDebug("-GetPastGuesses({date})", date);

            return new StatusCodeResult(500);
        }
    }

    [HttpGet("guess/anon/{id}")]
    public async Task<ActionResult> GuessMovieAnon(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
            [FromQuery, Required] Guid userId,
            int id
    )
    {
        logger.LogDebug("+GuessMovieAnon({date}, {userId}, {id}", date, userId, id);

        string anonUserId = userId.ToString();
        AnonUser? user = db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            logger.LogWarning("GuessMovieAnon: attempted access by invalid user: {userId}", anonUserId);
            logger.LogDebug("-GuessMovieAnon({date}, {userId}, {id}", date, userId, id);
            return new UnauthorizedResult();
        }

        try
        {
            GuessDto? guessDto = await GuessMovieInternal(id, date);
            UserGuess? x = db.AnonUserGuesses.FirstOrDefault(
                                           x => x.GuessMediaId == id && x.GameId == date && x.UserId == anonUserId
                                      );

            if (x is null)
            {
                int seqNo = (db.AnonUserGuesses.Where(x => x.GameId == date).OrderByDescending(x => x.SequenceId).FirstOrDefault()?.SequenceId ?? 0) + 1;
                db.AnonUserGuesses.Add(new UserGuess
                {
                    GameId = date,
                    UserId = anonUserId,
                    GuessMediaId = id,
                    SequenceId = seqNo,
                    Inserted = DateTime.Now,
                });

                await db.SaveChangesAsync();

                // Invalidate hints cache after new guess
                hintRepository.InvalidateHints(anonUserId, date);
            }

            logger.LogDebug("-GuessMovieAnon({date}, {userId}, {id})", date, userId, id);
            return new OkObjectResult(guessDto);

        }
        catch (Exception ex)
        {
            logger.LogError("GuessMovieAnon Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GuessMovieAnon({date}, {userId}, {id})", date, userId, id);

            return new StatusCodeResult(500);
        }
    }

    private async Task<GuessDto?> GuessMovieInternal(int id, string date)
    {
        MovieDto? guessMovie = await tmdbRepository.GetMovieById(id);

        if (guessMovie is null)
        {
            logger.LogDebug("-GuessMovieInternal({date}, {id})", date, id);
            return null;
        }

        MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(date);

        if (targetMovie is null)
        {
            logger.LogDebug("-GuessMovieInternal({date}, {id})", date, id);
            return null;
        }

        GuessDto? guessDto = guessRepository.Guess(guessMovie, targetMovie);

        if (guessDto is null)
        {
            logger.LogError("GuessMovieInternal: unable to create guess DTO");
            logger.LogDebug("-GuessMovieInternal({date}, {id})", date, id);
            return null;
        }

        return guessDto;
    }

    [Authorize]
    [HttpGet("guess/{id}")]
    public async Task<ActionResult> GuessMovie(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
            int id
    )
    {
        logger.LogDebug("+GuessMovie({date}, {id})", date, id);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GuessMovie({date}, {id})", date, id);
            return new UnauthorizedResult();
        }

        try
        {
            GuessDto? guessDto = await GuessMovieInternal(id, date);

            UserGuess? x = db.Guesses.FirstOrDefault(
                x => x.GuessMediaId == id && x.GameId == date && x.UserId == userId
            );

            if (x is null)
            {
                int seqNo = (db.Guesses.Where(
                    x => x.GameId == date && x.UserId == userId
                )
                .OrderByDescending(x => x.SequenceId)
                .FirstOrDefault()?.SequenceId ?? 0) + 1;

                db.Guesses.Add(new UserGuess
                {
                    GameId = date,
                    UserId = userId,
                    GuessMediaId = id,
                    SequenceId = seqNo,
                    Inserted = DateTime.Now,
                });

                await db.SaveChangesAsync();

                // Invalidate hints cache after new guess
                hintRepository.InvalidateHints(userId, date);
            }

            logger.LogDebug("-GuessMovie({date}, {id})", date, id);
            return new OkObjectResult(guessDto);

        }
        catch (Exception ex)
        {
            logger.LogError("GuessMovie Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GuessMovie({date}, {id})", date, id);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("hints")]
    public async Task<ActionResult> GetHints(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        logger.LogDebug("+GetHints({date})", date);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetHints({date})", date);
            return new UnauthorizedResult();
        }

        try
        {
            var hints = await hintRepository.GetHints(userId, date, isAnonymous: false, isCustomGame: false);
            logger.LogDebug("-GetHints({date})", date);
            return new OkObjectResult(hints);
        }
        catch (Exception ex)
        {
            logger.LogError("GetHints Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetHints({date})", date);
            return new StatusCodeResult(500);
        }
    }

    [HttpGet("hints/anon")]
    public async Task<ActionResult> GetHintsAnon(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
            [FromQuery, Required] Guid userId
    )
    {
        logger.LogDebug("+GetHintsAnon({date}, {userId})", date, userId);

        string anonUserId = userId.ToString();
        AnonUser? user = db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            logger.LogWarning("GetHintsAnon: attempted access by invalid user: {userId}", anonUserId);
            logger.LogDebug("-GetHintsAnon({date}, {userId})", date, userId);
            return new UnauthorizedResult();
        }

        try
        {
            var hints = await hintRepository.GetHints(anonUserId, date, isAnonymous: true, isCustomGame: false);
            logger.LogDebug("-GetHintsAnon({date}, {userId})", date, userId);
            return new OkObjectResult(hints);
        }
        catch (Exception ex)
        {
            logger.LogError("GetHintsAnon Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetHintsAnon({date}, {userId})", date, userId);
            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("hints/custom/{customGameId}")]
    public async Task<ActionResult> GetHintsCustomGame(
            string customGameId
    )
    {
        logger.LogDebug("+GetHintsCustomGame({customGameId})", customGameId);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetHintsCustomGame({customGameId})", customGameId);
            return new UnauthorizedResult();
        }

        try
        {
            var hints = await hintRepository.GetHints(userId, customGameId, isAnonymous: false, isCustomGame: true);
            logger.LogDebug("-GetHintsCustomGame({customGameId})", customGameId);
            return new OkObjectResult(hints);
        }
        catch (Exception ex)
        {
            logger.LogError("GetHintsCustomGame Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetHintsCustomGame({customGameId})", customGameId);
            return new StatusCodeResult(500);
        }
    }
}
