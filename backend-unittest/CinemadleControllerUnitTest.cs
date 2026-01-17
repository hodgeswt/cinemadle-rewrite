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

        CinemadleController controller = new(logger, configRepo, tmdbRepo, webHostEnv, guessRepo, Mocks.GetMockedHintRepository().Object, Mocks.GetMockedFeatureFlagRepository().Object,  db, Mocks.GetIdentityContext());

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

        CinemadleController controller = new(logger, configRepo, tmdbRepo, webHostEnv, guessRepo, Mocks.GetMockedHintRepository().Object, Mocks.GetMockedFeatureFlagRepository().Object,  db, Mocks.GetIdentityContext());

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

        CinemadleController controller = new(logger, configRepo, tmdbRepo, webHostEnv, guessRepo, Mocks.GetMockedHintRepository().Object, Mocks.GetMockedFeatureFlagRepository().Object,  db, Mocks.GetIdentityContext());

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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
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
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GuessMovie(date, movieId);

        Assert.IsType<UnauthorizedResult>(result);
    }

    #region Custom Game Tests

    [Fact]
    public async Task CreateCustomGame_Success_ReturnsOkWithCustomGameDto()
    {
        var userId = "test-user-id";
        var movieId = 123;
        var customGameCreate = new CustomGameCreateDto { Id = movieId };
        var movie = new MovieDto
        {
            Id = movieId,
            Title = "Test Movie",
            Genres = ["Action"],
            Cast = [],
            Creatives = [],
            BoxOffice = 1000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var logger = UnitTestAssist.GetLogger<CinemadleController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(movieId)).ReturnsAsync(movie);

        var db = Mocks.GetDatabaseContext();

        var controller = new CinemadleController(
            logger,
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.CreateCustomGame(customGameCreate);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<CustomGameDto>(okResult.Value);
        Assert.NotNull(returnedDto.Id);
        Assert.Equal(movieId, returnedDto.TargetMovieId);

        var savedGame = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == returnedDto.Id);
        Assert.NotNull(savedGame);
        Assert.Equal(userId, savedGame.CreatorUserId);
        Assert.Equal(movieId, savedGame.TargetMovieId);
    }

    [Fact]
    public async Task CreateCustomGame_NoAuth_ReturnsUnauthorized()
    {
        var customGameCreate = new CustomGameCreateDto { Id = 123 };

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.CreateCustomGame(customGameCreate);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCustomGame_MovieNotFound_ReturnsNotFound()
    {
        var userId = "test-user-id";
        var movieId = 999;
        var customGameCreate = new CustomGameCreateDto { Id = movieId };

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(movieId)).ReturnsAsync((MovieDto?)null);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.CreateCustomGame(customGameCreate);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCustomGame_Success_ReturnsOkWithCustomGameDto()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 123;

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetCustomGame(customGameId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<CustomGameDto>(okResult.Value);
        Assert.Equal(customGameId, returnedDto.Id);
        Assert.Equal(targetMovieId, returnedDto.TargetMovieId);
    }

    [Fact]
    public async Task GetCustomGame_NotFound_ReturnsNotFound()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetCustomGame(customGameId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCustomGameTarget_Success_ReturnsOkWithMovie()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 123;
        var targetMovie = new MovieDto
        {
            Id = targetMovieId,
            Title = "Target Movie",
            Genres = ["Action"],
            Cast = [],
            Creatives = [],
            BoxOffice = 5000000,
            Year = "2021",
            Rating = Rating.R
        };

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(targetMovieId)).ReturnsAsync(targetMovie);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetCustomGameTarget(customGameId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMovie = Assert.IsType<MovieDto>(okResult.Value);
        Assert.Equal(targetMovieId, returnedMovie.Id);
        Assert.Equal("Target Movie", returnedMovie.Title);
    }

    [Fact]
    public async Task GetCustomGameTarget_CustomGameNotFound_ReturnsNotFound()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetCustomGameTarget(customGameId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPastGuessesCustomGame_Success_ReturnsGuessedMovieIds()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 123;

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });

        db.Guesses.Add(new UserGuess
        {
            GameId = customGameId,
            UserId = userId,
            GuessMediaId = 100,
            SequenceId = 1,
            Inserted = DateTime.UtcNow
        });

        db.Guesses.Add(new UserGuess
        {
            GameId = customGameId,
            UserId = userId,
            GuessMediaId = 200,
            SequenceId = 2,
            Inserted = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = controller.GetPastGuessesCustomGame(customGameId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var guesses = Assert.IsAssignableFrom<IEnumerable<int>>(okResult.Value);
        var guessList = guesses.ToList();
        Assert.Equal(2, guessList.Count);
        Assert.Contains(100, guessList);
        Assert.Contains(200, guessList);
    }

    [Fact]
    public async Task GetPastGuessesCustomGame_CustomGameNotFound_ReturnsNotFound()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = controller.GetPastGuessesCustomGame(customGameId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GuessMovieCustomGame_Success_PersistsGuessAndReturnsDto()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 456;
        var guessMovieId = 123;

        var guessMovie = new MovieDto
        {
            Id = guessMovieId,
            Title = "Guess Movie",
            Genres = ["Action"],
            Cast = [],
            Creatives = [],
            BoxOffice = 1000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var targetMovie = new MovieDto
        {
            Id = targetMovieId,
            Title = "Target Movie",
            Genres = ["Drama"],
            Cast = [],
            Creatives = [],
            BoxOffice = 5000000,
            Year = "2021",
            Rating = Rating.R
        };

        var expectedGuessDto = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>
            {
                ["title"] = new FieldDto { Color = "red", Direction = 0, Values = ["Guess Movie"], Modifiers = [] }
            }
        };

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(guessMovieId)).ReturnsAsync(guessMovie);
        tmdbRepoMock.Setup(x => x.GetMovieById(targetMovieId)).ReturnsAsync(targetMovie);

        var guessRepoMock = new Mock<IGuessRepository>();
        guessRepoMock.Setup(x => x.Guess(It.IsAny<MovieDto>(), It.IsAny<MovieDto>()))
            .Returns(expectedGuessDto);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GuessMovieCustomGame(customGameId, guessMovieId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<GuessDto>(okResult.Value);
        Assert.Equal(expectedGuessDto.Fields.Count, returnedDto.Fields.Count);

        var savedGuess = await db.Guesses.FirstOrDefaultAsync(x =>
            x.GameId == customGameId &&
            x.UserId == userId &&
            x.GuessMediaId == guessMovieId
        );
        Assert.NotNull(savedGuess);
    }

    [Fact]
    public async Task GuessMovieCustomGame_CustomGameNotFound_ReturnsNotFound()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var guessMovieId = 123;

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext(),
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GuessMovieCustomGame(customGameId, guessMovieId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetGameSummaryCustomGame_Success_ReturnsSummary()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 456;
        var guessMovieId = 123;

        var guessMovie = new MovieDto
        {
            Id = guessMovieId,
            Title = "Guess Movie",
            Genres = ["Action"],
            Cast = [],
            Creatives = [],
            BoxOffice = 1000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var targetMovie = new MovieDto
        {
            Id = targetMovieId,
            Title = "Target Movie",
            Genres = ["Drama"],
            Cast = [],
            Creatives = [],
            BoxOffice = 5000000,
            Year = "2021",
            Rating = Rating.R
        };

        var guessDto = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>
            {
                ["title"] = new FieldDto { Color = "green", Direction = 0, Values = ["Movie"], Modifiers = [] },
                ["year"] = new FieldDto { Color = "yellow", Direction = 0, Values = ["2020"], Modifiers = [] },
                ["genres"] = new FieldDto { Color = "red", Direction = 0, Values = ["Action"], Modifiers = [] }
            }
        };

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        // Add 10 guesses (complete game)
        for (int i = 1; i <= 10; i++)
        {
            db.Guesses.Add(new UserGuess
            {
                GameId = customGameId,
                UserId = userId,
                GuessMediaId = guessMovieId,
                SequenceId = i,
                Inserted = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();        await db.SaveChangesAsync();

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(guessMovieId)).ReturnsAsync(guessMovie);
        tmdbRepoMock.Setup(x => x.GetMovieById(targetMovieId)).ReturnsAsync(targetMovie);

        var guessRepoMock = new Mock<IGuessRepository>();
        guessRepoMock.Setup(x => x.Guess(It.IsAny<MovieDto>(), It.IsAny<MovieDto>()))
            .Returns(guessDto);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetGameSummaryCustomGame(customGameId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaryDto = Assert.IsType<GameSummaryDto>(okResult.Value);
        Assert.NotNull(summaryDto.Summary);
        Assert.True(summaryDto.Summary.Count > 0);
        Assert.Contains($"play at https://cinemadle.com/customGame/{customGameId}", summaryDto.Summary);
    }

    [Fact]
    public async Task GetGameSummaryCustomGame_IncompleteGame_ReturnsNotFound()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 456;

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });

        // Add only 3 guesses (incomplete game, no win)
        for (int i = 1; i <= 3; i++)
        {
            db.Guesses.Add(new UserGuess
            {
                GameId = customGameId,
                UserId = userId,
                GuessMediaId = 100 + i,
                SequenceId = i,
                Inserted = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var targetMovie = new MovieDto
        {
            Id = targetMovieId,
            Title = "Target Movie",
            Genres = [],
            Cast = [],
            Creatives = [],
            BoxOffice = 5000000,
            Year = "2021",
            Rating = Rating.R
        };

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(targetMovieId)).ReturnsAsync(targetMovie);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetGameSummaryCustomGame(customGameId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetGameSummaryCustomGame_WonEarly_ReturnsOkWithSummary()
    {
        var userId = "test-user-id";
        var customGameId = Guid.NewGuid().ToString();
        var targetMovieId = 456;

        var guessMovie = new MovieDto
        {
            Id = 123,
            Title = "Guess Movie",
            Genres = ["Action"],
            Cast = [],
            Creatives = [],
            BoxOffice = 1000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var targetMovie = new MovieDto
        {
            Id = targetMovieId,
            Title = "Target Movie",
            Genres = ["Drama"],
            Cast = [],
            Creatives = [],
            BoxOffice = 5000000,
            Year = "2021",
            Rating = Rating.R
        };

        var guessDto = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>
            {
                ["title"] = new FieldDto { Color = "green", Direction = 0, Values = ["Movie"], Modifiers = [] }
            }
        };

        var db = Mocks.GetDatabaseContext();
        db.CustomGames.Add(new CustomGame
        {
            Id = customGameId,
            TargetMovieId = targetMovieId,
            CreatorUserId = userId,
            Inserted = DateTime.UtcNow
        });

        // Add 3 guesses with the winning guess at the end
        db.Guesses.Add(new UserGuess
        {
            GameId = customGameId,
            UserId = userId,
            GuessMediaId = 100,
            SequenceId = 1,
            Inserted = DateTime.UtcNow
        });

        db.Guesses.Add(new UserGuess
        {
            GameId = customGameId,
            UserId = userId,
            GuessMediaId = 200,
            SequenceId = 2,
            Inserted = DateTime.UtcNow
        });

        db.Guesses.Add(new UserGuess
        {
            GameId = customGameId,
            UserId = userId,
            GuessMediaId = targetMovieId, // Winning guess
            SequenceId = 3,
            Inserted = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(It.IsAny<int>())).ReturnsAsync(guessMovie);
        tmdbRepoMock.Setup(x => x.GetMovieById(targetMovieId)).ReturnsAsync(targetMovie);

        var guessRepoMock = new Mock<IGuessRepository>();
        guessRepoMock.Setup(x => x.Guess(It.IsAny<MovieDto>(), It.IsAny<MovieDto>()))
            .Returns(guessDto);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetGameSummaryCustomGame(customGameId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaryDto = Assert.IsType<GameSummaryDto>(okResult.Value);
        Assert.NotNull(summaryDto.Summary);
        Assert.True(summaryDto.Summary.Count > 0);
    }

    [Fact]
    public async Task GetGameSummary_WonEarly_ReturnsOkWithSummary()
    {
        var userId = "test-user-id";
        var date = "2024-01-01";
        var targetMovieId = 456;

        var guessMovie = new MovieDto
        {
            Id = 123,
            Title = "Guess Movie",
            Genres = ["Action"],
            Cast = [],
            Creatives = [],
            BoxOffice = 1000000,
            Year = "2020",
            Rating = Rating.PG13
        };

        var targetMovie = new MovieDto
        {
            Id = targetMovieId,
            Title = "Target Movie",
            Genres = ["Drama"],
            Cast = [],
            Creatives = [],
            BoxOffice = 5000000,
            Year = "2021",
            Rating = Rating.R
        };

        var guessDto = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>
            {
                ["title"] = new FieldDto { Color = "green", Direction = 0, Values = ["Movie"], Modifiers = [] }
            }
        };

        var db = Mocks.GetDatabaseContext();

        // Add 2 guesses with the winning guess at the end
        db.Guesses.Add(new UserGuess
        {
            GameId = date,
            UserId = userId,
            GuessMediaId = 100,
            SequenceId = 1,
            Inserted = DateTime.UtcNow
        });

        db.Guesses.Add(new UserGuess
        {
            GameId = date,
            UserId = userId,
            GuessMediaId = targetMovieId, // Winning guess
            SequenceId = 2,
            Inserted = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(It.IsAny<int>())).ReturnsAsync(guessMovie);
        tmdbRepoMock.Setup(x => x.GetTargetMovie(date)).ReturnsAsync(targetMovie);

        var guessRepoMock = new Mock<IGuessRepository>();
        guessRepoMock.Setup(x => x.Guess(It.IsAny<MovieDto>(), It.IsAny<MovieDto>()))
            .Returns(guessDto);

        var controller = new CinemadleController(
            UnitTestAssist.GetLogger<CinemadleController>(),
            Mocks.GetMockedConfigRepository().Object,
            tmdbRepoMock.Object,
            Mocks.GetMockedWebHostEnvironment().Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db,
            Mocks.GetIdentityContext()
        );

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await controller.GetGameSummary(date);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var summaryDto = Assert.IsType<GameSummaryDto>(okResult.Value);
        Assert.NotNull(summaryDto.Summary);
        Assert.True(summaryDto.Summary.Count > 0);
    }

    #endregion
}

