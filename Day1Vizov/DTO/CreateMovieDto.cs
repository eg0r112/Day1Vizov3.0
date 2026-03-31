using System.Collections.Generic;

namespace Day1Vizov.DTOs;

public class CreateMovieDto
{
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ActorInMovieDto> Actors { get; set; } = new();
}

public class ActorInMovieDto
{
    public int ActorId { get; set; }
    public string Role { get; set; } = string.Empty;
}