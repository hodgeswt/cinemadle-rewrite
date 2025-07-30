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
            Genres = new List<string>() {
                "Animation",
                "Family",
                "Comedy"
            },
            Cast = new List<PersonDto>() {
                new PersonDto { Name = "Mike Myers", Role = "Cast" },
                new PersonDto { Name = "Eddie Murphy", Role = "Cast" },
                new PersonDto { Name = "Cameron Diaz", Role = "Cast" }
            },
            Creatives = new List<PersonDto>() {
                new PersonDto { Name = "Andrew Adamson", Role = "Director" },
                new PersonDto { Name = "Andrew Adamson", Role = "Writer" },
            },
            BoxOffice = 935000000,
            Year = "2004",
            Rating = Rating.PG,
        };
    }

    [Fact]
    public void AllCategoriesGreenTest()
    {
        GuessRepository guessRepo = GetGuessRepository();
        MovieDto guess = GetTargetMovie();
        MovieDto target = GetTargetMovie();

        GuessDto expected = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>()
            {
                { "boxOffice", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.BoxOffice.ToString() ],
                        Modifiers = new(),
                    }
                },
                { "creatives", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Creatives.Select(x => GuessRepository.CreativeFromPerson(x)),
                        Modifiers = new(),
                    }
                },
                { "cast", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Cast.Select(x => x.Name),
                        Modifiers = new(),
                    }
                },
                { "rating", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Rating.ToString() ],
                        Modifiers = new(),
                    }
                },
                { "genre", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Genres,
                        Modifiers = new(),
                    }
                },
                { "year", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Year ],
                        Modifiers = new(),
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

        GuessDto expected = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>()
            {
                { "boxOffice", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.BoxOffice.ToString() ],
                        Modifiers = new(),
                    }
                },
                { "creatives", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Creatives.Select(x => GuessRepository.CreativeFromPerson(x)),
                        Modifiers = new(),
                    }
                },
                { "cast", new FieldDto
                    {
                        Color = "grey",
                        Direction = 0,
                        Values = guess.Cast.Select(x => x.Name),
                        Modifiers = new(),
                    }
                },
                { "rating", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Rating.ToString() ],
                        Modifiers = new(),
                    }
                },
                { "genre", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Genres,
                        Modifiers = new(),
                    }
                },
                { "year", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Year ],
                        Modifiers = new(),
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

        GuessDto expected = new GuessDto
        {
            Fields = new Dictionary<string, FieldDto>()
            {
                { "boxOffice", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.BoxOffice.ToString() ],
                        Modifiers = new(),
                    }
                },
                { "creatives", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Creatives.Select(x => GuessRepository.CreativeFromPerson(x)),
                        Modifiers = new(),
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
                        Modifiers = new(),
                    }
                },
                { "genre", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = target.Genres,
                        Modifiers = new(),
                    }
                },
                { "year", new FieldDto
                    {
                        Color = "green",
                        Direction = 0,
                        Values = [ target.Year ],
                        Modifiers = new(),
                    }
                }

            }
        };

        GuessDto o = guessRepo.Guess(guess, target);

        Assert.Equivalent(expected, o);
    }

}
