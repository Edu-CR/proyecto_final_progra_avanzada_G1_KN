using System;
using System.Collections.Generic;

namespace ProyectoFinalBioquimia_G1.Models
{
    // Fila de los listados de prototipos: "En trámite" (RF-10) e histórico (RF-16).
    public class PrototipoListaModel
    {
        public int IdPrototipo { get; set; }
        public string NombrePrototipo { get; set; }
        public string Categoria { get; set; }
        public string Quimico { get; set; }
        public string Estado { get; set; }
        public string ComentarioGerente { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Fecha del último movimiento: la modificación si existe, si no la creación.
        public DateTime UltimaActualizacion
        {
            get { return FechaModificacion ?? FechaCreacion; }
        }

        // Color del badge según el estado (Bootstrap 5).
        public static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case "Borrador": return "text-bg-secondary";
                case "Enviado": return "text-bg-primary";
                case "Devuelto": return "text-bg-warning";
                case "Aprobado": return "text-bg-success";
                case "Cancelado": return "text-bg-danger";
                default: return "text-bg-light";
            }
        }
    }

    // Filtros del histórico (RF-16).
    public class HistoricoFiltroModel
    {
        public string Buscar { get; set; }
        public int? IdEstado { get; set; }
        public int? IdCategoria { get; set; }
        public int? IdQuimico { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
    }

    // Detalle de un prototipo con su fórmula, empresas y cambios de estado.
    public class PrototipoDetalleModel : PrototipoListaModel
    {
        public string Descripcion { get; set; }
        public string CaracteristicasEnvase { get; set; }
        public string ImagenRuta { get; set; }
        public List<IngredienteFormulaModel> Formula { get; set; } = new List<IngredienteFormulaModel>();
        public List<string> Empresas { get; set; } = new List<string>();
        public List<CambioEstadoModel> Historial { get; set; } = new List<CambioEstadoModel>();
    }

    public class IngredienteFormulaModel
    {
        public string NombreIngrediente { get; set; }
        public string Funcion { get; set; }
        public string UnidadBase { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class CambioEstadoModel
    {
        public DateTime FechaCambio { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; }
        public string Usuario { get; set; }
        public string Justificacion { get; set; }
    }
}
