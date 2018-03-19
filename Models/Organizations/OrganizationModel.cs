using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;

namespace TS.Web.Models.Organizations
{
    public class OrganizationModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public OrganizationType OrganizationType { get; set; }


    }
}