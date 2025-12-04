using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UtopiaBS.Models
{
    public class RegisterViewModel
    {

        [Required, Display(Name = "Nombre de Usuario")]
        public string UserName { get; set; }

        [Required, Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required, Display(Name = "Apellido")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [StringLength(50)]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^[0-9]{8,15}$", ErrorMessage = "El teléfono solo puede contener números (8 a 15 dígitos).")]
        [Display(Name = "Teléfono")]
        public string PhoneNumber { get; set; }


        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime FechaNacimiento { get; set; }


        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }
    }
}