namespace Cinemadle.Interfaces;

public interface IFeatureFlagRepository
{
    public Task<bool> Get(string name);
    public Task<Dictionary<string, bool>> GetAll();
}