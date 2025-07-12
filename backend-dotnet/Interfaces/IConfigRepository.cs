using Cinemadle.Datamodel;

namespace Cinemadle.Interfaces;

public interface IConfigRepository
{
    CinemadleConfig GetConfig();
    bool IsLoaded();
}
