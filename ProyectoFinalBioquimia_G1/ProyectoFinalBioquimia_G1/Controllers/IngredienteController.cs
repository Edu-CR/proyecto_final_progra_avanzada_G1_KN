
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // RF-23: Catálogo de ingredientes, maneja 2 roles.
    // Químico: registra, edita, consulta y desactiva.
    // Gerente: solamente consulta el catálogo.
    public class IngredienteController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private const int RolQuimico = 1;
        private const int RolGerente = 2;

        private static readonly string[] UnidadesPermitidas = { "kg", "litro" };

        #region Validación de sesión y permisos

        // Verifica que exista una sesión válida, usuario activo y químico.
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
                   && idRol == RolQuimico
                   && estadoUsuario;
        }

        // Verifica que exista una sesión válida, usuario activo y gerente.
        private bool EsGerente()
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
                   && idRol == RolGerente
                   && estadoUsuario;
        }

        // Verifica que exista una sesión iniciada y que el usuario esté activo.
        private bool TieneSesionActiva()
        {
            if (Session["IdUsuarioSession"] == null ||
                Session["RolUsuarioSession"] == null ||
                Session["EstadoUsuarioSession"] == null)
            {
                return false;
            }

            int idUsuario;
            bool estadoUsuario;

            return int.TryParse(Convert.ToString(Session["IdUsuarioSession"]), out idUsuario)
                   && bool.TryParse(Convert.ToString(Session["EstadoUsuarioSession"]), out estadoUsuario)
                   && idUsuario > 0
                   && estadoUsuario;
        }

        // Controla los permisos antes de ejecutar cualquier acción.
        // Index: permitido para químico y gerente.
        // Resto de acciones: únicamente para químico.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (filterContext.Result != null)
                return;

            if (!TieneSesionActiva())
            {
                filterContext.Result = RedirectToAction("Login", "Auth");
                return;
            }

            string accion = filterContext.ActionDescriptor.ActionName;

            bool permitido = string.Equals(
                accion, "Index", StringComparison.OrdinalIgnoreCase)
                ? (EsQuimico() || EsGerente())
                : EsQuimico();

            if (!permitido)
            {
                filterContext.Result = RedirectToAction("Error404", "Error");
                return;
            }
        }

        #endregion

        #region Ver ingredientes

        [HttpGet]
        public ActionResult Index(string buscar, string filtro = "activos")
        {
            ViewBag.PuedeEditar = EsQuimico();

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

                        consulta = consulta.Where(i =>
                            i.NombreIngrediente.Contains(texto) ||
                            i.Funcion.Contains(texto));
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
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpGet -> Index()");

                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;

                return View(new List<TB_Ingrediente>());
            }
        }

        #endregion

        #region Crear ingrediente

        [HttpGet]
        public ActionResult Crear()
        {
            try
            {
                return View(new TB_Ingrediente
                {
                    UnidadBase = "kg",
                    Estado = true
                });
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpGet -> Crear()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include = "NombreIngrediente,Funcion,UnidadBase")]
            TB_Ingrediente modelo)
        {
            try
            {
                if (modelo == null)
                {
                    modelo = new TB_Ingrediente
                    {
                        UnidadBase = "kg",
                        Estado = true
                    };

                    ModelState.AddModelError("", "Los datos enviados no son válidos.");
                    return View(modelo);
                }

                Normalizar(modelo);
                Validar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    if (ModelState.IsValid &&
                        ExisteNombre(db, modelo.NombreIngrediente, null))
                    {
                        ModelState.AddModelError(
                            "NombreIngrediente",
                            "Ya existe un ingrediente con ese nombre.");
                    }

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
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpPost -> Crear()");

                TempData["Error"] = MensajeErrorGenerico;

                return View(modelo);
            }
        }

        #endregion

        #region Editar ingrediente

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
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpGet -> Editar()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            [Bind(Include = "IdIngrediente,NombreIngrediente,Funcion,UnidadBase")]
            TB_Ingrediente modelo)
        {
            try
            {
                if (modelo == null)
                {
                    modelo = new TB_Ingrediente();

                    ModelState.AddModelError("", "Los datos enviados no son válidos.");
                    return View(modelo);
                }

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

                    if (ModelState.IsValid &&
                        ExisteNombre(db, modelo.NombreIngrediente, modelo.IdIngrediente))
                    {
                        ModelState.AddModelError(
                            "NombreIngrediente",
                            "Ya existe un ingrediente con ese nombre.");
                    }

                    if (!ModelState.IsValid)
                    {
                        // Se conserva el estado original porque no se recibe desde el formulario.
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
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpPost -> Editar()");

                TempData["Error"] = MensajeErrorGenerico;

                return View(modelo);
            }
        }

        #endregion

        #region Desactivar ingrediente

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

                    // RF-23: no se puede desactivar si está en un prototipo
                    // Borrador, Enviado o Devuelto.
                    var estadosQueBloquean = new List<string>
                    {
                        "Borrador",
                        "Enviado",
                        "Devuelto"
                    };

                    var prototiposEnUso = db.TB_PrototipoIngrediente
                        .Where(pi =>
                            pi.IdIngrediente == id &&
                            estadosQueBloquean.Contains(
                                pi.TB_Prototipo.TB_EstadoPrototipo.NombreEstado))
                        .Select(pi => pi.TB_Prototipo.NombrePrototipo)
                        .Distinct()
                        .ToList();

                    if (prototiposEnUso.Any())
                    {
                        TempData["Error"] =
                            "No se puede desactivar \"" + ingrediente.NombreIngrediente +
                            "\" porque se usa en prototipos en estado Borrador, Enviado o Devuelto: " +
                            string.Join(", ", prototiposEnUso) + ".";

                        return RedirectToAction("Index");
                    }

                    ingrediente.Estado = false;

                    db.SaveChanges();

                    TempData["Exito"] =
                        "El ingrediente \"" + ingrediente.NombreIngrediente +
                        "\" se desactivó correctamente.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpPost -> Desactivar()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Activar ingrediente

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

                    TempData["Exito"] =
                        "El ingrediente \"" + ingrediente.NombreIngrediente +
                        "\" se activó correctamente.";
                }

                return RedirectToAction("Index", new { filtro = "inactivos" });
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex,
                    "Ocurrió una excepción, IngredienteController - HttpPost -> Activar()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index", new { filtro = "inactivos" });
            }
        }

        #endregion

        #region Normalización y validaciones

        // Quita espacios y normaliza las unidades para evitar inconsistencias.
        private static void Normalizar(TB_Ingrediente modelo)
        {
            if (modelo == null)
                return;

            modelo.NombreIngrediente = modelo.NombreIngrediente?.Trim();
            modelo.Funcion = modelo.Funcion?.Trim();
            modelo.UnidadBase = modelo.UnidadBase?.Trim().ToLowerInvariant();
        }

        // Validaciones manuales según los límites de la tabla TB_Ingrediente.
        // Se mantienen aquí para evitar depender de las clases generadas por EF.
        private void Validar(TB_Ingrediente modelo)
        {
            if (modelo == null)
            {
                ModelState.AddModelError("", "Los datos enviados no son válidos.");
                return;
            }

            // Validación del nombre.
            if (string.IsNullOrWhiteSpace(modelo.NombreIngrediente))
            {
                ModelState.AddModelError(
                    "NombreIngrediente",
                    "El nombre del ingrediente es obligatorio.");
            }
            else if (modelo.NombreIngrediente.Length > 100)
            {
                ModelState.AddModelError(
                    "NombreIngrediente",
                    "El nombre no puede tener más de 100 caracteres.");
            }

            // Validación de la función.
            if (string.IsNullOrWhiteSpace(modelo.Funcion))
            {
                ModelState.AddModelError(
                    "Funcion",
                    "La función del ingrediente es obligatoria.");
            }
            else if (modelo.Funcion.Length > 50)
            {
                ModelState.AddModelError(
                    "Funcion",
                    "La función no puede tener más de 50 caracteres.");
            }

            // Validación de la unidad base.
            if (string.IsNullOrWhiteSpace(modelo.UnidadBase) ||
                !UnidadesPermitidas.Contains(modelo.UnidadBase))
            {
                ModelState.AddModelError(
                    "UnidadBase",
                    "Seleccione una unidad base válida (kg o litro).");
            }
        }

        // La comparación no distingue mayúsculas porque la intercalación
        // de SQL Server es CI.
        private static bool ExisteNombre(
            BioquimiaDBEntities db,
            string nombre,
            int? idExcluir)
        {
            return db.TB_Ingrediente.Any(i =>
                i.NombreIngrediente == nombre &&
                (idExcluir == null || i.IdIngrediente != idExcluir));
        }

        #endregion
    }
}
