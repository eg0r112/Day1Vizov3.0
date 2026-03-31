namespace Day1Vizov.Models;

public class Actor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    
    public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
}