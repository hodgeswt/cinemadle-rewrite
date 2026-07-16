using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

[PrimaryKey(nameof(JobName))]
public class OneTimeJob
{
    public required string JobName { get; init; }
    
    public bool Completed { get; init; } 
}