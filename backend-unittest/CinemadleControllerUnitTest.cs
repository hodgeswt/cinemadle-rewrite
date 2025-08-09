using Cinemadle.Database;
using Cinemadle.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;
using Cinemadle.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Cinemadle.Datamodel;

namespace Cinemadle.UnitTest;

public class CinemadleControllerUnitTest
{
    [Fact]
    public void CinemadleControllerValidateEndpointTest()
    {
        ILogger<CinemadleController> logger = UnitTestAssist.GetLogger<CinemadleController>();
        Mock<IConfigRepository> configRepoMock = Mocks.GetMockedConfigRepository();
        IConfigRepository configRepo = configRepoMock.Object;

        Mock<ICacheRepository> cacheRepoMock = Mocks.GetMockedCacheRepository();
        ICacheRepository cacheRepo = cacheRepoMock.Object;

        Mock<ITmdbRepository> tmdbRepositoryMock = Mocks.GetMockedTmdbRepository();
        ITmdbRepository tmdbRepo = tmdbRepositoryMock.Object;

        Mock<IWebHostEnvironment> webHostEnvMock = new Mock<IWebHostEnvironment>();
        webHostEnvMock.SetupGet(e => e.EnvironmentName).Returns("Development");
        IWebHostEnvironment webHostEnv = webHostEnvMock.Object;

        DatabaseContext db = Mocks.GetDatabaseContext();

        Mock<IGuessRepository> guessRepoMock = Mocks.GetMockedGuessRepository();
        IGuessRepository guessRepo = guessRepoMock.Object;

        CinemadleController controller = new(logger, configRepo, tmdbRepo, webHostEnv, guessRepo, db);

        Assert.True(controller.Validate().Value);
    }

    [Fact]
    public void CinemadleControllerHeartbeatEndpointTest()
    {
        ILogger<CinemadleController> logger = UnitTestAssist.GetLogger<CinemadleController>();
        Mock<IConfigRepository> configRepoMock = Mocks.GetMockedConfigRepository();
        IConfigRepository configRepo = configRepoMock.Object;

        Mock<ICacheRepository> cacheRepoMock = Mocks.GetMockedCacheRepository();
        ICacheRepository cacheRepo = cacheRepoMock.Object;

        Mock<ITmdbRepository> tmdbRepositoryMock = Mocks.GetMockedTmdbRepository();
        ITmdbRepository tmdbRepo = tmdbRepositoryMock.Object;

        Mock<IWebHostEnvironment> webHostEnvMock = new Mock<IWebHostEnvironment>();
        webHostEnvMock.SetupGet(e => e.EnvironmentName).Returns("Development");
        IWebHostEnvironment webHostEnv = webHostEnvMock.Object;

        DatabaseContext db = Mocks.GetDatabaseContext();

        Mock<IGuessRepository> guessRepoMock = Mocks.GetMockedGuessRepository();
        IGuessRepository guessRepo = guessRepoMock.Object;

        CinemadleController controller = new(logger, configRepo, tmdbRepo, webHostEnv, guessRepo, db);

        Assert.True(controller.Heartbeat().Value);
    }

    [Fact]
    public async Task CinemadleControllerAnonUserIdEndpointSuccessTest()
    {
        ILogger<CinemadleController> logger = UnitTestAssist.GetLogger<CinemadleController>();
        Mock<IConfigRepository> configRepoMock = Mocks.GetMockedConfigRepository();
        IConfigRepository configRepo = configRepoMock.Object;

        Mock<ICacheRepository> cacheRepoMock = Mocks.GetMockedCacheRepository();
        ICacheRepository cacheRepo = cacheRepoMock.Object;

        Mock<ITmdbRepository> tmdbRepositoryMock = Mocks.GetMockedTmdbRepository();
        ITmdbRepository tmdbRepo = tmdbRepositoryMock.Object;

        Mock<IWebHostEnvironment> webHostEnvMock = new Mock<IWebHostEnvironment>();
        webHostEnvMock.SetupGet(e => e.EnvironmentName).Returns("Development");
        IWebHostEnvironment webHostEnv = webHostEnvMock.Object;

        DatabaseContext db = Mocks.GetDatabaseContext();

        Mock<IGuessRepository> guessRepoMock = Mocks.GetMockedGuessRepository();
        IGuessRepository guessRepo = guessRepoMock.Object;

        CinemadleController controller = new(logger, configRepo, tmdbRepo, webHostEnv, guessRepo, db);

        var anonUserIdResult = await controller.GetAnonUserId();

        Assert.IsType<OkObjectResult>(anonUserIdResult);

        OkObjectResult? ok = anonUserIdResult as OkObjectResult;
        Assert.NotNull(ok);

        Assert.IsType<string>(ok.Value);

        AnonUser? foundUser = db.AnonUsers.Where(x => x.UserId == ok.Value as string).FirstOrDefault();

        Assert.NotNull(foundUser);
        Assert.Equal(foundUser.UserId, ok.Value as string);
    }

    [Fact]
    public async Task GetTargetMovie_Success_ReturnsOkWithMovie()
    {
        var date = "2024-01-01";
        var expectedMovie = new MovieDto
        {
            Id = 1,
            Title = "Test Movie",
            Genres = [],
            Cast = [],
            Creatives = [],
            BoxOffice = 3,
            Rating = Rating.G,
            Year = "2017"
        };

        var logger = UnitTestAssist.GetLogger<CinemadleController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetTargetMovie(date))
            .ReturnsAsync(expectedMovie);

        var controller = new CinemadleController(
            logger,
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetDatabaseContext()
        );

        var result = await controller.GetTargetMovie(date);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMovie = Assert.IsType<MovieDto>(okResult.Value);
        Assert.Equal(expectedMovie.Id, returnedMovie.Id);
        Assert.Equal(expectedMovie.Title, returnedMovie.Title);
    }

    [Fact]
    public async Task GetTargetMovie_NotFound_ReturnsNotFoundResult()
    {
        var date = "2024-01-01";

        var logger = UnitTestAssist.GetLogger<CinemadleController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetTargetMovie(date))
            .ReturnsAsync((MovieDto?)null);

        var controller = new CinemadleController(
            logger,
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetDatabaseContext()
        );

        var result = await controller.GetTargetMovie(date);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetTargetMovie_ThrowsException_Returns500StatusCode()
    {
        var date = "2024-01-01";

        var logger = UnitTestAssist.GetLogger<CinemadleController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetTargetMovie(date))
            .ThrowsAsync(new Exception("Test exception"));

        var controller = new CinemadleController(
            logger,
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetDatabaseContext()
        );

        var result = await controller.GetTargetMovie(date);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
