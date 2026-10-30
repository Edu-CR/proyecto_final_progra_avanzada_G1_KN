using ProyectoFinalBioquimia_G1.EF;
using ProyectoFinalBioquimia_G1.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class AuthController : Controller
    {
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
                using (var context = new BioquimiaDBEntities())
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
            }
            catch (Exception ex)
            {

                BitacoraService.Registrar(ex, "Ocurrio una excepción, AuthController - HttpPost -> Login()");
                ViewBag.Message = "Se presentó un error, consulte con el administrador.";
            }

            return View(model); //asi no perdemos los datos
        }
    }
}
