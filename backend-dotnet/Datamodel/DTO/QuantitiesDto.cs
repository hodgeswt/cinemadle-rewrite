using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;

namespace Cinemadle.Datamodel.DTO;

public class QuantitiesDto
{
    public required Dictionary<AddOn, long> Quantities {get; set;}
}