using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string Email { get; set; }
    }
}
