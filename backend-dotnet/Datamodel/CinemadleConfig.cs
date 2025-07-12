namespace Cinemadle.Datamodel;

public class CinemadleConfig
{
    public required string TmdbApiKey { get; set; }
    public required int CastCount { get; set; }
    public required int GenresCount { get; set; }
    public required int CacheTTL { get; set; }
}
