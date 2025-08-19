using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;

namespace Cinemadle.Interfaces;

public interface ITmdbRepository
{
    public Task<MovieDto?> GetMovie(string name);
    public Task<MovieDto?> GetMovieById(int id);
    public Task<MovieDto?> GetTargetMovie(string date);
    public Task<Dictionary<string, int>> GetMovieList();
    public Task<byte[]?> GetMovieImageById(int id);
    public void RigMovie(int id);
    public void UnrigMovie();
}
