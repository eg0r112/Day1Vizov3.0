using System.Collections.Generic;

namespace Day1Vizov.DTOs;

public class AddActorsToMovieDto
{
    public List<int> ActorIds { get; set; } = new();
}