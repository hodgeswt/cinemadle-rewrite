namespace Cinemadle.Interfaces;

public interface ICacheRepository
{
    public bool Set(string key, object value);
    public bool TryGet<T>(string key, out T? value) where T : class;
    public void Remove(string key);
}
