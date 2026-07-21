using System.Security.Claims;
using Cinemadle.Controllers;
using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cinemadle.UnitTest;

public class CustomGamesControllerUnitTests
{
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

        var logger = UnitTestAssist.GetLogger<CustomGamesController>();
        var tmdbRepoMock = new Mock<ITmdbRepository>();
        tmdbRepoMock.Setup(x => x.GetMovieById(movieId)).ReturnsAsync(movie);

        var db = Mocks.GetDatabaseContext();

        var controller = new CustomGamesController(
            logger,
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext()
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext()
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext()
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext()
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext()
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            Mocks.GetMockedTmdbRepository().Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            Mocks.GetDatabaseContext()
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            Mocks.GetMockedGuessRepository().Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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

        var controller = new CustomGamesController(
            UnitTestAssist.GetLogger<CustomGamesController>(),
            Mocks.GetMockedConfigRepository(),
            tmdbRepoMock.Object,
            guessRepoMock.Object,
            Mocks.GetMockedHintRepository().Object,
            Mocks.GetMockedFeatureFlagRepository().Object,
            db
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
}