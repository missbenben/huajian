using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core;
using TS.Core.Domain.Customers;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;
using TS.Service.BusinessLogic.Models;
using TS.Service.DataAccess.Customers;
using TS.Service.DataAccess.Dictionaries;
using TS.Service.DataAccess.Organizetions;
using TS.Service.DataAccess.Permissions;
using TS.Service.DataAccess.Projects;
using TS.Web.Filters;
using TS.Web.Helper;
using TS.Web.Models.Projects;

namespace TS.Web.Controllers
{
    //[LoginFilter]
    public class ProjectController : BaseController
    {

        private CustomerService customerService;
        private CustomerRoleMappingService customerRoleMappingService;
        private ProjectService projectService;
        private OrganizationService organizationService;
        private OrganizationProjectMappingService organizationProjectMappingService;

        public ProjectController()
        {
            customerService = new CustomerService();
            customerRoleMappingService = new CustomerRoleMappingService();
            projectService = new ProjectService();
            organizationService = new OrganizationService();
            organizationProjectMappingService = new OrganizationProjectMappingService();
        }

        #region Utilities

        protected ProjectIndexModel PrepareProjectIndexModel(IQueryable<ProjectUserQueryEntity> query, ProjectPageType pageType)
        {
            ProjectIndexModel model = new ProjectIndexModel();

            model.PageType = pageType;
            model.OrganizationType = CurrentCustomer.Organization.OrganizationType;

            return model;
        }

        protected void PrepareProjectListModel(ProjectListModel model)
        {
            model.AvaliableProductCatalog.Add(new SelectListItem()
            {
                Selected = true,
                Value = "0",
                Text = "全部",
            });
            EnumHelper.EnumToSelectListItem<ProjectCatalog>().ForEach(e => model.AvaliableProductCatalog.Add(e));
        }

        protected PagedList<ProjectModel> PrepareProjectListPageData(IQueryable<ProjectUserQueryEntity> query, int pageIndex, int pageSize)
        {
            var groupQuery = query.GroupBy(e => e.ProjectId);
            var totalCount = groupQuery.Count();
            var list = groupQuery.OrderByDescending(e => e.Key).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList().Select(e => ToProjectModel(e));
            return new PagedList<ProjectModel>(list, pageIndex, pageSize, totalCount);
        }

        protected ProjectModel ToProjectModel(IGrouping<int, ProjectUserQueryEntity> projectQueryEntities)
        {
            var entity = projectQueryEntities.FirstOrDefault();
            if (entity == null)
                throw new ArgumentNullException("project query entity");

            var designCompany = projectQueryEntities.FirstOrDefault(e => e.OrganizationType == OrganizationType.DesignCompany);
            var constructionCompany = projectQueryEntities.FirstOrDefault(e => e.OrganizationType == OrganizationType.BuildingCompany);
            var censorship = projectQueryEntities.FirstOrDefault(e => e.OrganizationType == OrganizationType.Censorship);

            return new ProjectModel()
            {
                Roles = CurrentCustomer.GetCurrentRoles(entity.ProjectId),
                ProjectId = entity.ProjectId,
                ProjectName = entity.ProjectName,
                DeliverNo = entity.DeliverNo,
                ProjectCatalog = entity.ProjectCatalog,
                DesignCompany = designCompany == null ? string.Empty : designCompany.OrganizationName,
                ConstructionCompany = constructionCompany == null ? string.Empty : constructionCompany.OrganizationName,
                Censorship = censorship == null ? string.Empty : censorship.OrganizationName,
                IsFiled = entity.FileTime == null ? false : true,
                IsDistribute = true,
                PageLisgType = ProjectPageListUserType.UserProjectList
            };
        }

        protected ProjectContentModel PrepareProjectContentModel(int projectId)
        {
            ProjectContentModel model = new ProjectContentModel();
            model.ProjectId = projectId;

            var customerRole = CurrentCustomer.CustomerRoles.FirstOrDefault(e => e.ProjectId == projectId && e.FinishTime == null);
            if (customerRole == null)
                throw new ArgumentNullException("find cutomer role by current customer project Id");

            model.ProjectName = customerRole.Project.ProjectName;
            model.Roles = CurrentCustomer.GetCurrentRoles(projectId);

            if ((model.Roles.Contains(Role.Checker) || model.Roles.Contains(Role.Reviewer)) && !model.Roles.Contains(Role.CensorshipManager) && !model.Roles.Contains(Role.CensorshipEngreeingManager))
            {
                model.DrawingTab = "图纸审核";
                model.ModelTab = "模型审核";
            }
            else
            {
                model.DrawingTab = "图纸管理";
                model.ModelTab = "模型管理";
                model.ModelReviewedTab = "审查意见汇总";
            }

            Project project = customerRole.Project;

            foreach (var engineering in project.Engineerings)
            {
                model.AvaliableEngineers.Add(new SelectListItem()
                {
                    Text = engineering.Name,
                    Value = engineering.Id.ToString(),
                    Selected = false,
                });
            }

            if (model.AvaliableEngineers.Count > 0)
            {
                model.AvaliableEngineers.First().Selected = true;
            }

            foreach (var profession in DictionaryService.DrawingProfessionDictionary)
            {
                model.AvaliableProfession.Add(new ProjectContentModel.Profession()
                {
                    ProfessionIcon = string.IsNullOrWhiteSpace(profession.ExtraValue1) ? DictionaryService.defaultProfessionIconClassName : profession.ExtraValue1,
                    ProfessionId = profession.Id,
                    ProfessionName = profession.DisplayName,
                });
            }

            return model;
        }

