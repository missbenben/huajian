using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using TS.Core;
using TS.Core.Domain.EngineeringFiles;
using TS.Data;
using TS.Service.BusinessLogic.BIMModelOperation;
using TS.Service.DataAccess.Dictionaries;
using TS.Service.DataAccess.Permissions;
using TS.Service.DataAccess.EngineeringFiles;
using TS.Service.DataAccess.Projects;
using TS.Web.Models.EngineeringFiles;
using TS.Web.Models.Projects;
using TS.Core.Domain.Projects;
using TS.Web.Helper;
using TS.Data.Extensions;
using static TS.Web.Models.Comments.CommentListModel;
using TS.Core.Domain.Customers;
using TS.Core.Domain.Organizations;
using TS.Service.Facade;
using TS.Core.Domain.Comments;
using TS.Service.DataAccess.Comments;
using static TS.Web.Models.EngineeringFiles.ModelEvaluateModel;
using TS.Service.DataAccess.Customers;
using TS.Web.Filters;
using TS.Service;

namespace TS.Web.Controllers
{
    public class EngineeringFileController : BaseController
    {

        #region Utilites

        #region Project Content Page Data

        protected Expression<Func<EngineeringFile, bool>> PrepareDrawingSeriesModelPageLamda(int searchEngineerId = 0, int searchProfessionId = 0, string selectedState = "")
        {
            var lamda = EngineeringFileService.ExpressionTrue;

            lamda = lamda.And(e => e.FileType == FileType.Drawing);

            if (searchEngineerId != 0)
                lamda = lamda.And(e => e.EngineeringId == searchEngineerId);
            if (searchProfessionId != 0)
                lamda = lamda.And(e => e.DrawingProfessionId == searchProfessionId);

            //TODO 二期添加过滤 selectedState

            return lamda;
        }

        protected DrawingSeriesListModel PrepareDrawingSeriesListModel(PagedList<EngineeringFile> drawings, int projectId, int engineeringId,int profeesionId = 0,string selectedStateId = "")
        {
            DrawingSeriesListModel model = new DrawingSeriesListModel();

            var project = new ProjectService().Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var engineering = new EngineeringService().Get(e => e.Id == engineeringId);
            if (engineering == null)
                throw new ArgumentNullException(nameof(engineering));

            model.Roles = CurrentCustomer.GetCurrentRoles(profeesionId, engineering);
            model.ProjectIsFiled = project.IsProjectFiled();

            foreach (var drawing in drawings)
            {
                model.DrawingSeriesList.Add(new DrawingSeriesListModel.DrawingSeriesModel()
                {
                    Description = drawing.FileVersions.Where(e => e.FileType == FileType.Drawing).OrderByDescending(e => e.UpLoadeTime).FirstOrDefault()?.UploadDescription,
                    DrawingVersion = drawing.FileVersions.Where(e => e.FileType == FileType.Drawing).OrderByDescending(e => e.UpLoadeTime).FirstOrDefault()?.VersionNo,
                    FileSize = drawing.FileVersions.Where(e => e.FileType == FileType.Drawing).OrderByDescending(e => e.UpLoadeTime).FirstOrDefault()?.FileSize,
                    DrawingCategoryId = drawing.DrawingCatalogId.GetValueOrDefault(),
                    DrawingCategory = DictionaryService.DrawingCatalogDictionary.FirstOrDefault(e => e.Id == drawing.DrawingCatalogId)?.DisplayName,
                    DrawingProfessionId = drawing.DrawingProfessionId.GetValueOrDefault(),
                    DrawingProfession = DictionaryService.DrawingProfessionDictionary.FirstOrDefault(e => e.Id == drawing.DrawingProfessionId)?.DisplayName,
                    DrawingName = drawing.FileName,
                    DrawingSeriesId = drawing.Id,
                    DrawingStatus = drawing.Status,
                    PicUri = drawing.FileVersions.OrderByDescending(i => i.UpLoadeTime).FirstOrDefault()?.BIMFileId,
                    UpdateTime = drawing.UpLoadTime.ToString("yyyy-MM-dd HH:mm:ss"),
                });
            }

            //TODO 二期添加关于图纸审核的流程
            model.AvaliableStatus.Add(new SelectListItem() { Text = "已上传", Value = "", Selected = true });

            return model;
        }

