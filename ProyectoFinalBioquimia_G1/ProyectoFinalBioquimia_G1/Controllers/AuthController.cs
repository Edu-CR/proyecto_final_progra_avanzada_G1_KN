using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;



namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class AuthController : Controller
    {
        private const string MensajeErrorGenerico = "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.";

        private BioquimiaDBEntities context = new BioquimiaDBEntities(); //conexion mejor asi para no andar iniciandola a cada rato.

        #region Login/Logout
        [HttpGet]
        public ActionResult Login()
        {
            ViewBag.Message = TempData["Message"];
            return View();
        }

        [HttpPost]
        public ActionResult Login(TB_Usuario model)
        {
            try
            {
                var response = context.TB_Usuario.FirstOrDefault(usuario => usuario.CorreoElectronico == model.CorreoElectronico && usuario.Contrasenna == model.Contrasenna && usuario.Estado == true); //firstordefault x ser solo 1 registro

                if (response != null)
                {
                    Session["IdUsuarioSession"] = response.IdUsuario;
                    Session["NombreUsuarioSession"] = response.NombreCompleto;
                    Session["RolUsuarioSession"] = response.IdRol;
                    Session["EstadoUsuarioSession"] = response.Estado;
                    return RedirectToAction("Index", "Home"); //TO DO: Luego ver como redirigimos al usaurio segun el rol, x ahroa solo van al index
                }
                else
                {
                    //aqui no se manda al 404
                    ViewBag.Message = "No se autenticó su información, consulte con el administrador.";
                }
            }
            catch (Exception ex)
            {

                BitacoraService.Registrar(ex, "Ocurrio una excepción, AuthController - HttpPost -> Login()");
                TempData["Error"] = MensajeErrorGenerico;
            }

            return View(model); //asi no perdemos los datos
        }

        [HttpPost]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Auth");
        }
        #endregion


        #region Recuperar

        [HttpGet]
        public ActionResult Recuperar()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Recuperar(String correoElectronico)
        {
            if (string.IsNullOrEmpty(correoElectronico))
            {
                ViewBag.Message = "Por favor ingrese un correo electrónico válido."; //TO DO igual ver lo del JS
                return View();
            }

            try
            {
                var response = context.TB_Usuario.FirstOrDefault(usuario => usuario.CorreoElectronico == correoElectronico && usuario.Estado == true);

                if (response != null)
                {
                    bool usuarioTieneTokens = context.TB_TokenRecuperacion.Any(x => x.IdUsuario == response.IdUsuario);

                    if (usuarioTieneTokens)
                    {
                        List<TB_TokenRecuperacion> tokensParaActualizar = context.TB_TokenRecuperacion.Where(tokenUsuario => tokenUsuario.Usado == false && tokenUsuario.IdUsuario == response.IdUsuario).ToList();
                        tokensParaActualizar.ForEach(tokenUsuario => tokenUsuario.Usado = true);
                    }

                    string token = TokenService.GenerarToken();

                    TB_TokenRecuperacion nuevoToken = new TB_TokenRecuperacion
                    {
                        IdUsuario = response.IdUsuario,
                        Token = token,
                        FechaCreacion = DateTime.Now,
                        FechaVencimiento = DateTime.Now.AddMinutes(30),
                        Usado = false,
                        Intentos = 0
                    };

                    context.TB_TokenRecuperacion.Add(nuevoToken);
                    context.SaveChanges();

                    var ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "recursos", "RecuperarContrasena.html");

                    var html = System.IO.File.ReadAllText(ruta, Encoding.UTF8)
                        .Replace("{{Nombre}}", WebUtility.HtmlEncode(response.NombreCompleto)) // ya esta el usaurio extraido más ariiba en "repsonse"
                        .Replace("{{Token}}", WebUtility.HtmlEncode(token))
                        .Replace("{{Anio}}", DateTime.Now.Year.ToString());

                    EmailService emailService = new EmailService();//llamada al service

                    await emailService.EnviarCorreo(response.CorreoElectronico, "Recuperación de Contraseña", html);

                    return RedirectToAction("Restablecer", new { correoElectronico = correoElectronico });
                }
            }
            catch (Exception ex)
            {
                BitacoraService.Registrar(ex, "Ocurrio una excepción, AuthController - HttpPost -> Recuperar()");

                TempData["Error"] = MensajeErrorGenerico;
            }

            return RedirectToAction("Restablecer", new { correoElectronico = correoElectronico });//si no igual se pasa para disimular la URL
        }


        #endregion


        #region Restablecer 

        [HttpGet]
        public ActionResult Restablecer(String correoElectronico)
        {
            ViewBag.CorreoElectronico = correoElectronico;//solo mostrar el correo y se amarra con el mensaje en el front 
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restablecer(String correoElectronico, String Token, String Contrasenna, String ConfirmarContrasenna)
        {

            if (Contrasenna != ConfirmarContrasenna)
            {
                ViewBag.CorreoElectronico = correoElectronico;
                ViewBag.Message = "Las contraseñas no coinciden.";
                return View();
            }
            {
                try
                {
                    var response = context.TB_Usuario.FirstOrDefault(usuario =>
                        usuario.CorreoElectronico == correoElectronico &&
                        usuario.Estado == true);

                    if (response != null)
                    {
                        var tokenVigente = context.TB_TokenRecuperacion
                            .Where(token => token.IdUsuario == response.IdUsuario &&
                                            token.Usado == false &&
                                            token.FechaVencimiento > DateTime.Now)
                            .OrderByDescending(token => token.FechaCreacion)
                            .FirstOrDefault();

                        if (tokenVigente != null)
                        {
                            // Verificar si ya fueron los 5 intentos
                            if (tokenVigente.Intentos >= 5)
                            {
                                tokenVigente.Usado = true;
                                Session["IntentosRecuperacion"] = 5;
                                context.SaveChanges();

                                TempData["Message"] = "El código no es válido o ya venció. Solicite uno nuevo";
                                return RedirectToAction("Login");
                            }

                            // Comparar el código ingresado
                            if (tokenVigente.Token != Token)
                            {
                                tokenVigente.Intentos++;

                                if (tokenVigente.Intentos >= 5)
                                {
                                    tokenVigente.Usado = true;
                                    Session["IntentosRecuperacion"] = 5;
                                }

                                context.SaveChanges();

                                if (tokenVigente.Intentos >= 5)
                                {
                                    TempData["Message"] = "Máximo de intentos alcanzado de ingreso de tokens. Solicite un nuevo token de recuperación o contacte a soporte si tiene dudas.";
                                    return RedirectToAction("Login");
                                }

                                ViewBag.CorreoElectronico = correoElectronico;
                                ViewBag.Message = "El código no es válido o ya venció. Solicite uno nuevo";
                                return View();
                            }

                            // Código okey actualizar contraseña y consumir token
                            response.Contrasenna = Contrasenna;
                            tokenVigente.Usado = true;

                            context.SaveChanges();

                            Session.Remove("IntentosRecuperacion");

                            TempData["Message"] = "Contraseña actualizada. Ya puede iniciar sesión";
                            return RedirectToAction("Login");
                        }
                    }

                    // Si no existe el usuario o no tiene un token vigente
                    int intentos = Convert.ToInt32(Session["IntentosRecuperacion"] ?? 0);
                    intentos++;

                    Session["IntentosRecuperacion"] = intentos;

                    if (intentos >= 5)
                    {
                        Session.Remove("IntentosRecuperacion");
                        TempData["Message"] = "Máximo de intentos alcanzado de ingreso de tokens. Solicite un nuevo token de recuperación o contacte a soporte si tiene dudas.";
                        return RedirectToAction("Login");
                    }

                    ViewBag.CorreoElectronico = correoElectronico;
                    ViewBag.Message = "El código no es válido o ya venció. Solicite uno nuevo";
                    return View();
                }
                catch (Exception ex)
                {
                    BitacoraService.Registrar(ex, "Ocurrio una excepción, AuthController - HttpPost -> Restablecer()");

                    TempData["Error"] = MensajeErrorGenerico;
                    return RedirectToAction("Login");
                }
            }



        #endregion

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
