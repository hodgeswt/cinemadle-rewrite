using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cinemadle.Repositories;

public class HintRepository : IHintRepository
{
    private readonly ILogger<HintRepository> _logger;
    private readonly ICacheRepository _cache;
    private readonly CinemadleConfig _config;
    private readonly DatabaseContext _db;
    private readonly ITmdbRepository _tmdbRepo;
    private readonly IGuessRepository _guessRepo;

    private readonly string _hintCacheKeyTemplate = "HintRepository.Hints.{0}.{1}";

    public HintRepository(
        ILogger<HintRepository> logger,
        ICacheRepository cacheRepository,
        IConfigRepository configRepository,
        DatabaseContext db,
        ITmdbRepository tmdbRepo,
        IGuessRepository guessRepo)
    {
        _logger = logger;
        string type = GetType().AssemblyQualifiedName ?? "HintRepository";
        _logger.LogDebug("+ctor({type})", type);

        _cache = cacheRepository;
        _config = configRepository.GetConfig();
        _db = db;
        _tmdbRepo = tmdbRepo;
        _guessRepo = guessRepo;

        _logger.LogDebug("-ctor({type})", type);
    }

    public async Task<Dictionary<string, HintsDto>> GetHints(string userId, string gameId, bool isAnonymous = false, bool isCustomGame = false)
    {
        _logger.LogDebug("+GetHints({userId}, {gameId}, {isAnonymous}, {isCustomGame})", userId, gameId, isAnonymous, isCustomGame);

        string cacheKey = string.Format(_hintCacheKeyTemplate, userId, gameId);

        // Try cache first
        if (_cache.TryGet<Dictionary<string, HintsDto>>(cacheKey, out var cachedHints) && cachedHints is not null)
        {
            _logger.LogDebug("GetHints: Returning cached hints");
            return cachedHints;
        }

        // Try DB
        var storedHint = await _db.UserHints
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GameId == gameId);

        if (storedHint is not null)
        {
            var hints = JsonSerializer.Deserialize<Dictionary<string, HintsDto>>(storedHint.HintsJson);
            if (hints is not null)
            {
                _cache.Set(cacheKey, hints);
                _logger.LogDebug("GetHints: Returning DB-stored hints");
                return hints;
            }
        }

        // Compute hints from guesses
        var computedHints = await ComputeHintsFromGuesses(userId, gameId, isAnonymous, isCustomGame);

