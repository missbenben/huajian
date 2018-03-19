using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Web.Models.CustomerLogin;
using TS.Core.Domain.Customers;

namespace TS.Web.Filters
{
    public class RoleFilter : ActionFilterAttribute
    {
        private List<string> RoleTypes;

        public RoleFilter(CustomerType roleType)
        {
            RoleTypes = new List<string> { roleType.ToString() };
        }

        public RoleFilter(string roleTypes)
        {
            RoleTypes = roleTypes.Split(',').Select(s => s.ToString()).ToList();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpCookie isLoginCookie = System.Web.HttpContext.Current.Request.Cookies.Get(".ASPXAUTH");
            if (isLoginCookie != null)
            {
                bool isContain = false;
                HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies.Get("Role");
                if (cookie == null)
                {
                    filterContext.Result = new RedirectResult("/Customer/Login");
                }
                else
                {
                    string cookieValue = Convert.ToString(cookie.Value);
                    foreach (string roleType in RoleTypes)
                    {
                        if (roleType == cookieValue)
                        {
                            isContain = true;
                            break;
                        }
                    }

                }

                if (!isContain && cookie != null)
                {
                    string cookieValue = Convert.ToString(cookie.Value);
                    if (cookieValue == "Admin")
                    {
                        filterContext.Result = new RedirectResult("/Project/RoleDistributionList");
                    }
                    else if (cookieValue == "User")
                    {
                        filterContext.Result = new RedirectResult("/Project/ProjectIndex");
                    }
                    else if (cookieValue == "SuperAdmin")
                    {
                        filterContext.Result = new RedirectResult("/SuperAdmin/SuperAdminchecklist");
                    }
                    else
                    {
                        filterContext.Result = new RedirectResult("/Customer/Login");
                    }
                }
            }
            else
            {
                filterContext.Result = new RedirectResult("/Customer/Login");
            }
        }
    }
}