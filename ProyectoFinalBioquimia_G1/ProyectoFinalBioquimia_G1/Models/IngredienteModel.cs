using System.ComponentModel.DataAnnotations;

namespace ProyectoFinalBioquimia_G1.Models
{
    // Modelo de la pantalla del catálogo de ingredientes (RF-23).
    // Las validaciones van aquí porque las clases de EF se regeneran y se pierden los cambios.
    public class IngredienteModel
    {
        public int IdIngrediente { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre del ingrediente es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string NombreIngrediente { get; set; }

        [Display(Name = "Función")]
        [Required(ErrorMessage = "La función del ingrediente es obligatoria.")]
        [StringLength(50, ErrorMessage = "La función no puede tener más de 50 caracteres.")]
        public string Funcion { get; set; }

        [Display(Name = "Unidad base")]
        [Required(ErrorMessage = "Seleccione la unidad base.")]
        [RegularExpression("^(kg|litro)$", ErrorMessage = "La unidad base debe ser kg o litro.")]
        public string UnidadBase { get; set; }

        [Display(Name = "Estado")]
        public bool Estado { get; set; }
    }
}
