using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Comments;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;

namespace TS.Web.Models.Comments
{
    public class CommentDetailModel
    {
        public CommentDetailModel()
        {
            Annotations = new List<AnnotationModel>();
            Roles = new List<Role>();
        }
        public bool IsProjectFiled { get; set; }
        public string ProjectName { get; set; } 
        public string Floor { get; set; }
        public int CommentId { get; set; }
        public int PreCommentId { get; set; }
        public int NextCommentId { get; set; }
        public string EngineeringName { get; set; }
        public string CommentType { get; set; }
        public string Profession { get; set; }
        public List<Role> Roles { get; set; }
        public FlowCode Status { get; set; }
        public FlowCode ProfessionStatus { get; set; }
        public int CurrentModelVersionId { get; set; }
        public string StatusDes { get { return Status.GetDescription(); } }
        public string DesignCompany { get; set; }
        public string DesignCompanyManager { get; set; }
        public string BuildingCompany { get; set; }
        public string BuildingCompanyManager { get; set; }
        public string Censorship { get; set; }
        public string CensorshipManager { get; set; }
        public string Checker { get; set; }
        public string Reviewer { get; set; }
        public string Description { get; set; }
        public string ReturnDes { get; set; }
        public string CreateVersionId { get; set; }
        public string CreateTime { get; set; }
        public List<AnnotationModel> Annotations { get; set; } 
        public class AnnotationModel
        {
            public string Uri { get; set; }
            public string AnnotationId { get; set; }
            /// <summary>
            /// 高清缩略图的链接
            /// </summary>
            public string HDBimThumbUrl { get; set; }
            public BIMType type { get; set; }
            public string Name { get; set; }
        }

        public CommentButtom commentButton { get; set; }
        
        public class CommentButtom
        {
            public CommentButtom()
            {
                Roles = new List<Role>();
            }

            public bool IsInCommentDetail { get; set; }
            public int CommentId { get; set; }
            public int ProfessionId { get; set; }
            public FlowCode ProfessionStatus { get; set; }
            public FlowCode Status { get; set; }
            public FlowCode ModelStatus { get; set; }
            public List<Role> Roles { get; set; }
            public int CurrentModelVersionId { get; set; }
        }
    }
}