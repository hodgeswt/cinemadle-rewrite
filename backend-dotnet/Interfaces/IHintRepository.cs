using Cinemadle.Datamodel.DTO;

namespace Cinemadle.Interfaces;

public interface IHintRepository
{
    /// <summary>
    /// Gets hints for a user's game, computing and caching if necessary.
    /// </summary>
    /// <param name="userId">The user ID (can be authenticated user ID or anonymous user ID)</param>
    /// <param name="gameId">The game ID (date string for daily games, GUID for custom games)</param>
    /// <param name="isAnonymous">Whether this is an anonymous user</param>
    /// <param name="isCustomGame">Whether this is a custom game</param>
    /// <returns>Dictionary of field key to hints</returns>
    Task<Dictionary<string, HintsDto>> GetHints(string userId, string gameId, bool isAnonymous = false, bool isCustomGame = false);

    /// <summary>
    /// Invalidates cached hints for a user's game (call after a new guess).
    /// </summary>
    void InvalidateHints(string userId, string gameId);
}
