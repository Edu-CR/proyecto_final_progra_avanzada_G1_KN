
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class PerfilController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";


        private BioquimiaDBEntities context = new BioquimiaDBEntities();

        [HttpGet]
        public ActionResult VerPerfil()
        {
            try
            {
                // Cualquier usuario activo con sesion iniciada puede ver su perfil, independiente del rol
                if (Session["IdUsuarioSession"] == null ||
                    Session["EstadoUsuarioSession"] == null ||
                    !bool.TryParse(Convert.ToString(Session["EstadoUsuarioSession"]), out bool estadoUsuario) ||
                    !estadoUsuario)
                {
                    return RedirectToAction("Error404", "Error");
                }

                int idUsuarioConvertido;

                if (!int.TryParse(Convert.ToString(Session["IdUsuarioSession"]), out idUsuarioConvertido) ||
                    idUsuarioConvertido <= 0)
                {
                    Session.Clear();
                    Session.Abandon();
                    return RedirectToAction("Login", "Auth");
                }

                var response = (from u in context.TB_Usuario
                                join r in context.TB_Rol on u.IdRol equals r.IdRol
                                where u.IdUsuario == idUsuarioConvertido && u.Estado == true
                                select new
                                {
                                    u.Identificacion,
                                    u.NombreCompleto,
                                    u.CorreoElectronico,
                                    r.NombreRol,
                                    u.FechaCreacion
                                }).FirstOrDefault();

                if (response != null)
                {
                    ViewBag.Identificacion = response.Identificacion;
                    ViewBag.NombreCompleto = response.NombreCompleto;
                    ViewBag.CorreoElectronico = response.CorreoElectronico;
                    ViewBag.NombreRol = response.NombreRol;
                    ViewBag.FechaCreacion = response.FechaCreacion;

                    return View();
                }
                else
                {
                    Session.Clear();
                    Session.Abandon();

                    return RedirectToAction("Login", "Auth");
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, PerfilController - HttpGet -> VerPerfil()");

                TempData["Message"] = MensajeErrorGenerico;
                return RedirectToAction("Error404", "Error");
            }
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
