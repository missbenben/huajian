using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TS.Web.Filters
{
    public class LoginFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session["CurrentCustomerId"] == null)
            {
                string redirectOnSuccess = filterContext.HttpContext.Server.UrlEncode(filterContext.HttpContext.Request.Url.AbsolutePath);
                string returnUrl = string.Format("?returnUrl={0}", redirectOnSuccess);
                string loginUrl = "/Customer/Login" + returnUrl;
                filterContext.HttpContext.Response.Redirect(loginUrl, true);
            }
        }
    }
}