using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.Projects
{
    public class ProjectIndexModel
    {
        public OrganizationType OrganizationType { get; set; }
        public ProjectPageType PageType { get; set; }
    }
}