        protected Expression<Func<FileVersion, bool>> PrepareModelPageLamda(int searchEngineerId, string selectedState = "")
        {
            var lamda = FileVersionService.ExpressionTrue;

            var engineering = new EngineeringService().Get(e => e.Id == searchEngineerId);
            if (engineering == null)
                throw new ArgumentNullException("engineering");

            var model = engineering.EngineeringFiles.FirstOrDefault(e => e.FileType == FileType.Model);
            if (model == null)
                return lamda = lamda.And(e => false);

            if (searchEngineerId != 0)
                lamda = lamda.And(e => e.EngineeringFileId == model.Id);

            if (!string.IsNullOrWhiteSpace(selectedState))
            {
                var states = WorkFlow.GetStatusListByDescription(selectedState);
                lamda = lamda.And(e => states.Contains(e.Status));
            }

            lamda = lamda.And(e => e.FileType == FileType.Model);

            return lamda;
        }

        protected PagedList<FileVersion> GetAllModelVersion(int pageIndex, int pageSize, Expression<Func<FileVersion, bool>> lamda, int engineeringId)
        {
            var model = new EngineeringFileService().Get(e => e.EngineeringId == engineeringId && e.FileType == FileType.Model);
            if (model == null)
                return new PagedList<FileVersion>();

            var customerRoles = CurrentCustomer.GetCurrentRoles(0, model.Engineering);

            var query = new FileVersionService().GetListByRoleAndEngineeingId(customerRoles.OrderBy(e => (int)e).FirstOrDefault(), model.Id);
            query = query.Where(lamda).OrderByDescending(e => e.UpLoadeTime);

            return new PagedList<FileVersion>(query, pageIndex, pageSize);
        }

        protected ModelListModel PrepareModelListModel(PagedList<FileVersion> models, int projectId, int engineeringId, string selectedState = "")
        {
            ModelListModel model = new ModelListModel();

            var project = new ProjectService().Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            model.ProjectIsFiled = project.IsProjectFiled();
            model.ProjectId = project.Id;
            model.Roles = CurrentCustomer.GetCurrentRoles(projectId);

            //检查是否能上传模型
            var engineeringFile =
                new EngineeringFileService().Get(
                    ef => ef.EngineeringId == engineeringId && ef.FileType == FileType.Model);
            if (engineeringFile == null
                || engineeringFile.Status == FlowCode.Pre_DesignCompany_AwaitUpload
                || engineeringFile.Status == FlowCode.Pre_DesignCompany_DeletedCurrent
                || engineeringFile.Status == FlowCode.Pre_BuildCompany_Reject_DesignCompany
                || engineeringFile.Status == FlowCode.Pre_AuditCompany_Reject_DesignCompany
                || engineeringFile.Status == FlowCode.DesignCompany_DeletedCurrent
                || engineeringFile.Status == FlowCode.BuildCompany_Reject_DesignCompany
                || engineeringFile.Status == FlowCode.AuditCompany_ProjectManager_Confrim_CurrentProcess_End)
            {
                model.CanUploadModel = true;
            }

            foreach (var node in models)
            {
                model.ModelList.Add(new ModelListModel.ModelModel()
                {
                    UploadDescription = node.UploadDescription,
                    RejectDescription = node.RejectDescription,
                    ModelVersionId = node.Id,
                    ModelStatus = node.Status,
                    ModelVersionNo = node.VersionNo,
                    UpdateTime = node.UpdateStateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                });
            }

            model.SelectedStatus = selectedState;
            model.AvaliableStatus.Add(new SelectListItem() { Text = "全部", Value = "", Selected = string.IsNullOrWhiteSpace(selectedState) });

            WorkFlow.GetAllModelStatus().ForEach(e =>
            {
                model.AvaliableStatus.Add(new SelectListItem()
                {
                    Text = e,
                    Value = e,
                    Selected = e == selectedState,
                });
            });

            return model;
        }

