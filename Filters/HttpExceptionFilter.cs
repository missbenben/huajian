using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Helper;
using TS.Service.DataAccess.Logs;

namespace TS.Web.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class HttpExceptionFilter : FilterAttribute, IExceptionFilter
    {
        public virtual void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                //Because its a exception raised after ajax invocation
                //Lets return Json

                LogRecordService.Error(filterContext.Exception);
                //filterContext.Result = new JsonResult
                //{
                //    Data = new { result = false, errmsg = filterContext.Exception.Message },
                //    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                //};

                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.StatusCode = 500;
            }
        }
    }
}