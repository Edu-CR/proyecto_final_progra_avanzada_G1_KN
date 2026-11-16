
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System.Data.Entity;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-16: Histórico de prototipos con búsqueda y filtros.
    // Químico y gerente consultan todos los prototipos y el detalle de cada uno.
    public class HistoricoController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        // Verifica que exista una sesión valida, usuario activo y rol autorizado.
        // Permite gerente (2) y químico (1).
        private bool TieneAcceso()
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
                   && (idRol == 1 || idRol == 2)
                   && estadoUsuario;
        }

        #region Ver histórico de prototipos

        [HttpGet]
        public ActionResult Index(string buscar, int? idEstado, int? idCategoria, int? idQuimico, DateTime? desde, DateTime? hasta)
        {
            if (!TieneAcceso())
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    CargarListasViewBag(db, buscar, idEstado, idCategoria, idQuimico, desde, hasta);

                    if (desde.HasValue && hasta.HasValue && desde > hasta)
                    {
                        ViewBag.Advertencia = "La fecha \"Desde\" no puede ser mayor que la fecha \"Hasta\".";
                        return View(new List<TB_Prototipo>());
                    }

                    var consulta = db.TB_Prototipo
                        .Include(p => p.TB_Categoria)
                        .Include(p => p.TB_Usuario)
                        .Include(p => p.TB_EstadoPrototipo)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(buscar))
                    {
                        string texto = buscar.Trim();
                        consulta = consulta.Where(p => p.NombrePrototipo.Contains(texto)
                                                    || p.Descripcion.Contains(texto));
                    }

                    if (idEstado.HasValue)
                    {
                        int estadoVal = idEstado.Value;
                        consulta = consulta.Where(p => p.IdEstadoPrototipo == estadoVal);
                    }

                    if (idCategoria.HasValue)
                    {
                        int categoriaVal = idCategoria.Value;
                        consulta = consulta.Where(p => p.IdCategoria == categoriaVal);
                    }

                    if (idQuimico.HasValue)
                    {
                        int quimicoVal = idQuimico.Value;
                        consulta = consulta.Where(p => p.IdQuimico == quimicoVal);
                    }

                    if (desde.HasValue)
                    {
                        DateTime desdeVal = desde.Value.Date;
                        consulta = consulta.Where(p => p.FechaCreacion >= desdeVal);
                    }

                    if (hasta.HasValue)
                    {
                        // Incluye todo el día "Hasta".
                        DateTime hastaExclusivo = hasta.Value.Date.AddDays(1);
                        consulta = consulta.Where(p => p.FechaCreacion < hastaExclusivo);
                    }

                    // Se retorna directamente la lista de entidades de EF (TB_Prototipo).
                    var lista = consulta
                        .OrderByDescending(p => p.FechaCreacion)
                        .ToList();

                    return View(lista);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, HistoricoController - HttpGet -> Index()");

                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Estados = new SelectList(new List<SelectListItem>());
                ViewBag.Categorias = new SelectList(new List<SelectListItem>());
                ViewBag.Quimicos = new SelectList(new List<SelectListItem>());

                return View(new List<TB_Prototipo>());
            }
        }

        #endregion

        #region Detalle de prototipo

        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if (!TieneAcceso())
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipo = db.TB_Prototipo
                        .Include(p => p.TB_Categoria)
                        .Include(p => p.TB_Usuario)
                        .Include(p => p.TB_EstadoPrototipo)
                        .Include(p => p.TB_Empresa)
                        .Include(p => p.TB_PrototipoIngrediente.Select(pi => pi.TB_Ingrediente))
                        .Include(p => p.TB_HistorialEstado.Select(h => h.TB_EstadoPrototipo))
                        .Include(p => p.TB_HistorialEstado.Select(h => h.TB_EstadoPrototipo1))
                        .Include(p => p.TB_HistorialEstado.Select(h => h.TB_Usuario))
                        .SingleOrDefault(p => p.IdPrototipo == id);

                    if (prototipo == null)
                    {
                        TempData["Error"] = "El prototipo no existe.";
                        return RedirectToAction("Index");
                    }

                    return View(prototipo);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, HistoricoController - HttpGet -> Detalle()");

                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        #endregion

        // Carga los filtros de búsqueda para la vista.
        private void CargarListasViewBag(BioquimiaDBEntities db, string buscar, int? idEstado, int? idCategoria, int? idQuimico, DateTime? desde, DateTime? hasta)
        {
            ViewBag.Buscar = buscar;
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

            ViewBag.Estados = new SelectList(
                db.TB_EstadoPrototipo.OrderBy(e => e.IdEstadoPrototipo).ToList(),
                "IdEstadoPrototipo", "NombreEstado", idEstado);

            ViewBag.Categorias = new SelectList(
                db.TB_Categoria.OrderBy(c => c.NombreCategoria).ToList(),
                "IdCategoria", "NombreCategoria", idCategoria);

            ViewBag.Quimicos = new SelectList(
                db.TB_Usuario.Where(u => u.TB_Rol.NombreRol == "Quimico")
                             .OrderBy(u => u.NombreCompleto).ToList(),
                "IdUsuario", "NombreCompleto", idQuimico);
        }
    }
}
