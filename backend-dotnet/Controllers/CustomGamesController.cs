using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinemadle.Controllers;

[Route("custom")]
public class CustomGamesController(
    ILogger<CustomGamesController> logger,
    IOptions<CinemadleConfig> options,
    ITmdbRepository tmdbRepo,
    IGuessRepository guessRepo,
    IHintRepository hintRepo,
    IFeatureFlagRepository flagRepo,
    DatabaseContext db
)
    : CinemadleControllerBase
{

    private readonly CinemadleConfig _config = options.Value;
    
    [Authorize]
    [HttpPost("create")]
    public async Task<ActionResult> CreateCustomGame([FromBody] CustomGameCreateDto customGame)
    {
        logger.LogDebug("+CreateCustomGame({customGame})", customGame);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-CreateCustomGame({customGame})", customGame);
            return new UnauthorizedResult();
        }

        try
        {
            MovieDto? movie = await tmdbRepo.GetMovieById(customGame.Id);
            if (movie is null)
            {
                logger.LogDebug("-CreateCustomGame({customGame})", customGame);
                return new NotFoundResult();
            }

            CustomGame newCustomGame = new()
            {
                Id = Guid.NewGuid().ToString(),
                TargetMovieId = customGame.Id,
                CreatorUserId = userId,
                Inserted = DateTime.UtcNow
            };

            db.CustomGames.Add(newCustomGame);

            await db.SaveChangesAsync();

            logger.LogDebug("-CreateCustomGame()");
            return new OkObjectResult(new CustomGameDto
            {
                Id = newCustomGame.Id,
                TargetMovieId = newCustomGame.TargetMovieId
            });
        }
        catch (Exception ex)
        {
            logger.LogError("CreateCustomGame Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-CreateCustomGame({customGame})", customGame);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult> GetCustomGame(string id)
    {
        logger.LogDebug("+GetCustomGame({id})", id);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetCustomGame({id})", id);
            return new UnauthorizedResult();
        }

        try
        {
            CustomGame? customGame = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == id);
            if (customGame is null)
            {
                logger.LogDebug("-GetCustomGame({id})", id);
                return new NotFoundResult();
            }

            logger.LogDebug("-GetCustomGame({id})", id);
            return new OkObjectResult(new CustomGameDto
            {
                Id = customGame.Id,
                TargetMovieId = customGame.TargetMovieId
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetCustomGame Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetCustomGame({id})", id);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("{customGameId}/target")]
    public async Task<ActionResult> GetCustomGameTarget(string customGameId)
    {
        logger.LogDebug("+GetCustomGameTarget({customGameId})", customGameId);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetCustomGameTarget({customGameId})", customGameId);
            return new UnauthorizedResult();
        }

        try
        {
            CustomGame? customGame = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == customGameId);
            if (customGame is null)
            {
                logger.LogDebug("-GetCustomGameTarget({customGameId}): Custom game not found", customGameId);
                return new NotFoundResult();
            }

            MovieDto? targetMovie = await tmdbRepo.GetMovieById(customGame.TargetMovieId);
            if (targetMovie is null)
            {
                logger.LogDebug("-GetCustomGameTarget({customGameId}): Target movie not found", customGameId);
                return new NotFoundResult();
            }

            logger.LogDebug("-GetCustomGameTarget({customGameId})", customGameId);
            return new OkObjectResult(targetMovie);
        }
        catch (Exception ex)
        {
            logger.LogError("GetCustomGameTarget Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetCustomGameTarget({customGameId})", customGameId);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("{customGameId}/target/image")]
    public async Task<ActionResult> GetCustomGameImage(string customGameId)
    {
        logger.LogDebug("+GetCustomGameImage({customGameId})", customGameId);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);
            return new UnauthorizedResult();
        }

        bool paymentsEnabled = await flagRepo.Get(nameof(FeatureFlags.PaymentsEnabled));

        if (paymentsEnabled)
        {
            UserAccount? userAccount = db.UserAccounts.Include(x => x.AddOns).FirstOrDefault(x => x.UserId == userId);
            if (userAccount is null)
            {
                logger.LogDebug("GetCustomGameImage({customGameId}): user account does not exist", customGameId);
                logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);
                return new NotFoundResult();
            }

            AddOnRecord? addOn = userAccount.AddOns.FirstOrDefault(x => x.AddOn == AddOn.VisualClue);
            if ((addOn?.Count ?? 0) <= 0)
            {
                logger.LogDebug("GetCustomGameImage({customGameId}): user had no visual clues", customGameId);
                logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);
                return new UnauthorizedResult();
            }
        }

        try
        {
            CustomGame? customGame = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == customGameId);
            if (customGame is null)
            {
                logger.LogDebug("-GetCustomGameImage({customGameId}): Custom game not found", customGameId);
                return new NotFoundResult();
            }

            int userGuesses = db.Guesses.Where(x => x.GameId == customGameId && x.UserId == userId).Count();

            if (!_config.MovieImageBlurFactors.ContainsKey(userGuesses.ToString()))
            {
                logger.LogDebug("GetCustomGameImage({customGameId}): User attempted to access image on guess {number}", customGameId, userGuesses);
                logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);
                return new UnauthorizedResult();
            }

            bool win = db.Guesses.Where(x => x.GameId == customGameId && x.UserId == userId && x.GuessMediaId == customGame.TargetMovieId).Any();

            float blurFactor = (userGuesses >= _config.GameLength || win)
                ? 0.0F
                : _config.MovieImageBlurFactors[userGuesses.ToString()];

            ImageDto? image = await GetBlurredImageForMovie(customGame.TargetMovieId, blurFactor, tmdbRepo);

            if (image is null)
            {
                logger.LogDebug("GetCustomGameImage({customGameId}): No image found", customGameId);
                logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);
                return new NotFoundResult();
            }

            Clue? clue = db.UserClues.FirstOrDefault(x => x.GameId == customGameId && x.UserId == userId && x.ClueType == ClueType.Visual);
            if (clue is null)
            {
                try
                {
                    db.UserClues.Add(new Clue
                    {
                        UserId = userId,
                        GameId = customGameId,
                        ClueType = ClueType.Visual,
                        Inserted = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError("GetCustomGameImage Unable to save to DB. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
                    logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);

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

            logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);
            return new OkObjectResult(image);
        }
        catch (Exception ex)
        {
            logger.LogError("GetCustomGameImage Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetCustomGameImage({customGameId})", customGameId);

            return new StatusCodeResult(500);
        }
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
        logger.LogDebug("+GuessMovieCustomGame({customGameId}, {movieId})", customGameId, movieId);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId})", customGameId, movieId);
            return new UnauthorizedResult();
        }

        try
        {
            // Verify custom game exists and get target movie
            CustomGame? customGame = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == customGameId);
            if (customGame is null)
            {
                logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId}): Custom game not found", customGameId, movieId);
                return new NotFoundResult();
            }

            // Get the guess movie
            MovieDto? guessMovie = await tmdbRepo.GetMovieById(movieId);
            if (guessMovie is null)
            {
                logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId}): Guess movie not found", customGameId, movieId);
                return new NotFoundResult();
            }

            // Get the target movie
            MovieDto? targetMovie = await tmdbRepo.GetMovieById(customGame.TargetMovieId);
            if (targetMovie is null)
            {
                logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId}): Target movie not found", customGameId, movieId);
                return new NotFoundResult();
            }

            // Create guess DTO
            GuessDto? guessDto = guessRepo.Guess(guessMovie, targetMovie);
            if (guessDto is null)
            {
                logger.LogError("GuessMovieCustomGame: unable to create guess DTO");
                logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId})", customGameId, movieId);
                return new StatusCodeResult(500);
            }

            // Check if this guess already exists
            UserGuess? existingGuess = db.Guesses.FirstOrDefault(
                x => x.GuessMediaId == movieId && x.GameId == customGameId && x.UserId == userId
            );

            // If not, save the guess
            if (existingGuess is null)
            {
                int seqNo = (db.Guesses.Where(
                    x => x.GameId == customGameId && x.UserId == userId
                )
                .OrderByDescending(x => x.SequenceId)
                .FirstOrDefault()?.SequenceId ?? 0) + 1;

                db.Guesses.Add(new UserGuess
                {
                    GameId = customGameId,
                    UserId = userId,
                    GuessMediaId = movieId,
                    SequenceId = seqNo,
                    Inserted = DateTime.Now,
                });

                await db.SaveChangesAsync();

                // Invalidate hints cache after new guess
                hintRepo.InvalidateHints(userId, customGameId);
            }

            logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId})", customGameId, movieId);
            return new OkObjectResult(guessDto);
        }
        catch (Exception ex)
        {
            logger.LogError("GuessMovieCustomGame Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GuessMovieCustomGame({customGameId}, {movieId})", customGameId, movieId);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("{customGameId}/gameSummary")]
    public async Task<ActionResult> GetGameSummaryCustomGame(string customGameId)
    {
        logger.LogDebug("+GetGameSummaryCustomGame({customGameId})", customGameId);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogDebug("-GetGameSummaryCustomGame({customGameId})", customGameId);
            return new UnauthorizedResult();
        }

        try
        {
            // Verify custom game exists
            CustomGame? customGame = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == customGameId);
            if (customGame is null)
            {
                logger.LogDebug("-GetGameSummaryCustomGame({customGameId}): Custom game not found", customGameId);
                return new NotFoundResult();
            }

            IEnumerable<UserGuess> userGuesses = db.Guesses.Where(
                x => x.UserId == userId && x.GameId == customGameId
            ).OrderBy(x => x.SequenceId);

            // Get target movie for the custom game
            MovieDto? targetMovie = await tmdbRepo.GetMovieById(customGame.TargetMovieId);
            if (targetMovie is null)
            {
                logger.LogDebug("-GetGameSummaryCustomGame({customGameId}): Target movie not found", customGameId);
                return new NotFoundResult();
            }

            // Check if user has won
            bool hasWon = userGuesses.Any(x => x.GuessMediaId == customGame.TargetMovieId);

            // Allow summary if user has completed the game OR won
            if (userGuesses.Count() < _config.GameLength && !hasWon)
            {
                logger.LogDebug("-GetGameSummaryCustomGame({customGameId}): User tried summary gen on guess {guess}", customGameId, userGuesses.Count());
                logger.LogDebug("-GetGameSummaryCustomGame({customGameId})", customGameId);
                return new NotFoundResult();
            }

            // Generate summary using the existing logic
            var guessDtos = await Task.WhenAll(userGuesses.Select(async x => 
            {
                MovieDto? guessMovie = await tmdbRepo.GetMovieById(x.GuessMediaId);
                if (guessMovie is null) return null;
                return guessRepo.Guess(guessMovie, targetMovie);
            }));

            if (guessDtos is null || !guessDtos.All(x => x is not null))
            {
                logger.LogDebug("-GetGameSummaryCustomGame({customGameId}): Unable to make guess data", customGameId);
                return new NotFoundResult();
            }

            List<string> gameSummary = [];
            foreach (GuessDto? guessDto in guessDtos)
            {
                if (guessDto is null)
                {
                    return new NotFoundResult();
                }

                gameSummary.Add(string.Join("", guessDto.Fields.Select(x => MapColorToEmoji(x.Value.Color))));
            }

            gameSummary.Add($"play at https://cinemadle.com/customGame/{customGameId}");

            logger.LogDebug("-GetGameSummaryCustomGame({customGameId})", customGameId);
            return new OkObjectResult(new GameSummaryDto
            {
                Summary = gameSummary
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetGameSummaryCustomGame Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetGameSummaryCustomGame({customGameId})", customGameId);

            return new StatusCodeResult(500);
        }
    }
}