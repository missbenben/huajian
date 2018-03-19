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
    public class ProjectDetailModel
    {
        public ProjectDetailModel()
        {
            Engineerings = new List<EngineeringModel>();
        }

        public int ProjectId { get; set; }
        public bool IsFiled { get; set; }
        public List<Role> Roles { get; set; }
        public CustomerType customerType { get; set; }
        public OrganizationType? OrganizationType { get; set; }
        public string ProjectName { get; set; }
        public string DeliverNo { get; set; }
        public string BuildingCompany { get; set; }
        public string BuildingCompanyContacterName { get; set; }
        public string BuildingCompanyContacterPhone { get; set; }
        public ProjectCatalog ProjectCatalog { get; set; }
        public string ProjectCatalogDes { get { return ProjectCatalog.GetDescription(); } }
        public string IsPrefabricatedBuilding { get; set; }
        public string BuildingLocation { get; set; }
        public string Censorship { get; set; }
        public string DesignCompany { get; set; }
        public decimal BuildingArea { get; set; }
        public decimal CivilAirDefenseArea { get; set; }
        public string DesignCompanyManager { get; set; }
        public string BuildingCompanyManager { get; set; }
        public string CensorshipManager { get; set; }

        public List<EngineeringModel> Engineerings { get; set; }

        public class EngineeringModel
        {
            public EngineeringModel()
            {
                ProfessionRoles = new List<ProfessionRoleModel>();
            }

            public int Id { get; set; }
            public string Name { get; set; }
            public double Height { get; set; }
            public int FloorsAboveGround { get; set; }
            public int FloorsUnderGround { get; set; }
            public decimal AreaAboveGround { get; set; }
            public string Desription { get; set; }

            public List<ProfessionRoleModel> ProfessionRoles { get; set; }

            public class ProfessionRoleModel
            {
                public string ProfessionName { get; set; }
                public string EngineeringChecker { get; set; }
                public string EngineeringReviwer { get; set; }
            }
        }
    }
}