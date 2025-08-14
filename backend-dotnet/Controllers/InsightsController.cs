using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cinemadle.Controllers;

[Route("api/insights")]
[ApiController]
public class InsightsController : ControllerBase
{
    private readonly ILogger<InsightsController> _logger;
    private readonly DatabaseContext _db;
    private readonly IdentityContext _identity;
    private readonly ITmdbRepository _tmdbRepo;

    public InsightsController(ILogger<InsightsController> logger, DatabaseContext db, IdentityContext identity, ITmdbRepository tmdbRepo)
    {
        _logger = logger;

        _logger.LogDebug("+InsightsController.ctor");

        _db = db;
        _identity = identity;
        _tmdbRepo = tmdbRepo;

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
    public async Task<ActionResult> GetGuessTimes()
    {
        _logger.LogDebug("+GetGuessTimes()");
        try
        {
            IEnumerable<TimeDomain> guesses = [.. _db.Guesses
                .Concat(_db.AnonUserGuesses)
                .GroupBy(x => x.Inserted)
                .Select(x => new TimeDomain {
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
}