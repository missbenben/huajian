using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;
using TS.Web.Models.Projects;
using System.Web.Mvc;

namespace TS.Web.Models.EngineeringFiles
{
    public class DrawingSeriesListModel
    {
        public DrawingSeriesListModel()
        {
            AvaliableStatus = new List<SelectListItem>();
            DrawingSeriesList = new List<DrawingSeriesModel>();
            Roles = new List<Role>();
        }
        public List<Role> Roles { get; set; }
        public bool ProjectIsFiled { get; set; }
        public List<SelectListItem> AvaliableStatus { get; set; }

        public List<DrawingSeriesModel> DrawingSeriesList { get; set; }
        public class DrawingSeriesModel
        {
            public int DrawingId { get; set; }
            public string PicUri { get; set; }
            public int DrawingSeriesId { get; set; }
            public string DrawingName { get; set; }
            public string DrawingVersion { get; set; }
            public string Description { get; set; }
            public int DrawingProfessionId { get; set; }
            public string DrawingProfession { get; set; }
            public int DrawingCategoryId { get; set; }
            public string DrawingCategory { get; set; }
            public string FileSize { get; set; }
            public FlowCode DrawingStatus { get; set; }
            public string DrawingStatusDes
            {
                get
                {
                    //TODO 二期添加图纸状态描述
                    //return this.DrawingStatus.GetDescription();
                    return "已上传";
                }
            }
            public string UpdateTime { get; set; }
        }
    }
}