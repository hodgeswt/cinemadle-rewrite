using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Cinemadle.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cinemadle.UnitTest;

public class Mocks
{
    private static readonly CinemadleConfig _defaultConfig = new CinemadleConfig
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
        MinimumRuntimePossible = 70
    };

    public static IMemoryCache GetMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
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
        Mock<IConfigRepository> configRepo = new Mock<IConfigRepository>();

        configRepo.Setup(x => x.GetConfig())
            .Returns(usableConfig);
        configRepo.Setup(x => x.IsLoaded())
            .Returns(true);

        return configRepo;
    }
}
