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

        private const int RolQuimico = 1;

        // Se ejecuta antes de cada acción: sin sesión activa va al login,
        // y si el perfil no es Químico va a la página 404.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true)
            {
                filterContext.Result = RedirectToAction("Login", "Auth");
                return;
            }

            if (Convert.ToInt32(Session["RolUsuarioSession"]) != RolQuimico)
            {
                filterContext.Result = RedirectToAction("Error404", "Error");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    int idQuimico = Convert.ToInt32(Session["IdUsuarioSession"]);
                    var quimico = db.TB_Usuario.FirstOrDefault(u => u.IdUsuario == idQuimico);
                    if (quimico == null)
                    {
                        TempData["Error"] = "No se encontró el usuario químico de la sesión.";
                        return View(new List<TB_Prototipo>());
                    }

                    var estadosEnTramite = new List<string> { "Borrador", "Enviado", "Devuelto" };

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
                BitacoraService.Registrar(ex, "Ocurrio una excepción, TramiteController - HttpGet -> Index()");
                TempData["Error"] = MensajeErrorGenerico;
                return View(new List<TB_Prototipo>());
            }
        }
    }
}
