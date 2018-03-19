using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Projects;
using TS.Web.Models.Projects;
using System.Web.Mvc;
using TS.Data.Extensions;

namespace TS.Web.Models.EngineeringFiles
{
    public class ModelListModel
    {
        public ModelListModel()
        {
            ModelList = new List<ModelModel>();
            AvaliableStatus = new List<SelectListItem>();
            Roles = new List<Role>();
        }

        public int ProjectId { get; set; }
        public List<Role> Roles { get; set; }
        public bool ProjectIsFiled { get; set; }

        public bool CanUploadModel { get; set; }

        public string SelectedStatus { get; set; }
        public List<SelectListItem> AvaliableStatus { get; set; }

        public List<ModelModel> ModelList { get; set; }
        public class ModelModel
        {
            public int ModelVersionId { get; set; }
            public string ModelVersionNo { get; set; }
            public string UploadDescription { get; set; }
            public string RejectDescription { get; set; }
            public FlowCode ModelStatus { get; set; }
            public string ModelStatusDes { get { return ModelStatus.GetDescription(); } }
            public string UpdateTime { get; set; }
        }
    }
}