namespace Day1Vizov.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
}