        protected ModelReviewedListModel PrepareModelReviewedListModel(PagedList<FileVersion> models, int projectId)
        {
            ModelReviewedListModel model = new ModelReviewedListModel();

            if (models.Count > 0)
            {
                var lastModelVersion = models.First().EngineeringFile.FileVersions.OrderByDescending(e => e.UpLoadeTime).First();
                var roles = CurrentCustomer.GetCurrentRoles(0, lastModelVersion.EngineeringFile.Engineering);

                foreach (var node in models)
                {
                    var query = new CommentService().GetCommentByRoleAndEngineeringFileId(roles.OrderBy(e => (int)e).FirstOrDefault(), node.Id);

                    model.ModelReviewedList.Add(new ModelReviewedListModel.ModelReviewedModel()
                    {
                        UploadDescription = node.UploadDescription,
                        ModelVersionId = node.Id,
                        ModelVersionNo = node.VersionNo,
                        UpdateTime = node.UpdateStateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        CommnetCount = query.Count(),
                    });
                }
            }
            model.ProjectId = projectId;
            model.OrganizationType = CurrentCustomer.Organization.OrganizationType;

            return model;
        }

        #endregion

        #region Drawing List Page Data

        protected DrawingListModel PrepareDrawingListModel(PagedList<FileVersion> list, int projectId)
        {
            DrawingListModel model = new DrawingListModel();

            var project = new ProjectService().Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            model.ProjectIsFiled = project.IsProjectFiled();
            model.Roles = CurrentCustomer.GetCurrentRoles(projectId);

            foreach (var node in list)
            {
                model.Drawings.Add(new DrawingListModel.DrawingModel()
                {
                    Description = node.UploadDescription,
                    DrawingCatalog = DictionaryService.DrawingCatalogDictionary.Find(e => e.Id == node.DrawingCatalog).DisplayName,
                    DrawingId = node.Id,
                    DrawingName = node.FileName,
                    DrawingStatus = node.Status,
                    FileSize = node.FileSize,
                    DrawingVersion = node.VersionNo,
                    UpdateTime = node.UpdateStateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Uri = node.BIMFileId,
                });
            }

            return model;
        }

        protected List<ProfessionStateModel> PrepareProfessionStateModel(List<ModelProfession> list)
        {
            List<ProfessionStateModel> result = new List<ProfessionStateModel>();
            foreach (var modelProfession in list)
            {
                var model = new ProfessionStateModel();

                var profession = DictionaryService.CommentProfessionDictionary.Find(e => e.Id == modelProfession.ProfessionId);

                model.ProfessionName = profession.DisplayName;
                model.IconClass = string.IsNullOrWhiteSpace(profession.ExtraValue1) ? DictionaryService.defaultProfessionIconClassName : profession.ExtraValue1;
                model.Status = modelProfession.Status;
                model.Grade = modelProfession.Grade.HasValue ? modelProfession.Grade.Value.ToString() : "";

                result.Add(model);
            }

            return result;
        }

        #endregion

        #region Model Profession Operate Query

