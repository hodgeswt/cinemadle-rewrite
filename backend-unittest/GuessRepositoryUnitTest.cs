using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Cinemadle.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cinemadle.UnitTest;

public class GuessRepositoryUnitTest
{
    private static GuessRepository GetGuessRepository(CinemadleConfig? config = null)
    {
        ILogger<GuessRepository> logger = UnitTestAssist.GetLogger<GuessRepository>();
        Mock<IConfigRepository> configRepoMock = Mocks.GetMockedConfigRepository();
        IConfigRepository configRepo = configRepoMock.Object;

        Mock<ICacheRepository> cacheRepoMock = Mocks.GetMockedCacheRepository();
        ICacheRepository cacheRepo = cacheRepoMock.Object;

        DatabaseContext db = Mocks.GetDatabaseContext();

        return new GuessRepository(logger, cacheRepo, configRepo, db);
    }

    private static MovieDto GetTargetMovie()
    {
        return new MovieDto
        {
            Id = 2,
            Title = "Shrek 2",
            Genres = [
                "Animation",
                "Family",
                "Comedy"
            ],
            Cast = [
                new() { Name = "Mike Myers", Role = "Cast" },
                new() { Name = "Eddie Murphy", Role = "Cast" },
                new() { Name = "Cameron Diaz", Role = "Cast" }
            ],
            Creatives = [
                new() { Name = "Andrew Adamson", Role = "Director" },
                new() { Name = "Andrew Adamson", Role = "Writer" },
            ],
            BoxOffice = 935000000,
            Year = "2004",
            Rating = Rating.PG,
        };
    }

    private static CinemadleConfig GetConfig()
    {
        return Mocks.GetMockedConfigRepository().Object.GetConfig();
    }

    [Fact]
    public void AllCategoriesGreenTest()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();

        GuessDto expected = new()
        {
            Fields = new Dictionary<string, FieldDto>()
            {
                { "boxOffice", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.BoxOffice.ToString() ],
                        Modifiers = [],
                    }
                },
                { "creatives", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Creatives.Select(x => GuessRepository.CreativeFromPerson(x)),
                        Modifiers = [],
                    }
                },
                { "cast", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Cast.Select(x => x.Name),
                        Modifiers = [],
                    }
                },
                { "rating", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Rating.ToString() ],
                        Modifiers = [],
                    }
                },
                { "genre", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Genres,
                        Modifiers = [],
                    }
                },
                { "year", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Year ],
                        Modifiers = [],
                    }
                }

            }
        };

        GuessDto o = guessRepo.Guess(guess, target);

        Assert.Equivalent(expected, o);
    }

    [Fact]
    public void CastGreyNoBoldTest()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        guess.Cast = [
            new PersonDto { Name = "Harrison Ford", Role = "Cast" },
            new PersonDto { Name = "Tom Hiddleston", Role = "Cast" },
            new PersonDto { Name = "Emilia Clarke", Role = "Cast" }
        ];
        MovieDto target = GetTargetMovie();

        GuessDto expected = new()
        {
            Fields = new Dictionary<string, FieldDto>()
            {
                { "boxOffice", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.BoxOffice.ToString() ],
                        Modifiers = [],
                    }
                },
                { "creatives", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Creatives.Select(x => GuessRepository.CreativeFromPerson(x)),
                        Modifiers = [],
                    }
                },
                { "cast", new FieldDto
                    {
                        Color = "grey",
                        Direction = 0,
                        Values = guess.Cast.Select(x => x.Name),
                        Modifiers = [],
                    }
                },
                { "rating", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Rating.ToString() ],
                        Modifiers = [],
                    }
                },
                { "genre", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Genres,
                        Modifiers = [],
                    }
                },
                { "year", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Year ],
                        Modifiers = [],
                    }
                }

            }
        };

        GuessDto o = guessRepo.Guess(guess, target);

        Assert.Equivalent(expected, o);
    }

    [Fact]
    public void CastYellowTest()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        guess.Cast = [
            new PersonDto { Name = "Mike Myers", Role = "Cast" },
            new PersonDto { Name = "Tom Hiddleston", Role = "Cast" },
            new PersonDto { Name = "Emilia Clarke", Role = "Cast" }
        ];
        MovieDto target = GetTargetMovie();

        GuessDto expected = new()
        {
            Fields = new Dictionary<string, FieldDto>()
            {
                { "boxOffice", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.BoxOffice.ToString() ],
                        Modifiers = [],
                    }
                },
                { "creatives", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Creatives.Select(x => GuessRepository.CreativeFromPerson(x)),
                        Modifiers = [],
                    }
                },
                { "cast", new FieldDto
                    {
                        Color = "yellow",
                        Direction = 0,
                        Values = guess.Cast.Select(x => x.Name),
                        Modifiers = new Dictionary<string, List<string>>()
                        {
                            { "Mike Myers", new List<string>() { "bold" } },
                        }
                    }
                },
                { "rating", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Rating.ToString() ],
                        Modifiers = [],
                    }
                },
                { "genre", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Genres,
                        Modifiers = [],
                    }
                },
                { "year", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Year ],
                        Modifiers = [],
                    }
                }

            }
        };

        GuessDto o = guessRepo.Guess(guess, target);

        Assert.Equivalent(expected, o);
    }

    [Fact]
    public void BoxOfficeGreenWhenExactMatch()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("green", result.Fields["boxOffice"].Color);
        Assert.Equal(0, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeYellowWhenWithinThreshold()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();

        guess.BoxOffice = target.BoxOffice - GetConfig().BoxOfficeYellowThreshold;

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("yellow", result.Fields["boxOffice"].Color);
        Assert.Equal(1, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeGreyWithSingleUpArrowWhenBelowTarget()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();
        
        guess.BoxOffice = target.BoxOffice - (GetConfig().BoxOfficeSingleArrowThreshold / 2);

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("grey", result.Fields["boxOffice"].Color);
        Assert.Equal(1, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeGreyWithDoubleUpArrowWhenFarBelowTarget()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();
        
        guess.BoxOffice = target.BoxOffice - (GetConfig().BoxOfficeSingleArrowThreshold * 2);

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("grey", result.Fields["boxOffice"].Color);
        Assert.Equal(2, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeGreyWithSingleDownArrowWhenAboveTarget()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();
        
        guess.BoxOffice = target.BoxOffice + (GetConfig().BoxOfficeSingleArrowThreshold / 2);

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("grey", result.Fields["boxOffice"].Color);
        Assert.Equal(-1, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeGreyWithDoubleDownArrowWhenFarAboveTarget()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();
        
        guess.BoxOffice = target.BoxOffice + (GetConfig().BoxOfficeSingleArrowThreshold * 2);

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("grey", result.Fields["boxOffice"].Color);
        Assert.Equal(-2, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeHandlesZeroBoxOfficeGuess()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();
        
        guess.BoxOffice = 0;

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("grey", result.Fields["boxOffice"].Color);
        Assert.Equal(2, result.Fields["boxOffice"].Direction);
    }

    [Fact]
    public void BoxOfficeHandlesZeroBoxOfficeTarget()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();
        
        target.BoxOffice = 0;
        guess.BoxOffice = GetConfig().BoxOfficeSingleArrowThreshold * 2;

        GuessDto result = guessRepo.Guess(guess, target);

        Assert.Equal("grey", result.Fields["boxOffice"].Color);
        Assert.Equal(-2, result.Fields["boxOffice"].Direction);
    }
}
