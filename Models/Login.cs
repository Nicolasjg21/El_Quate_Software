using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Campo requerido")]
        [EmailAddress(ErrorMessage = "Correo electrónico no válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string Password { get; set; }
    }
}