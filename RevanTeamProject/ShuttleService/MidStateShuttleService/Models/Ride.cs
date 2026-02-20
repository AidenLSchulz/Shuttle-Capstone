using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Ride")]
public class Ride
{
    [Key]
    public int RideId { get; set; }

    public int RequestDayId { get; set; }

    [ForeignKey(nameof(RequestDayId))]
    public RequestDay? RequestDay { get; set; }

    // Locations
    [Required]
    public int PickUpLocationID { get; set; }

    [Required]
    public int DropOffLocationID { get; set; }

    // Time they must arrive
    [Required]
    public TimeOnly DropOffTime { get; set; }
}