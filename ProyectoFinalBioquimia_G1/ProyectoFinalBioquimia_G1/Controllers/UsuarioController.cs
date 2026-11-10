using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // TODO: roles. Validar que Session["Rol"] sea "Gerente" cuando el login esté listo.
    public class UsuarioController : Controller
    {
        private const string MensajeErrorGenerico ="Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private BioquimiaDBEntities context = new BioquimiaDBEntities();

        [HttpGet]
        public ActionResult VerUsuarios(string buscar, string filtro = "activos")
        {
            try
            {
                var consulta = context.TB_Usuario
                                      .Include(u => u.TB_Rol)
                                      .AsQueryable();

                if (filtro == "activos")
                    consulta = consulta.Where(u => u.Estado);
                else if (filtro == "inactivos")
                    consulta = consulta.Where(u => !u.Estado);

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string texto = buscar.Trim();
                    consulta = consulta.Where(u => u.NombreCompleto.Contains(texto)
                                                || u.Identificacion.Contains(texto)
                                                || u.CorreoElectronico.Contains(texto));
                }

                var lista = consulta.OrderBy(u => u.NombreCompleto).ToList();

                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;
                return View("Index", lista);
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpGet -> VerUsuarios()");
                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;
                return View("Index", new List<TB_Usuario>());
            }
        }



        #region Dispose
        protected override void Dispose(bool disposing)// Como el context es un campo de la clase, hay que liberarlo al terminar la petición como buena practica
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