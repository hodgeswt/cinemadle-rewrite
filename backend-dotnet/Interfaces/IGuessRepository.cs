using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;

namespace Cinemadle.Interfaces;

public interface IGuessRepository
{
    public GuessDto Guess(MovieDto guess, MovieDto target);

    public static readonly string RatingKey = "rating";
    public static readonly string CreativesKey = "creatives";
    public static readonly string BoxOfficeKey = "boxOffice";
    public static readonly string YearKey = "year";
    public static readonly string GenreKey = "genre";
    public static readonly string CastKey = "cast";

    public static readonly List<string> AllRatings = new() { "G", "PG", "PG13", "R", "NC17" };
}
