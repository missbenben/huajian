using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Data.Extensions;

namespace TS.Web.Controllers
{
    public class HomeController : BaseController
    {
        // GET: Home
        public ActionResult Index()
        {
            ViewBag.CustomerType = CurrentCustomer.CustomerType.GetDescription();
            if (CurrentCustomer.CustomerType.GetDescription() != "系统管理员")
            {
                ViewBag.CustomerOrganizationType = CurrentCustomer.Organization.OrganizationType.GetDescription();
            }
            else ViewBag.CustomerOrganizationType = "超级管理员";

            return View();
        }
    }
}