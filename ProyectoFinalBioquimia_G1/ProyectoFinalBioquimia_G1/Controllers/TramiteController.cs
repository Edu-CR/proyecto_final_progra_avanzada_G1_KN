using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Models;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-10: Página "En trámite".
    // El químico ve sus prototipos que todavía no terminan el proceso
    // (Borrador, Enviado o Devuelto) y la última justificación del gerente.
    // TODO: roles. Cuando el login esté listo, validar que Session["Rol"] sea "Quimico".
    public class TramiteController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var quimico = ObtenerQuimicoActual(db);
                    if (quimico == null)
                    {
                        TempData["Error"] = "No se encontró el usuario químico de la sesión.";
                        return View(new List<PrototipoListaModel>());
                    }

                    var estadosEnTramite = new List<string> { "Borrador", "Enviado", "Devuelto" };
                    int idQuimico = quimico.IdUsuario;

                    var lista = db.TB_Prototipo
                        .Where(p => p.IdQuimico == idQuimico
                                 && estadosEnTramite.Contains(p.TB_EstadoPrototipo.NombreEstado))
                        .Select(p => new PrototipoListaModel
                        {
                            IdPrototipo = p.IdPrototipo,
                            NombrePrototipo = p.NombrePrototipo,
                            Categoria = p.TB_Categoria.NombreCategoria,
                            Quimico = p.TB_Usuario.NombreCompleto,
                            Estado = p.TB_EstadoPrototipo.NombreEstado,
                            ComentarioGerente = p.ComentarioGerente,
                            FechaCreacion = p.FechaCreacion,
                            FechaModificacion = p.FechaModificacion
                        })
                        .ToList()
                        // Primero los devueltos (el químico tiene que corregirlos), luego por fecha.
                        .OrderBy(p => p.Estado == "Devuelto" ? 0 : 1)
                        .ThenByDescending(p => p.UltimaActualizacion)
                        .ToList();

                    ViewBag.NombreQuimico = quimico.NombreCompleto;
                    ViewBag.TotalBorrador = lista.Count(p => p.Estado == "Borrador");
                    ViewBag.TotalEnviado = lista.Count(p => p.Estado == "Enviado");
                    ViewBag.TotalDevuelto = lista.Count(p => p.Estado == "Devuelto");
                    return View(lista);
                }
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                return View(new List<PrototipoListaModel>());
            }
        }

        // TODO: roles. Hoy, sin login, se usa el primer químico activo de los datos de prueba.
        // Cuando Persona 1 termine el login, dejar solo la lectura de Session["UsuarioId"].
        private TB_Usuario ObtenerQuimicoActual(BioquimiaDBEntities db)
        {
            if (Session["UsuarioId"] != null)
            {
                int idSesion = Convert.ToInt32(Session["UsuarioId"]);
                return db.TB_Usuario.FirstOrDefault(u => u.IdUsuario == idSesion);
            }

            return db.TB_Usuario
                .Where(u => u.Estado && u.TB_Rol.NombreRol == "Quimico")
                .OrderBy(u => u.IdUsuario)
                .FirstOrDefault();
        }
    }
}
