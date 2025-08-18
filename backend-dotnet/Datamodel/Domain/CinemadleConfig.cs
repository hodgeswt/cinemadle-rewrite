namespace Cinemadle.Datamodel.Domain;

public class CinemadleConfig
{
    public required string TmdbApiKey { get; set; }
    public required string PaymentsApiKey { get; set; }
    public required string WebhookSecret { get; set; }
    public required int CastCount { get; set; }
    public required int GenresCount { get; set; }
    public required int CacheTTL { get; set; }
    public required uint YearYellowThreshold { get; set; }
    public required uint BoxOfficeYellowThreshold { get; set; }
    public required uint YearSingleArrowThreshold { get; set; }
    public required uint BoxOfficeSingleArrowThreshold { get; set; }
    public required uint YearDoubleArrowThreshold { get; set; }
    public required string OldestMoviePossible { get; set; }
    public required int MinimumVotesPossible { get; set; }
    public required int MinimumScorePossible { get; set; }
    public required int MinimumRuntimePossible { get; set; }
    public required Dictionary<string, float> MovieImageBlurFactors { get; set; }
    public required int GameLength { get; set; }
    public required string PaymentSuccessUrl { get; set; }
    public required string PaymentFailureUrl { get; set; }
    public required Dictionary<string, AddOnDetails> AddOnMapping { get; set; }
}
