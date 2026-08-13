using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ElQuateSoftware.Models
{
    public class Cuentas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idCuenta { get; set; }
        [Required]
        [ForeignKey(nameof(idMesa))]
        public int idMesa { get; set; }
        [Required]
        [StringLength(20, ErrorMessage = "El estado no puede tener más de 20 caracteres.")]
        public string estado { get; set; }
        [Required]
        public DateTime fechaApertura { get; set; }
        public DateTime? fechaCierre { get; set; }
        [Required]
        [Precision(10, 2)]
        public decimal total { get; set; }
    }
}