        protected ModelProfessionOperateModel PrepareOperateModel(ModelProfession modelProfession, List<Role> roles)
        {
            if (!roles.Contains(Role.Checker) && !roles.Contains(Role.Reviewer))
                return null;

            if (modelProfession.EngineerFile.Engineering.Project.FileTime.HasValue)
                return null;

            ModelProfessionOperateModel model = new ModelProfessionOperateModel();

            model.ModelProfessionId = modelProfession.Id;
            model.Status = modelProfession.Status;
            model.Roles = roles;

            if (roles.Contains(Role.Checker) && (modelProfession.Status == FlowCode.Pre_AuditCompany_ProjectManager_ConformComplate || modelProfession.Status == FlowCode.AuditCompany_ProfessionReaudit_Reject_ProfessionAudit || modelProfession.Status == FlowCode.AuditCompany_ProjectManager_Sign_BuildCompany))
            {
                model.CanCheckerSubmit = !new CommentService().HasAuditorNoAuditComment(modelProfession.ProfessionId, modelProfession.EngineeringFileId);
            }
            if (roles.Contains(Role.Reviewer) && modelProfession.Status == FlowCode.AuditCompany_ProfessionAudit_Submit)
            {
                model.CanReviewerSubmit = new CommentService().HasReaudorCanSubmitToProjectManagerComment(modelProfession.ProfessionId, modelProfession.EngineeringFileId);
                model.CanReviewerReturn = !new CommentService().ReaudorHasNoOperationToProfessionAudit(modelProfession.ProfessionId, modelProfession.EngineeringFileId);
            }
            if (roles.Contains(Role.Reviewer) && modelProfession.Status == FlowCode.DesignCompany_Submit_AuditCompany_ProfessionReaudit)
            {
                model.CanReviewerSubmit = new CommentService().HasReaudorCanSubmitToProjectManagerComment(modelProfession.ProfessionId, modelProfession.EngineeringFileId);
            }

            return model;
        }

        #endregion

        #region Model Evaluate

        protected ModelEvaluateModel PrepareModelEvaluateModel(Project project)
        {
            var model = new ModelEvaluateModel()
            {
                ProjectName = project.ProjectName,
                AvailableEngineering = project.Engineerings.Select(e =>
                 {
                     return new SelectListItem()
                     {
                         Text = e.Name,
                         Value = e.Id.ToString(),
                     };
                 }).ToList(),
            };

            model.AvailableEngineering.First().Selected = true;

            return model;
        }

        protected Expression<Func<Comment, bool>> PrepareEchartsDataLamda(Engineering engineering)
        {
            var lamda = CommentService.ExpressionTrue;

            var modelId = engineering.EngineeringFiles.FirstOrDefault(e => e.FileType == FileType.Model)?.Id;
            if (!modelId.HasValue)
                return lamda = lamda.And(e => false);

            lamda = lamda.And(e => e.EngineeringFileId == modelId);

            var avalibelStates = WorkFlow.AvailableCommentStatusList();
            lamda = lamda.And(e => avalibelStates.Contains(e.Status));

            return lamda;
        }

        protected List<EChartsCommentLevel> PrepareEchartData(List<Comment> comments)
        {
            var result = new List<EChartsCommentLevel>();

            var commentLevels = DictionaryService.CommentLevelDictionary;
            var commentTypes = DictionaryService.DesignCommentTypeDictionary;

            var echartsComments = comments.Join(commentTypes, comment => comment.CommentTypeId, commentType => commentType.Id, (comment, commentType) => new { comment.Id, comment.ProfessionId, CommentTypeId = commentType.Id, CommentLevel = commentType.ExtraValue1 });

            foreach (var level in commentLevels.Select(e => e.DisplayName))
            {
                var echartsCommentLevel = new EChartsCommentLevel()
                {
                    CommentType = string.Format("{0}类意见", level),
                };

                var list = echartsComments.Where(e => e.CommentLevel == level);
                foreach (var type in DictionaryService.CommentProfessionDictionary)
                {
                    echartsCommentLevel.Comments.Add(new EchartsProfession()
                    {
                        Profession = type.DisplayName,
                        Amount = list.Where(e => e.ProfessionId == type.Id).Count(),
                    });
                }
                result.Add(echartsCommentLevel);
            }

            return result;
        }

