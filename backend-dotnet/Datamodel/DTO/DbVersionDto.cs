namespace Cinemadle.Datamodel.DTO;

public readonly record struct DbVersionDto(string MainDbVersion, string IdentityDbVersion);