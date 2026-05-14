using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MidStateShuttleService.Models
{
    //THIS MODEL IS FOR STUDENTS
    [Table("Message")]
    public partial class Message
    {
        [Key]
        [Column("MessageId")]
        public int id { get; set; }

        [Required(ErrorMessage = "A message is required.")]
        [StringLength(160, ErrorMessage = "This field must not be more than 50 characters.")]
        [RegularExpression("^[A-Za-z\\s]{2,}$", ErrorMessage = "Must contain only characters and be at least 2 characters long")]
        public string name { get; set; }

        [Required(ErrorMessage = "A message is required.")]
        [StringLength(160, ErrorMessage = "This field must not be more than 160 characters.")]
        public string message { get; set; }

        [DefaultValue(false)]
        public bool responseRequired { get; set; }

        public string? Email { get; set; }

        public bool IsActive { get; set; }
    }
}
