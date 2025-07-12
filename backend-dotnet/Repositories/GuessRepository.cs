using Cinemadle.Datamodel;
using Cinemadle.Interfaces;

namespace Cinemadle.Repositories;

public class GuessRepository : IGuessRepository
{
    private ILogger<GuessRepository> _logger;
    private ICacheRepository _cache;
    private readonly CinemadleConfig _config;

    private readonly string _ratingKey = "rating";
    private readonly string _yearKey = "year";
    private readonly string _genreKey = "genre";
    private readonly string _castKey = "cast";

    private readonly string _guessCacheKeyTemplate = "GuessRepository.Guess.{0}.{1}";

    public GuessRepository(ILogger<GuessRepository> logger, ICacheRepository cacheRepository, IConfigRepository configRepository)
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "GuessRepository";
        _logger.LogDebug("+ctor({type})", type);

        _cache = cacheRepository;
        _config = configRepository.GetConfig();

        _logger.LogDebug("-ctor({type})", type);
    }

    public GuessDto Guess(MovieDto guess, MovieDto target)
    {
        string cacheKey = string.Format(_guessCacheKeyTemplate, guess.Id, target.Id);
        if (_cache.TryGet<GuessDto>(cacheKey, out GuessDto? guessDto) && guessDto is not null)
        {
            _logger.LogDebug("Guess: Returning cached guess data");
            return guessDto;
        }

        Dictionary<string, FieldDto> fields = new Dictionary<string, FieldDto>();

        if (IsRatingMismatch(guess.Rating, target.Rating, out FieldDto? ratingOut) && ratingOut is not null)
        {
            fields.Add(_ratingKey, ratingOut);
        }
        else
        {
            fields.Add(_ratingKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = new(),
                Values = new List<string> { guess.Rating.ToString() }
            });
        }

        if (IsListMismatch(guess.Genres, target.Genres, out FieldDto? genreOut) && genreOut is not null)
        {
            fields.Add(_genreKey, genreOut);
        }
        else
        {
            fields.Add(_genreKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = new(),
                Values = guess.Genres
            });
        }

        if (IsListMismatch(guess.Cast.Select(x => x.Name), target.Cast.Select(x => x.Name), out FieldDto? castOut) && castOut is not null)
        {
            fields.Add(_castKey, castOut);
        }
        else
        {
            fields.Add(_castKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = new(),
                Values = guess.Cast.Select(x => x.Name)
            });
        }

        if (IsYearMismatch(guess.Year, target.Year, out FieldDto? yearOut) && yearOut is not null)
        {
            fields.Add(_yearKey, yearOut);
        }
        else
        {
            fields.Add(_yearKey, new FieldDto
            {
                Color = "green",
                Direction = 0,
                Modifiers = new(),
                Values = new List<string> { guess.Year }
            });
        }

        return new GuessDto
        {
            Fields = fields
        };
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
        if (yearDiffAbs < _config.YearYellowThreshold)
        {
            color = "yellow";
        }

        int yearDirection = 0;

        if (yearDiffAbs > _config.YearDoubleArrowThreshold)
        {
            yearDirection = 2;
        }
        else if (yearDiffAbs > _config.YearSingleArrowThreshold)
        {
            yearDirection = 1;
        }

        if (yearDiff > 0)
        {
            yearDirection *= -1;
        }

        yearOut = new FieldDto
        {
            Direction = yearDirection,
            Color = color,
            Modifiers = new(),
            Values = new List<string> { guessYear }
        };

        return true;
    }

    public bool IsListMismatch(IEnumerable<string> guessList, IEnumerable<string> targetList, out FieldDto? fieldOut)
    {
        fieldOut = null;

        IEnumerable<string> shared = guessList.Intersect(targetList);

        if (guessList.Count() == shared.Count())
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
            Modifiers = new()
        };

        return true;
    }
}
