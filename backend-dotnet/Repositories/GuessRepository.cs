using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using System.Reflection;

namespace Cinemadle.Repositories;

public class GuessRepository : IGuessRepository
{
    private readonly ILogger<GuessRepository> _logger;
    private readonly ICacheRepository _cache;
    private readonly CinemadleConfig _config;
    private readonly DatabaseContext _db;

    private readonly string _guessCacheKeyTemplate = "GuessRepository.Guess.{0}.{1}";

    public GuessRepository(ILogger<GuessRepository> logger, ICacheRepository cacheRepository, IConfigRepository configRepository, DatabaseContext db)
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "GuessRepository";
        _logger.LogDebug("+ctor({type})", type);

        _cache = cacheRepository;
        _config = configRepository.GetConfig();
        _db = db;

        _logger.LogDebug("-ctor({type})", type);
    }

    public static string CreativeFromPerson(PersonDto person)
    {
        return $"{person.Role ?? "UNK"}: {person.Name ?? "UNK"}";
    }

    private void ApplyDataOverrides(ref MovieDto guess)
    {
        try
        {
            int id = guess.Id;
            IEnumerable<DataOverride> overrides = _db.DataOverrides
                .Where(x => x.MovieId == id);

            if (!overrides.Any())
            {
                _logger.LogInformation("Did not override any fields for movie {id}", id);
                return;
            }

            foreach (DataOverride o in overrides)
            {
                Type t = guess.GetType();
                PropertyInfo? f = t.GetProperty(o.Category);

                if (f is null)
                {
                    _logger.LogWarning("Unable to override field {fieldName}", o.Category);
                    continue;
                }

                Type fieldType = f.PropertyType;

                try
                {
                    object v = Convert.ChangeType(o.Data, fieldType);

                    f.SetValue(guess, v);
                }
                catch
                {
                    _logger.LogWarning("Unable to override field {fieldName} of type {fieldType} with data {data}", o.Category, nameof(fieldType), o.Data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Guess: failed to override data. Exception: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
        }
    }

    public GuessDto Guess(MovieDto guess, MovieDto target, IEnumerable<GuessDto>? previousGuesses = null)
    {
        string cacheKey = string.Format(_guessCacheKeyTemplate, guess.Id, target.Id);
        // Note: We don't use cache when previousGuesses are provided since hints depend on them
        if (previousGuesses == null && _cache.TryGet<GuessDto>(cacheKey, out GuessDto? guessDto) && guessDto is not null)
        {
            _logger.LogDebug("Guess: Returning cached guess data");
            return guessDto;
        }

        ApplyDataOverrides(ref guess);

        Dictionary<string, FieldDto> fields = [];

        if (IsBoxOfficeMismatch(guess.BoxOffice, target.BoxOffice, out FieldDto? boxOfficeOut) && boxOfficeOut is not null)
        {
            fields.Add(IGuessRepository.BoxOfficeKey, boxOfficeOut);
        }
        else
        {
            fields.Add(IGuessRepository.BoxOfficeKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = [],
                Values = new List<string> { target.BoxOffice.ToString() },
            });
        }

        if (IsListMismatch(guess.Creatives.Select(x => CreativeFromPerson(x)), target.Creatives.Select(x => CreativeFromPerson(x)), out FieldDto? creativesOut) && creativesOut is not null)
        {
            fields.Add(IGuessRepository.CreativesKey, creativesOut);
        }
        else
        {
            fields.Add(IGuessRepository.CreativesKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = [],
                Values = target.Creatives.Select(x => CreativeFromPerson(x))
            });
        }

        if (IsRatingMismatch(guess.Rating, target.Rating, out FieldDto? ratingOut) && ratingOut is not null)
        {
            fields.Add(IGuessRepository.RatingKey, ratingOut);
        }
        else
        {
            fields.Add(IGuessRepository.RatingKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = [],
                Values = new List<string> { target.Rating.ToString() }
            });
        }

        if (IsListMismatch(guess.Genres, target.Genres, out FieldDto? genreOut) && genreOut is not null)
        {
            fields.Add(IGuessRepository.GenreKey, genreOut);
        }
        else
        {
            fields.Add(IGuessRepository.GenreKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = [],
                Values = target.Genres
            });
        }

        if (IsListMismatch(guess.Cast.Select(x => x.Name), target.Cast.Select(x => x.Name), out FieldDto? castOut) && castOut is not null)
        {
            fields.Add(IGuessRepository.CastKey, castOut);
        }
        else
        {
            fields.Add(IGuessRepository.CastKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = [],
                Values = target.Cast.Select(x => x.Name)
            });
        }

        if (IsYearMismatch(guess.Year, target.Year, out FieldDto? yearOut) && yearOut is not null)
        {
            fields.Add(IGuessRepository.YearKey, yearOut);
        }
        else
        {
            fields.Add(IGuessRepository.YearKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = [],
                Values = new List<string> { target.Year }
            });
        }

        // Create the current guess DTO (without hints yet)
        var currentGuess = new GuessDto { Fields = fields };

        // Compute hints including the current guess
        var allGuesses = previousGuesses?.ToList() ?? new List<GuessDto>();
        allGuesses.Add(currentGuess);
        var hints = ComputeHints(allGuesses);

        // Apply hints to the fields
        if (hints.TryGetValue(IGuessRepository.BoxOfficeKey, out var boxOfficeHints))
            fields[IGuessRepository.BoxOfficeKey].Hints = boxOfficeHints;
        if (hints.TryGetValue(IGuessRepository.CreativesKey, out var creativesHints))
            fields[IGuessRepository.CreativesKey].Hints = creativesHints;
        if (hints.TryGetValue(IGuessRepository.RatingKey, out var ratingHints))
            fields[IGuessRepository.RatingKey].Hints = ratingHints;
        if (hints.TryGetValue(IGuessRepository.GenreKey, out var genreHints))
            fields[IGuessRepository.GenreKey].Hints = genreHints;
        if (hints.TryGetValue(IGuessRepository.CastKey, out var castHints))
            fields[IGuessRepository.CastKey].Hints = castHints;
        if (hints.TryGetValue(IGuessRepository.YearKey, out var yearHints))
            fields[IGuessRepository.YearKey].Hints = yearHints;

        return currentGuess;
    }

    public bool IsBoxOfficeMismatch(long guessBoxOffice, long targetBoxOffice, out FieldDto? boxOfficeOut)
    {
        boxOfficeOut = null;

        if (guessBoxOffice == targetBoxOffice)
        {
            return false;
        }

        long boxOfficeDiff = guessBoxOffice - targetBoxOffice;
        long boxOfficeDiffAbs = Math.Abs(boxOfficeDiff);

        string color = "grey";

        if (boxOfficeDiffAbs <= _config.BoxOfficeYellowThreshold)
        {
            color = "yellow";
        }

        int yearDirection = 0;

        if (boxOfficeDiffAbs <= _config.BoxOfficeSingleArrowThreshold)
        {
            yearDirection = 1;
        }
        else
        {
            yearDirection = 2;
        }

        if (boxOfficeDiff > 0)
        {
            yearDirection *= -1;
        }

        boxOfficeOut = new FieldDto
        {
            Direction = yearDirection,
            Color = color,
            Modifiers = [],
            Values = new List<string> { guessBoxOffice.ToString() }
        };

        return true;

    }

    public bool IsYearMismatch(string guessYear, string targetYear, out FieldDto? yearOut)
    {
        yearOut = null;

        if (string.Equals(guessYear, targetYear, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(guessYear, out int iGuessYear))
        {
            throw new ArgumentException("Expected numeric guess year");
        }

        if (!int.TryParse(targetYear, out int iTargetYear))
        {
            throw new ArgumentException("Expected numeric target year");
        }

        int yearDiff = iGuessYear - iTargetYear;
        int yearDiffAbs = Math.Abs(yearDiff);

        string color = "grey";
        if (yearDiffAbs <= _config.YearYellowThreshold)
        {
            color = "yellow";
        }

        int yearDirection = 0;

        if (yearDiffAbs <= _config.YearSingleArrowThreshold)
        {
            yearDirection = 1;
        }
        else
        {
            yearDirection = 2;
        }

        if (yearDiff > 0)
        {
            yearDirection *= -1;
        }

        yearOut = new FieldDto
        {
            Direction = yearDirection,
            Color = color,
            Modifiers = [],
            Values = new List<string> { guessYear }
        };

        return true;
    }

    public bool IsListMismatch(IEnumerable<string> guessList, IEnumerable<string> targetList, out FieldDto? fieldOut)
    {
        fieldOut = null;

        IEnumerable<string> shared = guessList.Intersect(targetList);

        if (targetList.Count() == shared.Count())
        {
            return false;
        }

        string color = shared.Count() > 0 ? "yellow" : "grey";

        fieldOut = new FieldDto
        {
            Direction = 0,
            Color = color,
            Values = guessList,
            Modifiers = shared.ToDictionary(k => k, v => new List<string>() { "bold" })
        };

        return true;
    }

    public bool IsRatingMismatch(Rating guessRating, Rating targetRating, out FieldDto? ratingOut)
    {
        ratingOut = null;

        if (guessRating == targetRating)
        {
            return false;
        }

        string color = "grey";

        switch (Math.Abs(guessRating - targetRating))
        {
            case 0:
                color = "green";
                break;
            case 1:
                color = "yellow";
                break;
            default:
                break;
        }

        ratingOut = new FieldDto
        {
            Color = color,
            Direction = 0,
            Values = new List<string>() { guessRating.ToString() },
            Modifiers = []
        };

        return true;
    }

    /// <summary>
    /// Computes hints based on previous guesses to help narrow down possible values.
    /// </summary>
    private Dictionary<string, HintsDto> ComputeHints(IEnumerable<GuessDto>? previousGuesses)
    {
        var hints = new Dictionary<string, HintsDto>();

        if (previousGuesses == null || !previousGuesses.Any())
        {
            return hints;
        }

        // Box Office hints: compute known range
        var boxOfficeHints = ComputeRangeHints(previousGuesses, IGuessRepository.BoxOfficeKey);
        if (boxOfficeHints != null)
        {
            hints[IGuessRepository.BoxOfficeKey] = boxOfficeHints;
        }

        // Year hints: compute known range
        var yearHints = ComputeRangeHints(previousGuesses, IGuessRepository.YearKey);
        if (yearHints != null)
        {
            hints[IGuessRepository.YearKey] = yearHints;
        }

        // Rating hints: compute possible ratings
        var ratingHints = ComputeRatingHints(previousGuesses);
        if (ratingHints != null)
        {
            hints[IGuessRepository.RatingKey] = ratingHints;
        }

        // Genre hints: known matching values
        var genreHints = ComputeListHints(previousGuesses, IGuessRepository.GenreKey);
        if (genreHints != null)
        {
            hints[IGuessRepository.GenreKey] = genreHints;
        }

        // Cast hints: known matching values
        var castHints = ComputeListHints(previousGuesses, IGuessRepository.CastKey);
        if (castHints != null)
        {
            hints[IGuessRepository.CastKey] = castHints;
        }

        // Creatives hints: known matching values
        var creativesHints = ComputeListHints(previousGuesses, IGuessRepository.CreativesKey);
        if (creativesHints != null)
        {
            hints[IGuessRepository.CreativesKey] = creativesHints;
        }

        return hints;
    }

    /// <summary>
    /// Computes min/max range hints for numeric fields (year, box office)
    /// based on the direction indicators from previous guesses.
    /// </summary>
    private HintsDto? ComputeRangeHints(IEnumerable<GuessDto> previousGuesses, string fieldKey)
    {
        long? minBound = null;  // Lower bound (target must be >= this)
        long? maxBound = null;  // Upper bound (target must be <= this)

        // Get the appropriate threshold based on field type
        long threshold = fieldKey == IGuessRepository.YearKey
            ? _config.YearSingleArrowThreshold
            : _config.BoxOfficeSingleArrowThreshold;

        foreach (var guess in previousGuesses)
        {
            if (!guess.Fields.TryGetValue(fieldKey, out var field))
            {
                continue;
            }

            if (field.Color == "green")
            {
                // Exact match found, no need for range hints
                return null;
            }

            if (!field.Values.Any() || !long.TryParse(field.Values.First(), out long guessValue))
            {
                continue;
            }

            if (field.Direction == 1)
            {
                // Target is higher, within threshold: target in [guess+1, guess+threshold]
                long newMin = guessValue + 1;
                long newMax = guessValue + threshold;
                minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
                maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
            }
            else if (field.Direction == 2)
            {
                // Target is much higher: target > guess + threshold
                long newMin = guessValue + threshold + 1;
                minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
            }
            else if (field.Direction == -1)
            {
                // Target is lower, within threshold: target in [guess-threshold, guess-1]
                long newMin = guessValue - threshold;
                long newMax = guessValue - 1;
                minBound = minBound.HasValue ? Math.Max(minBound.Value, newMin) : newMin;
                maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
            }
            else if (field.Direction == -2)
            {
                // Target is much lower: target < guess - threshold
                long newMax = guessValue - threshold - 1;
                maxBound = maxBound.HasValue ? Math.Min(maxBound.Value, newMax) : newMax;
            }
        }

        if (!minBound.HasValue && !maxBound.HasValue)
        {
            return null;
        }

        return new HintsDto
        {
            Min = minBound?.ToString(),
            Max = maxBound?.ToString()
        };
    }

    /// <summary>
    /// Computes possible rating hints by eliminating ratings that are too far from the target.
    /// </summary>
    private HintsDto? ComputeRatingHints(IEnumerable<GuessDto> previousGuesses)
    {
        var possibleRatings = new HashSet<string>(IGuessRepository.AllRatings);

        foreach (var guess in previousGuesses)
        {
            if (!guess.Fields.TryGetValue(IGuessRepository.RatingKey, out var field))
            {
                continue;
            }

            if (field.Color == "green")
            {
                // Exact match found
                return null;
            }

            if (!field.Values.Any())
            {
                continue;
            }

            string guessRating = field.Values.First();

            if (field.Color == "grey")
            {
                // Grey means the rating is more than 1 step away
                // Remove ratings that are within 1 step of the guess
                int guessIndex = IGuessRepository.AllRatings.IndexOf(guessRating);
                if (guessIndex >= 0)
                {
                    // Remove the guessed rating and adjacent ratings (they would be yellow or green)
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
                // Yellow means exactly 1 step away
                int guessIndex = IGuessRepository.AllRatings.IndexOf(guessRating);
                if (guessIndex >= 0)
                {
                    // Keep only adjacent ratings
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

    /// <summary>
    /// Computes known matching values for list-based fields (genre, cast, creatives).
    /// </summary>
    private HintsDto? ComputeListHints(IEnumerable<GuessDto> previousGuesses, string fieldKey)
    {
        var knownValues = new HashSet<string>();

        foreach (var guess in previousGuesses)
        {
            if (!guess.Fields.TryGetValue(fieldKey, out var field))
            {
                continue;
            }

            // Collect values marked as bold (matching the target)
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
