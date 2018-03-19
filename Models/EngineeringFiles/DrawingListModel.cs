using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;
using TS.Web.Models.Projects;

namespace TS.Web.Models.EngineeringFiles
{
    public class DrawingListModel
    {
        public DrawingListModel()
        {
            Drawings = new List<DrawingModel>();
            Roles = new List<Role>();
        }
        public string ProjectName { get; set; }
        public int DrawingSeriesId { get; set; }
        public bool ProjectIsFiled { get; set; }
        public List<Role> Roles { get; set; }

        public List<DrawingModel> Drawings { get; set; }
        public class DrawingModel
        {
            public int DrawingId { get; set; }
            public string DrawingName { get; set; }
            public string DrawingVersion { get; set; }
            public string DrawingCatalog { get; set; }
            public FlowCode DrawingStatus { get; set; }
            public string StatusDes
            {
                get
                {
                    return DrawingStatus.GetDescription();
                }
            }
            public string Description { get; set; }
            public string UpdateTime { get; set; }
            public string FileSize { get; set; }
            public string Uri { get; set; }
        }
    }    
}