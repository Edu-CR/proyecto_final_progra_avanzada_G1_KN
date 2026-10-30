using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;

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

                    // No se puede proyectar (Select) a una entidad de EF en LINQ to Entities,
                    // por eso se devuelve la lista directamente.
                    var lista = consulta
                        .OrderBy(e => e.NombreEmpresa)
                        .ToList();

                    ViewBag.Buscar = buscar;
                    ViewBag.Filtro = filtro;
                    return View(lista);
                }
            }
            catch (Exception ex)
            {
                // Registramos el error en la bitácora indicando el módulo correspondiente, ejemplo de uso de este service
                BitacoraService.Registrar(ex, "Ocurrio una excepción, EmpresaController - HttpGet -> Index()");
                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;
                return View(new List<TB_Empresa>());
            }
        }

        [HttpGet]
        public ActionResult Crear()
        {
            return View(new TB_Empresa { Estado = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include = "NombreEmpresa,Contacto,Telefono,CorreoElectronico")] TB_Empresa modelo)
        {
            try
            {
                Normalizar(modelo);
                Validar(modelo);

                using (var db = new BioquimiaDBEntities())
                {
                    if (ModelState.IsValid && ExisteNombre(db, modelo.NombreEmpresa, null))
                        ModelState.AddModelError("NombreEmpresa", "Ya existe una empresa con ese nombre.");

                    if (!ModelState.IsValid)
                    {
                        modelo.Estado = true;
                        return View(modelo);
                    }

                    modelo.Estado = true;
                    modelo.FechaCreacion = DateTime.Now;
                    db.TB_Empresa.Add(modelo);
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

                    return View(empresa);
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
        public ActionResult Editar(
            [Bind(Include = "IdEmpresa,NombreEmpresa,Contacto,Telefono,CorreoElectronico")] TB_Empresa modelo)
        {
            try
            {
                Normalizar(modelo);
                Validar(modelo);

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
                        modelo.FechaCreacion = empresa.FechaCreacion;
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
        private static void Normalizar(TB_Empresa modelo)
        {
            if (modelo == null) return;
            modelo.NombreEmpresa = modelo.NombreEmpresa?.Trim();
            modelo.Contacto = string.IsNullOrWhiteSpace(modelo.Contacto) ? null : modelo.Contacto.Trim();
            modelo.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
            modelo.CorreoElectronico = string.IsNullOrWhiteSpace(modelo.CorreoElectronico) ? null : modelo.CorreoElectronico.Trim();
        }

        // Las clases de EF se regeneran, así que las validaciones que antes estaban
        // como atributos en EmpresaModel se hacen aquí.
        private void Validar(TB_Empresa modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.NombreEmpresa))
                ModelState.AddModelError("NombreEmpresa", "El nombre de la empresa es obligatorio.");
            else if (modelo.NombreEmpresa.Length > 150)
                ModelState.AddModelError("NombreEmpresa", "El nombre no puede tener más de 150 caracteres.");

            if (modelo.Contacto != null && modelo.Contacto.Length > 150)
                ModelState.AddModelError("Contacto", "El contacto no puede tener más de 150 caracteres.");

            if (modelo.Telefono != null)
            {
                if (modelo.Telefono.Length > 20)
                    ModelState.AddModelError("Telefono", "El teléfono no puede tener más de 20 caracteres.");
                else if (!Regex.IsMatch(modelo.Telefono, @"^[0-9+\-\s()]{8,20}$"))
                    ModelState.AddModelError("Telefono", "Ingrese un teléfono válido (solo números, espacios, +, - o paréntesis; mínimo 8).");
            }

            if (modelo.CorreoElectronico != null)
            {
                if (modelo.CorreoElectronico.Length > 100)
                    ModelState.AddModelError("CorreoElectronico", "El correo no puede tener más de 100 caracteres.");
                else if (!new EmailAddressAttribute().IsValid(modelo.CorreoElectronico))
                    ModelState.AddModelError("CorreoElectronico", "Ingrese un correo electrónico válido.");
            }
        }

        // La comparación no distingue mayúsculas porque la intercalación de SQL Server es CI.
        private static bool ExisteNombre(BioquimiaDBEntities db, string nombre, int? idExcluir)
        {
            return db.TB_Empresa.Any(e => e.NombreEmpresa == nombre
                                       && (idExcluir == null || e.IdEmpresa != idExcluir));
        }
    }
}