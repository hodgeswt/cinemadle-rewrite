using Cinemadle.Datamodel;

namespace Cinemadle.Interfaces;

public interface ITmdbRepository
{
    public Task<MovieDto?> GetMovie(string name);
    public Task<MovieDto?> GetMovieById(int id);
    public Task<MovieDto?> GetTargetMovie(string date);
    public Task<Dictionary<string, int>> GetMovieList();
}
