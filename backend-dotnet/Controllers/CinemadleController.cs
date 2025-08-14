using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Cinemadle.Controllers;

[Route("api/cinemadle")]
[ApiController]
public class CinemadleController : ControllerBase
{
    private readonly CinemadleConfig _config;
    private ITmdbRepository _tmdbRepo;
    private IGuessRepository _guessRepo;
    private ILogger<CinemadleController> _logger;
    private DatabaseContext _db;

    private readonly bool _isDevelopment;

    public CinemadleController(
            ILogger<CinemadleController> logger,
            IConfigRepository configRepository,
            ITmdbRepository tmdbRepository,
            IWebHostEnvironment env,
            IGuessRepository guessRepository,
            DatabaseContext db
    )
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "CinemadleController";
        _logger.LogDebug("+ctor({type})", type);

        _db = db;
        _config = configRepository.GetConfig();
        _tmdbRepo = tmdbRepository;
        _guessRepo = guessRepository;
        _isDevelopment = env.IsDevelopment();

        _logger.LogDebug("-ctor({type})", type);
    }

    protected string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [Authorize]
    [HttpGet("validate")]
    public ActionResult<bool> Validate()
    {
        return true;
    }

    [HttpGet("heartbeat")]
    public ActionResult<bool> Heartbeat()
    {
        _logger.LogDebug("Heartbeat");
        return true;
    }

    [HttpGet("anonUserId")]
    public async Task<ActionResult> GetAnonUserId()
    {
        _logger.LogDebug("+GetAnonUserId");

        IEnumerable<AnonUser>? users = null;
        int count = 0;
        string userId = string.Empty;

        while ((users == null || users.Any()) && count < 5)
        {
            userId = Guid.NewGuid().ToString();

            users = _db.AnonUsers
                .Where(x => x.UserId == userId);

            count++;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogDebug("-GetAnonUserId");
            return new StatusCodeResult(500);
        }

        _db.AnonUsers.Add(new()
        {
            UserId = userId
        });

        await _db.SaveChangesAsync();

        _logger.LogDebug("-GetAnonUserId");
        return new OkObjectResult(userId);
    }


    private async Task<ImageDto?> GetBlurredImage(string date, float blurFactor)
    {
        _logger.LogDebug("+GetBlurredImage({date}, {blurFactor})", date, blurFactor);
        MovieDto? targetMovie = await _tmdbRepo.GetTargetMovie(date);
        if (targetMovie is null)
        {
            _logger.LogDebug("GetBlurredImage({date}, {blurFactor}): Unable to find target movie", date, blurFactor);
            _logger.LogDebug("-GetBlurredImage({date}, {blurFactor})", date, blurFactor);
            return null;
        }

        byte[]? imageBytes = await _tmdbRepo.GetMovieImageById(targetMovie.Id);

        if (imageBytes is null)
        {
            _logger.LogDebug("GetBlurredImage({date}, {blurFactor}): Unable to find target movie image", date, blurFactor);
            _logger.LogDebug("-GetBlurredImage({date}, {blurFactor})", date, blurFactor);
            return null;
        }

        using Image image = Image.Load(imageBytes);

        if (blurFactor > 0)
        {
            image.Mutate(x => x.GaussianBlur(blurFactor));
        }
        
        string base64 = image.ToBase64String(PngFormat.Instance);

        return new ImageDto
        {
            ImageData = base64
        };
    }

    private static string MapColorToEmoji(string color)
    {
        return color switch
        {
            "green" => "🟩",
            "yellow" => "🟨",
            _ => "⬛",
        };
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
        _logger.LogDebug("+GetGameSummaryAnon({date}, {userId})", date, userId);

        string anonUserId = userId.ToString();
        AnonUser? user = _db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            _logger.LogWarning("GetGameSummaryAnon: attempted access by invalid user: {userId}", anonUserId);
            _logger.LogDebug("-GetGameSummaryAnon({date}, {userId}", date, userId);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> userGuesses = _db.AnonUserGuesses.Where(x => x.UserId == userId.ToString() && x.GameId == date).OrderBy(x => x.SequenceId);

            if (userGuesses.Count() < _config.GameLength)
            {
                _logger.LogDebug("-GetGameSummary({date}, {userId}): User tried summary gen on guess {guess}", date, userId, userGuesses.Count());
                _logger.LogDebug("-GetGameSummary({date}, {userId})", date, userId);
                return new NotFoundResult();
            }

            List<string>? gameSummary = await GetGameSummaryInternal(userGuesses.Select(x => x.GuessMediaId), date);

            if (gameSummary is null || gameSummary.Count == 0)
            {
                _logger.LogDebug("-GetGameSummary({date}, {userId}): Unable to make guess data", date, userId);
                _logger.LogDebug("-GetGameSummary({date}, {userId})", date, userId);
                return new NotFoundResult();
            }

            return new OkObjectResult(new GameSummaryDto
            {
                Summary = gameSummary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetGameSummary Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GetGameSummary({date}, {userId})", date, userId);

            return new StatusCodeResult(500);
        }
    }


    [Authorize]
    [HttpGet("gameSummary")]
    public async Task<ActionResult> GetGameSummary(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        _logger.LogDebug("+GetGameSummary({date})", date);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogDebug("-GetGameSummary({date})", date);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> userGuesses = _db.Guesses.Where(x => x.UserId == userId && x.GameId == date).OrderBy(x => x.SequenceId);

            if (userGuesses.Count() < _config.GameLength)
            {
                _logger.LogDebug("-GetGameSummary({date}): User tried summary gen on guess {guess}", date, userGuesses.Count());
                _logger.LogDebug("-GetGameSummary({date})", date);
                return new NotFoundResult();
            }

            List<string>? gameSummary = await GetGameSummaryInternal(userGuesses.Select(x => x.GuessMediaId), date);

            if (gameSummary is null || gameSummary.Count == 0)
            {
                _logger.LogDebug("-GetGameSummary({date}): Unable to make guess data", date);
                _logger.LogDebug("-GetGameSummary({date})", date);
                return new NotFoundResult();
            }

            return new OkObjectResult(new GameSummaryDto
            {
                Summary = gameSummary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetGameSummary Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GetGameSummary({date})", date);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("target/image")]
    public async Task<ActionResult> GetMovieImage(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        _logger.LogDebug("+GetMovieImage({date})", date);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogDebug("-GetMovieImage({date})", date);
            return new UnauthorizedResult();
        }

        try
        {
            int userGuesses = _db.Guesses.Where(x => x.GameId == date && x.UserId == userId).Count();

            if (!_config.MovieImageBlurFactors.ContainsKey(userGuesses.ToString()))
            {
                _logger.LogDebug("GetMovieImage({date}): User attempted to access image on guess {number}", date, userGuesses);
                _logger.LogDebug("-GetMovieImage({date})", date);
                return new UnauthorizedResult();
            }

            float blurFactor = userGuesses >= _config.GameLength ? 0.0F : _config.MovieImageBlurFactors[userGuesses.ToString()];

            ImageDto? image = await GetBlurredImage(date, blurFactor);

            if (image is null)
            {
                _logger.LogDebug("GetMovieImage({date}): No image found", date);
                _logger.LogDebug("-GetMovieImage({date})", date);
                return new NotFoundResult();
            }

            Clue? clue = _db.UserClues.Where(x => x.GameId == date && x.UserId == userId && x.ClueType == ClueType.Visual).FirstOrDefault();
            if (clue is null)
            {
                try
                {
                    _db.UserClues.Add(new Clue
                    {
                        UserId = userId,
                        GameId = date,
                        ClueType = ClueType.Visual,
                        Inserted = DateTime.Now
                    });

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                     _logger.LogError("GetMovieImage Unable to save to DB. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
                    _logger.LogDebug("-GetMovieImage({date})", date);

                    return new StatusCodeResult(500);
                }
                
            }

            _logger.LogDebug("-GetMovieImage({date})", date);
            return new OkObjectResult(image);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetMovieImage Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GetMovieImage({date})", date);

            return new StatusCodeResult(500);
        }
    }

    [HttpGet("target")]
    public async Task<ActionResult> GetTargetMovie(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        _logger.LogDebug("+GetTargetMovie({date})", date);
        try
        {
            MovieDto? targetMovie = await _tmdbRepo.GetTargetMovie(date);

            if (targetMovie is null)
            {
                return new NotFoundResult();
            }

            _logger.LogDebug("-GetTargetMovie({date})", date);
            return new OkObjectResult(targetMovie);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTargetMovie Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            _logger.LogDebug("-GetTargetMovie({date}", date);

            return new StatusCodeResult(500);

        }
    }

    [HttpGet("movielist")]
    public async Task<ActionResult> GetMovieList()
    {
        _logger.LogDebug("+GetMovieList");

        try
        {
            Dictionary<string, int> movies = await _tmdbRepo.GetMovieList();
            return new OkObjectResult(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetMovieList Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);

            _logger.LogDebug("-GetMovieList");
            return new StatusCodeResult(500);
        }
    }

    [HttpGet("guesses/anon")]
    public ActionResult GetPastGuessesAnon(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
        [FromQuery, Required] Guid userId
    )
    {
        _logger.LogDebug("+GetPastGuessesAnon({date}, {userId})", date, userId);

        string anonUserId = userId.ToString();
        AnonUser? user = _db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            _logger.LogWarning("GetPastGuessesAnon: attempted access by invalid user: {userId}", anonUserId);
            _logger.LogDebug("-GetPastGuessesAnon({date}, {userId}", date, userId);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> guesses = _db.AnonUserGuesses.Where(
                x => x.GameId == date && x.UserId == userId.ToString()
            )
            .OrderBy(x => x.SequenceId);

            _logger.LogDebug("-GetPastGuessesAnon({date}, {userId})", date, userId);
            return new OkObjectResult(guesses.Select(x => x.GuessMediaId));
        }
        catch (Exception ex)
        {
            _logger.LogError("GetPastGuesses Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            _logger.LogDebug("-GetPastGuesses({date}, {userId})", date, userId);

            return new StatusCodeResult(500);
        }
    }

    [Authorize]
    [HttpGet("guesses")]
    public ActionResult GetPastGuesses(
        [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        _logger.LogDebug("+GetPastGuesses({date})", date);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogDebug("-GetPastGuesses({date})", date);
            return new UnauthorizedResult();
        }

        try
        {
            IEnumerable<UserGuess> guesses = _db.Guesses.Where(
                x => x.GameId == date && x.UserId == userId
            )
            .OrderBy(x => x.SequenceId);

            _logger.LogDebug("GetPastGuesses({date}): {data}", date, guesses.Count());

            _logger.LogDebug("-GetPastGuesses({date})", date);
            return new OkObjectResult(guesses.Select(x => x.GuessMediaId));
        }
        catch (Exception ex)
        {
            _logger.LogError("GetPastGuesses Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            _logger.LogDebug("-GetPastGuesses({date})", date);

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
        _logger.LogDebug("+GuessMovieAnon({date}, {userId}, {id}", date, userId, id);

        string anonUserId = userId.ToString();
        AnonUser? user = _db.AnonUsers.Where(x => x.UserId == anonUserId).FirstOrDefault();

        if (user is null)
        {
            _logger.LogWarning("GuessMovieAnon: attempted access by invalid user: {userId}", anonUserId);
            _logger.LogDebug("-GuessMovieAnon({date}, {userId}, {id}", date, userId, id);
            return new UnauthorizedResult();
        }

        try
        {
            GuessDto? guessDto = await GuessMovieInternal(id, date);
            UserGuess? x = _db.AnonUserGuesses.FirstOrDefault(
                                           x => x.GuessMediaId == id && x.GameId == date && x.UserId == anonUserId
                                      );

            if (x is null)
            {
                int seqNo = (_db.AnonUserGuesses.Where(x => x.GameId == date).OrderByDescending(x => x.SequenceId).FirstOrDefault()?.SequenceId ?? 0) + 1;
                _db.AnonUserGuesses.Add(new UserGuess
                {
                    GameId = date,
                    UserId = anonUserId,
                    GuessMediaId = id,
                    SequenceId = seqNo,
                    Inserted = DateTime.Now,
                });

                await _db.SaveChangesAsync();
            }

            _logger.LogDebug("-GuessMovieAnon({date}, {userId}, {id})", date, userId, id);
            return new OkObjectResult(guessDto);

        }
        catch (Exception ex)
        {
            _logger.LogError("GuessMovieAnon Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GuessMovieAnon({date}, {userId}, {id})", date, userId, id);

            return new StatusCodeResult(500);
        }
    }

    private async Task<GuessDto?> GuessMovieInternal(int id, string date)
    {
        MovieDto? guessMovie = await _tmdbRepo.GetMovieById(id);

        if (guessMovie is null)
        {
            _logger.LogDebug("-GuessMovieInternal({date}, {id})", date, id);
            return null;
        }

        MovieDto? targetMovie = await _tmdbRepo.GetTargetMovie(date);

        if (targetMovie is null)
        {
            _logger.LogDebug("-GuessMovieInternal({date}, {id})", date, id);
            return null;
        }

        GuessDto? guessDto = _guessRepo.Guess(guessMovie, targetMovie);

        if (guessDto is null)
        {
            _logger.LogError("GuessMovieInternal: unable to create guess DTO");
            _logger.LogDebug("-GuessMovieInternal({date}, {id})", date, id);
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
        _logger.LogDebug("+GuessMovie({date}, {id})", date, id);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
            return new UnauthorizedResult();
        }

        try
        {

            GuessDto? guessDto = await GuessMovieInternal(id, date);

            UserGuess? x = _db.Guesses.FirstOrDefault(
                x => x.GuessMediaId == id && x.GameId == date && x.UserId == userId
            );

            if (x is null)
            {
                int seqNo = (_db.Guesses.Where(
                    x => x.GameId == date && x.UserId == userId
                )
                .OrderByDescending(x => x.SequenceId)
                .FirstOrDefault()?.SequenceId ?? 0) + 1;

                _db.Guesses.Add(new UserGuess
                {
                    GameId = date,
                    UserId = userId,
                    GuessMediaId = id,
                    SequenceId = seqNo,
                    Inserted = DateTime.Now,
                });

                await _db.SaveChangesAsync();
            }

            _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
            return new OkObjectResult(guessDto);

        }
        catch (Exception ex)
        {
            _logger.LogError("GuessMovie Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GuessMovie({date}, {id})", date, id);

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

        MovieDto? movie = await _tmdbRepo.GetMovie(movieName);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(movie!);
    }
}
