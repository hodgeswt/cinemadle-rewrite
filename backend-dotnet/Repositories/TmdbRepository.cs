using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;

using System.Text.Json;

namespace Cinemadle.Repositories;

public class TmdbRepository : ITmdbRepository
{
    private ILogger<TmdbRepository> _logger;

    private CinemadleConfig _config;
    private TMDbClient _tmdbClient;
    private ICacheRepository _cache;

    private bool _initialized { get; set; }

    private readonly string _iso3166USA = "US";
    private readonly string _getMovieCacheKeyTemplate = "TmdbRepository.GetMovie.{0}";

    public TmdbRepository(ILogger<TmdbRepository> logger, IConfigRepository config, ICacheRepository cache)
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "TmdbRepository";
        _logger.LogDebug("+ctor({type})", type);

        _config = config.GetConfig();
        _cache = cache;
        _tmdbClient = new TMDbClient(config.GetConfig().TmdbApiKey);

        _logger.LogDebug("-ctor({type})", type);
    }

    public async Task Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _ = await _tmdbClient.GetConfigAsync();
    }

    public async Task<MovieDto?> GetMovie(string title)
    {
        _logger.LogDebug("+GetMovie({title})", title);

        if (_cache.TryGet<MovieDto>(string.Format(_getMovieCacheKeyTemplate, title), out MovieDto? cachedMovie) && cachedMovie is not null)
        {
            return cachedMovie;
        }

        SearchMovie? result = (await _tmdbClient.SearchMovieAsync(title))
            .Results
            .FirstOrDefault();
        if (result is null)
        {
            _logger.LogDebug("-GetMovie({title})", title);
            return null;
        }

        int id = result.Id;

        MovieMethods extraMethods = MovieMethods.Credits | MovieMethods.ReleaseDates;
        Movie movie = await _tmdbClient.GetMovieAsync(id, extraMethods);

        if (movie is null) {
            _logger.LogDebug("GetMovie: movie was null");
            return null;
        }

        _logger.LogDebug("GetMovie: Movie JSON, {json}", JsonSerializer.Serialize(movie));

        ReleaseDateItem? release = movie
                .ReleaseDates
                ?.Results
                ?.FirstOrDefault(x => string.Equals(x.Iso_3166_1, _iso3166USA, StringComparison.OrdinalIgnoreCase))
                ?.ReleaseDates
                ?.FirstOrDefault(x => x.Type == ReleaseDateType.Theatrical);

        MovieDto movieDto = new MovieDto
        {
            Id = id,
            Title = movie.Title ?? string.Empty,
            Cast = movie.Credits
                ?.Cast
                ?.Take(_config.CastCount)
                ?.Select(x => new PersonDto { Name = x.Name, Role = "Cast" }) ?? new List<PersonDto>(),
            Genres = movie.Genres
                ?.Take(_config.GenresCount)
                ?.Select(x => x.Name) ?? new List<string>(),
            Year = release?.ReleaseDate.Year.ToString() ?? "9999",
            Rating = MapCertificationToRating(release?.Certification)
        };

        _cache.Set(string.Format(_getMovieCacheKeyTemplate, title), movieDto);

        return movieDto;
    }

    private static Rating MapCertificationToRating(string? certification)
    {
        if (string.IsNullOrWhiteSpace(certification))
        {
            return Rating.UNKNOWN;
        }

        string preprocessed = certification.Replace("-", "").ToUpperInvariant();
        if (Enum.TryParse(preprocessed, out Rating rating))
        {
            return rating;
        }

        return Rating.UNKNOWN;
    }
}