        protected List<SelectListItem> PrepareScoreModelVersionModel(Engineering engineering)
        {
            var modelId = engineering.EngineeringFiles.FirstOrDefault(e => e.FileType == FileType.Model)?.Id;
            if (!modelId.HasValue)
                return new List<SelectListItem>();

            return new FileVersionService().GetListByRoleAndEngineeingId(CurrentCustomer.GetCurrentRoles(0, engineering).OrderBy(e => (int)e).FirstOrDefault(),modelId.Value).OrderByDescending(e => e.UpLoadeTime).ToList().Select(e =>
            {
                return new SelectListItem()
                {
                    Text = string.Format("{0}-{1}", e.FileName, e.VersionNo),
                    Value = e.Id.ToString(),
                };
            }).ToList();
        }

        protected List<ProfessionEvaluateModel> PrepareProfessionEvaluateModels(List<ModelProfession> modelProfessions,out int canPass)
        {
            var result = new List<ProfessionEvaluateModel>();

            if (modelProfessions.Count == 0)
            {
                canPass = 0;
                return result;
            }
                               
            var engineeringId = modelProfessions.First().EngineerFile.EngineeringId;

            var customerRoles = new CustomerRoleMappingService().GetMany(e => engineeringId == e.EngineeringId && e.ProfessionId != 0 && e.FinishTime == null);

            canPass = 1;

            foreach(var modelProfession in modelProfessions)
            {
                var res = "未知";
                var pass = DictionaryService.PassScoreDictionary.FirstOrDefault(i => i.ExtraValue1 == modelProfession.ProfessionId.ToString())?.ExtraValue2;
                if (!string.IsNullOrWhiteSpace(pass) && modelProfession.Grade.HasValue)
                {
                    if (Convert.ToDouble(pass) > modelProfession.Grade)
                    {
                        res = "不合格";
                        canPass = 0;
                    }
                    else
                    {
                        res = "合格";
                    }
                }
                else
                {
                    canPass = 1;
                }


                result.Add(new ProfessionEvaluateModel()
                {
                    Result = res,
                    ProfessionName = DictionaryService.CommentProfessionDictionary.FirstOrDefault(i => i.Id == modelProfession.ProfessionId)?.DisplayName,
                    CheckerName = customerRoles.FirstOrDefault(i => i.ProfessionId == modelProfession.ProfessionId && i.Role == Role.Checker)?.CustomerName,
                    ReviewerName = customerRoles.FirstOrDefault(i => i.ProfessionId == modelProfession.ProfessionId && i.Role == Role.Reviewer)?.CustomerName,
                    Grade = modelProfession.Grade.HasValue ? Math.Round(modelProfession.Grade.Value, 2).ToString() : "",
                });
            }

            return result;
        }

        #endregion

        #endregion

        #region Method

        #region Project Content Page Data

