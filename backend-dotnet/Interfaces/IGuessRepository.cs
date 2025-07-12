using Cinemadle.Datamodel;

namespace Cinemadle.Interfaces;

public interface IGuessRepository
{
    public GuessDto Guess(MovieDto guess, MovieDto target);
}
