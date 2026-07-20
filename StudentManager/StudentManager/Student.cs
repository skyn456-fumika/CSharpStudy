using System.Text.Json.Serialization;

public class Student
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Korean { get; set; }

    public int English { get; set; }

    public int Math { get; set; }

    [JsonIgnore]
    public int Total => Korean + English + Math;

    [JsonIgnore]
    public double Average => Total / 3.0;

    [JsonIgnore]
    public bool IsPassed => Average >= 60;
}