        public ActionResult DrawingSeriesPageData(int pageIndex, int pageSize, int projectId, int searchEngineerId, int searchProfessionId = 0, string selectedState = "")
        {
            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                return NoAuthorityJson();

            var lamda = PrepareDrawingSeriesModelPageLamda(searchEngineerId, searchProfessionId, selectedState);

            var drawings = new EngineeringFileService().GetPageDataDes(pageIndex, pageSize, lamda, e => e.UpLoadTime);

            var drawingSeriesListModel = PrepareDrawingSeriesListModel(drawings, projectId, searchEngineerId, searchProfessionId, selectedState);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_DrawingSeriesList", drawingSeriesListModel), total = drawings.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModelPageData(int pageIndex, int pageSize, int projectId, int searchEngineerId, string selectedState = "")
        {
            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                return NoAuthorityJson();

            var lamda = PrepareModelPageLamda(searchEngineerId, selectedState);

            var models = GetAllModelVersion(pageIndex, pageSize, lamda, searchEngineerId);

            var modelListModel = PrepareModelListModel(models, projectId, searchEngineerId, selectedState);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_ModelList", modelListModel), total = models.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModelReviewedPageData(int pageIndex, int pageSize, int projectId, int searchEngineerId)
        {
            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                return NoAuthorityJson();

            var lamda = PrepareModelPageLamda(searchEngineerId);
            var models = GetAllModelVersion(pageIndex, pageSize, lamda, searchEngineerId);

            var modelReviewedListModel = PrepareModelReviewedListModel(models, projectId);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_ModelReviewedList", modelReviewedListModel), total = models.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Drawing List Page Data

        [RoleFilter(CustomerType.User)]
        public ActionResult DrawingList(int drawingSeriesId)
        {
            var drawingSeries = new EngineeringFileService().Get(e => e.Id == drawingSeriesId);
            if (drawingSeries == null)
                throw new ArgumentNullException(nameof(drawingSeries));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, drawingSeries.Engineering.ProjectId))
                return new HttpUnauthorizedResult();

            return View(new DrawingListModel { DrawingSeriesId = drawingSeriesId, ProjectName = drawingSeries.Engineering.Project.ProjectName });

        }

        public ActionResult DrawingListPageData(int pageIndex, int pageSize, int drawingSeriesId)
        {
            var drawingSeries = new EngineeringFileService().Get(e => e.Id == drawingSeriesId);
            if (drawingSeries == null)
                throw new ArgumentNullException(nameof(drawingSeries));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, drawingSeries.Engineering.ProjectId))
                return NoAuthorityJson();

            var drawings = new FileVersionService().GetPagedData(pageIndex, pageSize, e => e.EngineeringFileId == drawingSeriesId && e.FileType == FileType.Drawing, e => e.UpLoadeTime);

            var drawingListModel = PrepareDrawingListModel(drawings, drawingSeries.Engineering.ProjectId);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_Drawing", drawingListModel), total = drawings.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Model Operation