        public ProjectDetailModel PrepareProjectDetailModel(int projectId)
        {
            var project = new ProjectService().Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var organizations = project.OrganizationProjectMappings.Where(e => e.FinishTime == null).Select(e => e.Organization).ToList();

            var customerRoles = CurrentCustomer.GetCurrentRoles(project.Id);

            ProjectDetailModel model = new ProjectDetailModel()
            {
                ProjectId = project.Id,
                Roles = customerRoles,
                OrganizationType = CurrentCustomer.Organization?.OrganizationType,
                customerType = CurrentCustomer.CustomerType,
                IsFiled = project.FileTime.HasValue,
                BuildingArea = project.BuildingArea,
                BuildingCompany = organizations.FirstOrDefault(e => e.OrganizationType == OrganizationType.BuildingCompany)?.Name,
                BuildingCompanyContacterName = project.ConstructionCompanyContacterName,
                BuildingCompanyContacterPhone = project.ConstructionCompanyContacterPhone,
                BuildingLocation = project.BuildingLocation,
                Censorship = organizations.FirstOrDefault(e => e.OrganizationType == OrganizationType.Censorship)?.Name,
                CivilAirDefenseArea = project.CivilAirDefenseArea,
                DeliverNo = project.DeliverNo,
                DesignCompany = organizations.FirstOrDefault(e => e.OrganizationType == OrganizationType.DesignCompany)?.Name,
                IsPrefabricatedBuilding = project.IsPrefabricatedBuilding ? "是" : "否",
                ProjectCatalog = project.ProjectCatalog,
                ProjectName = project.ProjectName,
                DesignCompanyManager = project.CustomerRoles.FirstOrDefault(e => e.FinishTime == null && e.Role == Role.DesignCompanyManager)?.CustomerName,
                BuildingCompanyManager = project.CustomerRoles.FirstOrDefault(e => e.FinishTime == null && e.Role == Role.BuildingCompanyManager)?.CustomerName,
                CensorshipManager = project.CustomerRoles.FirstOrDefault(e => e.FinishTime == null && e.Role == Role.CensorshipManager)?.CustomerName,
                Engineerings = project.Engineerings.Select(e =>
                    new ProjectDetailModel.EngineeringModel()
                    {
                        Id = e.Id,
                        Desription = e.Description,
                        FloorsAboveGround = e.FloorsAboveGround,
                        FloorsUnderGround = e.FloorsUnderGround,
                        AreaAboveGround = e.AreaAboveGround,
                        Height = e.Height,
                        Name = e.Name,
                        ProfessionRoles = DictionaryService.CommentProfessionDictionary.Select(profession =>
                        {
                            return new ProjectDetailModel.EngineeringModel.ProfessionRoleModel()
                            {
                                ProfessionName = profession.DisplayName,
                                EngineeringChecker = project.CustomerRoles.FirstOrDefault(role => role.EngineeringId == e.Id && role.ProfessionId == profession.Id && role.Role == Role.Checker && role.FinishTime == null)?.CustomerName,
                                EngineeringReviwer = project.CustomerRoles.FirstOrDefault(role => role.EngineeringId == e.Id && role.ProfessionId == profession.Id && role.Role == Role.Reviewer && role.FinishTime == null)?.CustomerName,
                            };
                        }).ToList(),
                    }).ToList(),
            };
          
            return model;
        }

        #endregion

        #region Method

        // GET: Project
        public ActionResult Index()
        {
            return RedirectToAction("ProjectIndex");
        }

        [ChildActionOnly]
        public ActionResult ProjectHeader()
        {
            return View(CurrentCustomer.Organization.OrganizationType);
        }

        [RoleFilter(CustomerType.User)]
        public ActionResult ProjectIndex(ProjectPageType pageType = ProjectPageType.ManageredProject)
        {
            if (CurrentCustomer.CustomerType != CustomerType.User)
                return new HttpUnauthorizedResult();

            var model = new ProjectListModel() {
                PageType = pageType,
            };
            PrepareProjectListModel(model);

            return View(model);
        }

        [RoleFilter(CustomerType.User)]
        public ActionResult ProjectList(ProjectListModel model)
        {
            if (CurrentCustomer.CustomerType != CustomerType.User)
                return new HttpUnauthorizedResult();

            PrepareProjectListModel(model);

            return View(model);
        }

