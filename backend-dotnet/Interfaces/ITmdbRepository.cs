using Cinemadle.Datamodel;

namespace Cinemadle.Interfaces;

public interface ITmdbRepository
{
    public Task<MovieDto?> GetMovie(string name);
}
