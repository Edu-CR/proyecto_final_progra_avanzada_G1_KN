
using System;
using System.Collections.Generic;
using System.Data.Entity; // necesario para .Include(x => x.Propiedad)
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-10: Página "En trámite".
    // El químico ve sus prototipos que todavía no terminan el proceso
    // (Borrador, Enviado o Devuelto) y la última justificación del gerente.
    public class TramiteController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private bool EsQuimico()
        {
            if (Session["IdUsuarioSession"] == null ||
                Session["RolUsuarioSession"] == null ||
                Session["EstadoUsuarioSession"] == null)
            {
                return false;
            }

            int idUsuario;
            int idRol;
            bool estadoUsuario;

            return int.TryParse(Convert.ToString(Session["IdUsuarioSession"]), out idUsuario)
                   && int.TryParse(Convert.ToString(Session["RolUsuarioSession"]), out idRol)
                   && bool.TryParse(Convert.ToString(Session["EstadoUsuarioSession"]), out estadoUsuario)
                   && idUsuario > 0
                   && idRol == 1
                   && estadoUsuario;
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var quimico = ObtenerQuimicoActual(db);

                    if (quimico == null)
                    {
                        Session.Clear();
                        Session.Abandon();

                        return RedirectToAction("Login", "Auth");
                    }

                    var estadosEnTramite = new List<string>
                    {
                        "Borrador",
                        "Enviado",
                        "Devuelto"
                    };

                    int idQuimico = quimico.IdUsuario;

                    // Include carga las tablas relacionadas antes de cerrar el contexto,
                    // para que la vista pueda leer Categoria, Usuario y Estado sin lazy loading.
                    var lista = db.TB_Prototipo
                        .Include(p => p.TB_Categoria)
                        .Include(p => p.TB_Usuario)
                        .Include(p => p.TB_EstadoPrototipo)
                        .Where(p => p.IdQuimico == idQuimico
                                 && estadosEnTramite.Contains(p.TB_EstadoPrototipo.NombreEstado))
                        .ToList()
                        // Primero los devueltos (el químico tiene que corregirlos), luego por fecha.
                        .OrderBy(p => p.TB_EstadoPrototipo.NombreEstado == "Devuelto" ? 0 : 1)
                        .ThenByDescending(p => p.FechaModificacion ?? p.FechaCreacion)
                        .ToList();

                    ViewBag.NombreQuimico = quimico.NombreCompleto;
                    ViewBag.TotalBorrador = lista.Count(p => p.TB_EstadoPrototipo.NombreEstado == "Borrador");
                    ViewBag.TotalEnviado = lista.Count(p => p.TB_EstadoPrototipo.NombreEstado == "Enviado");
                    ViewBag.TotalDevuelto = lista.Count(p => p.TB_EstadoPrototipo.NombreEstado == "Devuelto");

                    return View(lista);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, TramiteController - HttpGet -> Index()");

                TempData["Error"] = MensajeErrorGenerico;
                return View(new List<TB_Prototipo>());
            }
        }

        // Obtiene al químico correspondiente a la sesión actual.
        // No se utiliza un usuario de prueba como alternativa, ahora cada químico ve exclusivamente sus propios prototipos, según su IdUsuarioSession.
        private TB_Usuario ObtenerQuimicoActual(BioquimiaDBEntities db)
        {
            int idSesion;

            if (!int.TryParse(Convert.ToString(Session["IdUsuarioSession"]), out idSesion) ||
                idSesion <= 0)
            {
                return null;
            }

            return db.TB_Usuario.FirstOrDefault(u =>
                u.IdUsuario == idSesion &&
                u.Estado == true &&
                u.IdRol == 1);
        }
    }
}