        public ActionResult ProjectListPageData(int pageIndex, int pageSize, ProjectListModel model)
        {
            if (CurrentCustomer.CustomerType != CustomerType.User)
                return NoAuthorityJson();

            model.OrganizationType = CurrentCustomer.Organization.OrganizationType;
            var query = new ProjectService().GetProjectUserQueryEntity(CurrentCustomer,model.RoleType, model.IsFiled, pageIndex, pageSize, model.SearchFuzzyInput, model.SearchProjectCatalogId);
            var pageList = PrepareProjectListPageData(query, pageIndex, pageSize);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_Projects", pageList.Select(s => PrepareProjectModel(s)).ToList()), total = pageList.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        [RoleFilter(CustomerType.User)]
        public ActionResult ProjectContent(int projectId)
        {
            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                return RedirectToAction("ProjectIndex");

            ProjectContentModel model = PrepareProjectContentModel(projectId);

            return View(model);
        }

        [RoleFilter("User,Admin")]
        public ActionResult ProjectDetail(int projectId)
        {
            if (CurrentCustomer.CustomerType == CustomerType.Admin)
            {
                if (!new PermissionService().CanAdminVisitProject(CurrentCustomer, projectId))
                    return RedirectToAction("RoleDistributionList");
            }
            else if (CurrentCustomer.CustomerType == CustomerType.User)
            {
                if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                    return RedirectToAction("ProjectIndex");
            }
            else
            {
                return new HttpUnauthorizedResult();
            }
            ProjectDetailModel model = PrepareProjectDetailModel(projectId);

            return View(model);
        }

        [HttpPost]
        public ActionResult FileProject(int projectId)
        {
            var projectService = new ProjectService();

            var project = projectService.Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (!new PermissionService().CanManagerProject(CurrentCustomer, project))
                return NoAuthorityJson();

            if (CurrentCustomer.Organization.OrganizationType != OrganizationType.BuildingCompany)
                return NoAuthorityJson();

            project.FileTime = DateTime.Now;
            projectService.Update(project);

            return Json(new { result = true, errmsg = string.Empty });
        }

        [HttpPost]
        public ActionResult RestoreProject(int projectId)
        {
            var projectService = new ProjectService();

            var project = projectService.Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (!new PermissionService().CanManagerProjectAllowFiled(CurrentCustomer, project))
                return NoAuthorityJson();

            if (CurrentCustomer.Organization.OrganizationType != OrganizationType.BuildingCompany)
                return NoAuthorityJson();

            project.FileTime = null;
            var result = projectService.Update(project);

            return Json(new { result = true, errmsg = string.Empty });
        }


        public ActionResult ResponsibleProjectList(int customerid)
        {
            var customer = customerService.Get(s => s.Id == customerid);
            ViewBag.Customer = customer;

            return View();
        }

        public ActionResult GetResponsibleProjectList(int pageIndex, int pageSize, int customerId, string searchFuzzyInput)
        {
            var pageList = customerRoleMappingService.GetUserResponsibleProjectList(pageIndex, pageSize, customerId, searchFuzzyInput);

            return Json(new { listHtml = this.RenderPartialViewToString("_Projects", pageList.Select(s => PrepareProjectModel(s, ProjectPageListUserType.AdminResponsibleProjectList)).ToList()), total = pageList.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 新建项目
        /// </summary>
        /// <returns></returns>
        [RoleFilter(CustomerType.Admin)]
        public ActionResult Create()
        {
            if (base.CurrentCustomer.Organization.OrganizationType != OrganizationType.BuildingCompany)
                return RedirectToAction("RoleDistributionList");

            //构建ProjectLogicModel
            var prjectLogicModel = new ProjectLogicModel
            {
                BIMProjectId = string.Empty,
                Engineerings = new List<Service.BusinessLogic.Models.EngineeringModel>()
            };

            //获取建设单位信息
            prjectLogicModel.ConstructionCompanyContacterName = base.CurrentCustomer.Organization.ContacterName;
            prjectLogicModel.ConstructionCompanyContacterPhone = base.CurrentCustomer.Organization.ContacterPhone;
            prjectLogicModel.ConstructionCompany = base.CurrentCustomer.Organization.Name;
            prjectLogicModel.ConstructionCompanyId = base.CurrentCustomer.Organization.Id;

            //此处数据不在页面第一次加载显示 后续通过ajax 异步加载 方式提供
            ////建设单位列表数据
            //prjectLogicModel.ConstructionCompanyList = organizationService.GetMany(s => s.OrganizationType == OrganizationType.BuildingCompany && s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            //{
            //    Id = s.Id,
            //    OrganizationType = s.OrganizationType,
            //    ContacterName = s.ContacterName,
            //    ContacterPhone = s.ContacterPhone,
            //    Name = s.Name,
            //    OrganizationAddress = s.OrganizationAddress,
            //    OrganizationNumber = s.OrganizationNumber,
            //    OrganizationTelephone = s.OrganizationTelephone,
            //    ProposerEmail = s.ProposerEmail,
            //    ProposerName = s.ProposerName,
            //    ProposerPhone = s.ProposerPhone,
            //    ZipCode = s.ZipCode,
            //    BusinessLicensePicUri = s.BusinessLicensePicUri
            //}).ToList();
            ////设计单位列表数据
            //prjectLogicModel.DesignCompanyList = organizationService.GetMany(s => s.OrganizationType == OrganizationType.DesignCompany && s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            //{
            //    Id = s.Id,
            //    OrganizationType = s.OrganizationType,
            //    ContacterName = s.ContacterName,
            //    ContacterPhone = s.ContacterPhone,
            //    Name = s.Name,
            //    OrganizationAddress = s.OrganizationAddress,
            //    OrganizationNumber = s.OrganizationNumber,
            //    OrganizationTelephone = s.OrganizationTelephone,
            //    ProposerEmail = s.ProposerEmail,
            //    ProposerName = s.ProposerName,
            //    ProposerPhone = s.ProposerPhone,
            //    ZipCode = s.ZipCode,
            //    BusinessLicensePicUri = s.BusinessLicensePicUri
            //}).ToList();
            ////审核单位列表数据
            //prjectLogicModel.CensorshipList = organizationService.GetMany(s => s.OrganizationType == OrganizationType.Censorship && s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            //{
            //    Id = s.Id,
            //    OrganizationType = s.OrganizationType,
            //    ContacterName = s.ContacterName,
            //    ContacterPhone = s.ContacterPhone,
            //    Name = s.Name,
            //    OrganizationAddress = s.OrganizationAddress,
            //    OrganizationNumber = s.OrganizationNumber,
            //    OrganizationTelephone = s.OrganizationTelephone,
            //    ProposerEmail = s.ProposerEmail,
            //    ProposerName = s.ProposerName,
            //    ProposerPhone = s.ProposerPhone,
            //    ZipCode = s.ZipCode,
            //    BusinessLicensePicUri = s.BusinessLicensePicUri
            //}).ToList();

            return View(prjectLogicModel);
        }

        /// <summary>
        /// 添加或者修改项目
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [RoleFilter(CustomerType.Admin)]
        public ActionResult CreateOrUpdate(ProjectLogicModel model)
        {
            if (base.CurrentCustomer.Organization.OrganizationType != OrganizationType.BuildingCompany)
                return RedirectToAction("RoleDistributionList");
            try
            {
                projectService.Update(model, CurrentCustomer);
            }
            catch (Exception ex)
            {
                //TODO:write ex in database
                return Json(new { success = false, message = "失败" });
            }
            return Json(new { success = true, message = "成功" });
        }

        [RoleFilter(CustomerType.Admin)]
        public ActionResult Edit(int id)
        {
            if (base.CurrentCustomer.Organization.OrganizationType != OrganizationType.BuildingCompany)
                return RedirectToAction("RoleDistributionList");

            var prjectLogicModel = new ProjectLogicModel
            {
                BIMProjectId = string.Empty,
                Engineerings = new List<Service.BusinessLogic.Models.EngineeringModel>()
            };

            //获取建设单位信息
            //prjectLogicModel.ConstructionCompanyContacterName = base.CurrentCustomer.Organization.ContacterName;
            //prjectLogicModel.ConstructionCompanyContacterPhone = base.CurrentCustomer.Organization.ContacterPhone;
            //prjectLogicModel.ConstructionCompany = base.CurrentCustomer.Organization.Name;
            //prjectLogicModel.ConstructionCompanyId = base.CurrentCustomer.Organization.Id;

            ////建设单位列表数据
            //prjectLogicModel.ConstructionCompanyList = organizationService.GetMany(s => s.OrganizationType == OrganizationType.BuildingCompany && s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            //{
            //    Id = s.Id,
            //    OrganizationType = s.OrganizationType,
            //    ContacterName = s.ContacterName,
            //    ContacterPhone = s.ContacterPhone,
            //    Name = s.Name,
            //    OrganizationAddress = s.OrganizationAddress,
            //    OrganizationNumber = s.OrganizationNumber,
            //    OrganizationTelephone = s.OrganizationTelephone,
            //    ProposerEmail = s.ProposerEmail,
            //    ProposerName = s.ProposerName,
            //    ProposerPhone = s.ProposerPhone,
            //    ZipCode = s.ZipCode,
            //    BusinessLicensePicUri = s.BusinessLicensePicUri
            //}).ToList();
            ////设计单位列表数据
            //prjectLogicModel.DesignCompanyList = organizationService.GetMany(s => s.OrganizationType == OrganizationType.DesignCompany && s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            //{
            //    Id = s.Id,
            //    OrganizationType = s.OrganizationType,
            //    ContacterName = s.ContacterName,
            //    ContacterPhone = s.ContacterPhone,
            //    Name = s.Name,
            //    OrganizationAddress = s.OrganizationAddress,
            //    OrganizationNumber = s.OrganizationNumber,
            //    OrganizationTelephone = s.OrganizationTelephone,
            //    ProposerEmail = s.ProposerEmail,
            //    ProposerName = s.ProposerName,
            //    ProposerPhone = s.ProposerPhone,
            //    ZipCode = s.ZipCode,
            //    BusinessLicensePicUri = s.BusinessLicensePicUri
            //}).ToList();
            ////审核单位列表数据
            //prjectLogicModel.CensorshipList = organizationService.GetMany(s => s.OrganizationType == OrganizationType.Censorship && s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            //{
            //    Id = s.Id,
            //    OrganizationType = s.OrganizationType,
            //    ContacterName = s.ContacterName,
            //    ContacterPhone = s.ContacterPhone,
            //    Name = s.Name,
            //    OrganizationAddress = s.OrganizationAddress,
            //    OrganizationNumber = s.OrganizationNumber,
            //    OrganizationTelephone = s.OrganizationTelephone,
            //    ProposerEmail = s.ProposerEmail,
            //    ProposerName = s.ProposerName,
            //    ProposerPhone = s.ProposerPhone,
            //    ZipCode = s.ZipCode,
            //    BusinessLicensePicUri = s.BusinessLicensePicUri
            //}).ToList();

            //获取基本信息
            var prjectInfo = projectService.Get(s => s.Id == id);

            PrepareProjectLogicModel(prjectInfo, prjectLogicModel);

            return View(prjectLogicModel);
        }

        //public ActionResult Edit(ProjectLogicModel model)
        //{
        //    return View();
        //}

        [RoleFilter(CustomerType.Admin)]
        public ActionResult RoleDistributionList()
        {
            ViewBag.CustomerOrganizationType = CurrentCustomer.Organization.OrganizationType;

            return View();
        }

        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetDistributionProject(int pageIndex, int pageSize, int projectCatalog, string searchFuzzyInput, int distributeOption)
        {
            var pageList = organizationProjectMappingService.GetProjectListByDistribution(pageIndex, pageSize, CurrentCustomer.Organization.Id, projectCatalog, CurrentCustomer.Organization.OrganizationType, searchFuzzyInput, distributeOption == 1);

            return Json(new { listHtml = this.RenderPartialViewToString("_Projects", pageList.Select(s => PrepareProjectModel(s)).ToList()), total = pageList.TotalCount }, JsonRequestBehavior.AllowGet);
        }


        private void PrepareProjectLogicModel(Project project, ProjectLogicModel logicModel)
        {
            logicModel.BIMProjectId = project.BIMProjectId;
            logicModel.ProjectId = project.Id;
            logicModel.DeliverNo = project.DeliverNo;
            logicModel.ProjectName = project.ProjectName;
            logicModel.ProjectCatalog = project.ProjectCatalog;
            logicModel.BuildingArea = project.BuildingArea;
            logicModel.BuildingLocation = project.BuildingLocation;
            logicModel.ConstructionCompanyContacterName = project.ConstructionCompanyContacterName;
            logicModel.ConstructionCompanyContacterPhone = project.ConstructionCompanyContacterPhone;
            logicModel.CivilAirDefenseArea = project.CivilAirDefenseArea;

            logicModel.Engineerings = project.Engineerings.Select(s => new Service.BusinessLogic.Models.EngineeringModel
            {
                Name = s.Name,
                Height = s.Height,
                Description = s.Description,
                FloorsAboveGround = s.FloorsAboveGround,
                FloorsUnderGround = s.FloorsUnderGround,
                Id = s.Id,
                AreaAboveGround = s.AreaAboveGround,
                ModelIterationCount = s.ModelIterationCount,
            }).ToList();
            var projectOrganizationMap = organizationProjectMappingService.GetMany(s => s.ProjectId == project.Id).ToList();

            foreach (var item in projectOrganizationMap)
            {
                switch (item.OrganizationType)
                {
                    case OrganizationType.DesignCompany:
                        var designCom = projectOrganizationMap.FirstOrDefault(s => s.OrganizationType == OrganizationType.DesignCompany);
                        if (designCom != null)
                        {
                            logicModel.DesignCompanyId = designCom.OrganizationId;
                            logicModel.DesignCompany = designCom.OrganizationName;
                        }

                        break;
                    case OrganizationType.BuildingCompany:
                        var buildCom = projectOrganizationMap.FirstOrDefault(s => s.OrganizationType == OrganizationType.BuildingCompany);
                        if (buildCom != null)
                        {
                            logicModel.ConstructionCompanyId = buildCom.OrganizationId;
                            logicModel.ConstructionCompany = buildCom.OrganizationName;
                        }

                        break;
                    case OrganizationType.Censorship:
                        var consorshipCom = projectOrganizationMap.FirstOrDefault(s => s.OrganizationType == OrganizationType.Censorship);
                        if (consorshipCom != null)
                        {
                            logicModel.CensorshipId = consorshipCom.OrganizationId;
                            logicModel.Censorship = consorshipCom.OrganizationName;
                        }

                        break;
                    default:
                        break;
                }
            }

        }

        private ProjectModel PrepareProjectModel(Project project, ProjectPageListUserType pageLisgType = ProjectPageListUserType.AdminDistribute)
        {
            var projectModel = new ProjectModel
            {
                BIMProjectId = project.BIMProjectId,
                ProjectId = project.Id,
                DeliverNo = project.DeliverNo,
                ProjectName = project.ProjectName,
                ProjectCatalog = project.ProjectCatalog,
                PageLisgType = pageLisgType
            };           
            var projectOrganizationMap = organizationProjectMappingService.GetMany(s => s.ProjectId == project.Id).ToList();
            foreach (var item in projectOrganizationMap)
            {
                switch (item.OrganizationType)
                {
                    case OrganizationType.DesignCompany:
                        var designCom = projectOrganizationMap.FirstOrDefault(s => s.OrganizationType == OrganizationType.DesignCompany);
                        if (designCom != null)
                        {
                            projectModel.DesignCompany = designCom.OrganizationName;
                            projectModel.IsDistribute = project.DesignCompanyDistribution;
                            projectModel.NeedOperate = false;
                            //当前project查询归属的engineering
                            if (CurrentCustomer.Organization.OrganizationType == OrganizationType.DesignCompany)
                            {
                                foreach (var projectEngineering in project.Engineerings)
                                {

                                    var lastFile = projectEngineering.EngineeringFiles.LastOrDefault(e => e.FileType == FileType.Model);

                                    if (lastFile != null)
                                    {
                                        var fileVersions = lastFile.FileVersions.LastOrDefault();
                                        if (fileVersions != null)
                                        {
                                            if (fileVersions.Status == FlowCode.Pre_DesignCompany_Uploaded || fileVersions.Status == FlowCode.DesignCompany_Uploaded)
                                            {
                                                projectModel.NeedOperate = true;
                                            }
                                            if (fileVersions.Status == FlowCode.BuildCompany_Submit_DesignCompany)
                                            {
                                                projectModel.NeedOperate = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case OrganizationType.BuildingCompany:
                        var buildCom = projectOrganizationMap.FirstOrDefault(s => s.OrganizationType == OrganizationType.BuildingCompany);
                        if (buildCom != null)
                        {
                            projectModel.ConstructionCompany = buildCom.OrganizationName;
                            projectModel.IsDistribute = project.ConstructionDistribution;

                            projectModel.NeedOperate = false;
                            //当前project查询归属的engineering
                            if (CurrentCustomer.Organization.OrganizationType == OrganizationType.BuildingCompany)
                            {
                                foreach (var projectEngineering in project.Engineerings)
                                {
                                    var lastFile = projectEngineering.EngineeringFiles.LastOrDefault();
                                    if (lastFile != null)
                                    {
                                        var fileVersions = lastFile.FileVersions.LastOrDefault();

                                        if (fileVersions != null)
                                        {
                                            if (fileVersions.Status == FlowCode.Pre_BuildCompany_Sign_DesignCompany ||
                                                fileVersions.Status == FlowCode.BuildCompany_Sign_DesignCompany)
                                            {
                                                projectModel.NeedOperate = true;
                                            }

                                            else if (fileVersions.Status == FlowCode.Pre_DesignCompany_Submit ||
                                                     fileVersions.Status == FlowCode.DesignCompany_Submit_BuildCompany ||
                                                     fileVersions.Status ==
                                                     FlowCode.AuditCompany_ProjectManager_Reject_BuildCompany_Comment)
                                            {
                                                projectModel.NeedOperate = true;
                                            }

                                            else if (fileVersions.Status == FlowCode.BuildCompany_Sign_AuditCompany)
                                            {
                                                projectModel.NeedOperate = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    case OrganizationType.Censorship:
                        var consorshipCom = projectOrganizationMap.FirstOrDefault(s => s.OrganizationType == OrganizationType.Censorship);
                        if (consorshipCom != null)
                        {
                            projectModel.Censorship = consorshipCom.OrganizationName;
                            projectModel.IsDistribute = project.CensorshipDistribution;

                            projectModel.NeedOperate = false;
                            //当前project查询归属的engineering

                            if (CurrentCustomer.Organization.OrganizationType == OrganizationType.Censorship)
                            {
                                foreach (var projectEngineering in project.Engineerings)
                                {
                                    var roles = CurrentCustomer.GetCurrentRoles(0, projectEngineering);

                                    if (roles.Contains(Role.CensorshipEngreeingManager) || roles.Contains(Role.CensorshipManager))
                                    {
                                        var lastFile = projectEngineering.EngineeringFiles.LastOrDefault(e => e.FileType == FileType.Model);
                                        if (lastFile != null)
                                        {
                                            var fileVersions = lastFile.FileVersions.LastOrDefault();
                                            if (fileVersions != null)
                                            {
                                                if (fileVersions.Status == FlowCode.Pre_BuildCompany_Submit_AuditCompany ||
                                                    fileVersions.Status == FlowCode.BuildCompany_Submit_AuditCompany)
                                                {
                                                    projectModel.NeedOperate = true;
                                                }
                                                else if (fileVersions.Status ==
                                                         FlowCode.Pre_AuditCompany_ProfessionReaudit_Submit_Imperfect)
                                                {
                                                    projectModel.NeedOperate = true;
                                                }
                                                else if (fileVersions.Status ==
                                                         FlowCode.Pre_AuditCompany_ProfessionReaudit_Submit_Perfect)
                                                {
                                                    projectModel.NeedOperate = true;
                                                }
                                                else if (fileVersions.Status ==
                                                         FlowCode.AuditCompany_ProfessionReaudit_Submit_ProjectManager)
                                                {
                                                    projectModel.NeedOperate = true;
                                                }
                                                else if (fileVersions.Status ==
                                                         FlowCode.AuditCompany_ProfessionReaudit_Submit_ProjectManager_Comments)
                                                {
                                                    projectModel.NeedOperate = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return projectModel;

        }

        private ProjectModel PrepareProjectModel(ProjectModel projectModel, ProjectPageListUserType pageLisgType = ProjectPageListUserType.AdminDistribute)
        {
            var project = new ProjectService().GetById(projectModel.ProjectId);

            var projectOrganizationMap = organizationProjectMappingService.GetMany(s => s.ProjectId == projectModel.ProjectId && s.OrganizationType == CurrentCustomer.Organization.OrganizationType).ToList();

            foreach (var item in projectOrganizationMap)
            {
                switch (item.OrganizationType)
                {
                    case OrganizationType.DesignCompany:
                        //当前project查询归属的engineering

                        foreach (var projectEngineering in project.Engineerings)
                        {

                            var lastFile = projectEngineering.EngineeringFiles.LastOrDefault(e => e.FileType == FileType.Model);

                            if (lastFile != null)
                            {
                                var fileVersions = lastFile.FileVersions.LastOrDefault();
                                if (fileVersions != null)
                                {


                                    if (fileVersions.Status == FlowCode.Pre_DesignCompany_Uploaded || fileVersions.Status == FlowCode.DesignCompany_Uploaded)
                                    {
                                        projectModel.NeedOperate = true;
                                    }
                                    if (fileVersions.Status == FlowCode.BuildCompany_Submit_DesignCompany)
                                    {
                                        projectModel.NeedOperate = true;
                                    }
                                }
                            }
                        }
                        break;
                    case OrganizationType.BuildingCompany:
                        //当前project查询归属的engineering

                        foreach (var projectEngineering in project.Engineerings)
                        {

                            var lastFile = projectEngineering.EngineeringFiles.LastOrDefault(e => e.FileType == FileType.Model);
                            if (lastFile != null)
                            {
                                var fileVersions = lastFile.FileVersions.LastOrDefault();
                                if (fileVersions != null)
                                {
                                    if (fileVersions.Status == FlowCode.Pre_BuildCompany_Sign_DesignCompany ||
                                        fileVersions.Status == FlowCode.BuildCompany_Sign_DesignCompany)
                                    {
                                        projectModel.NeedOperate = true;
                                    }
                                    else if (fileVersions.Status == FlowCode.Pre_DesignCompany_Submit ||
                                             fileVersions.Status == FlowCode.DesignCompany_Submit_BuildCompany ||
                                             fileVersions.Status ==
                                             FlowCode.AuditCompany_ProjectManager_Reject_BuildCompany_Comment)
                                    {
                                        projectModel.NeedOperate = true;
                                    }
                                    else if (fileVersions.Status == FlowCode.BuildCompany_Sign_AuditCompany)
                                    {
                                        projectModel.NeedOperate = true;
                                    }
                                }
                            }
                        }
                        break;
                    case OrganizationType.Censorship:
                        //当前project查询归属的engineering

                        foreach (var projectEngineering in project.Engineerings)
                        {
                            var roles = CurrentCustomer.GetCurrentRoles(0, projectEngineering);

                            if (roles.Contains(Role.CensorshipEngreeingManager) || roles.Contains(Role.CensorshipManager))
                            {
                                var lastFile = projectEngineering.EngineeringFiles.LastOrDefault(e => e.FileType == FileType.Model);
                                if (lastFile != null)
                                {
                                    var fileVersions = lastFile.FileVersions.LastOrDefault();
                                    if (fileVersions != null)
                                    {
                                        if (fileVersions.Status == FlowCode.Pre_BuildCompany_Submit_AuditCompany ||
                                            fileVersions.Status == FlowCode.BuildCompany_Submit_AuditCompany)
                                        {
                                            projectModel.NeedOperate = true;
                                        }
                                        else if (fileVersions.Status ==
                                                 FlowCode.Pre_AuditCompany_ProfessionReaudit_Submit_Imperfect)
                                        {
                                            projectModel.NeedOperate = true;
                                        }
                                        else if (fileVersions.Status ==
                                                 FlowCode.Pre_AuditCompany_ProfessionReaudit_Submit_Perfect)
                                        {
                                            projectModel.NeedOperate = true;
                                        }
                                        else if (fileVersions.Status ==
                                                 FlowCode.AuditCompany_ProfessionReaudit_Submit_ProjectManager)
                                        {
                                            projectModel.NeedOperate = true;
                                        }
                                        else if (fileVersions.Status ==
                                                 FlowCode.AuditCompany_ProfessionReaudit_Submit_ProjectManager_Comments)
                                        {
                                            projectModel.NeedOperate = true;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return projectModel;
        }

        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetDistributionByProjectId(int projectId)
        {
            return DistribtuonPopup(projectId);
        }

        /// <summary>
        /// 获取弹出框信息
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public ActionResult DistribtuonPopup(int projectId)
        {
            var organizationMapping = organizationProjectMappingService.Get(s => s.ProjectId == projectId && s.OrganizationType == CurrentCustomer.Organization.OrganizationType);
            if (organizationMapping == null)
            {
                return new HttpUnauthorizedResult();
            }

            var organizatinId = CurrentCustomer.Organization.Id;
            var userList = new CustomerService().GetMany(s => s.OrganizationId == organizatinId && s.CustomerStatus == CustomerStatus.Available && s.CustomerType == CustomerType.User).ToList();
            //获取项目经理信息

            var distribution = new DistributionModel
            {
                ProjectId = projectId,
                CompanyCustomerList = userList,
                OrganizationType = CurrentCustomer.Organization.OrganizationType,
                DistributionEngreeingList = new List<DistributionEngreeingModel>()
            };

            var projectRoleMapping = customerRoleMappingService.GetMany(s => s.ProjectId == projectId).ToList();

            switch (CurrentCustomer.Organization.OrganizationType)
            {
                case OrganizationType.DesignCompany:
                    var designCompanyManager = projectRoleMapping.FirstOrDefault(s => s.Role == Role.DesignCompanyManager);
                    if (designCompanyManager != null)
                    {
                        distribution.ProjectManagerId = designCompanyManager.CustomerId;
                        distribution.ProjectManagerName = designCompanyManager.CustomerName;
                    }
                    break;
                case OrganizationType.BuildingCompany:
                    //获取建设公司的id
                    var buildingCompanyManager = projectRoleMapping.FirstOrDefault(s => s.Role == Role.BuildingCompanyManager);
                    if (buildingCompanyManager != null)
                    {
                        distribution.ProjectManagerId = buildingCompanyManager.CustomerId;
                        distribution.ProjectManagerName = buildingCompanyManager.CustomerName;
                    }
                    break;
                case OrganizationType.Censorship:
                    //获取审核公司的id
                    var censorshiCompanyManager = projectRoleMapping.FirstOrDefault(s => s.Role == Role.CensorshipManager && s.EngineeringId == null);
                    if (censorshiCompanyManager != null)
                    {
                        distribution.ProjectManagerId = censorshiCompanyManager.CustomerId;
                        distribution.ProjectManagerName = censorshiCompanyManager.CustomerName;
                    }

                    AddAllEngineering(distribution);

                    var engreeingList = projectService.Get(s => s.Id == projectId).Engineerings.ToList();

                    //读取模型上的信息
                    foreach (var engreeing in engreeingList)
                    {
                        var distributionEngreeingModel = new DistributionEngreeingModel
                        {
                            EngreeingId = engreeing.Id,
                            EngreeingName = engreeing.Name,
                            ProfessionDistributionList = new List<ProfessionalDistributionModel>()
                        };

                        //获取模型的项目经理信息
                        var engreeingManager = projectRoleMapping.FirstOrDefault(s => s.Role == Role.CensorshipEngreeingManager && s.EngineeringId == engreeing.Id);
                        if (engreeingManager != null)
                        {
                            distributionEngreeingModel.ProjectManagerId = engreeingManager.CustomerId;
                            distributionEngreeingModel.ProjectManagerName = engreeingManager.CustomerName;
                        }
                        distribution.DistributionEngreeingList.Add(distributionEngreeingModel);
                        //获取 专业列表
                        foreach (var item in DictionaryService.CommentProfessionDictionary)
                        {
                            var professionModel = new ProfessionalDistributionModel()
                            {
                                ProfessionId = item.Id,
                                ProfessionName = item.DisplayName,
                            };
                            var auditor = projectRoleMapping.FirstOrDefault(s => s.EngineeringId == engreeing.Id && s.ProfessionId == item.Id && s.Role == Role.Checker);
                            var reauditor = projectRoleMapping.FirstOrDefault(s => s.EngineeringId == engreeing.Id && s.ProfessionId == item.Id && s.Role == Role.Reviewer);
                            if (auditor != null)
                            {
                                professionModel.ProfessionAuidtId = auditor.CustomerId;
                                professionModel.ProfessionAuidtName = auditor.CustomerName;
                            }

                            if (reauditor != null)
                            {
                                professionModel.ProfessionReAuidtId = reauditor.CustomerId;
                                professionModel.ProfessionReAuidtName = reauditor.CustomerName;
                            }
                            distributionEngreeingModel.ProfessionDistributionList.Add(professionModel);
                        }
                    }
                    break;

                default:
                    return Json(new { succes = false, message = "" }, JsonRequestBehavior.AllowGet);
            }

            var content = this.RenderPartialViewToString("_DistributionPopup", distribution);

            return Json(new { succes = true, message = content, }, JsonRequestBehavior.AllowGet);
        }

        protected void AddAllEngineering(DistributionModel distribution)
        {
            distribution.DistributionEngreeingList.Add(new DistributionEngreeingModel
            {
                EngreeingId = 0,
                EngreeingName = "所有",
                ProfessionDistributionList = DictionaryService.CommentProfessionDictionary.Where(e => e.IsDeleted == false).Select(e =>
                {
                    return new ProfessionalDistributionModel()
                    {
                        ProfessionId = e.Id,
                        ProfessionName = e.DisplayName,
                    };
                }).ToList(),
            });
        }

        /// <summary>
        /// 给建设公司，设计公司、审查单位 的项目分配用户角色
        /// </summary>
        /// <param name="distributionModel"></param>
        /// <returns></returns>
        [RoleFilter(CustomerType.Admin)]
        public ActionResult DistribteRoles(DistributionModel distributionModel)
        {
            if (distributionModel == null)
                throw new ArgumentNullException("DistributionModel");
            var result = false;
            try
            {
                result = projectService.DistributeProjectRoles(distributionModel);
            }
            catch (Exception)
            {
                //TODO:wtite log
            }
            return Json(new { success = result, message = result ? "更新成功" : "更新失败" }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 企业管理员查看角色所有的项目
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        [RoleFilter(CustomerType.Admin)]
        public ActionResult AdminOverView(int projectId)
        {
            if (CurrentCustomer.CustomerType == CustomerType.Admin)
            {
                if (!new PermissionService().CanAdminVisitProject(CurrentCustomer, projectId))
                    return RedirectToAction("RoleDistributionList");
            }
            else if (CurrentCustomer.CustomerType == CustomerType.User)
            {
                if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                    return RedirectToAction("ProjectIndex");
            }
            else
            {
                return new HttpUnauthorizedResult();
            }
            ProjectDetailModel model = PrepareProjectDetailModel(projectId);

            return View(model);
        }

        [HttpPost]
        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetCompanyList(string keyword, int type)
        {
            var organizationType = (OrganizationType)type;
            var data = organizationService.GetMany(s => s.OrganizationType == organizationType && s.Name.Contains(keyword) &&
                                                        s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
                                                        {
                                                            Id = s.Id,
                                                            OrganizationType = s.OrganizationType,
                                                            ContacterName = s.ContacterName,
                                                            ContacterPhone = s.ContacterPhone,
                                                            Name = s.Name,
                                                            OrganizationAddress = s.OrganizationAddress,
                                                            OrganizationNumber = s.OrganizationNumber,
                                                            OrganizationTelephone = s.OrganizationTelephone,
                                                            ProposerEmail = s.ProposerEmail,
                                                            ProposerName = s.ProposerName,
                                                            ProposerPhone = s.ProposerPhone,
                                                            ZipCode = s.ZipCode,
                                                            BusinessLicensePicUri = s.BusinessLicensePicUri
                                                        }).ToList();
            return Json(data);
        }

        [HttpPost]
        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetFullCompanyList(/*string keyword,*/ int type)
        {
            var organizationType = (OrganizationType)type;
            var data = organizationService.GetMany(s => s.OrganizationType == organizationType /*&& s.Name.Contains(keyword)*/ &&
                                                        s.OrganizationStatus == OrganizationStatus.Available).ToList().Select(s => new Organization
            {
                Id = s.Id,
                OrganizationType = s.OrganizationType,
                ContacterName = s.ContacterName,
                ContacterPhone = s.ContacterPhone,
                Name = s.Name,
                OrganizationAddress = s.OrganizationAddress,
                OrganizationNumber = s.OrganizationNumber,
                OrganizationTelephone = s.OrganizationTelephone,
                ProposerEmail = s.ProposerEmail,
                ProposerName = s.ProposerName,
                ProposerPhone = s.ProposerPhone,
                ZipCode = s.ZipCode,
                BusinessLicensePicUri = s.BusinessLicensePicUri
            }).ToList();
            return Json(data);
        }

        [HttpPost]
        public ActionResult CheckProcess(int projectId)
        {
            var project = new ProjectService().GetById(projectId);

            var dataList = new List<ProjectProcessModel>();

            //得到各个子项
            var engineerings = project.Engineerings;
            foreach (var engineering in engineerings)
            {
                var data = new ProjectProcessModel();
                data.Name = engineering.Name;
                if (engineering.EngineeringFiles.Count > 0)
                {
                    var lastFile = engineering.EngineeringFiles.Where(z => z.FileType == FileType.Model).
                        OrderByDescending(z => z.UpLoadTime).
                        FirstOrDefault();
                    var status = lastFile.Status.GetDescription();
                    data.Description = status;
                }
                dataList.Add(data);
            }

            return Json(new { html = this.RenderPartialViewToString("_ProjectProcess", dataList) }, JsonRequestBehavior.AllowGet);
            //Html = this.RenderPartialViewToString("_Projects", pageList)
        }

        #endregion

    }
}