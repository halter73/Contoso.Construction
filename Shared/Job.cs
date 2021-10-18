using System.ComponentModel.DataAnnotations.Schema;

public class Job
{
    [DatabaseGenerated(
        DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; }
        = string.Empty;
}