namespace Cinemadle.Datamodel.Domain;

public enum Rating
{
    UNKNOWN = 0,
    G,
    PG,
    PG13,
    R,
    NC17
}

public enum CustomRoles
{
    Admin,
}

public enum AddOn
{
    VisualClue,
    CategoryReveal,
}

public enum FeatureFlags
{
    PaymentsEnabled,
    MigrateUsers
}