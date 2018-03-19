using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.Projects
{
    public class ProjectContentModel
    {
        public ProjectContentModel()
        {
            AvaliableEngineers = new List<SelectListItem>();
            AvaliableProfession = new List<Profession>();
            Roles = new List<Role>();
        }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string DrawingTab { get; set; }
        public string ModelTab { get; set; }
        public string ModelReviewedTab { get; set; }

        public List<Role> Roles { get; set; }

        public List<SelectListItem> AvaliableEngineers { get; set; }
        public List<Profession> AvaliableProfession { get; set; }

        public class Profession
        {
            public int ProfessionId { get; set; }
            public string ProfessionName { get; set; }
            public string ProfessionIcon { get; set; }
        }
    }
}