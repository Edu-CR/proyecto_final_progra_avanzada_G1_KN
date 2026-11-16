
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class PrototipoController : Controller
    {
        private const string MensajeErrorGenerico =
            "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private const int RolQuimico = 1;
        private const string EstadoBorrador = "Borrador";
        private const string EstadoDevuelto = "Devuelto";
        private const string EstadoEnviado = "Enviado";
        private const int MaximoImagenBytes = 5 * 1024 * 1024;

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


        #region Listar prototipos

        // RF: Listar prototipos del químico.
        [HttpGet]
        public ActionResult Index()
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            int idQuimico = Convert.ToInt32(
                Session["IdUsuarioSession"]);

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipos = db.TB_Prototipo
                        .Include(p => p.TB_Categoria)
                        .Include(p => p.TB_EstadoPrototipo)
                        .Where(p => p.IdQuimico == idQuimico)
                        .OrderByDescending(p =>
                            p.FechaModificacion ?? p.FechaCreacion)
                        .ToList();

                    return View(prototipos);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - HttpGet -> Index()");

                TempData["Error"] = MensajeErrorGenerico;

                return View(new List<TB_Prototipo>());
            }
        }

        #endregion


        #region Crear prototipo

        // RF-07: Mostrar formulario de creación.
        [HttpGet]
        public ActionResult Crear()
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    CargarListas(db, null);

                    ViewBag.IngredientesActuales =
                        new List<TB_PrototipoIngrediente>();

                    return View(new TB_Prototipo());
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - HttpGet -> Crear()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        // RF-07: Guardar prototipo como Borrador.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            TB_Prototipo modelo,
            int[] idsIngredientes,
            decimal[] porcentajes,
            HttpPostedFileBase imagen)
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            int idQuimico = Convert.ToInt32(
                Session["IdUsuarioSession"]);

            string rutaImagen = null;
            bool prototipoGuardado = false;

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    ValidarIngredientes(
                        db, idsIngredientes, porcentajes);

                    ValidarImagen(imagen, true);

                    modelo.NombrePrototipo =
                        modelo.NombrePrototipo == null
                        ? null
                        : modelo.NombrePrototipo.Trim();

                    if (!string.IsNullOrWhiteSpace(modelo.NombrePrototipo) &&
                        db.TB_Prototipo.Any(p =>
                            p.IdQuimico == idQuimico &&
                            p.NombrePrototipo == modelo.NombrePrototipo))
                    {
                        ModelState.AddModelError(
                            "NombrePrototipo",
                            "Ya tienes un prototipo con ese nombre.");
                    }

                    if (!ModelState.IsValid)
                    {
                        CargarListas(db, modelo.IdCategoria);

                        ViewBag.IngredientesActuales =
                            ConstruirIngredientesActuales(
                                idsIngredientes, porcentajes);

                        return View(modelo);
                    }

                    rutaImagen = GuardarImagen(imagen);

                    using (var transaccion =
                        db.Database.BeginTransaction())
                    {
                        try
                        {
                            var estadoBorrador =
                                db.TB_EstadoPrototipo.FirstOrDefault(
                                    e => e.NombreEstado == EstadoBorrador);

                            if (estadoBorrador == null)
                            {
                                throw new InvalidOperationException(
                                    "No existe el estado Borrador.");
                            }

                            var prototipo = new TB_Prototipo
                            {
                                NombrePrototipo = modelo.NombrePrototipo,
                                IdCategoria = modelo.IdCategoria,
                                Descripcion = modelo.Descripcion,
                                CaracteristicasEnvase =
                                    modelo.CaracteristicasEnvase,
                                ImagenRuta = rutaImagen,
                                IdQuimico = idQuimico,
                                IdEstadoPrototipo =
                                    estadoBorrador.IdEstadoPrototipo,
                                FechaCreacion = DateTime.Now
                            };

                            db.TB_Prototipo.Add(prototipo);
                            db.SaveChanges();

                            AgregarIngredientes(
                                db,
                                prototipo.IdPrototipo,
                                idsIngredientes,
                                porcentajes);

                            db.TB_HistorialEstado.Add(
                                new TB_HistorialEstado
                                {
                                    IdPrototipo = prototipo.IdPrototipo,
                                    IdEstadoAnterior = null,
                                    IdEstadoNuevo =
                                        estadoBorrador.IdEstadoPrototipo,
                                    IdUsuario = idQuimico,
                                    Justificacion =
                                        "Creación del prototipo en estado Borrador.",
                                    FechaCambio = DateTime.Now
                                });

                            db.SaveChanges();
                            transaccion.Commit();

                            prototipoGuardado = true;
                        }
                        catch
                        {
                            transaccion.Rollback();
                            throw;
                        }
                    }
                }

                TempData["Exito"] =
                    "El prototipo se guardó como Borrador correctamente.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - HttpPost -> Crear()");

                // Evitar dejar una imagen sin asociar a un prototipo.
                if (!prototipoGuardado)
                {
                    EliminarImagenConRegistro(
                        rutaImagen,
                        "Error durante la limpieza de imagen después de Crear()");
                }

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        #endregion


        #region Editar prototipo

        // RF-08: Mostrar formulario de edición.
        [HttpGet]
        public ActionResult Editar(int id)
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            int idQuimico = Convert.ToInt32(
                Session["IdUsuarioSession"]);

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipo = db.TB_Prototipo
                        .Include(p => p.TB_EstadoPrototipo)
                        .FirstOrDefault(p =>
                            p.IdPrototipo == id &&
                            p.IdQuimico == idQuimico);

                    if (prototipo == null)
                    {
                        return HttpNotFound();
                    }

                    if (!PuedeEditar(
                        prototipo.TB_EstadoPrototipo.NombreEstado))
                    {
                        TempData["Error"] =
                            "Solo puedes editar prototipos en estado Borrador o Devuelto.";

                        return RedirectToAction("Index");
                    }

                    CargarListas(db, prototipo.IdCategoria);

                    ViewBag.IngredientesActuales =
                        db.TB_PrototipoIngrediente
                        .Where(pi => pi.IdPrototipo == id)
                        .ToList();

                    return View(prototipo);
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - HttpGet -> Editar()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        // RF-08: Guardar cambios del prototipo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            TB_Prototipo modelo,
            int[] idsIngredientes,
            decimal[] porcentajes,
            HttpPostedFileBase imagen)
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            int idQuimico = Convert.ToInt32(
                Session["IdUsuarioSession"]);

            string nuevaRuta = null;
            bool cambiosGuardados = false;

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipo = db.TB_Prototipo
                        .Include(p => p.TB_EstadoPrototipo)
                        .FirstOrDefault(p =>
                            p.IdPrototipo == modelo.IdPrototipo &&
                            p.IdQuimico == idQuimico);

                    if (prototipo == null)
                    {
                        return HttpNotFound();
                    }

                    if (!PuedeEditar(
                        prototipo.TB_EstadoPrototipo.NombreEstado))
                    {
                        TempData["Error"] =
                            "El prototipo ya no se puede editar porque su estado cambió.";

                        return RedirectToAction("Index");
                    }

                    ValidarIngredientes(
                        db, idsIngredientes, porcentajes);

                    ValidarImagen(imagen, false);

                    modelo.NombrePrototipo =
                        modelo.NombrePrototipo == null
                        ? null
                        : modelo.NombrePrototipo.Trim();

                    if (!string.IsNullOrWhiteSpace(modelo.NombrePrototipo) &&
                        db.TB_Prototipo.Any(p =>
                            p.IdQuimico == idQuimico &&
                            p.IdPrototipo != modelo.IdPrototipo &&
                            p.NombrePrototipo == modelo.NombrePrototipo))
                    {
                        ModelState.AddModelError(
                            "NombrePrototipo",
                            "Ya tienes otro prototipo con ese nombre.");
                    }

                    if (!ModelState.IsValid)
                    {
                        CargarListas(db, modelo.IdCategoria);

                        ViewBag.IngredientesActuales =
                            ConstruirIngredientesActuales(
                                idsIngredientes, porcentajes);

                        return View(modelo);
                    }

                    nuevaRuta =
                        imagen != null && imagen.ContentLength > 0
                        ? GuardarImagen(imagen)
                        : null;

                    string rutaAnterior = prototipo.ImagenRuta;

                    using (var transaccion =
                        db.Database.BeginTransaction())
                    {
                        try
                        {
                            prototipo.NombrePrototipo =
                                modelo.NombrePrototipo;

                            prototipo.IdCategoria =
                                modelo.IdCategoria;

                            prototipo.Descripcion =
                                modelo.Descripcion;

                            prototipo.CaracteristicasEnvase =
                                modelo.CaracteristicasEnvase;

                            if (nuevaRuta != null)
                            {
                                prototipo.ImagenRuta = nuevaRuta;
                            }

                            prototipo.FechaModificacion =
                                DateTime.Now;

                            var ingredientesAnteriores =
                                db.TB_PrototipoIngrediente
                                .Where(pi =>
                                    pi.IdPrototipo == prototipo.IdPrototipo)
                                .ToList();

                            db.TB_PrototipoIngrediente.RemoveRange(
                                ingredientesAnteriores);

                            AgregarIngredientes(
                                db,
                                prototipo.IdPrototipo,
                                idsIngredientes,
                                porcentajes);

                            db.SaveChanges();
                            transaccion.Commit();

                            cambiosGuardados = true;
                        }
                        catch
                        {
                            transaccion.Rollback();
                            throw;
                        }
                    }

                    // El prototipo ya tiene asociada la nueva imagen.
                    // La imagen anterior se elimina después de guardar los cambios.
                    if (nuevaRuta != null && rutaAnterior != nuevaRuta)
                    {
                        EliminarImagenConRegistro(
                            rutaAnterior,
                            "Error durante la eliminación de la imagen anterior en Editar()");
                    }
                }

                TempData["Exito"] =
                    "El prototipo se actualizó correctamente.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - HttpPost -> Editar()");

                // No eliminar la imagen nueva si los cambios ya se guardaron
                // y el registro de base de datos ya la está utilizando.
                if (!cambiosGuardados)
                {
                    EliminarImagenConRegistro(
                        nuevaRuta,
                        "Error durante la limpieza de imagen después de Editar()");
                }

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        #endregion


        #region Enviar prototipo

        // RF-09: Enviar prototipo a evaluación.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Enviar(int id)
        {
            if (!EsQuimico())
            {
                return RedirectToAction("Error404", "Error");
            }

            int idQuimico = Convert.ToInt32(
                Session["IdUsuarioSession"]);

            try
            {
                using (var db = new BioquimiaDBEntities())
                {
                    var prototipo = db.TB_Prototipo
                        .Include(p => p.TB_EstadoPrototipo)
                        .FirstOrDefault(p =>
                            p.IdPrototipo == id &&
                            p.IdQuimico == idQuimico);

                    if (prototipo == null)
                    {
                        return HttpNotFound();
                    }

                    string estadoAnterior =
                        prototipo.TB_EstadoPrototipo.NombreEstado;

                    if (!PuedeEditar(estadoAnterior))
                    {
                        TempData["Error"] =
                            "Solo puedes enviar prototipos en estado Borrador o Devuelto.";

                        return RedirectToAction("Index");
                    }

                    decimal total =
                        db.TB_PrototipoIngrediente
                        .Where(pi => pi.IdPrototipo == id)
                        .Select(pi => (decimal?)pi.Porcentaje)
                        .Sum() ?? 0m;

                    if (total != 100m)
                    {
                        TempData["Error"] =
                            "Los porcentajes deben sumar exactamente 100 %.";

                        return RedirectToAction("Index");
                    }

                    var estadoEnviado =
                        db.TB_EstadoPrototipo.FirstOrDefault(
                            e => e.NombreEstado == EstadoEnviado);

                    if (estadoEnviado == null)
                    {
                        TempData["Error"] =
                            "No existe el estado Enviado en el catálogo.";

                        return RedirectToAction("Index");
                    }

                    int idEstadoAnterior =
                        prototipo.IdEstadoPrototipo;

                    prototipo.IdEstadoPrototipo =
                        estadoEnviado.IdEstadoPrototipo;

                    prototipo.ComentarioGerente = null;
                    prototipo.FechaModificacion = DateTime.Now;

                    db.TB_HistorialEstado.Add(
                        new TB_HistorialEstado
                        {
                            IdPrototipo = prototipo.IdPrototipo,
                            IdEstadoAnterior = idEstadoAnterior,
                            IdEstadoNuevo =
                                estadoEnviado.IdEstadoPrototipo,
                            IdUsuario = idQuimico,
                            Justificacion =
                                "El químico envió el prototipo a evaluación.",
                            FechaCambio = DateTime.Now
                        });

                    db.SaveChanges();
                }

                TempData["Exito"] =
                    "El prototipo se envió a evaluación correctamente.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - HttpPost -> Enviar()");

                TempData["Error"] = MensajeErrorGenerico;

                return RedirectToAction("Index");
            }
        }

        #endregion


        #region Métodos auxiliares

        private static bool PuedeEditar(string estado)
        {
            return estado == EstadoBorrador ||
                   estado == EstadoDevuelto;
        }

        private void CargarListas(
            BioquimiaDBEntities db,
            int? categoriaSeleccionada)
        {
            ViewBag.Categorias = new SelectList(
                db.TB_Categoria
                    .Where(c => c.Estado)
                    .OrderBy(c => c.NombreCategoria)
                    .ToList(),
                "IdCategoria",
                "NombreCategoria",
                categoriaSeleccionada);

            ViewBag.Ingredientes =
                db.TB_Ingrediente
                    .Where(i => i.Estado)
                    .OrderBy(i => i.NombreIngrediente)
                    .ToList();
        }

        private void ValidarIngredientes(
            BioquimiaDBEntities db,
            int[] ids,
            decimal[] porcentajes)
        {
            if (ids == null ||
                porcentajes == null ||
                ids.Length == 0 ||
                ids.Length != porcentajes.Length)
            {
                ModelState.AddModelError(
                    "Ingredientes",
                    "Agrega al menos un ingrediente y su porcentaje.");

                return;
            }

            if (ids.Any(id => id <= 0) ||
                ids.Distinct().Count() != ids.Length)
            {
                ModelState.AddModelError(
                    "Ingredientes",
                    "Selecciona ingredientes válidos y no los repitas.");
            }

            if (porcentajes.Any(p => p <= 0 || p > 100))
            {
                ModelState.AddModelError(
                    "Ingredientes",
                    "Cada porcentaje debe ser mayor que 0 y menor o igual a 100.");
            }

            if (porcentajes.Sum() != 100m)
            {
                ModelState.AddModelError(
                    "Ingredientes",
                    "La suma de los porcentajes debe ser exactamente 100 %.");
            }

            var idsActivos =
                db.TB_Ingrediente
                .Where(i => i.Estado && ids.Contains(i.IdIngrediente))
                .Select(i => i.IdIngrediente)
                .ToList();

            if (idsActivos.Count != ids.Distinct().Count())
            {
                ModelState.AddModelError(
                    "Ingredientes",
                    "Uno o más ingredientes no existen o están inactivos.");
            }
        }

        private static List<TB_PrototipoIngrediente>
            ConstruirIngredientesActuales(
                int[] ids,
                decimal[] porcentajes)
        {
            var lista =
                new List<TB_PrototipoIngrediente>();

            if (ids == null || porcentajes == null)
            {
                return lista;
            }

            for (int i = 0;
                 i < Math.Min(ids.Length, porcentajes.Length);
                 i++)
            {
                lista.Add(
                    new TB_PrototipoIngrediente
                    {
                        IdIngrediente = ids[i],
                        Porcentaje = porcentajes[i]
                    });
            }

            return lista;
        }

        private static void AgregarIngredientes(
            BioquimiaDBEntities db,
            int idPrototipo,
            int[] ids,
            decimal[] porcentajes)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                db.TB_PrototipoIngrediente.Add(
                    new TB_PrototipoIngrediente
                    {
                        IdPrototipo = idPrototipo,
                        IdIngrediente = ids[i],
                        Porcentaje = porcentajes[i]
                    });
            }
        }

        private void ValidarImagen(
            HttpPostedFileBase imagen,
            bool obligatoria)
        {
            if (imagen == null || imagen.ContentLength == 0)
            {
                if (obligatoria)
                {
                    ModelState.AddModelError(
                        "Imagen",
                        "Selecciona una fotografía del prototipo.");
                }

                return;
            }

            string extension =
                Path.GetExtension(imagen.FileName);

            string[] permitidas =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!permitidas.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "Imagen",
                    "La imagen debe ser JPG, JPEG, PNG o WEBP.");
            }

            if (imagen.ContentLength > MaximoImagenBytes)
            {
                ModelState.AddModelError(
                    "Imagen",
                    "La imagen no puede superar los 5 MB.");
            }
        }

        private string GuardarImagen(
            HttpPostedFileBase imagen)
        {
            string carpeta =
                Server.MapPath("~/assets/img/prototipos");

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            string extension =
                Path.GetExtension(imagen.FileName).ToLowerInvariant();

            string nombre =
                Guid.NewGuid().ToString("N") + extension;

            imagen.SaveAs(
                Path.Combine(carpeta, nombre));

            return "~/assets/img/prototipos/" + nombre;
        }

        private void EliminarImagen(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }

            string fisica = Server.MapPath(ruta);

            if (System.IO.File.Exists(fisica))
            {
                System.IO.File.Delete(fisica);
            }
        }

        // Intenta eliminar una imagen sin interrumpir el manejo del error original.
        private void EliminarImagenConRegistro(
            string ruta,
            string contexto)
        {
            try
            {
                EliminarImagen(ruta);
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(
                    ex,
                    "Ocurrió una excepción, PrototipoController - " + contexto);
            }
        }

        #endregion
    }
}
