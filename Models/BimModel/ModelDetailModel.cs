using System.Collections.Generic;
using System.Web.Mvc;
using TS.Core.Domain.Customers;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.BimModel
{
    public class ModelDetailModel
    {
        public ModelDetailModel()
        {
            Comments = new List<CommentModel>();
            AvailableRoles = new List<SelectListItem>();
            ModelProfessions = new List<SelectListItem>();
            IntegralityCommentTypes = new List<SelectListItem>();
            CommentTypes = new List<SelectListItem>();
            NewComment = new AddNewCommentModel();
            AvailableFloors = new List<SelectListItem>();
        }

        /// <summary>
        /// 完整性审查阶段-是否可以提交给复核人
        /// </summary>
        public bool Pre_CanCheckerSubmit { get; set; }
        /// <summary>
        /// 完整性审查阶段-是否可以提交给经理
        /// </summary>
        public bool Pre_CanReviewerSubmit { get; set; }
        /// <summary>
        /// 完整性审查阶段-是否可以驳回给审查人
        /// </summary>
        public bool Pre_CanReviewerReturn { get; set; }

        /// <summary>
        /// 设计审查阶段-是否可以提交给复核人
        /// </summary>
        public bool CanCheckerSubmit { get; set; }

        /// <summary>
        /// 设计审查阶段-是否可以提交给经理
        /// </summary>
        public bool CanReviewerSubmit { get; set; }

        /// <summary>
        /// 设计审查阶段-是否可以驳回给审查人
        /// </summary>
        public bool CanReviewerReturn { get; set; }

        public bool CanViewComments { get; set; }

        public string ProjectGuid { get; set; }

        public string ProjectName { get; set; }

        public string ProjectManagerName { get; set; }

        public string ModelGuid { get; set; }

        public int CommentId { get; set; }

        public int CurrentProfessionId { get; set; }

        public FileVersion CurrentFileVersion { get; set; }

        public EngineeringFile EngineeringFile { get; set; }

        public Customer CurrentCustomer { get; set; }

        public int CurrentFileVersionId { get; set; }

        public ModelProfession CurrentModelProfession { get; set; }

        public string TargetAnnotationId { get; set; }

        ///// <summary>
        ///// 当前登录用户的激活角色
        ///// </summary>
        //public Role Role { get; set; }

        public List<SelectListItem> AvailableRoles { get; set; }

        /// <summary>
        /// 当前专业的状态
        /// </summary>
        public FlowCode ProfessionStatus { get; set; }

        /// <summary>
        /// 楼层
        /// </summary>
        public List<SelectListItem> AvailableFloors { get; set; }

        /// <summary>
        /// 新建意见的类型
        /// </summary>
        public List<SelectListItem> IntegralityCommentTypes { get; set; }

        /// <summary>
        /// 新建意见的类型
        /// </summary>
        public List<SelectListItem> CommentTypes { get; set; }

        /// <summary>
        /// 模型可选专业
        /// </summary>
        public List<SelectListItem> ModelProfessions { get; set; }

        public List<CommentModel> Comments { get; set; }

        public AddNewCommentModel NewComment { get; set; }

    }
}