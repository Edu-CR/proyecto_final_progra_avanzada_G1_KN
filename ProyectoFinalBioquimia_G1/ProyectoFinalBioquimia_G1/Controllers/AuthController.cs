using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class AuthController : Controller
    {
        private BioquimiaDBEntities context = new BioquimiaDBEntities(); //conexion mejor asi para no andar iniciandola a cada rato.

        #region Login/Logout
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(TB_Usuario model)
        {
            try
            {
                var response = context.TB_Usuario.FirstOrDefault(x => x.CorreoElectronico == model.CorreoElectronico && x.Contrasenna == model.Contrasenna && x.Estado == true); //firstordefault x ser solo 1 registro

                if (response != null)
                {
                    Session["IdUsuarioSession"] = response.IdUsuario;
                    Session["NombreUsuarioSession"] = response.NombreCompleto;
                    Session["RolUsuarioSession"] = response.IdRol;
                    return RedirectToAction("Index", "Home"); //TO DO: Luego ver como redirigimos al usaurio segun el rol, x ahroa solo van al index
                }
                else
                {

                    ViewBag.Message = "No se autenticó su información, consulte con el administrador.";
                }
            }
            catch (Exception ex)
            {

                BitacoraService.Registrar(ex, "Ocurrio una excepción, AuthController - HttpPost -> Login()");
                ViewBag.Message = "Se presentó un error, consulte con el administrador.";
            }

            return View(model); //asi no perdemos los datos
        }

        public ActionResult Logout()//TO DO
        {
            return null;
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
        public ActionResult Recuperar(String correoElectronico)
        {
            if (string.IsNullOrEmpty(correoElectronico)) //TO DO: Revisar si seeria mejjor meterle JS
            {
                ViewBag.Message = "Por favor ingrese un correo electrónico válido.";
                return View();
            }
            try
            {
                var response = context.TB_Usuario.FirstOrDefault(x => x.CorreoElectronico == correoElectronico && x.Estado == true); //ver primero si el usuario existe

                if (response != null) {
                    bool usuarioTieneTokens = context.TB_TokenRecuperacion.Any(x => x.IdUsuario == response.IdUsuario);
                    if (usuarioTieneTokens) {  //se actualizan todos los tokens que estaban sin usarse
                        List<TB_TokenRecuperacion> tokensParaActualizar = context.TB_TokenRecuperacion.Where(token => token.Usado == false).ToList(); //si usado es false es pq en BD es 0, osea se pudieron haber enviado pero no se ha confirmado, pasan a 1 xq solo puede haber 1 token q sirva
                        tokensParaActualizar.ForEach(token => token.Usado = true);
                        context.SaveChanges();
                    }


                }

















                if (usuarioTieneTokens == true) {
                    




                    return RedirectToAction("Restablecer", new { correoElectronico = correoElectronico });
                }
                else
                {
                    return RedirectToAction("Restablecer", new { correoElectronico = correoElectronico }); //el correo siempre pasa porque si no existe pq igual hay que disimular en la URL
                }



            }
            catch (Exception ex)
            {



            }





            return View();
        }



        #endregion


        #region Restablecer

        [HttpGet]
        public ActionResult Restablecer(String correoElectronico)
        {
            return View(); 
        }


        #endregion
    }
}