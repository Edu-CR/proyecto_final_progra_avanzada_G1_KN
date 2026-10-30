using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoFinalBioquimia_G1.Models
{
    // Modelo de la pantalla de gestión de empresas (RF-14).
    // Las validaciones van aquí porque las clases de EF se regeneran y se pierden los cambios.
    public class EmpresaModel
    {
        public int IdEmpresa { get; set; }

        [Display(Name = "Nombre de la empresa")]
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede tener más de 150 caracteres.")]
        public string NombreEmpresa { get; set; }

        [Display(Name = "Persona de contacto")]
        [StringLength(150, ErrorMessage = "El contacto no puede tener más de 150 caracteres.")]
        public string Contacto { get; set; }

        [Display(Name = "Teléfono")]
        [StringLength(20, ErrorMessage = "El teléfono no puede tener más de 20 caracteres.")]
        [RegularExpression(@"^[0-9+\-\s()]{8,20}$", ErrorMessage = "Ingrese un teléfono válido (solo números, espacios, +, - o paréntesis; mínimo 8).")]
        public string Telefono { get; set; }

        [Display(Name = "Correo electrónico")]
        [StringLength(100, ErrorMessage = "El correo no puede tener más de 100 caracteres.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        public string CorreoElectronico { get; set; }

        [Display(Name = "Estado")]
        public bool Estado { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime FechaCreacion { get; set; }
    }
}
