using System.ComponentModel.DataAnnotations;

namespace RamaExpress.Models
{
    public class Admin
    {
        public int Id { get; set; }
        [Required]
        public string Nama { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
