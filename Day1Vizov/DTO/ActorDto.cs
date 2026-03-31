using System.Collections.Generic;

namespace Day1Vizov.DTOs;

public class ActorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    public int MoviesCount { get; set; }
    public List<MovieSimpleDto> Movies { get; set; } = new();
}

public class MovieSimpleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
}