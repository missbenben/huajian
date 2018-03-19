using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Web.Models.Projects;

namespace TS.Web.Models.EngineeringFiles
{
    public class ModelReviewedListModel
    {
        public ModelReviewedListModel()
        {
            ModelReviewedList = new List<ModelReviewedModel>();
        }
        public int ProjectId { get; set; }
        public OrganizationType OrganizationType { get; set; }
        public List<ModelReviewedModel> ModelReviewedList { get; set; }
        public class ModelReviewedModel
        {
            public int ModelVersionId { get; set; }
            public string ModelVersionNo { get; set; }
            public string UploadDescription { get; set; }
            public string RejectDescription { get; set; }
            public string UpdateTime { get; set; }
            public int CommnetCount { get; set; }
        }
    }
}