using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Models;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-14: Gestión de empresas.
    // Gerente: registra, edita, consulta y desactiva (eliminación lógica).
    // Químico: no tiene acceso.
    // TODO: roles. Cuando el login esté listo, validar que Session["Rol"] sea "Gerente"
    // en todas las acciones; si no, redirigir al login o a la página de inicio.
    public class EmpresaController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        [HttpGet]
        public ActionResult Index(string buscar, string filtro = "activas")
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var consulta = db.TB_Empresa.AsQueryable();

                    if (filtro == "activas")
                        consulta = consulta.Where(e => e.Estado);
                    else if (filtro == "inactivas")
                        consulta = consulta.Where(e => !e.Estado);

                    if (!string.IsNullOrWhiteSpace(buscar))
                    {
                        string texto = buscar.Trim();
                        consulta = consulta.Where(e => e.NombreEmpresa.Contains(texto)
                                                    || e.Contacto.Contains(texto)
                                                    || e.CorreoElectronico.Contains(texto));
                    }

                    var lista = consulta
                        .OrderBy(e => e.NombreEmpresa)
                        .Select(e => new EmpresaModel
                        {
                            IdEmpresa = e.IdEmpresa,
                            NombreEmpresa = e.NombreEmpresa,
                            Contacto = e.Contacto,
                            Telefono = e.Telefono,
                            CorreoElectronico = e.CorreoElectronico,
                            Estado = e.Estado,
                            FechaCreacion = e.FechaCreacion
                        })
                        .ToList();

                    ViewBag.Buscar = buscar;
                    ViewBag.Filtro = filtro;
                    return View(lista);
                }
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;
                return View(new List<EmpresaModel>());
            }
        }

        [HttpGet]
        public ActionResult Crear()
        {
            return View(new EmpresaModel { Estado = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(EmpresaModel modelo)
        {
            try
            {
                Normalizar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    if (ModelState.IsValid && ExisteNombre(db, modelo.NombreEmpresa, null))
                        ModelState.AddModelError("NombreEmpresa", "Ya existe una empresa con ese nombre.");

                    if (!ModelState.IsValid)
                        return View(modelo);

                    db.TB_Empresa.Add(new TB_Empresa
                    {
                        NombreEmpresa = modelo.NombreEmpresa,
                        Contacto = modelo.Contacto,
                        Telefono = modelo.Telefono,
                        CorreoElectronico = modelo.CorreoElectronico,
                        Estado = true,
                        FechaCreacion = DateTime.Now
                    });
                    db.SaveChanges();
                }

                TempData["Exito"] = "La empresa se registró correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                return View(modelo);
            }
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var empresa = db.TB_Empresa.Find(id);
                    if (empresa == null)
                    {
                        TempData["Error"] = "La empresa no existe.";
                        return RedirectToAction("Index");
                    }

                    return View(new EmpresaModel
                    {
                        IdEmpresa = empresa.IdEmpresa,
                        NombreEmpresa = empresa.NombreEmpresa,
                        Contacto = empresa.Contacto,
                        Telefono = empresa.Telefono,
                        CorreoElectronico = empresa.CorreoElectronico,
                        Estado = empresa.Estado,
                        FechaCreacion = empresa.FechaCreacion
                    });
                }
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(EmpresaModel modelo)
        {
            try
            {
                Normalizar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    var empresa = db.TB_Empresa.Find(modelo.IdEmpresa);
                    if (empresa == null)
                    {
                        TempData["Error"] = "La empresa no existe.";
                        return RedirectToAction("Index");
                    }

                    if (ModelState.IsValid && ExisteNombre(db, modelo.NombreEmpresa, modelo.IdEmpresa))
                        ModelState.AddModelError("NombreEmpresa", "Ya existe una empresa con ese nombre.");

                    if (!ModelState.IsValid)
                    {
                        modelo.Estado = empresa.Estado;
                        return View(modelo);
                    }

                    empresa.NombreEmpresa = modelo.NombreEmpresa;
                    empresa.Contacto = modelo.Contacto;
                    empresa.Telefono = modelo.Telefono;
                    empresa.CorreoElectronico = modelo.CorreoElectronico;
                    db.SaveChanges();
                }

                TempData["Exito"] = "La empresa se actualizó correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                return View(modelo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Desactivar(int id)
        {
            return CambiarEstado(id, false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Activar(int id)
        {
            return CambiarEstado(id, true);
        }

        private ActionResult CambiarEstado(int id, bool activar)
        {
            object filtroRegreso = activar ? new { filtro = "inactivas" } : null;

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var empresa = db.TB_Empresa.Find(id);
                    if (empresa == null)
                    {
                        TempData["Error"] = "La empresa no existe.";
                        return RedirectToAction("Index", filtroRegreso);
                    }

                    empresa.Estado = activar;
                    db.SaveChanges();
                    TempData["Exito"] = "La empresa \"" + empresa.NombreEmpresa + "\" se "
                        + (activar ? "activó" : "desactivó") + " correctamente.";
                }
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
            }

            return RedirectToAction("Index", filtroRegreso);
        }

        // Quita espacios y guarda null en los campos opcionales vacíos.
        private static void Normalizar(EmpresaModel modelo)
        {
            if (modelo == null) return;
            modelo.NombreEmpresa = modelo.NombreEmpresa?.Trim();
            modelo.Contacto = string.IsNullOrWhiteSpace(modelo.Contacto) ? null : modelo.Contacto.Trim();
            modelo.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
            modelo.CorreoElectronico = string.IsNullOrWhiteSpace(modelo.CorreoElectronico) ? null : modelo.CorreoElectronico.Trim();
        }

        // La comparación no distingue mayúsculas porque la intercalación de SQL Server es CI.
        private static bool ExisteNombre(BioquimiaDBEntities db, string nombre, int? idExcluir)
        {
            return db.TB_Empresa.Any(e => e.NombreEmpresa == nombre
                                       && (idExcluir == null || e.IdEmpresa != idExcluir));
        }
    }
}
