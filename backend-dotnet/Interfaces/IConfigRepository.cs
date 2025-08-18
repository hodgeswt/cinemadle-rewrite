using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;

namespace Cinemadle.Interfaces;

public interface IConfigRepository
{
    CinemadleConfig GetConfig();
    bool IsLoaded();
}
