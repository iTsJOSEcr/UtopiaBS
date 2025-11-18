using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace UtopiaBS.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Usuario o Correo")]
        public string UserName { get; set; }


        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
