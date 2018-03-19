using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Customers;
using TS.Core.Domain.Projects;

namespace TS.Web.Helper
{
    public class CookieHelper
    {
        //public CustomerState GetCustomerStateCookie(HttpContextBase httpContext)
        //{
        //    var requestCookie = httpContext.Request.Cookies["CustomerState"];
        //    var currentStateCookieValue = requestCookie?.Value;
        //    if (!string.IsNullOrWhiteSpace(currentStateCookieValue))
        //    {
        //        return JsonConvert.DeserializeObject<CustomerState>(currentStateCookieValue);
        //    }

        //    return new CustomerState
        //    {
        //        CustomerId = 0,
        //        ModelId = null,
        //        ProfessionId = null,
        //        ProjectId = null,
        //        Role = null,
        //        ProjectIsFiled = null,
        //        IsProjectManager = null,
        //    };

        //}

        //public void SetCustomerStateCookie(HttpContextBase httpContext,Customer customer)
        //{
        //    if (customer == null)
        //        throw new ArgumentNullException("set customer cookie false");

        //    var state = GetCustomerStateCookie(httpContext);
        //    state.CustomerId = customer.Id;
        //    state.Role = customer.CurrentRole;
        //    state.ProjectId = customer.CurrentProjectId;
        //    state.ModelId = customer.CurrentModelId;
        //    state.ProfessionId = customer.CurrentProfessionId;
        //    state.ProjectIsFiled = customer.CurrentProjectIsFiled;
        //    state.IsProjectManager = customer.IsCurrentProjectManager;

        //    var cookie = new HttpCookie("CustomerState");
        //    cookie.HttpOnly = true;
        //    cookie.Value = JsonConvert.SerializeObject(state);
        //    cookie.Expires = DateTime.Now.AddMinutes(30);

        //    httpContext.Response.Cookies.Remove("CustomerState");
        //    httpContext.Response.Cookies.Add(cookie);
        //}
    }

}