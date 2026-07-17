using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Cinemadle.Repositories;

public class TmdbRepository : ITmdbRepository
{
    private ILogger<TmdbRepository> _logger;

    private readonly CinemadleConfig _config;
    private readonly TMDbClient _tmdbClient;
    private readonly ICacheRepository _cache;
    private readonly DatabaseContext _db;

    private bool Initialized { get; set; }

    private readonly string _iso3166Usa = "US";
    private readonly string _getMovieCacheKeyTemplate = "TmdbRepository.GetMovie.{0}";
    private readonly string _getMovieByIdCacheKeyTemplate = "TmdbRepository.GetMovieById.{0}";
    private readonly string _getTargetMovieCacheKeyTemplate = "TmdbRepository.GetTargetMovie.{0}";
    private readonly string _getMovieListCacheKey = "TmdbRepository.GetMovieList";
    private readonly string _getMovieImageByIdCacheKeyTemplate = "TmdbRepository.GetMovieImageById.{0}";

    private static readonly HttpClient HttpClient = new();

    private readonly bool _isDevelopment;

    private static int? RiggedMovie { get; set; }

    public TmdbRepository(
        ILogger<TmdbRepository> logger,
        IOptions<CinemadleConfig> config,
        ICacheRepository cache,
        DatabaseContext dbContext,
        IWebHostEnvironment env
    )
    {
        _logger = logger;
        var type = this.GetType().AssemblyQualifiedName ?? "TmdbRepository";
        _logger.LogDebug("+ctor({type})", type);

        _config = config.Value;
        _cache = cache;
        _tmdbClient = new TMDbClient(_config.TmdbApiKey);
        _db = dbContext;
        _isDevelopment = env.IsDevelopment();

        _logger.LogDebug("-ctor({type})", type);
    }

    private async Task Initialize()
    {
        if (Initialized)
        {
            return;
        }

        try
        {
            await _tmdbClient.GetConfigAsync();
            Initialized = true;
        }
        catch (Exception)
        {
            Initialized = false;
        }
        
    }

