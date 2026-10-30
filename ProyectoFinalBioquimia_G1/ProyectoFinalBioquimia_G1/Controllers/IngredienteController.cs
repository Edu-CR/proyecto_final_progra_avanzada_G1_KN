using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Models;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-23: Catálogo de ingredientes.
    // Químico: registra, edita, consulta y desactiva (eliminación lógica).
    // Gerente: solo consulta el catálogo.
    // TODO: roles. Cuando el login esté listo, validar Session["Rol"]:
    // Crear, Editar, Desactivar y Activar solo para "Quimico"; Index para ambos perfiles.
    public class IngredienteController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        [HttpGet]
        public ActionResult Index(string buscar, string filtro = "activos")
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var consulta = db.TB_Ingrediente.AsQueryable();

                    if (filtro == "activos")
                        consulta = consulta.Where(i => i.Estado);
                    else if (filtro == "inactivos")
                        consulta = consulta.Where(i => !i.Estado);

                    if (!string.IsNullOrWhiteSpace(buscar))
                    {
                        string texto = buscar.Trim();
                        consulta = consulta.Where(i => i.NombreIngrediente.Contains(texto)
                                                    || i.Funcion.Contains(texto));
                    }

                    var lista = consulta
                        .OrderBy(i => i.NombreIngrediente)
                        .Select(i => new IngredienteModel
                        {
                            IdIngrediente = i.IdIngrediente,
                            NombreIngrediente = i.NombreIngrediente,
                            Funcion = i.Funcion,
                            UnidadBase = i.UnidadBase,
                            Estado = i.Estado
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
                return View(new List<IngredienteModel>());
            }
        }

        [HttpGet]
        public ActionResult Crear()
        {
            return View(new IngredienteModel { UnidadBase = "kg", Estado = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(IngredienteModel modelo)
        {
            try
            {
                Normalizar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    if (ModelState.IsValid && ExisteNombre(db, modelo.NombreIngrediente, null))
                        ModelState.AddModelError("NombreIngrediente", "Ya existe un ingrediente con ese nombre.");

                    if (!ModelState.IsValid)
                        return View(modelo);

                    db.TB_Ingrediente.Add(new TB_Ingrediente
                    {
                        NombreIngrediente = modelo.NombreIngrediente,
                        Funcion = modelo.Funcion,
                        UnidadBase = modelo.UnidadBase,
                        Estado = true
                    });
                    db.SaveChanges();
                }

                TempData["Exito"] = "El ingrediente se registró correctamente.";
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
                    var ingrediente = db.TB_Ingrediente.Find(id);
                    if (ingrediente == null)
                    {
                        TempData["Error"] = "El ingrediente no existe.";
                        return RedirectToAction("Index");
                    }

                    return View(new IngredienteModel
                    {
                        IdIngrediente = ingrediente.IdIngrediente,
                        NombreIngrediente = ingrediente.NombreIngrediente,
                        Funcion = ingrediente.Funcion,
                        UnidadBase = ingrediente.UnidadBase,
                        Estado = ingrediente.Estado
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
        public ActionResult Editar(IngredienteModel modelo)
        {
            try
            {
                Normalizar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    var ingrediente = db.TB_Ingrediente.Find(modelo.IdIngrediente);
                    if (ingrediente == null)
                    {
                        TempData["Error"] = "El ingrediente no existe.";
                        return RedirectToAction("Index");
                    }

                    if (ModelState.IsValid && ExisteNombre(db, modelo.NombreIngrediente, modelo.IdIngrediente))
                        ModelState.AddModelError("NombreIngrediente", "Ya existe un ingrediente con ese nombre.");

                    if (!ModelState.IsValid)
                    {
                        modelo.Estado = ingrediente.Estado;
                        return View(modelo);
                    }

                    ingrediente.NombreIngrediente = modelo.NombreIngrediente;
                    ingrediente.Funcion = modelo.Funcion;
                    ingrediente.UnidadBase = modelo.UnidadBase;
                    db.SaveChanges();
                }

                TempData["Exito"] = "El ingrediente se actualizó correctamente.";
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
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var ingrediente = db.TB_Ingrediente.Find(id);
                    if (ingrediente == null)
                    {
                        TempData["Error"] = "El ingrediente no existe.";
                        return RedirectToAction("Index");
                    }

                    // RF-23: no se puede desactivar si está en un prototipo Borrador, Enviado o Devuelto.
                    var estadosQueBloquean = new List<string> { "Borrador", "Enviado", "Devuelto" };
                    var prototiposEnUso = db.TB_PrototipoIngrediente
                        .Where(pi => pi.IdIngrediente == id
                                  && estadosQueBloquean.Contains(pi.TB_Prototipo.TB_EstadoPrototipo.NombreEstado))
                        .Select(pi => pi.TB_Prototipo.NombrePrototipo)
                        .Distinct()
                        .ToList();

                    if (prototiposEnUso.Any())
                    {
                        TempData["Error"] = "No se puede desactivar \"" + ingrediente.NombreIngrediente
                            + "\" porque se usa en prototipos en estado Borrador, Enviado o Devuelto: "
                            + string.Join(", ", prototiposEnUso) + ".";
                        return RedirectToAction("Index");
                    }

                    ingrediente.Estado = false;
                    db.SaveChanges();
                    TempData["Exito"] = "El ingrediente \"" + ingrediente.NombreIngrediente + "\" se desactivó correctamente.";
                }

                return RedirectToAction("Index");
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
        public ActionResult Activar(int id)
        {
            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var ingrediente = db.TB_Ingrediente.Find(id);
                    if (ingrediente == null)
                    {
                        TempData["Error"] = "El ingrediente no existe.";
                        return RedirectToAction("Index", new { filtro = "inactivos" });
                    }

                    ingrediente.Estado = true;
                    db.SaveChanges();
                    TempData["Exito"] = "El ingrediente \"" + ingrediente.NombreIngrediente + "\" se activó correctamente.";
                }

                return RedirectToAction("Index", new { filtro = "inactivos" });
            }
            catch (Exception)
            {
                // TODO: bitácora
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index", new { filtro = "inactivos" });
            }
        }

        // Quita espacios al inicio y al final para evitar duplicados como "Glicerina " y "Glicerina".
        private static void Normalizar(IngredienteModel modelo)
        {
            if (modelo == null) return;
            modelo.NombreIngrediente = modelo.NombreIngrediente?.Trim();
            modelo.Funcion = modelo.Funcion?.Trim();
            modelo.UnidadBase = modelo.UnidadBase?.Trim();
        }

        // La comparación no distingue mayúsculas porque la intercalación de SQL Server es CI.
        private static bool ExisteNombre(BioquimiaDBEntities db, string nombre, int? idExcluir)
        {
            return db.TB_Ingrediente.Any(i => i.NombreIngrediente == nombre
                                           && (idExcluir == null || i.IdIngrediente != idExcluir));
        }
    }
}
