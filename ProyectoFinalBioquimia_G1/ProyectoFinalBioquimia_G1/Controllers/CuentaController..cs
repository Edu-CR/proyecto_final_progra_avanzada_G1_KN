using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProyectoFinalBioquimia_G1.Controllers
{
    public class CuentaController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
    }
}