    public async Task<Dictionary<string, int>> GetMovieList()
    {
        if (_cache.TryGet<Dictionary<string, int>>(_getMovieListCacheKey, out Dictionary<string, int>? movieList) && movieList is not null)
        {
            _logger.LogDebug("GetMovieList: using cached movie list");
            return movieList;
        }
        
        await Initialize();

        var discover = _tmdbClient.DiscoverMoviesAsync()
                .OrderBy(DiscoverMovieSortBy.ReleaseDateDesc)
                .WhereCertificationIsAtMost("US", nameof(Rating.R))
                .WhereCertificationIsAtLeast("US", nameof(Rating.G))
                .WhereRuntimeIsAtLeast(_config.MinimumRuntimePossible)
                .WhereVoteAverageIsAtLeast(_config.MinimumScorePossible)
                .WhereVoteCountIsAtLeast((int)_config.MinimumVotesPossible)
                .WithAllOfReleaseTypes(ReleaseDateType.Theatrical)
                .IncludeAdultMovies(false)
                .WhereReleaseDateIsAfter(DateTime.ParseExact(_config.OldestMoviePossible, "yyyy-MM-dd", CultureInfo.InvariantCulture));

        Dictionary<string, int> movies = [];

        var page = 0;
        CancellationTokenSource c = new(TimeSpan.FromSeconds(90));
        while (movies.Count < 2000 && !c.Token.IsCancellationRequested)
        {
            var results = await discover.Query(page, c.Token);
            foreach (var movie in results?.Results ?? [])
            {
                try
                {
                    movies.Add(movie.Title!, movie.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting movie list");
                }
            }
            page++;
        }

        c.Dispose();
        _cache.Set(_getMovieListCacheKey, movies);
        return movies;
    }

    public async Task<MovieDto?> GetTargetMovie(string date)
    {
        if (RiggedMovie is { } riggedId)
        {
            return await GetMovieByIdInternal(riggedId);
        }
        
        var cacheKey = string.Format(_getTargetMovieCacheKeyTemplate, date);

        if (_cache.TryGet(cacheKey, out MovieDto? movieDto) && movieDto is not null)
        {
            _logger.LogDebug("GetTargetMovie: Returning cached movie");
            return movieDto;
        }
        
        await Initialize();

        var dbTargetMovie = _db.TargetMovies
            .FirstOrDefault(x => x.GameId == date);

        if (dbTargetMovie is not null)
        {
            return await GetMovieByIdInternal(dbTargetMovie.TargetMovieId);
        }

        var seed = _isDevelopment ? 2025 : 0;
        _logger.LogDebug("GetTargetMovie({date}): Seed, {seed}", date, seed);

        var dateStripped = date.Replace("-", string.Empty);
        seed += int.Parse(dateStripped);

        Random r = new(seed);
        var movieIndex = r.Next(0, 1999);

        var movies = await GetMovieList();

        if (movieIndex > movies.Count - 1)
        {
            movieIndex = r.Next(0, movies.Count - 1);
        }

        var movieId = movies.Values.ElementAt(movieIndex);
        _db.TargetMovies
            .Add(new TargetMovie
            {
                GameId = date,
                TargetMovieId = movieId,
                Inserted = DateTime.Now,
            });
        await _db.SaveChangesAsync();

        return await GetMovieByIdInternal(movieId);
    }

    private async Task<MovieDto?> GetMovieByIdInternal(int id)
    {
        var cacheKey = string.Format(_getMovieByIdCacheKeyTemplate, id);

        if (_cache.TryGet<MovieDto>(cacheKey, out var movieDto) && movieDto is not null)
        {
            _logger.LogDebug("GetMovieByIdInternal: returning cached movie");
            return movieDto;
        }
        
        await Initialize();

        const MovieMethods extraMethods = MovieMethods.Credits | MovieMethods.ReleaseDates;
        var movie = await _tmdbClient.GetMovieAsync(id, extraMethods);

        if (movie is null)
        {
            _logger.LogDebug("GetMovie: movie was null");
            return null;
        }

        _logger.LogDebug("GetMovie: Movie JSON, {json}", JsonSerializer.Serialize(movie));

        var release = movie
                .ReleaseDates
                ?.Results
                ?.FirstOrDefault(x => string.Equals(x.Iso_3166_1, _iso3166Usa, StringComparison.OrdinalIgnoreCase))
                ?.ReleaseDates
                ?.FirstOrDefault(x => x.Type == ReleaseDateType.Theatrical);

        var director = movie
            .Credits
            ?.Crew
            ?.Where(x => string.Equals(x.Job, "director", StringComparison.OrdinalIgnoreCase))
            ?.Select(x => new PersonDto
            {
                Name = x.Name ?? string.Empty,
                Role = "Director"
            })
            ?.FirstOrDefault();

        var writer = movie
            .Credits
            ?.Crew
            ?.Where(x => string.Equals(x.Job, "writer", StringComparison.OrdinalIgnoreCase))
            ?.Select(x => new PersonDto
            {
                Name = x.Name ?? string.Empty,
                Role = "Writer"
            })
            ?.FirstOrDefault();

        List<PersonDto> creatives = [];

        if (director is not null)
        {
            creatives.Add(director);
        }

        if (writer is not null)
        {
            creatives.Add(writer);
        }

        movieDto = new MovieDto
        {
            Id = id,
            Title = movie.Title ?? string.Empty,
            Cast = movie.Credits
                ?.Cast
                ?.Take(_config.CastCount)
                ?.Select(x => new PersonDto { Name = x.Name ?? string.Empty, Role = "Cast" }) ?? new List<PersonDto>(),
            Genres = movie.Genres
                ?.Take(_config.GenresCount)
                ?.Select(x => x.Name) ?? new List<string>(),
            Year = release?.ReleaseDate.Year.ToString() ?? "9999",
            BoxOffice = movie.Revenue,
            Creatives = creatives,
            Rating = MapCertificationToRating(release?.Certification)
        };

        _cache.Set(cacheKey, movieDto);
        return movieDto;
    }

    public async Task<MovieDto?> GetMovieById(int id)
    {
        return await GetMovieByIdInternal(id);
    }

    public async Task<MovieDto?> GetMovie(string title)
    {
        _logger.LogDebug("+GetMovie({title})", title);

        var cacheKey = string.Format(_getMovieCacheKeyTemplate, title);
        SearchMovie? searchMovie;
        if (_cache.TryGet(cacheKey, out SearchMovie? cachedSearchMovie) && cachedSearchMovie is not null)
        {
            _logger.LogDebug("GetMovie: using cached search movie");
            searchMovie = cachedSearchMovie;
        }
        else
        {
            await Initialize();
            var searchMovies = (await _tmdbClient.SearchMovieAsync(title))?.Results;
            searchMovie = searchMovies?.FirstOrDefault();
        }

        if (searchMovie is null)
        {
            _logger.LogDebug("-GetMovie({title})", title);
            return null;
        }

        _cache.Set(cacheKey, searchMovie);
        return await GetMovieByIdInternal(searchMovie.Id);
    }

    private static Rating MapCertificationToRating(string? certification)
    {
        if (string.IsNullOrWhiteSpace(certification))
        {
            return Rating.UNKNOWN;
        }

        var preprocessed = certification.Replace("-", "").ToUpperInvariant();
        return Enum.TryParse(preprocessed, out Rating rating) ? rating : Rating.UNKNOWN;
    }

    public async Task<byte[]?> GetMovieImageById(int id)
    {
        _logger.LogDebug("+GetMovieImageById({id})", id);

        var cacheKey = string.Format(_getMovieImageByIdCacheKeyTemplate, id);

        if (_cache.TryGet(cacheKey, out byte[]? cachedImage) && cachedImage is not null)
        {
            _logger.LogDebug("GetMovieImageById({id}): returning cached image", id);
            _logger.LogDebug("-GetMovieImageById({id})", id);
            return cachedImage;
        }

        await Initialize();
        var imagesWithId = await _tmdbClient.GetMovieImagesAsync(id);

        var imageData = imagesWithId?.Backdrops?.FirstOrDefault();

        if (imageData is null)
        {
            return null;
        }

        try
        {
            _logger.LogDebug("GetMovieImageById({id}: path {path}", id, imageData.FilePath);
            var fullUri = $"https://image.tmdb.org/t/p/w500/{imageData.FilePath}";
            var bytes = await HttpClient.GetByteArrayAsync(fullUri);

            _cache.Set(cacheKey, bytes);

            _logger.LogDebug("-GetMovieImageById({id})", id);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting image: {message}, {stackTrace}", ex.Message, ex.StackTrace);
            _logger.LogDebug("-GetMovieImageById({id})", id);
            return null;
        }
    }

    public void RigMovie(int id)
    {
        RiggedMovie = id;
    }

    public void UnrigMovie()
    {
        RiggedMovie = null;
    }

    public async Task<bool> ValidateApiKey()
    {
        try
        {
            var config = await _tmdbClient.GetConfigAsync();
            return config?.Images is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid TMDB configuration");
            return false;
        }
    }
}
