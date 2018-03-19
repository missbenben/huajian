using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using TS.Service.DataAccess.Projects;
using TS.Web.Helper;
using TS.Web.Models.Projects;

namespace TS.Web.Controllers
{
    public class CommonController : BaseController
    {
        public static readonly int G_BLOCK_LEN_PER = 2 * 1024 * 1024;
        // GET: Common
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult PageNotFound()
        {
            this.Response.StatusCode = 404;
            this.Response.TrySkipIisCustomErrors = true;
            this.Response.ContentType = "text/html";

            return View();
        }

        #region Method    

        #endregion
    }
}