        // Store in DB
        if (storedHint is not null)
        {
            storedHint.HintsJson = JsonSerializer.Serialize(computedHints);
            storedHint.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            _db.UserHints.Add(new UserHint
            {
                UserId = userId,
                GameId = gameId,
                HintsJson = JsonSerializer.Serialize(computedHints),
                LastUpdated = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        // Cache
        _cache.Set(cacheKey, computedHints);

        _logger.LogDebug("-GetHints({userId}, {gameId})", userId, gameId);
        return computedHints;
    }

    public void InvalidateHints(string userId, string gameId)
    {
        _logger.LogDebug("+InvalidateHints({userId}, {gameId})", userId, gameId);

        string cacheKey = string.Format(_hintCacheKeyTemplate, userId, gameId);

        // Remove from cache by setting null (cache will handle expiry)
        // We'll also mark DB record for recomputation by deleting it
        var storedHint = _db.UserHints
            .FirstOrDefault(x => x.UserId == userId && x.GameId == gameId);

        if (storedHint is not null)
        {
            _db.UserHints.Remove(storedHint);
            _db.SaveChanges();
        }

        _logger.LogDebug("-InvalidateHints({userId}, {gameId})", userId, gameId);
    }

    private async Task<Dictionary<string, HintsDto>> ComputeHintsFromGuesses(string userId, string gameId, bool isAnonymous, bool isCustomGame)
    {
        var hints = new Dictionary<string, HintsDto>();

        // Get target movie
        MovieDto? targetMovie;
        if (isCustomGame)
        {
            var customGame = await _db.CustomGames.FirstOrDefaultAsync(x => x.Id == gameId);
            if (customGame is null)
            {
                return hints;
            }
            targetMovie = await _tmdbRepo.GetMovieById(customGame.TargetMovieId);
        }
        else
        {
            targetMovie = await _tmdbRepo.GetTargetMovie(gameId);
        }

        if (targetMovie is null)
        {
            return hints;
        }

        // Get user guesses
        List<UserGuess> userGuesses;
        if (isAnonymous)
        {
            userGuesses = await _db.AnonUserGuesses
                .Where(x => x.UserId == userId && x.GameId == gameId)
                .OrderBy(x => x.SequenceId)
                .ToListAsync();
        }
        else
        {
            userGuesses = await _db.Guesses
                .Where(x => x.UserId == userId && x.GameId == gameId)
                .OrderBy(x => x.SequenceId)
                .ToListAsync();
        }

        if (!userGuesses.Any())
        {
            return hints;
        }

        // Convert guesses to GuessDtos
        var guessDtos = new List<GuessDto>();
        foreach (var userGuess in userGuesses)
        {
            var guessMovie = await _tmdbRepo.GetMovieById(userGuess.GuessMediaId);
            if (guessMovie is not null)
            {
                var guessDto = _guessRepo.Guess(guessMovie, targetMovie);
                if (guessDto is not null)
                {
                    guessDtos.Add(guessDto);
                }
            }
        }

        // Compute hints from the guess DTOs
        hints = ComputeHints(guessDtos, targetMovie);

        return hints;
    }

    private Dictionary<string, HintsDto> ComputeHints(List<GuessDto> guesses, MovieDto targetMovie)
    {
        var hints = new Dictionary<string, HintsDto>();

        if (!guesses.Any())
        {
            return hints;
        }

        // Box Office hints
        var boxOfficeHints = ComputeRangeHints(guesses, IGuessRepository.BoxOfficeKey, targetMovie.BoxOffice);
        if (boxOfficeHints != null)
        {
            hints[IGuessRepository.BoxOfficeKey] = boxOfficeHints;
        }

        // Year hints
        if (long.TryParse(targetMovie.Year, out long targetYear))
        {
            var yearHints = ComputeRangeHints(guesses, IGuessRepository.YearKey, targetYear);
            if (yearHints != null)
            {
                hints[IGuessRepository.YearKey] = yearHints;
            }
        }

        // Rating hints
        var ratingHints = ComputeRatingHints(guesses, targetMovie.Rating.ToString());
        if (ratingHints != null)
        {
            hints[IGuessRepository.RatingKey] = ratingHints;
        }

        // Genre hints
        var genreHints = ComputeListHints(guesses, IGuessRepository.GenreKey, targetMovie.Genres);
        if (genreHints != null)
        {
            hints[IGuessRepository.GenreKey] = genreHints;
        }

        // Cast hints
        var castHints = ComputeListHints(guesses, IGuessRepository.CastKey, targetMovie.Cast.Select(x => x.Name));
        if (castHints != null)
        {
            hints[IGuessRepository.CastKey] = castHints;
        }

        // Creatives hints
        var creativesHints = ComputeListHints(guesses, IGuessRepository.CreativesKey, 
            targetMovie.Creatives.Select(x => $"{x.Role ?? "UNK"}: {x.Name ?? "UNK"}"));
        if (creativesHints != null)
        {
            hints[IGuessRepository.CreativesKey] = creativesHints;
        }

        return hints;
    }

    private HintsDto? ComputeRangeHints(List<GuessDto> guesses, string fieldKey, long targetValue)
    {
        long? minBound = null;
        long? maxBound = null;

        long yellowThreshold = fieldKey == IGuessRepository.YearKey
            ? _config.YearYellowThreshold
            : _config.BoxOfficeYellowThreshold;
        long singleArrowThreshold = fieldKey == IGuessRepository.YearKey
            ? _config.YearSingleArrowThreshold
            : _config.BoxOfficeSingleArrowThreshold;

        foreach (var guess in guesses)
        {
            if (!guess.Fields.TryGetValue(fieldKey, out var field))
            {
                continue;
            }

            if (field.Color == "green")
            {
                // Exact match - return the target value
                return new HintsDto { Min = targetValue.ToString(), Max = targetValue.ToString() };
            }

            if (!field.Values.Any() || !long.TryParse(field.Values.First(), out long guessValue))
            {
                continue;
            }

            bool isYellow = field.Color == "yellow";

            if (field.Direction == 1)
            {
                if (isYellow)
                {
                    long newMin = guessValue + 1;
                    long newMax = guessValue + yellowThreshold;
                    minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
                    maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
                }
                else
                {
                    long newMin = guessValue + yellowThreshold + 1;
                    long newMax = guessValue + singleArrowThreshold;
                    minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
                    maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
                }
            }
            else if (field.Direction == 2)
            {
                long newMin = guessValue + singleArrowThreshold + 1;
                minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
            }
            else if (field.Direction == -1)
            {
                if (isYellow)
                {
                    long newMin = guessValue - yellowThreshold;
                    long newMax = guessValue - 1;
                    minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
                    maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
                }
                else
                {
                    long newMin = guessValue - singleArrowThreshold;
                    long newMax = guessValue - yellowThreshold - 1;
                    minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
                    maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
                }
            }
            else if (field.Direction == -2)
            {
                long newMax = guessValue - singleArrowThreshold - 1;
                maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
            }
        }

        if (!minBound.HasValue && !maxBound.HasValue)
        {
            return null;
        }

        if (minBound.HasValue && minBound.Value < 0)
        {
            minBound = 0;
        }

        return new HintsDto
        {
            Min = minBound?.ToString(),
            Max = maxBound?.ToString()
        };
    }

    private HintsDto? ComputeRatingHints(List<GuessDto> guesses, string targetRating)
    {
        var possibleRatings = new HashSet<string>(IGuessRepository.AllRatings);

        foreach (var guess in guesses)
        {
            if (!guess.Fields.TryGetValue(IGuessRepository.RatingKey, out var field))
            {
                continue;
            }

            if (field.Color == "green")
            {
                return new HintsDto { PossibleValues = new List<string> { targetRating } };
            }

            if (!field.Values.Any())
            {
                continue;
            }

            string guessRating = field.Values.First();

            if (field.Color == "grey")
            {
                int guessIndex = IGuessRepository.AllRatings.IndexOf(guessRating);
                if (guessIndex >= 0)
                {
                    possibleRatings.Remove(guessRating);
                    if (guessIndex > 0)
                    {
                        possibleRatings.Remove(IGuessRepository.AllRatings[guessIndex - 1]);
                    }
                    if (guessIndex < IGuessRepository.AllRatings.Count - 1)
                    {
                        possibleRatings.Remove(IGuessRepository.AllRatings[guessIndex + 1]);
                    }
                }
            }
            else if (field.Color == "yellow")
            {
                int guessIndex = IGuessRepository.AllRatings.IndexOf(guessRating);
                if (guessIndex >= 0)
                {
                    var adjacent = new HashSet<string>();
                    if (guessIndex > 0)
                    {
                        adjacent.Add(IGuessRepository.AllRatings[guessIndex - 1]);
                    }
                    if (guessIndex < IGuessRepository.AllRatings.Count - 1)
                    {
                        adjacent.Add(IGuessRepository.AllRatings[guessIndex + 1]);
                    }
                    possibleRatings.IntersectWith(adjacent);
                }
            }
        }

        if (possibleRatings.Count == 0 || possibleRatings.Count == IGuessRepository.AllRatings.Count)
        {
            return null;
        }

        return new HintsDto
        {
            PossibleValues = possibleRatings.OrderBy(r => IGuessRepository.AllRatings.IndexOf(r)).ToList()
        };
    }

    private HintsDto? ComputeListHints(List<GuessDto> guesses, string fieldKey, IEnumerable<string> targetValues)
    {
        var knownValues = new HashSet<string>();

        foreach (var guess in guesses)
        {
            if (!guess.Fields.TryGetValue(fieldKey, out var field))
            {
                continue;
            }

            if (field.Color == "green")
            {
                return new HintsDto { KnownValues = targetValues.ToList() };
            }

            foreach (var modifier in field.Modifiers)
            {
                if (modifier.Value.Contains("bold"))
                {
                    knownValues.Add(modifier.Key);
                }
            }
        }

        if (knownValues.Count == 0)
        {
            return null;
        }

        return new HintsDto
        {
            KnownValues = knownValues.ToList()
        };
    }
}
