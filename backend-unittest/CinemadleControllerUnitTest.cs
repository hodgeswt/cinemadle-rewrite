using Cinemadle.Database;
using Cinemadle.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;
using Cinemadle.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

        Mock<IWebHostEnvironment> webHostEnvMock = new();
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

        Mock<IWebHostEnvironment> webHostEnvMock = new();
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

        Mock<IWebHostEnvironment> webHostEnvMock = new();
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

    [Fact]
    public async Task GuessMovie_Success_PersistsGuessAndReturnsGuessDto()
    {
        var date = "2024-01-01";
        var movieId = 123;
        var userId = "test-user-id";
        var guessMovie = new MovieDto
        {
            Id = movieId,
            Title = "Test Guess Movie",
            Genres = ["Action", "Adventure"],
            Cast = [
                new PersonDto { Name = "Actor One", Role = "Character One" },
                new PersonDto { Name = "Actor Two", Role = "Character Two" }
            ],
            Creatives = [
                new PersonDto { Name = "Director One", Role = "Director" }
            ],
            BoxOffice = 100000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var targetMovie = new MovieDto
        {
            Id = 456,
            Title = "Test Target Movie",
            Genres = ["Action", "Drama"],
            Cast = [
                new PersonDto { Name = "Actor Three", Role = "Character Three" }
            ],
            Creatives = [
                new PersonDto { Name = "Director Two", Role = "Director" }
            ],
            BoxOffice = 150000000,
            Year = "2021",
            Rating = Rating.R
        };

        var expectedGuessDto = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>
            {
                ["title"] = new FieldDto
                {
                    Color = "red",
                    Direction = 0,
                    Values = [guessMovie.Title],
                    Modifiers = []
                },
                ["year"] = new FieldDto
                {
                    Color = "yellow",
                    Direction = 1,
                    Values = [guessMovie.Year],
                    Modifiers = []
                },
                ["genres"] = new FieldDto
                {
                    Color = "yellow",
                    Direction = 0,
                    Values = guessMovie.Genres,
                    Modifiers = []
                },
                ["cast"] = new FieldDto
                {
                    Color = "red",
                    Direction = 0,
                    Values = guessMovie.Cast.Select(c => c.Name),
                    Modifiers = []
                },
                ["rating"] = new FieldDto
                {
                    Color = "red",
                    Direction = 0,
                    Values = [guessMovie.Rating.ToString()],
                    Modifiers = []
                }
            }
        };
        var logger = UnitTestAssist.GetLogger<CinemadleController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        var guessRepoMock = new Mock<IGuessRepository>();
        var db = Mocks.GetDatabaseContext();

        tmdbRepoMock.Setup(x => x.GetMovieById(movieId))
            .ReturnsAsync(guessMovie);
        tmdbRepoMock.Setup(x => x.GetTargetMovie(date))
            .ReturnsAsync(targetMovie);
        guessRepoMock.Setup(x => x.Guess(It.IsAny<MovieDto>(), It.IsAny<MovieDto>()))
            .Returns(expectedGuessDto);

        var controller = new CinemadleController(
            logger,
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            guessRepoMock.Object,
            db
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GuessMovie(date, movieId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGuessDto = Assert.IsType<GuessDto>(okResult.Value);
        Assert.Equal(expectedGuessDto.Fields.Count, returnedGuessDto.Fields.Count);
        Assert.Equal(expectedGuessDto.Fields["title"].Values, returnedGuessDto.Fields["title"].Values);

        var savedGuess = await db.Guesses.FirstOrDefaultAsync(x =>
            x.GameId == date &&
            x.UserId == userId &&
            x.GuessMediaId == movieId
        );
        Assert.NotNull(savedGuess);
    }

    [Fact]
    public async Task GuessMovieAnon_Success_PersistsGuessAndReturnsGuessDto()
    {
        var date = "2024-01-01";
        var movieId = 123;
        var userId = Guid.NewGuid();

        var guessMovie = new MovieDto
        {
            Id = movieId,
            Title = "Test Guess Movie",
            Genres = ["Action", "Adventure"],
            Cast = [
                new PersonDto { Name = "Actor One", Role = "Character One" }
            ],
            Creatives = [
                new PersonDto { Name = "Director One", Role = "Director" }
            ],
            BoxOffice = 100000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var targetMovie = new MovieDto
        {
            Id = 456,
            Title = "Test Target Movie",
            Genres = ["Action", "Drama"],
            Cast = [
                new PersonDto { Name = "Actor Three", Role = "Character Three" }
            ],
            Creatives = [
                new PersonDto { Name = "Director Two", Role = "Director" }
            ],
            BoxOffice = 150000000,
            Year = "2021",
            Rating = Rating.R
        };

        var expectedGuessDto = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>
            {
                ["title"] = new FieldDto
                {
                    Color = "red",
                    Direction = 0,
                    Values = [guessMovie.Title],
                    Modifiers = []
                },
                ["year"] = new FieldDto
                {
                    Color = "yellow",
                    Direction = 1,
                    Values = [guessMovie.Year],
                    Modifiers = []
                }
            }
        };

        var logger = UnitTestAssist.GetLogger<CinemadleController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        var guessRepoMock = new Mock<IGuessRepository>();
        var db = Mocks.GetDatabaseContext();

        db.AnonUsers.Add(new AnonUser { UserId = userId.ToString() });
        await db.SaveChangesAsync();

        tmdbRepoMock.Setup(x => x.GetMovieById(movieId))
            .ReturnsAsync(guessMovie);
        tmdbRepoMock.Setup(x => x.GetTargetMovie(date))
            .ReturnsAsync(targetMovie);
        guessRepoMock.Setup(x => x.Guess(It.IsAny<MovieDto>(), It.IsAny<MovieDto>()))
            .Returns(expectedGuessDto);

        var controller = new CinemadleController(
            logger,
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            guessRepoMock.Object,
            db
        );

        var result = await controller.GuessMovieAnon(date, userId, movieId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedGuessDto = Assert.IsType<GuessDto>(okResult.Value);
        Assert.Equal(expectedGuessDto.Fields.Count, returnedGuessDto.Fields.Count);
        Assert.Equal(expectedGuessDto.Fields["title"].Values, returnedGuessDto.Fields["title"].Values);

        var savedGuess = await db.AnonUserGuesses.FirstOrDefaultAsync(x =>
            x.GameId == date &&
            x.UserId == userId.ToString() &&
            x.GuessMediaId == movieId
        );
        Assert.NotNull(savedGuess);
    }

    [Fact]
    public async Task GuessMovieAnon_InvalidUser_ReturnsUnauthorized()
    {
        var date = "2024-01-01";
        var movieId = 123;
        var invalidUserId = Guid.NewGuid();

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetDatabaseContext()
        );

        var result = await controller.GuessMovieAnon(date, invalidUserId, movieId);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GuessMovie_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        var date = "2024-01-01";
        var movieId = 123;

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetDatabaseContext()
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GuessMovie(date, movieId);

        Assert.IsType<UnauthorizedResult>(result);
    }
}

