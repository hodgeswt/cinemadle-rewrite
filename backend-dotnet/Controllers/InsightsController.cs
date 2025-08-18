using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cinemadle.Controllers;

[Route("api/insights")]
[ApiController]
public class InsightsController : CinemadleControllerBase
{
    private readonly ILogger<InsightsController> _logger;
    private readonly DatabaseContext _db;
    private readonly IdentityContext _identity;
    private readonly ITmdbRepository _tmdbRepo;
    private readonly UserManager<IdentityUser> _userManager;

    public InsightsController(
        ILogger<InsightsController> logger,
        DatabaseContext db,
        IdentityContext identity,
        ITmdbRepository tmdbRepo,
        UserManager<IdentityUser> userManager
    )
    {
        _logger = logger;

        _logger.LogDebug("+InsightsController.ctor");

        _db = db;
        _identity = identity;
        _tmdbRepo = tmdbRepo;
        _userManager = userManager;

        _logger.LogDebug("-InsightsController.ctor");
    }

    [HttpGet("guesses")]
    public async Task<ActionResult> GetTopGuesses()
    {
        _logger.LogDebug("+GetTopGuesses");
        try
        {
            IEnumerable<GuessDataDto> guesses = [.. _db.Guesses
                .Concat(_db.AnonUserGuesses)
                .GroupBy(x => x.GuessMediaId)
                .Select(x => new GuessDataDto
                {
                    GuessMediaId = x.Key,
                    GuessCount = x.Count()
                })
            ];

            foreach (GuessDataDto guess in guesses)
            {
                _logger.LogDebug("GetTopGuesses(): found guess {id}", guess.GuessMediaId);
                MovieDto? movie = await _tmdbRepo.GetMovieById(guess.GuessMediaId);

                if (movie is not null)
                {
                    guess.Movie = movie;
                }
                else
                {
                    _logger.LogDebug("GetTopGuesses(): unable to get title for guess {id}", guess.GuessMediaId);
                }
            }

            _logger.LogDebug("-GetTopGuesses()");
            return new OkObjectResult(guesses);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTopGuesses Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GetTopGuesses()");

            return new StatusCodeResult(500);
        }
    }


    [HttpGet("times")]
    public ActionResult GetGuessTimes()
    {
        _logger.LogDebug("+GetGuessTimes()");
        try
        {
            IEnumerable<Time> guesses = [.. _db.Guesses
                .Concat(_db.AnonUserGuesses)
                .GroupBy(x => x.Inserted)
                .Select(x => new Time {
                    DateTime = x.Key,
                    DayOfWeek = x.Key.DayOfWeek,
                    TimeOnly = TimeOnly.FromDateTime(x.Key),
                    Count = x.Count()
                })
            ];

            var modeDay = guesses
                .GroupBy(x => x.DayOfWeek)
                .OrderByDescending(g => g.Sum(x => x.Count))
                .First()
                .Key;

            var modeTime = guesses
                .OrderByDescending(x => x.Count)
                .First()
                .TimeOnly;

            var meanDayValue = guesses
                .SelectMany(x => Enumerable.Repeat((int)x.DayOfWeek, x.Count))
                .Average();
            var meanDay = (DayOfWeek)Math.Round(meanDayValue);

            var meanMinutes = guesses
                .SelectMany(x => Enumerable.Repeat(x.TimeOnly.ToTimeSpan().TotalMinutes, x.Count))
                .Average();
            var meanTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(meanMinutes));

            var expandedDays = guesses
                .SelectMany(x => Enumerable.Repeat(x.DayOfWeek, x.Count))
                .OrderBy(x => x)
                .ToList();
            var medianDay = expandedDays[expandedDays.Count / 2];

            var expandedTimes = guesses
                .SelectMany(x => Enumerable.Repeat(x.TimeOnly, x.Count))
                .OrderBy(x => x)
                .ToList();
            var medianTime = expandedTimes[expandedTimes.Count / 2];

            var minTime = guesses.Min(x => x.TimeOnly);
            var maxTime = guesses.Max(x => x.TimeOnly);

            var guessTimeDto = new GuessTimeDto
            {
                MeanDay = meanDay,
                MedianDay = medianDay,
                ModeDay = modeDay,
                MeanTime = meanTime,
                MedianTime = medianTime,
                ModeTime = modeTime,
                MinTime = minTime,
                MaxTime = maxTime
            };

            _logger.LogDebug("-GetGuessTimes()");
            return new OkObjectResult(guessTimeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetGuessTimes Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            _logger.LogDebug("-GetGuessTimes()");

            return new StatusCodeResult(500);
        }
    }

    [HttpGet("user/{email}")]
    [Authorize]
    public async Task<ActionResult> GetUserData(string email)
    {
        _logger.LogDebug("+GetUserData({email})", email);
        string? userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogDebug("-GetUserData({email})", email);
            return new UnauthorizedResult();
        }

        IdentityUser? requestingUser = _identity.Users.Where(x => x.Id == userId).FirstOrDefault();

        if (requestingUser is null)
        {
            _logger.LogDebug("-GetUserData({email})", email);
            return new NotFoundResult();
        }

        if (requestingUser.Email != email && !UserHasRole(nameof(CustomRoles.Admin)))
        {
            _logger.LogDebug("-GetUserData({email})", email);
            return new UnauthorizedResult();
        }

        IdentityUser? targetUser;

        if (requestingUser.Email == email)
        {
            targetUser = requestingUser;
        }
        else
        {
            targetUser = await _userManager.FindByEmailAsync(email);
        }

        if (targetUser is null)
        {
            _logger.LogDebug("-GetUserData({email})", email);
            return new NotFoundResult();
        }

        IEnumerable<UserGuess> userGuesses = _db.Guesses.Where(x => x.UserId == targetUser.Id);

        long gamesPlayed = userGuesses.GroupBy(x => x.GameId).Count();
        var distinctGuesses = userGuesses.GroupBy(x => x.GuessMediaId);

        return new OkObjectResult(new UserDataDto
        {
            Email = email,
            GamesPlayed = gamesPlayed,
            DistinctGuesses = distinctGuesses.Select(x => x.Key),
            TotalGuesses = userGuesses.Count()
        });
    }
}