using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Discover;
using TMDbLib.Objects.General;

using System.Globalization;
using System.Text.Json;
namespace Cinemadle.Repositories;

public class TmdbRepository : ITmdbRepository
{
    private ILogger<TmdbRepository> _logger;

    private readonly CinemadleConfig _config;
    private TMDbClient _tmdbClient;
    private ICacheRepository _cache;
    private DatabaseContext _db;

    private bool _initialized { get; set; }

    private readonly string _iso3166USA = "US";
    private readonly string _getMovieCacheKeyTemplate = "TmdbRepository.GetMovie.{0}";
    private readonly string _getMovieByIdCacheKeyTemplate = "TmdbRepository.GetMovieById.{0}";
    private readonly string _getTargetMovieCacheKeyTemplate = "TmdbRepository.GetTargetMovie.{0}";
    private readonly string _getMovieListCacheKey = "TmdbRepository.GetMovieList";
    private readonly string _getMovieImageByIdCacheKeyTemplate = "TmdbRepository.GetMovieImageById.{0}";

    private static readonly HttpClient _httpClient = new();

    private readonly bool _isDevelopment;

    public TmdbRepository(
        ILogger<TmdbRepository> logger,
        IConfigRepository config,
        ICacheRepository cache,
        DatabaseContext dbContext,
        IWebHostEnvironment env
    )
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "TmdbRepository";
        _logger.LogDebug("+ctor({type})", type);

        _config = config.GetConfig();
        _cache = cache;
        _tmdbClient = new TMDbClient(config.GetConfig().TmdbApiKey);
        _db = dbContext;
        _isDevelopment = env.IsDevelopment();

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

    public async Task<Dictionary<string, int>> GetMovieList()
    {
        if (_cache.TryGet<Dictionary<string, int>>(_getMovieListCacheKey, out Dictionary<string, int>? movieList) && movieList is not null)
        {
            _logger.LogDebug("GetMovieList: using cached movie list");
            return movieList;
        }

        DiscoverMovie discover = _tmdbClient.DiscoverMoviesAsync()
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

        int page = 0;
        CancellationTokenSource c = new(TimeSpan.FromSeconds(90));
        while (movies.Count < 2000 && !c.Token.IsCancellationRequested)
        {
            SearchContainer<SearchMovie> results = await discover.Query(page, c.Token);
            foreach (SearchMovie movie in results.Results)
            {
                try
                {
                    movies.Add(movie.Title, movie.Id);
                }
                catch (Exception)
                {
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
        string cacheKey = string.Format(_getTargetMovieCacheKeyTemplate, date);

        if (_cache.TryGet<MovieDto>(cacheKey, out MovieDto? movieDto) && movieDto is not null)
        {
            _logger.LogDebug("GetTargetMovie: Returning cached movie");
            return movieDto;
        }

        TargetMovie? dbTargetMovie = _db.TargetMovies
            .Where(x => x.GameId == date)
            .FirstOrDefault();

        if (dbTargetMovie is not null)
        {
            return await GetMovieByIdInternal(dbTargetMovie.TargetMovieId);
        }

        int seed = _isDevelopment ? 1 : 0;

        string dateStripped = date.Replace("-", string.Empty);
        seed += int.Parse(dateStripped);

        Random r = new(seed);
        int movieIndex = r.Next(0, 1999);

        Dictionary<string, int> movies = await GetMovieList();

        if (movieIndex > movies.Count - 1)
        {
            movieIndex = r.Next(0, movies.Count - 1);
        }

        int movieId = movies.Values.ElementAt(movieIndex);
        _db.TargetMovies
            .Add(new TargetMovie
            {
                GameId = date,
                TargetMovieId = movieId,
                Inserted = DateTime.Now,
            });
        _db.SaveChanges();

        return await GetMovieByIdInternal(movieId);
    }

    private async Task<MovieDto?> GetMovieByIdInternal(int id)
    {
        string cacheKey = string.Format(_getMovieByIdCacheKeyTemplate, id);

        if (_cache.TryGet<MovieDto>(cacheKey, out MovieDto? movieDto) && movieDto is not null)
        {
            _logger.LogDebug("GetMovieByIdInternal: returning cached movie");
            return movieDto;
        }

        MovieMethods extraMethods = MovieMethods.Credits | MovieMethods.ReleaseDates;
        Movie movie = await _tmdbClient.GetMovieAsync(id, extraMethods);

        if (movie is null)
        {
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

        PersonDto? director = movie
            .Credits
            ?.Crew
            ?.Where(x => string.Equals(x.Job, "director", StringComparison.OrdinalIgnoreCase))
            ?.Select(x => new PersonDto
            {
                Name = x.Name,
                Role = "Director"
            })
            ?.FirstOrDefault();

        PersonDto? writer = movie
            .Credits
            ?.Crew
            ?.Where(x => string.Equals(x.Job, "writer", StringComparison.OrdinalIgnoreCase))
            ?.Select(x => new PersonDto
            {
                Name = x.Name,
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
                ?.Select(x => new PersonDto { Name = x.Name, Role = "Cast" }) ?? new List<PersonDto>(),
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

        string cacheKey = string.Format(_getMovieCacheKeyTemplate, title);
        SearchMovie? searchMovie;
        if (_cache.TryGet<SearchMovie>(cacheKey, out SearchMovie? cachedSearchMovie) && cachedSearchMovie is not null)
        {
            _logger.LogDebug("GetMovie: using cached search movie");
            searchMovie = cachedSearchMovie;
        }
        else
        {
            searchMovie = (await _tmdbClient.SearchMovieAsync(title))
                       .Results
                       .FirstOrDefault();
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

        string preprocessed = certification.Replace("-", "").ToUpperInvariant();
        if (Enum.TryParse(preprocessed, out Rating rating))
        {
            return rating;
        }

        return Rating.UNKNOWN;
    }

    public async Task<byte[]?> GetMovieImageById(int id)
    {
        _logger.LogDebug("+GetMovieImageById({id})", id);

        string cacheKey = string.Format(_getMovieImageByIdCacheKeyTemplate, id);

        if (_cache.TryGet(cacheKey, out byte[]? cachedImage) && cachedImage is not null)
        {
            _logger.LogDebug("GetMovieImageById({id}): returning cached image", id);
            _logger.LogDebug("-GetMovieImageById({id})", id);
            return cachedImage;
        }

        ImagesWithId? imagesWithId = await _tmdbClient.GetMovieImagesAsync(id);

        if (imagesWithId is null)
        {
            return null;
        }

        ImageData? imageData = imagesWithId.Backdrops.FirstOrDefault();

        if (imageData is null)
        {
            return null;
        }

        try
        {
            _logger.LogDebug("GetMovieImageById({id}: path {path}", id, imageData.FilePath);
            string fullUri = $"https://image.tmdb.org/t/p/w500/{imageData.FilePath}";
            byte[] bytes = await _httpClient.GetByteArrayAsync(fullUri);

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

}
