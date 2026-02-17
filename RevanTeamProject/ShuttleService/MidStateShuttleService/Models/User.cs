using System.ComponentModel.DataAnnotations;

namespace MidStateShuttleService.Models
{
    public class User
    {
        public int Id { get; set; }

        //Object ID For the Microsoft Identity Account
        public string AzureAdObjectId { get; set; }

        //The User's Phone Number
        [RegularExpression(@"^$|^\d{10}$",
        ErrorMessage = "Phone number must be exactly 10 digits if provided. (Only Numbers, No Spaces or Dashes)")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        //Mid-State Student ID
        [RegularExpression(@"^$|^\d{8}$",
        ErrorMessage = "Student ID must be exactly 8 digits if provided.")]
        public string? StudentId { get; set; }

        //The date the account was created, for record purposes
        public DateTime CreatedDate { get; set; }
    }
}