using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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

    private bool _isDevelopment;

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
            userId = System.Guid.NewGuid().ToString();

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
            MovieDto? guessMovie = await _tmdbRepo.GetMovieById(id);

            if (guessMovie is null)
            {
                _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
                return new NotFoundResult();
            }

            MovieDto? targetMovie = await _tmdbRepo.GetTargetMovie(date);

            if (targetMovie is null)
            {
                _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
                return new NotFoundResult();
            }

            GuessDto? guessDto = _guessRepo.Guess(guessMovie, targetMovie);

            if (guessDto is null)
            {
                _logger.LogError("GuessMovie: unable to create guess DTO");
                _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
                return new NotFoundResult();
            }

            UserGuess? x = _db.Guesses.FirstOrDefault(
                                x => x.GuessMediaId == id && x.GameId == date
                           );

            if (x is null)
            {
                int seqNo = (_db.Guesses.FirstOrDefault(x => x.GameId == date)?.SequenceId ?? 0) + 1;
                _db.Add(new UserGuess
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
