using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Cinemadle.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics.Metrics;

namespace Cinemadle.UnitTest;

public class Mocks
{
    private static readonly CinemadleConfig _defaultConfig = new()
    {
        TmdbApiKey = string.Empty,
        CastCount = 3,
        GenresCount = 3,
        CacheTTL = 10,
        YearYellowThreshold = 5,
        BoxOfficeYellowThreshold = 100000000,
        BoxOfficeSingleArrowThreshold = 300000000,
        YearSingleArrowThreshold = 10,
        YearDoubleArrowThreshold = 15,
        OldestMoviePossible = "1960-01-01",
        MinimumVotesPossible = 2000,
        MinimumScorePossible = 5,
        MinimumRuntimePossible = 70,
        MovieImageBlurFactors = new()
        {
            { "6", 14.0F },
            { "7", 12.0F },
            { "8", 9.0F },
            { "9", 6.0F }
        },
        GameLength = 10,
    };

    public static IMemoryCache GetMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    public static DatabaseContext GetDatabaseContext()
    {
        var opt = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        var ctx = new DatabaseContext(opt);

        ctx.Database.EnsureCreated();
        return ctx;
    }

    public static Mock<ICacheRepository> GetMockedCacheRepository()
    {
        var cacheRepo = new Mock<ICacheRepository>();

        cacheRepo.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
        cacheRepo.Setup(x => x.TryGet<It.IsAnyType>(It.IsAny<string>(), out It.Ref<It.IsAnyType?>.IsAny))
                .Returns(false);

        return cacheRepo;
    }

    public static Mock<IConfigRepository> GetMockedConfigRepository(CinemadleConfig? config = null)
    {
        CinemadleConfig usableConfig = config ?? _defaultConfig;
        Mock<IConfigRepository> configRepo = new();

        configRepo.Setup(x => x.GetConfig())
            .Returns(usableConfig);
        configRepo.Setup(x => x.IsLoaded())
            .Returns(true);

        return configRepo;
    }

    public static IGuessRepository GetGuessRepository(CinemadleConfig? config = null)
    {
        ILogger<GuessRepository> logger = UnitTestAssist.GetLogger<GuessRepository>();
        Mock<IConfigRepository> configRepoMock = GetMockedConfigRepository();
        IConfigRepository configRepo = configRepoMock.Object;

        Mock<ICacheRepository> cacheRepoMock = GetMockedCacheRepository();
        ICacheRepository cacheRepo = cacheRepoMock.Object;

        DatabaseContext db = GetDatabaseContext();

        return new GuessRepository(logger, cacheRepo, configRepo, db);
    }

    public static Mock<IGuessRepository> GetMockedGuessRepository()
    {
        Mock<IGuessRepository> guessRepoMock = new();

        return guessRepoMock;
    }

    public static Mock<ITmdbRepository> GetMockedTmdbRepository()
    {
        Mock<ITmdbRepository> tmdbRepoMock = new();

        return tmdbRepoMock;
    }

    public static Mock<IWebHostEnvironment> GetMockedWebHostEnvironment()
    {
        Mock<IWebHostEnvironment> webHostEnvMock = new();
        webHostEnvMock.SetupGet(e => e.EnvironmentName).Returns("Development");

        return webHostEnvMock;
    }
}