using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;

namespace TS.Web.Models.Comments
{
    public class CommentListModel
    {
        public CommentListModel()
        {
            AvaliableVersions = new List<SelectListItem>();
            AvaliableProfessions = new List<SelectListItem>();
            AvaliableStatus = new List<SelectListItem>();
            AvaliableCommentType = new List<SelectListItem>();
        }

        public int SelectedProfessionId { get; set; }
        public int SelectedVersionId { get; set; }
        public string SelectedState { get; set; }
        public CommentType SelectedCommentType { get; set; }

        public string ProjectName { get; set; }
        public OrganizationType OrganizationType { get; set; }
        public int ModelId { get; set; }
        public string EngineeringName { get; set; }
        public FlowCode ModelStatus { get; set; }
        public string ModelStatusDes { get { return ModelStatus.GetDescription(); } }
        public int CommentCount { get; set; }
        public string StatusUpdateTime { get; set; }

        public List<SelectListItem> AvaliableVersions { get; set; }
        public List<SelectListItem> AvaliableProfessions { get; set; }
        public List<SelectListItem> AvaliableStatus { get; set; }
        public List<SelectListItem> AvaliableCommentType { get; set; }

        public class ProfessionStateModel
        {
            public string ProfessionName { get; set; }
            public string IconClass { get; set; }
            public string Grade { get; set; }
            public FlowCode Status { get; set; }
            public string StatusDes { get { return Status.GetDescription(); } }
        }

        public class CommentModel
        {
            public int CommnenId { get; set; }
            public int EngineeringFileCommentId { get; set; }
            public string Floor { get; set; }
            public string Checker { get; set; }
            public string Reviewer { get; set; }
            public string CommentType { get; set; }
            public FlowCode CommnetStatus { get; set; }
            public string CommentStatusDes{ get { return CommnetStatus.GetDescription(); } }
            public string Operation { get; set; }
            public string UpdateTime { get; set; }
            public string CreateVersion { get; set; }
        }
    }
}