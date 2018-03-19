using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Customers;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;

namespace TS.Web.Models.Projects
{
    public class ProjectProcessModel
    {
        public ProjectProcessModel()
        {
            Description = "无";
        }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}