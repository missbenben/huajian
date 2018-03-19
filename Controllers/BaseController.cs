using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TS.Core.Domain.Customers;
using TS.Core.Domain.Projects;
using TS.Service.DataAccess.Customers;
using TS.Web.Filters;
using TS.Web.Helper;

namespace TS.Web.Controllers
{
    [HttpExceptionFilter]
    public class BaseController : System.Web.Mvc.Controller
    {

        private Customer customer;

        #region  Fields

        protected Customer CurrentCustomer
        {
            get
            {
                if (customer != null)
                    return customer;
                
                //var formsIdentity = (FormsIdentity)HttpContext.User.Identity;
                //var userGuid = formsIdentity.Ticket.UserData;
                var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                string userGuid = ticket.UserData;

                if (String.IsNullOrWhiteSpace(userGuid))
                    customer = null;

                Guid guid;
                if (Guid.TryParse(userGuid, out guid))
                {
                    customer = new CustomerService().Get(e => e.CustomerGuid == guid);

                }

                if (customer == null) RedirectToAction("UserExit", "Customer");
                    //throw new Exception("can find login customer");

                return customer;
            }
        }

        #endregion

        #region Utilities

        protected void SetCustomerCookie(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException("set test customer fail");

            var now = DateTime.UtcNow.ToLocalTime();

            var ticket = new FormsAuthenticationTicket(
                1 /*version*/,
                customer.EmployeeId,
                now,
                now.Add(FormsAuthentication.Timeout),
                false,
                customer.CustomerGuid.ToString(),
                FormsAuthentication.FormsCookiePath);

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket) { HttpOnly = true };
            if (ticket.IsPersistent)
            {
                cookie.Expires = ticket.Expiration;
            }
            cookie.Secure = FormsAuthentication.RequireSSL;
            cookie.Path = FormsAuthentication.FormsCookiePath;
            if (FormsAuthentication.CookieDomain != null)
            {
                cookie.Domain = FormsAuthentication.CookieDomain;
            }

            Response.Cookies.Add(cookie);
        }

        #endregion

        #region Method

        public virtual void SetTestCurrentCustomer(int customerId)
        {
            var customer = new CustomerService().Get(e => e.Id == customerId);

            SetCustomerCookie(customer);
        }

        public virtual void SetTestCurrentCustomer(Customer customer)
        {
            SetCustomerCookie(customer);
        }

        public virtual string RenderPartialViewToString(string viewName, object model)
        {
            //Original source code: http://craftycodeblog.com/2010/05/15/asp-net-mvc-render-partial-view-to-string/
            if (string.IsNullOrEmpty(viewName))
                viewName = this.ControllerContext.RouteData.GetRequiredString("action");

            this.ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = System.Web.Mvc.ViewEngines.Engines.FindPartialView(this.ControllerContext, viewName);
                var viewContext = new ViewContext(this.ControllerContext, viewResult.View, this.ViewData, this.TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        public JsonResult NoAuthorityJson(string errmsg = "无操作权限")
        {
            return Json(new { result = false, errmsg = errmsg }, JsonRequestBehavior.AllowGet);
        }

        #endregion

    }
}