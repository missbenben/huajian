using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TS.Web.Filters
{
    public class IsLoginFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpCookie isLoginCookie = System.Web.HttpContext.Current.Request.Cookies.Get(".ASPXAUTH");
            HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies.Get("Role");
            if (isLoginCookie != null)
            {
                if (cookie != null)
                {
                    string cookieValue = Convert.ToString(cookie.Value);

                    if (cookieValue == "Admin")
                    {
                        filterContext.Result = new RedirectResult("/Project/RoleDistributionList");
                    }
                    else if (cookieValue == "SuperAdmin")
                    {
                        filterContext.Result = new RedirectResult("/SuperAdmin/SuperAdminchecklist");
                    }
                    else if (cookieValue == "User")
                    {
                        filterContext.Result = new RedirectResult("/Project/ProjectIndex");
                    }
                }
            }
        }
    }
}