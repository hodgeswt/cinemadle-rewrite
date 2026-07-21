using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System.ComponentModel.DataAnnotations;
using Cinemadle.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    private async Task<List<string>?> GetGameSummaryInternal(IEnumerable<int> userGuesses, string date)
    {
        var guessDtos = await Task.WhenAll(userGuesses.Select(x => GuessMovieInternal(x, date)));
        if (guessDtos is null || !guessDtos.All(x => x is not null))
        {
            return null;
        }

        List<string> o = [];
        foreach (GuessDto? guessDto in guessDtos)
        {
            if (guessDto is null)
            {
                return null;
            }

            o.Add(string.Join("", guessDto.Fields.Select(x => MapColorToEmoji(x.Value.Color))));
        }

        o.Add($"cinemadle {date}");
        o.Add("play at https://cinemadle.com");

        return o;
    }

    [HttpGet("gameSummary/anon")]
    public async Task<ActionResult> GetGameSummaryAnon(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
        [FromQuery, Required] Guid userId
    )
    {
        logger.LogDebug("+GetGameSummaryAnon({date}, {userId})", date, userId);

        string anonUserId = userId.ToString();
        AnonUser? user = db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            logger.LogWarning("GetGameSummaryAnon: attempted access by invalid user: {userId}", anonUserId);
            logger.LogDebug("-GetGameSummaryAnon({date}, {userId}", date, userId);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> userGuesses = db.AnonUserGuesses.Where(x => x.UserId == userId.ToString() && x.GameId == date).OrderBy(x => x.SequenceId);

            // Check if user has won
            MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(date);
            bool hasWon = targetMovie != null && userGuesses.Any(x => x.GuessMediaId == targetMovie.Id);

            // Allow summary if user has completed the game OR won
            if (userGuesses.Count() < _config.GameLength && !hasWon)
            {
                logger.LogDebug("-GetGameSummaryAnon({date}, {userId}): User tried summary gen on guess {guess}", date, userId, userGuesses.Count());
                logger.LogDebug("-GetGameSummaryAnon({date}, {userId})", date, userId);
                return new NotFoundResult();
            }

            List<string>? gameSummary = await GetGameSummaryInternal(userGuesses.Select(x => x.GuessMediaId), date);

            if (gameSummary is null || gameSummary.Count == 0)
            {
                logger.LogDebug("-GetGameSummary({date}, {userId}): Unable to make guess data", date, userId);
                logger.LogDebug("-GetGameSummary({date}, {userId})", date, userId);
                return new NotFoundResult();
            }

            return new OkObjectResult(new GameSummaryDto
            {
                Summary = gameSummary
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetGameSummary Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetGameSummary({date}, {userId})", date, userId);

            return new StatusCodeResult(500);
        }
    }


    [Authorize]
    [HttpGet("gameSummary")]
    public async Task<ActionResult> GetGameSummary(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        logger.LogDebug("+GetGameSummary({date})", date);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetGameSummary({date})", date);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> userGuesses = db.Guesses.Where(x => x.UserId == userId && x.GameId == date).OrderBy(x => x.SequenceId);

            // Check if user has won
            MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(date);
            bool hasWon = targetMovie != null && userGuesses.Any(x => x.GuessMediaId == targetMovie.Id);

            // Allow summary if user has completed the game OR won
            if (userGuesses.Count() < _config.GameLength && !hasWon)
            {
                logger.LogDebug("-GetGameSummary({date}): User tried summary gen on guess {guess}", date, userGuesses.Count());
                logger.LogDebug("-GetGameSummary({date})", date);
                return new NotFoundResult();
            }

            List<string>? gameSummary = await GetGameSummaryInternal(userGuesses.Select(x => x.GuessMediaId), date);

            if (gameSummary is null || gameSummary.Count == 0)
            {
                logger.LogDebug("-GetGameSummary({date}): Unable to make guess data", date);
                logger.LogDebug("-GetGameSummary({date})", date);
                return new NotFoundResult();
            }

            return new OkObjectResult(new GameSummaryDto
            {
                Summary = gameSummary
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetGameSummary Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetGameSummary({date})", date);

            return new StatusCodeResult(500);
        }
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

        bool paymentsEnabled = await flagRepo.Get(nameof(FeatureFlags.PaymentsEnabled));

        if (paymentsEnabled)
        {
            UserAccount? userAccount = db.UserAccounts.Include(x => x.AddOns).FirstOrDefault(x => x.UserId == userId);
            if (userAccount is null)
            {
                logger.LogDebug("GetMovieImage({date}): user account does not exist", date);
                logger.LogDebug("-GetMovieImage({date})", date);
                return new NotFoundResult();
            }

            AddOnRecord? addOn = userAccount.AddOns.FirstOrDefault(x => x.AddOn == AddOn.VisualClue);
            if ((addOn?.Count ?? 0) <= 0)
            {
                logger.LogDebug("GetMovieImage({date}): user had no visual clues", date);
                logger.LogDebug("-GetMovieImage({date})", date);
                return new UnauthorizedResult();
            }
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

            if (paymentsEnabled)
            {
                AddOnRecord? record = db.UserAccounts.FirstOrDefault(x => x.UserId == userId)?.AddOns.FirstOrDefault(x => x.AddOn == AddOn.VisualClue);

                if (record is null)
                {
                    return new StatusCodeResult(500);
                }

                if (clue is null)
                {
                    record.Count -= 1;
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

    [HttpGet("movie/{movieName}")]
    public async Task<ActionResult> GetMovie(string movieName)
    {
        if (!_isDevelopment)
        {
            return new NotFoundResult();
        }

        MovieDto? movie = await tmdbRepository.GetMovie(movieName);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(movie!);
    }

    
}
