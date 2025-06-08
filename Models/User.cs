using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Nama { get; set; }
        public string Posisi { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; }
    }
}
