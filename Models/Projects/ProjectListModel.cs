using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Domain.Projects;
using TS.Core.Domain.Organizations;
using TS.Data.Extensions;

namespace TS.Web.Models.Projects
{
    public class ProjectListModel
    {
        public ProjectListModel()
        {
            Projects = new List<ProjectModel>();
            AvaliableProductCatalog = new List<SelectListItem>();
        }
        public List<SelectListItem> AvaliableProductCatalog { get; set; }

        public string PageDes { get { return PageType.GetDescription(); } }
        public ProjectPageType PageType { get; set; }
        public string SearchFuzzyInput { get; set; }
        public int SearchProjectCatalogId { get; set; }
        public List<ProjectModel> Projects { get; set; }
        public OrganizationType OrganizationType { get; set; }
        public Role RoleType {
            get
            {
                switch (this.PageType)
                {
                    case ProjectPageType.CheckedProject:
                        return Role.Checker;
                    case ProjectPageType.ReviewedProject:
                        return Role.Reviewer;
                    default:
                        if (OrganizationType == OrganizationType.Censorship)
                            return Role.CensorshipManager;
                        else if (OrganizationType == OrganizationType.BuildingCompany)
                            return Role.BuildingCompanyManager;
                        else return Role.DesignCompanyManager;
                }
            }
        }

        public bool IsFiled
        {
            get
            {
                return this.PageType == ProjectPageType.FiledProject;
            }
        }
    }
}