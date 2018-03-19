using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;

namespace TS.Web.Models.Projects
{
    public class ProjectModel
    {
        public ProjectModel()
        {
            Roles = new List<Role>();
        }

        public int ProjectId { get; set; }
        public string DeliverNo { get; set; }
        public string ProjectName { get; set; }
        public ProjectCatalog ProjectCatalog { get; set; }
        public string ProjectCatalogDes
        {
            get
            {
                return ProjectCatalog.GetDescription();
            }
        }

        /// <summary>
        /// 设计公司名称
        /// </summary>
        public string DesignCompany { get; set; }

        /// <summary>
        /// 建设公司
        /// </summary>
        public string ConstructionCompany { get; set; }

        /// <summary>
        /// 审查公司
        /// </summary>
        public string Censorship { get; set; }

        /// <summary>
        /// 是否审核
        /// </summary>
        public bool IsFiled { get; set; }

        public List<Role> Roles { get; set; }

        public bool IsDistribute { get; set; }

        public ProjectPageListUserType PageLisgType { get; set; }

        /// <summary>
        /// BIM 引擎的项目ID
        /// </summary>
        public string BIMProjectId { get; set; }

        /// <summary>
        /// 建设地点
        /// </summary>
        public string BuildingLocation { get; set; }

        /// <summary>
        /// 建筑面积
        /// </summary>
        public long BuildingArea { get; set; }

        /// <summary>
        /// 人防面积
        /// </summary>
        public decimal CivilAirDefenseArea { get; set; }

        /// <summary>
        /// 是否装配式建筑
        /// </summary>
        public bool IsPrefabricatedBuilding { get; set; }

        /// <summary>
        /// 装配式建筑面积
        /// </summary>
        public decimal PrefabricatedBuildingArea { get; set; }

        /// <summary>
        /// 创建人名称
        /// </summary>
        public string CreatorName { get; set; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        public int CreatorId { get; set; }

        /// <summary>
        /// 项目设置类别，子项目信息
        /// </summary>
        public List<EngineeringModel> Engineerings { get; set; }

        /// <summary>
        /// 项目设置类别，子项目信息
        /// </summary>
        public List<Organization> DesignCompanyList { get; set; }


        /// <summary>
        /// 项目设置类别，子项目信息
        /// </summary>
        public List<Organization> ConstructionCompanyList { get; set; }


        /// <summary>
        /// 项目设置类别，子项目信息
        /// </summary>
        public List<Organization> CensorshipList { get; set; }

        public bool NeedOperate { get; set; }
    }

    public partial class EngineeringModel
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public string Name { get; set; }

        public double Height { get; set; }

        public string Description { get; set; }

        public int FloorsAboveGround { get; set; }

        public int FloorsUnderGround { get; set; }

        public decimal AreaAboveGround { get; set; }

        public int ModelIterationCount { get; set; }

    }

    public enum ProjectPageListUserType
    {
        /// <summary>
        /// 用户查看项目列表
        /// </summary>
        UserProjectList = 1,


        /// <summary>
        /// 企业管理员分配角色使用
        /// </summary>
        AdminDistribute = 2,

        /// <summary>
        /// 企业管理员查看企业用户管理的项目列表
        /// </summary>
        AdminResponsibleProjectList = 3
    }
}