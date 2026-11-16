using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-23: Catálogo de ingredientes.
    // Químico: registra, edita, consulta y desactiva (eliminación lógica).
    // Gerente: solo consulta el catálogo.
    public class IngredienteController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private const int RolQuimico = 1;
        private const int RolGerente = 2;

        private static readonly string[] UnidadesPermitidas = { "kg", "litro" };

        // Se ejecuta antes de cada acción: sin sesión activa va al login.
        // Index lo pueden ver químico y gerente; el resto de acciones son solo del químico.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true)
            {
                filterContext.Result = RedirectToAction("Login", "Auth");
                return;
            }

            int rol = Convert.ToInt32(Session["RolUsuarioSession"]);
            string accion = filterContext.ActionDescriptor.ActionName;

            bool permitido = accion == "Index"
                ? (rol == RolQuimico || rol == RolGerente)
                : rol == RolQuimico;

            if (!permitido)
            {
                filterContext.Result = RedirectToAction("Error404", "Error");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        [HttpGet]
        public ActionResult Index(string buscar, string filtro = "activos")
        {
            ViewBag.PuedeEditar = Convert.ToInt32(Session["RolUsuarioSession"]) == RolQuimico;

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

                    // ToList() ejecuta la consulta antes de cerrar el contexto.
                    var lista = consulta
                        .OrderBy(i => i.NombreIngrediente)
                        .ToList();

                    ViewBag.Buscar = buscar;
                    ViewBag.Filtro = filtro;
                    return View(lista);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, IngredienteController - HttpGet -> Index()");
                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;
                return View(new List<TB_Ingrediente>());
            }
        }

        [HttpGet]
        public ActionResult Crear()
        {
            return View(new TB_Ingrediente { UnidadBase = "kg", Estado = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include = "NombreIngrediente,Funcion,UnidadBase")] TB_Ingrediente modelo)
        {
            try
            {
                Normalizar(modelo);
                Validar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    if (ModelState.IsValid && ExisteNombre(db, modelo.NombreIngrediente, null))
                        ModelState.AddModelError("NombreIngrediente", "Ya existe un ingrediente con ese nombre.");

                    if (!ModelState.IsValid)
                    {
                        modelo.Estado = true;
                        return View(modelo);
                    }

                    modelo.Estado = true;
                    db.TB_Ingrediente.Add(modelo);
                    db.SaveChanges();
                }

                TempData["Exito"] = "El ingrediente se registró correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, IngredienteController - HttpPost -> Crear()");
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

                    return View(ingrediente);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, IngredienteController - HttpGet -> Editar()");
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            [Bind(Include = "IdIngrediente,NombreIngrediente,Funcion,UnidadBase")] TB_Ingrediente modelo)
        {
            try
            {
                Normalizar(modelo);
                Validar(modelo);

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
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, IngredienteController - HttpPost -> Editar()");
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
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, IngredienteController - HttpPost -> Desactivar()");
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
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, IngredienteController - HttpPost -> Activar()");
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index", new { filtro = "inactivos" });
            }
        }

        // Quita espacios al inicio y al final para evitar duplicados como "Glicerina " y "Glicerina".
        private static void Normalizar(TB_Ingrediente modelo)
        {
            if (modelo == null) return;
            modelo.NombreIngrediente = modelo.NombreIngrediente?.Trim();
            modelo.Funcion = modelo.Funcion?.Trim();
            modelo.UnidadBase = modelo.UnidadBase?.Trim();
        }

        // Las clases de EF se regeneran, así que las validaciones se hacen aquí
        // con los mismos límites de la tabla TB_Ingrediente.
        private void Validar(TB_Ingrediente modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.NombreIngrediente))
                ModelState.AddModelError("NombreIngrediente", "El nombre del ingrediente es obligatorio.");
            else if (modelo.NombreIngrediente.Length > 100)
                ModelState.AddModelError("NombreIngrediente", "El nombre no puede tener más de 100 caracteres.");

            if (string.IsNullOrWhiteSpace(modelo.Funcion))
                ModelState.AddModelError("Funcion", "La función del ingrediente es obligatoria.");
            else if (modelo.Funcion.Length > 50)
                ModelState.AddModelError("Funcion", "La función no puede tener más de 50 caracteres.");

            if (string.IsNullOrWhiteSpace(modelo.UnidadBase) || !UnidadesPermitidas.Contains(modelo.UnidadBase))
                ModelState.AddModelError("UnidadBase", "Seleccione una unidad base válida (kg o litro).");
        }

        // La comparación no distingue mayúsculas porque la intercalación de SQL Server es CI.
        private static bool ExisteNombre(BioquimiaDBEntities db, string nombre, int? idExcluir)
        {
            return db.TB_Ingrediente.Any(i => i.NombreIngrediente == nombre
                                           && (idExcluir == null || i.IdIngrediente != idExcluir));
        }
    }
}