        //设计公司项目经理操作模型
        [HttpPost]
        public ActionResult DesignCompanyMangerOperateModel(int modelVersionId, DesignModelOperationType type)
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanManagerProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.Project))
                return NoAuthorityJson();

            CurrentCustomer.CurrentRole = Role.DesignCompanyManager;

            var designCompanyProjectManager = new DesignCompanyProjectManager(CurrentCustomer, modelVersion.EngineeringFile, modelVersion);

            OperationResultCode result;

            if (type == DesignModelOperationType.Submit)
            {
                result = designCompanyProjectManager.DesignCompanyUploadedSubmit();
            }
            else
            {
                result = designCompanyProjectManager.DeleteCurrentModel();
            }

            if (result.Succeed)
                new ProjectService().UpdateProjectNeedOperateFlag(modelVersion, Role.DesignCompanyManager);

            return Json(new { result = false, errmsg = result.MessageValue });
        }

        //建筑公司项目经理操作模型
        public ActionResult BuildingCompanyManagerOperateModel(int modelVersionId, BuildingModelOperationType type, string rejectDes = "")
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanManagerProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.Project))
                return NoAuthorityJson();

            CurrentCustomer.CurrentRole = Role.BuildingCompanyManager;
            var buildingCompanyProjectManager = new BuildingCompanyProjectManager(CurrentCustomer, modelVersion.EngineeringFile, modelVersion);

            var result = false;

            if (type == BuildingModelOperationType.Transpond)
            {
                result = buildingCompanyProjectManager.Submit();
            }
            else if (type == BuildingModelOperationType.Sign)
            {
                result = buildingCompanyProjectManager.Sign();
            }
            else if (type == BuildingModelOperationType.SendBack)
            {
                result = buildingCompanyProjectManager.RejectFromDesignCompany(rejectDes);
            }

            if (result)
                new ProjectService().UpdateProjectNeedOperateFlag(modelVersion, Role.BuildingCompanyManager);

            return Json(new { result = result, errmsg = "" });
        }

        //审查机构项目经理操作模型
        [HttpPost]
        public ActionResult CensorshipManagerOperateModel(int modelVersionId, CensorshipModelOperationType type)
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanManagerProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.Project))
                return NoAuthorityJson();

            CurrentCustomer.CurrentRole = Role.CensorshipManager;
            var auditCompanyProjectManager = new AuditCompanyProjectManager(CurrentCustomer, modelVersion.EngineeringFile, modelVersion);

            var result = false;
            if (type == CensorshipModelOperationType.Sign)
            {
                result = auditCompanyProjectManager.Sign();
            }
            else if (type == CensorshipModelOperationType.AcceptancePass)
            {
                result = auditCompanyProjectManager.TranferToProfessionalAudit();
            }
            else if (type == CensorshipModelOperationType.AcceptanceBack)
            {
                result = auditCompanyProjectManager.SendToDesignProjectManager();
            }
            else if (type == CensorshipModelOperationType.Send)
            {
                result = auditCompanyProjectManager.SendToBuildingCompany();
            }
            else if (type == CensorshipModelOperationType.CurrrentAuditCompleted)
            {
                result = auditCompanyProjectManager.CloseCurrentVersion();
            }
            else if (type == CensorshipModelOperationType.AduitPass)
            {
                result = auditCompanyProjectManager.CloseAuditStep();
            }

            if (result)
                new ProjectService().UpdateProjectNeedOperateFlag(modelVersion, Role.CensorshipManager);

            return Json(new { result = result, errmsg = "" });
        }

        #endregion

        #region Model Vsersion Profession Query

        public ActionResult GetAllModelProfessionState(int modelVersionId)
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.ProjectId))
                return NoAuthorityJson();

            var modelProfessions = new ModelProfessionService().GetMany(e => e.ModelVersionId == modelVersionId).ToList();

            List<ProfessionStateModel> list = PrepareProfessionStateModel(modelProfessions);

            return Json(new { result = true, list = list }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetModelProfessionState(int modelVersionId, int professionId)
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.ProjectId))
                return NoAuthorityJson();

            var modelProfession = new ModelProfessionService().Get(e => e.ModelVersionId == modelVersionId && e.ProfessionId == professionId);

            var customerRoles = CurrentCustomer.CustomerRoles.Where(e => e.ProjectId == modelVersion.EngineeringFile.Engineering.ProjectId && e.FinishTime == null);

            string roleDes = CurrentCustomer.GetCurrentRoleDes(professionId, modelVersion.EngineeringFile.Engineering);

            ModelProfessionOperateModel operateModel = null;
            if (professionId != 0 && modelProfession != null)
            {
                var roles = CurrentCustomer.GetCurrentRoles(professionId, modelProfession.EngineerFile.Engineering);
                operateModel = PrepareOperateModel(modelProfession, roles);
            }

            return Json(new { result = true, roleDes = roleDes, htmlStr = base.RenderPartialViewToString("_ModelProfessionOperate", operateModel) }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Model Profession Operate

        [HttpPost]
        public ActionResult OperateModelProfession(int modelProfessionId, ModelProfessionOperationType type)
        {
            var modelProfession = new ModelProfessionService().Get(e => e.Id == modelProfessionId);
            if (modelProfession == null)
                throw new ArgumentNullException(nameof(modelProfession));

            var professionalAudit = new ProfessionalAudit(CurrentCustomer, modelProfession.EngineerFile, modelProfession.ModelVersion, modelProfession.ProfessionId);
            var professionalReAudit = new ProfessionalReAudit(CurrentCustomer, modelProfession.EngineerFile, modelProfession.ModelVersion, modelProfession.ProfessionId);

            var result = false;
            if (type == ModelProfessionOperationType.CheckerApplyForReview)
            {
                if (!new PermissionService().CanCheckProject(CurrentCustomer, modelProfession.EngineerFile.Engineering, modelProfession.ProfessionId))
                    return NoAuthorityJson();

                CurrentCustomer.CurrentRole = Role.Checker;
                result = professionalAudit.Submit();
            }
            else if (type == ModelProfessionOperationType.ReviewerPassCheckerApply)
            {
                if (!new PermissionService().CanReviewProject(CurrentCustomer, modelProfession.EngineerFile.Engineering, modelProfession.ProfessionId))
                    return NoAuthorityJson();

                CurrentCustomer.CurrentRole = Role.Reviewer;
                result = professionalReAudit.Submit();
            }
            else if (type == ModelProfessionOperationType.ReviewerSendBackCheckerApply)
            {
                if (!new PermissionService().CanReviewProject(CurrentCustomer, modelProfession.EngineerFile.Engineering, modelProfession.ProfessionId))
                    return NoAuthorityJson();

                CurrentCustomer.CurrentRole = Role.Reviewer;
                result = professionalReAudit.Reject();
            }
            else if (type == ModelProfessionOperationType.ReviewerHandledDesignManagerReject)
            {
                if (!new PermissionService().CanReviewProject(CurrentCustomer, modelProfession.EngineerFile.Engineering, modelProfession.ProfessionId))
                    return NoAuthorityJson();

                CurrentCustomer.CurrentRole = Role.Reviewer;
                result = professionalReAudit.Submit();
            }

            return Json(new { result = result, errmsg = "" });
        }

        #endregion

        #region Model Evaluate

        [RoleFilter(CustomerType.User)]
        public ActionResult ModelEvaluate(int projectId)
        {
            var project = new ProjectService().Get(e => e.Id == projectId);
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, projectId))
                return new HttpUnauthorizedResult();

            if (CurrentCustomer.Organization.OrganizationType != OrganizationType.Censorship)
                return new HttpUnauthorizedResult();

            return View(PrepareModelEvaluateModel(project));
        }

        public ActionResult GetScoreModelVersionAndEchartsData(int engineeringId)
        {
            var engineering = new EngineeringService().Get(e => e.Id == engineeringId);
            if (engineering == null)
                throw new ArgumentNullException(nameof(engineering));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, engineering.ProjectId))
                return NoAuthorityJson();

            if (CurrentCustomer.Organization.OrganizationType != OrganizationType.Censorship)
                return NoAuthorityJson();

            var lamda = PrepareEchartsDataLamda(engineering);
            var comments = new CommentService().GetCommentByRoleAndEngineeringFileId(CurrentCustomer.GetCurrentRoles(0,engineering).OrderBy(e => (int)e).FirstOrDefault(),engineering.EngineeringFiles.FirstOrDefault(e => e.FileType == FileType.Model).FileVersions.OrderByDescending(e => e.UpLoadeTime).FirstOrDefault().Id).Where(lamda).ToList();

            var echartsData = PrepareEchartData(comments);

            var modelVersions = PrepareScoreModelVersionModel(engineering);

            return Json(new { result = true, echartsData = echartsData, modelVersions = modelVersions }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult ScoreProfessionData(int modelVersionId)
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.ProjectId))
                return NoAuthorityJson();

            if (CurrentCustomer.Organization.OrganizationType != OrganizationType.Censorship)
                return NoAuthorityJson();

            int canPass;

            var modelProfessions = new ModelProfessionService().GetMany(e => e.ModelVersionId == modelVersionId).ToList();
            var models = PrepareProfessionEvaluateModels(modelProfessions,out canPass);

            string lastVersionId = modelVersion.EngineeringFile.FileVersions.OrderByDescending(e => e.UpLoadeTime).FirstOrDefault(e => e.FileType == FileType.Model)?.Id.ToString();

            return Json(new { result = true, lastVersionId = lastVersionId, canPass = canPass, listHtml = base.RenderPartialViewToString("_ProfessionEvaluate", models) }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #endregion
    }
}