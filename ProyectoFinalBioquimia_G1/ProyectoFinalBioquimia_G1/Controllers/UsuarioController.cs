using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    // Controller excluvsivo para el gerente
    public class UsuarioController : Controller
    {
        private const string MensajeErrorGenerico ="Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private BioquimiaDBEntities context = new BioquimiaDBEntities();


        #region Ver usuarios
        [HttpGet]
        public ActionResult Index(string buscar, string filtro = "activos")
        {

            //EJEMPLO COMO MANEJAR LOS ROLES------------
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)// si esta desactivado, no tiene sesion o no es el rol que le corresponde, 2 pa gerente , 1 pa quimico,o si es diferente de geretne no pasa y va para la 404
            {
                return RedirectToAction("Error404", "Error");
            }
            //FIN DE MANEJO DE ROLES------------


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
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpGet -> Index()");
                TempData["Error"] = MensajeErrorGenerico;
                ViewBag.Buscar = buscar;
                ViewBag.Filtro = filtro;
                return View("Index", new List<TB_Usuario>());
            }
        }
        #endregion

       
        #region Crear usuario

        // Llena el dropdown con los roles de TB_Rol (muestra NombreRol, guarda IdRol)
        private void CargarRoles(int? seleccionado = null)
        {
            var roles = context.TB_Rol.OrderBy(r => r.NombreRol).ToList();
            ViewBag.Roles = new SelectList(roles, "IdRol", "NombreRol", seleccionado);
        }

        [HttpGet]
        public ActionResult Crear()
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                CargarRoles();
                return View();
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpGet -> Crear()");
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string identificacion, string nombreCompleto, string correoElectronico,string contrasenna, string confirmarContrasenna, int idRol)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                identificacion = identificacion?.Trim();
                nombreCompleto = nombreCompleto?.Trim();
                correoElectronico = correoElectronico?.Trim();

                // Para volver a pintar el formulario si hay error (nunca se devuelve la contraseña)
                ViewBag.Identificacion = identificacion;
                ViewBag.NombreCompleto = nombreCompleto;
                ViewBag.CorreoElectronico = correoElectronico;

                // VALIDACIONES EXTRA EN BACKEND para no dejar solo el JS x seguridad, y esto tambien los usamos en las otras actions x aquello
                if (string.IsNullOrWhiteSpace(identificacion) || string.IsNullOrWhiteSpace(nombreCompleto)
                    || string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(contrasenna)
                    || string.IsNullOrWhiteSpace(confirmarContrasenna))
                {
                    ViewBag.Error = "Todos los campos son obligatorios.";
                    CargarRoles(idRol);
                    return View();
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(correoElectronico, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ViewBag.Error = "El correo electrónico no tiene un formato válido.";
                    CargarRoles(idRol);
                    return View();
                }


                if (!System.Text.RegularExpressions.Regex.IsMatch(identificacion, @"^[0-9]{10}$"))
                {
                    ViewBag.Error = "El código debe tener exactamente 10 dígitos.";
                    CargarRoles(idRol);
                    return View();
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(contrasenna, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{8,15}$"))
                {
                    ViewBag.Error = "La contraseña debe tener de 8 a 15 caracteres, con al menos una mayúscula, una minúscula y un número.";
                    CargarRoles(idRol);
                    return View();
                }

               
                if (contrasenna != confirmarContrasenna)
                {
                    ViewBag.Error = "La confirmación no coincide con la contraseña.";
                    CargarRoles(idRol);
                    return View();
                }

                if (!context.TB_Rol.Any(r => r.IdRol == idRol))
                {
                    ViewBag.Error = "El rol seleccionado no es válido.";
                    CargarRoles();
                    return View();
                }

                if (context.TB_Usuario.Any(u => u.CorreoElectronico == correoElectronico))
                {
                    ViewBag.Error = "Ya existe un usuario con ese correo electrónico.";
                    CargarRoles(idRol);
                    return View();
                }

                if (context.TB_Usuario.Any(u => u.Identificacion == identificacion))
                {
                    ViewBag.Error = "Ya existe un usuario con ese código.";
                    CargarRoles(idRol);
                    return View();
                }

                var nuevo = new TB_Usuario
                {
                    Identificacion = identificacion,
                    NombreCompleto = nombreCompleto,
                    CorreoElectronico = correoElectronico,
                    Contrasenna = contrasenna,
                    IdRol = idRol,
                    Estado = true,
                    FechaCreacion = DateTime.Now
                };

                context.TB_Usuario.Add(nuevo);
                context.SaveChanges();

                TempData["Exito"] = "Usuario creado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpPost -> Crear()");
                ViewBag.Error = MensajeErrorGenerico;
                CargarRoles(idRol);
                return View();
            }
        }

        #endregion

        #region Desactivar / Reactivar usuario

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Desactivar(int id)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                // Un gerente no puede desactivarse a sí mismo (xq se quedaría sin acceso)
                if (id == Convert.ToInt32(Session["IdUsuarioSession"]))
                {
                    TempData["Error"] = "No puedes desactivar tu propio usuario.";
                    return RedirectToAction("Index");
                }

                var usuario = context.TB_Usuario.FirstOrDefault(u => u.IdUsuario == id);

                if (usuario == null)
                {
                    TempData["Error"] = "El usuario no existe.";
                    return RedirectToAction("Index");
                }

                if (!usuario.Estado)
                {
                    TempData["Error"] = "El usuario ya estaba desactivado.";
                    return RedirectToAction("Index");
                }

                // Siempre debe quedar al menos un gerente activo
                if (usuario.IdRol == 2)
                {
                    bool hayOtroGerente = context.TB_Usuario.Any(u => u.IdRol == 2
                                                                   && u.Estado
                                                                   && u.IdUsuario != id);
                    if (!hayOtroGerente)
                    {
                        TempData["Error"] = "No puedes desactivar al único gerente activo del sistema.";
                        return RedirectToAction("Index");
                    }
                }

                usuario.Estado = false;
                context.SaveChanges();

                TempData["Exito"] = "Usuario desactivado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpPost -> Desactivar()");
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reactivar(int id)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                var usuario = context.TB_Usuario.FirstOrDefault(u => u.IdUsuario == id);

                if (usuario == null)
                {
                    TempData["Error"] = "El usuario no existe.";
                    return RedirectToAction("Index");
                }

                if (usuario.Estado)
                {
                    TempData["Error"] = "El usuario ya estaba activo.";
                    return RedirectToAction("Index");
                }

                usuario.Estado = true;
                context.SaveChanges();

                TempData["Exito"] = "Usuario reactivado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpPost -> Reactivar()");
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Editar usuario

        [HttpGet]
        public ActionResult Editar(int id)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                var usuario = context.TB_Usuario.FirstOrDefault(u => u.IdUsuario == id);

                if (usuario == null)
                {
                    TempData["Error"] = "El usuario no existe.";
                    return RedirectToAction("Index");
                }

                ViewBag.Id = usuario.IdUsuario;
                ViewBag.Identificacion = usuario.Identificacion;
                ViewBag.NombreCompleto = usuario.NombreCompleto;
                ViewBag.CorreoElectronico = usuario.CorreoElectronico;
                ViewBag.EsMismo = (id == Convert.ToInt32(Session["IdUsuarioSession"]));
                CargarRoles(usuario.IdRol);

                return View();
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpGet -> Editar()");
                TempData["Error"] = MensajeErrorGenerico;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(int id, string identificacion, string nombreCompleto,
                                   string correoElectronico, int idRol)
        {
            if (Session["IdUsuarioSession"] == null || (bool?)Session["EstadoUsuarioSession"] != true || Convert.ToInt32(Session["RolUsuarioSession"]) != 2)
            {
                return RedirectToAction("Error404", "Error");
            }

            try
            {
                var usuario = context.TB_Usuario.FirstOrDefault(u => u.IdUsuario == id);

                if (usuario == null)
                {
                    TempData["Error"] = "El usuario no existe.";
                    return RedirectToAction("Index");
                }

                bool esMismo = (id == Convert.ToInt32(Session["IdUsuarioSession"]));

                // Un gerente no puede cambiarse el rol a sí mismo (xq perdería el acceso)
                if (esMismo)
                {
                    idRol = usuario.IdRol;
                }

                identificacion = identificacion?.Trim();
                nombreCompleto = nombreCompleto?.Trim();
                correoElectronico = correoElectronico?.Trim();

                // Para volver a pintar el formulario si hay error
                ViewBag.Id = id;
                ViewBag.EsMismo = esMismo;
                ViewBag.Identificacion = identificacion;
                ViewBag.NombreCompleto = nombreCompleto;
                ViewBag.CorreoElectronico = correoElectronico;

                
                if (string.IsNullOrWhiteSpace(identificacion) || string.IsNullOrWhiteSpace(nombreCompleto)
                    || string.IsNullOrWhiteSpace(correoElectronico))
                {
                    ViewBag.Error = "Todos los campos son obligatorios.";
                    CargarRoles(idRol);
                    return View();
                }

              
                if (!System.Text.RegularExpressions.Regex.IsMatch(correoElectronico, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ViewBag.Error = "El correo electrónico no tiene un formato válido.";
                    CargarRoles(idRol);
                    return View();
                }

               
                if (!System.Text.RegularExpressions.Regex.IsMatch(identificacion, @"^[0-9]{10}$"))
                {
                    ViewBag.Error = "El código debe tener exactamente 10 dígitos.";
                    CargarRoles(idRol);
                    return View();
                }

                // Validaciones que solo el backend deberia de hacer
                if (!context.TB_Rol.Any(r => r.IdRol == idRol))
                {
                    ViewBag.Error = "El rol seleccionado no es válido.";
                    CargarRoles(usuario.IdRol);
                    return View();
                }

                // Duplicados: se excluye al propio usuario que se está editando
                if (context.TB_Usuario.Any(u => u.CorreoElectronico == correoElectronico && u.IdUsuario != id))
                {
                    ViewBag.Error = "Ya existe otro usuario con ese correo electrónico.";
                    CargarRoles(idRol);
                    return View();
                }

                if (context.TB_Usuario.Any(u => u.Identificacion == identificacion && u.IdUsuario != id))
                {
                    ViewBag.Error = "Ya existe otro usuario con ese código.";
                    CargarRoles(idRol);
                    return View();
                }

                // Siempre debe quedar al menos un gerente activo
                bool eraGerenteActivo = usuario.IdRol == 2 && usuario.Estado;
                bool dejaDeSerGerente = idRol != 2;

                if (eraGerenteActivo && dejaDeSerGerente)
                {
                    bool hayOtroGerente = context.TB_Usuario.Any(u => u.IdRol == 2 && u.Estado && u.IdUsuario != id);
                    if (!hayOtroGerente)
                    {
                        ViewBag.Error = "No puede cambiar el rol: debe quedar al menos un gerente activo en el sistema.";
                        CargarRoles(usuario.IdRol);
                        return View();
                    }
                }

                usuario.Identificacion = identificacion;
                usuario.NombreCompleto = nombreCompleto;
                usuario.CorreoElectronico = correoElectronico;
                usuario.IdRol = idRol;
                context.SaveChanges();

                // Si se editó a sí mismo, refrescar el nombre que se muestra en el header
                if (esMismo)
                {
                    Session["NombreUsuarioSession"] = usuario.NombreCompleto;
                }

                TempData["Exito"] = "Usuario actualizado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrió una excepción, UsuarioController - HttpPost -> Editar()");
                ViewBag.Error = MensajeErrorGenerico;
                ViewBag.Id = id;
                CargarRoles(idRol);
                return View();
            }
        }

        #endregion

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