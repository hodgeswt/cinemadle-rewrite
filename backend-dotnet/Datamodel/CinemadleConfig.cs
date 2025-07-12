namespace Cinemadle.Datamodel;

public class CinemadleConfig
{
    public required string TmdbApiKey { get; set; }
    public required int CastCount { get; set; }
    public required int GenresCount { get; set; }
    public required int CacheTTL { get; set; }
    public required uint YearYellowThreshold { get; set; }
    public required uint YearSingleArrowThreshold { get; set; }
    public required uint YearDoubleArrowThreshold { get; set; }
    public required string OldestMoviePossible { get; set; }
    public required int MinimumVotesPossible { get; set; }
    public required int MinimumScorePossible { get; set; }
    public required int MinimumRuntimePossible { get; set; }
}
