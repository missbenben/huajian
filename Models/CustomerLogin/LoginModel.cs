using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.CustomerLogin
{
    
    public class LoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsRemember { get; set; }
        public string JustRegister { get; set; }

    }
}