using System;
using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.Models
{
    public class UpdateProfileViewModel
    {
        [Required, Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required, Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required, Display(Name = "Apellido")]
        public string Apellido { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{8,15}$", ErrorMessage = "El teléfono solo puede contener números (8 a 15 dígitos).")]
        [Display(Name = "Teléfono")]
        public string PhoneNumber { get; set; }


        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime FechaNacimiento { get; set; }
    }
}
