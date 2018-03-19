using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using TS.Core;
using TS.Core.Domain.Comments;
using TS.Core.Domain.Customers;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Projects;
using TS.Data;
using TS.Data.Extensions;
using TS.Service.BusinessLogic.BIMModelOperation;
using TS.Service.DataAccess.Comments;
using TS.Service.DataAccess.Customers;
using TS.Service.DataAccess.Dictionaries;
using TS.Service.DataAccess.EngineeringFiles;
using TS.Service.DataAccess.Organizetions;
using TS.Service.DataAccess.Permissions;
using TS.Service.Facade;
using TS.Web.Filters;
using TS.Web.Helper;
using TS.Web.Models.Comments;
using TS.Web.Models.Projects;
using static TS.Web.Models.Comments.CommentListModel;

namespace TS.Web.Controllers
{
    public class CommentController : BaseController
    {
        #region Fields

        private readonly CommentService _commentService = new CommentService();

        #endregion

        #region Utilities

        protected CommentListModel PrepareCommentListModel(FileVersion modelVersion)
        {
            CommentListModel result = new CommentListModel();
            var modelFile = modelVersion.EngineeringFile;
            result.ModelId = modelFile.Id;

            result.ProjectName = modelFile.Engineering.Project.ProjectName;
            result.CommentCount = modelFile.Comments.Count();
            result.EngineeringName = modelFile.Engineering.Name;    
            result.ModelStatus = modelVersion.Status;
            result.StatusUpdateTime = modelVersion.UpdateStateTime.ToString("yyyy-MM-dd HH:mm:ss");
            result.OrganizationType = CurrentCustomer.Organization.OrganizationType;

            result.AvaliableProfessions.Add(new SelectListItem() { Text = "全部", Value = "0", Selected = true });
            modelVersion.ModelProfessions.ToList().ForEach(e =>
            {
                result.AvaliableProfessions.Add(new SelectListItem()
                {
                    Text = DictionaryService.CommentProfessionDictionary.Find(n => n.Id == e.ProfessionId).DisplayName,
                    Value = e.ProfessionId.ToString()
                });
            });

            result.AvaliableStatus.Add(new SelectListItem() { Text = "全部", Value = "", Selected = true });
            WorkFlow.GetAllCommentStatus().ForEach(e => result.AvaliableStatus.Add(new SelectListItem()
            {
                Text = e,
                Value = e,
            }));

            result.AvaliableCommentType = EnumHelper.EnumToSelectListItem<CommentType>();

            var lastesModelVersion = modelFile.FileVersions.Where(e => e.FileType == FileType.Model).OrderByDescending(e => e.UpLoadeTime).FirstOrDefault();
            if (lastesModelVersion == null)
                throw new ArgumentException(nameof(lastesModelVersion));

            modelFile.FileVersions.Where(e => e.FileType == FileType.Model).OrderByDescending(e => e.UpLoadeTime).ToList().ForEach(e =>
            {
                if (e.Id == modelVersion.Id)
                    result.SelectedVersionId = e.Id;

                result.AvaliableVersions.Add(new SelectListItem()
                {
                    Text = string.Format("模型版本{0}", e.VersionNo),
                    Value = e.Id.ToString(),
                    Selected = e.Id == modelVersion.Id,
                });
            });

            result.AvaliableVersions.Find(e => e.Value == lastesModelVersion.Id.ToString()).Text = "当前版本";

            return result;
        }

        protected Expression<Func<Comment, bool>> PrepareCommentListLamda(CommentListModel model, EngineeringFile modelFile)
        {
            var lastesModelVersion = modelFile.FileVersions.OrderByDescending(e => e.UpLoadeTime).FirstOrDefault();
            if (lastesModelVersion == null)
                throw new ArgumentException(nameof(lastesModelVersion));

            var lamda = CommentService.ExpressionTrue;

            lamda = lamda.And(e => e.EngineeringFileId == model.ModelId);

            if (model.SelectedProfessionId != 0)
            {
                lamda = lamda.And(e => e.ProfessionId == model.SelectedProfessionId);
            }
            if (!string.IsNullOrWhiteSpace(model.SelectedState))
            {
                var states = WorkFlow.GetStatusListByDescription(model.SelectedState);
                lamda = lamda.And(e => states.Contains(e.Status));
            }
            if (model.SelectedCommentType != CommentType.All)
            {
                if (model.SelectedCommentType == CommentType.Integrality)
                {
                    var types = DictionaryService.IntegralityCommentTypeDictionary.Where(e => e.IsDeleted == false).Select(e => e.Id).ToArray();
                    lamda = lamda.And(e => types.Contains(e.CommentTypeId));
                }
                else
                {
                    var types = DictionaryService.DesignCommentTypeDictionary.Where(e => e.IsDeleted == false).Select(e => e.Id).ToArray();
                    lamda = lamda.And(e => types.Contains(e.CommentTypeId));
                }
            }
            return lamda;
        }

        protected PagedList<Comment> GetVersionComments(int pageIndex, int pageSize, Expression<Func<Comment, bool>> lamda, EngineeringFile model,int fileVersionId, int professionId = 0)
        {
            var customerRoles = CurrentCustomer.CustomerRoles.Where(e => e.ProjectId == model.Engineering.ProjectId && e.FinishTime == null);
            CustomerRoleMapping customerRole;

            customerRole = customerRoles.FirstOrDefault(e => e.Role != Role.Checker && e.Role != Role.Reviewer);
            if (customerRole == null)
            {
                if (professionId == 0)
                {
                    customerRole = customerRoles.FirstOrDefault();
                }
                else
                {
                    customerRole = customerRoles.FirstOrDefault(e => e.EngineeringId == model.EngineeringId && e.ProfessionId == professionId);
                }
            }

            var comments = new CommentService().GetCommentByRoleAndEngineeringFileId(customerRole?.Role ?? Role.CensorshipManager, fileVersionId);

            return new PagedList<Comment>(comments.Where(lamda).OrderByDescending(e => e.CreateTime), pageIndex, pageSize);
        }

        protected List<CommentModel> PrepareCommentModelPageData(PagedList<Comment> comments, EngineeringFile model)
        {
            var list = new List<CommentModel>();

            var customerRoles = new CustomerRoleMappingService().GetMany(e => e.ProjectId == model.Engineering.ProjectId && e.EngineeringId == model.EngineeringId && (e.Role == Role.Checker || e.Role == Role.Reviewer)).ToList();

            comments.ForEach(comment =>
            {
                var commentType = DictionaryService.DesignCommentTypeDictionary.FirstOrDefault(e => e.Id == comment.CommentTypeId);
                if (commentType == null)
                    commentType = DictionaryService.IntegralityCommentTypeDictionary.FirstOrDefault(e => e.Id == comment.CommentTypeId);

                string typeDes;
                if (commentType == null)
                    typeDes = comment.CommentTypeId == 0 ? "专业不完整" : "专业完整";
                else
                    typeDes = commentType.DisplayName;

                var node = new CommentModel()
                {
                    Checker = customerRoles.FirstOrDefault(e => e.ProfessionId == comment.ProfessionId && e.Role == Role.Checker)?.CustomerName,
                    CommentType = typeDes,
                    CommnenId = comment.Id,
                    EngineeringFileCommentId = comment.EngineeringFileCommentId,
                    CommnetStatus = comment.Status,
                    CreateVersion = comment.CreateVersion.VersionNo,
                    Floor = comment.Floor,
                    Operation = comment.Operation,
                    Reviewer = customerRoles.FirstOrDefault(e => e.ProfessionId == comment.ProfessionId && e.Role == Role.Reviewer)?.CustomerName,
                    UpdateTime = comment.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                };
                list.Add(node);
            });

            return list;
        }

        protected CommentDetailModel PrepareCommentDetailModel(Comment comment)
        {
            CommentDetailModel model = new CommentDetailModel();

            var engineering = comment.EngineeringFile.Engineering;
            var organizations = new OrganizationProjectMappingService().GetMany(e => e.ProjectId == engineering.ProjectId && e.FinishTime == null);
            var customerRoles = new CustomerRoleMappingService().GetMany(e => e.ProjectId == engineering.ProjectId && e.FinishTime == null && ((e.Role == Role.CensorshipManager || e.Role == Role.DesignCompanyManager || e.Role == Role.BuildingCompanyManager) || ((e.Role == Role.Checker || e.Role == Role.Reviewer) && e.EngineeringId == engineering.Id && e.ProfessionId == comment.ProfessionId)));

            model.BuildingCompany = organizations.FirstOrDefault(e => e.OrganizationType == OrganizationType.BuildingCompany)?.OrganizationName;
            model.Censorship = organizations.FirstOrDefault(e => e.OrganizationType == OrganizationType.Censorship)?.OrganizationName;
            model.DesignCompany = organizations.FirstOrDefault(e => e.OrganizationType == OrganizationType.DesignCompany)?.OrganizationName;

            model.BuildingCompanyManager = customerRoles.FirstOrDefault(e => e.Role == Role.BuildingCompanyManager)?.CustomerName;
            model.CensorshipManager = customerRoles.FirstOrDefault(e => e.Role == Role.CensorshipManager)?.CustomerName;
            model.DesignCompanyManager = customerRoles.FirstOrDefault(e => e.Role == Role.DesignCompanyManager)?.CustomerName;

            model.Checker = customerRoles.FirstOrDefault(e => e.Role == Role.Checker)?.CustomerName;
            model.Reviewer = customerRoles.FirstOrDefault(e => e.Role == Role.Reviewer)?.CustomerName;

            var commentType = DictionaryService.IntegralityCommentTypeDictionary.FirstOrDefault(e => e.Id == comment.CommentTypeId);
            commentType = commentType ?? DictionaryService.DesignCommentTypeDictionary.FirstOrDefault(e => e.Id == comment.CommentTypeId);

            string typeDes;
            if (commentType == null)
                typeDes = comment.CommentTypeId == 0 ? "专业不完整" : "专业完整";
            else
                typeDes = commentType.DisplayName;

            model.CommentType = typeDes;

            var profession = DictionaryService.CommentProfessionDictionary.FirstOrDefault(e => e.Id == comment.ProfessionId);
            model.Profession = profession?.DisplayName;

            model.ProjectName = engineering.Project.ProjectName;
            model.IsProjectFiled = engineering.Project.IsProjectFiled();
            model.CommentId = comment.Id;
            model.EngineeringName = engineering.Name;
            model.CreateTime = comment.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
            model.CreateVersionId = comment.CreateVersionId.ToString();
            model.Description = comment.Description;
            model.Status = comment.Status;
            model.Floor = comment.Floor;

            model.ReturnDes = comment.CommentRecords.OrderByDescending(e => e.UpdateTime).FirstOrDefault(e => e.Status == FlowCode.DesignCompany_Disagree_AuditCompany_Comment)?.Reason;

            model.Roles = CurrentCustomer.GetCurrentRoles(comment);

            var commentProfession = comment.EngineeringFile.ModelProfessions.OrderByDescending(e => e.ModelVersionId).FirstOrDefault(e => e.ProfessionId == comment.ProfessionId);
            if (commentProfession == null)
                throw new ArgumentNullException(nameof(commentProfession));

            model.ProfessionStatus = commentProfession.Status;
            model.CurrentModelVersionId = commentProfession.ModelVersionId;

            model.commentButton = new CommentDetailModel.CommentButtom()
            {
                CommentId = comment.Id,
                Status = comment.Status,
                ProfessionId = comment.ProfessionId,
                ProfessionStatus = commentProfession.Status,
                Roles = model.Roles,
                CurrentModelVersionId = commentProfession.ModelVersionId,
                ModelStatus = comment.EngineeringFile.Status,
                IsInCommentDetail = true,
            };

            model.Annotations = comment.BIMElements.Where(e => e.IsDelete == false).Select(element =>
            {
                return new CommentDetailModel.AnnotationModel()
                {
                    AnnotationId = element.BIMId,
                    HDBimThumbUrl = element.HDBimThumbUrl,
                    Uri = element.BimThumb,
                    Name = element.Name,
                    type = element.BIMElementType
                };
            }).ToList();

            //上一页，下一页
            var nearbyCommentsQuery = new CommentService().TableNoTracking.Where(e => e.EngineeringFileId == comment.EngineeringFileId);

            model.PreCommentId = nearbyCommentsQuery.Where(e => e.Id < comment.Id).OrderByDescending(e => e.CreateTime).FirstOrDefault()?.Id ?? 0;
            model.NextCommentId = nearbyCommentsQuery.Where(e => e.Id > comment.Id).OrderBy(e => e.CreateTime).FirstOrDefault()?.Id ?? 0;

            return model;
        }

        protected List<CommentRecordModel> PrepareCommentRecordModels(PagedList<CommentRecord> commentRecords)
        {
            return commentRecords.Select(commentRecord =>
            {
                return new CommentRecordModel()
                {
                    ChangeType = commentRecord.ChangeType,
                    Operation = commentRecord.Operation,
                    OperatorName = string.Format("{0}{1}-{2}",
                        commentRecord.Operator.Organization.OrganizationType.GetDescription(),
                        commentRecord.OperatorRole.GetDescription(),
                        commentRecord.Operator.Name),
                    Reason = commentRecord.Reason,
                    Status = commentRecord.Status,
                    UpdateTime = commentRecord.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }).ToList();
        }

        protected string SetBulkOperationErrsmg(string errmsg, int id)
        {
            errmsg += new CommentService().GetById(id).EngineeringFileCommentId + "意见修改失败;";
            return errmsg;
        }

        #endregion

        #region Method

        [RoleFilter(CustomerType.User)]
        public ActionResult CommentList(int modelVersionId)
        {
            var modelVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, modelVersion.EngineeringFile.Engineering.ProjectId))
                return new HttpUnauthorizedResult();

            var result = PrepareCommentListModel(modelVersion);
            return View(result);
        }

        public ActionResult CommentModelPageData(int pageIndex, int pageSize, CommentListModel commentsModel)
        {
            var model = new EngineeringFileService().Get(e => e.Id == commentsModel.ModelId);
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, model.Engineering.ProjectId))
                return NoAuthorityJson();

            var lamda = PrepareCommentListLamda(commentsModel, model);

            PagedList<Comment> comments = GetVersionComments(pageIndex, pageSize, lamda, model, commentsModel.SelectedVersionId, commentsModel.SelectedProfessionId);

            var commentModels = PrepareCommentModelPageData(comments, model);
            var roleDes = CurrentCustomer.GetCurrentRoleDes(commentsModel.SelectedProfessionId, model.Engineering);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_Comment", commentModels), total = comments.TotalCount, roleDes = roleDes, statusDescription = model.FileVersions.FirstOrDefault(e => e.Id == commentsModel.SelectedVersionId)?.Status.GetDescription() }, JsonRequestBehavior.AllowGet);
        }

        [RoleFilter(CustomerType.User)]
        public ActionResult CommentDetail(int commentId)
        {
            var comment = new CommentService().Get(e => e.Id == commentId);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, comment.EngineeringFile.Engineering.ProjectId))
                return new HttpUnauthorizedResult();

            var model = PrepareCommentDetailModel(comment);

            return View(model);
        }

        public ActionResult CommentRecordModelPagaData(int pageIndex, int pageSize, int commentId)
        {
            var comment = new CommentService().Get(e => e.Id == commentId);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, comment.EngineeringFile.Engineering.ProjectId))
                return NoAuthorityJson();

            var roles = CurrentCustomer.GetCurrentRoles(comment);

            var query = new CommentRecordService().GetCommentRecordByRoleAndCommentId(roles.OrderBy(e => (int)e).FirstOrDefault(), commentId).OrderBy(e => e.UpdateTime);
            var records = new PagedList<CommentRecord>(query, pageIndex, pageSize);

            var recordModels = PrepareCommentRecordModels(records);

            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_CommentRecord", recordModels), total = records.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AduitOperateComment(int commentId, AduitCommentOperationType operationType)
        {
            var comment = new CommentService().Get(e => e.Id == commentId);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            if (!new PermissionService().CanCheckProject(CurrentCustomer, comment.EngineeringFile.Engineering, comment.ProfessionId))
                return NoAuthorityJson();

            var professionalAudit = new ProfessionalAudit(CurrentCustomer, comment.EngineeringFile, comment.EngineeringFile.FileVersions.OrderByDescending(e => e.Id).FirstOrDefault(), comment.ProfessionId);

            CurrentCustomer.CurrentRole = Role.Checker;

            var result = false;

            if (operationType == AduitCommentOperationType.Pre_AuditDeleteComment)
            {
                result = professionalAudit.DeleteComment(commentId, string.Empty, operationType.GetDescription(),
                    FlowCode.Pre_AuditCompany_ProfessionAudit_Delete_Comment);
                
            }

            else if (operationType == AduitCommentOperationType.DeleteComment)
            {
                result = professionalAudit.DeleteComment(commentId, string.Empty, operationType.GetDescription());
                
            }
            else if (operationType == AduitCommentOperationType.AgreeRepaired)
            {
                result = professionalAudit.AgreeRepari(commentId, string.Empty, operationType.GetDescription());
                
            }
            else if (operationType == AduitCommentOperationType.DisagreeRepaired)
            {
                result = professionalAudit.DisagreeRepari(commentId, string.Empty, operationType.GetDescription());
                
            }
            else if (operationType == AduitCommentOperationType.Back)
            {
                result = professionalAudit.UndoComment(commentId, string.Empty, operationType.GetDescription());
                
            }

            return Json(new { result = result, errmsg = "" });
        }

        [HttpPost]
        public ActionResult ReAduitOperateComment(int commentId, ReAduitCommentOperationType operationType, string reason = "")
        {
            var comment = new CommentService().Get(e => e.Id == commentId);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            if (!new PermissionService().CanReviewProject(CurrentCustomer, comment.EngineeringFile.Engineering, comment.ProfessionId))
                return NoAuthorityJson();

            var professionalReAudit = new ProfessionalReAudit(CurrentCustomer, comment.EngineeringFile, comment.EngineeringFile.FileVersions.OrderByDescending(e => e.Id).FirstOrDefault(), comment.ProfessionId);

            CurrentCustomer.CurrentRole = Role.Reviewer;

            var result = false;

            //完整性检查阶段
            //TODO 输出的文字提示待更新
            if (operationType == ReAduitCommentOperationType.Pre_AgreeComment)
            {
                result = true;
                    professionalReAudit.ReauditApproveProfessionalComment(comment, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.Pre_DisAgreeComment)
            {
                result = true;
                    professionalReAudit.ReauditRejectProfessionalComment(comment, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.Pre_Back)
            {
                result = professionalReAudit.UndoComment(commentId, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.Pre_ReauditDeleteComment)
            {
                result = professionalReAudit.DeleteComment(commentId, string.Empty, operationType.GetDescription(),
                    FlowCode.Pre_AuditCompany_ProfessionReaudit_Delete_Comment);
                
            }
            //设计审查阶段
            else if (operationType == ReAduitCommentOperationType.PassComment)
            {
                result = professionalReAudit.Agree(commentId, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.ReturnComment)
            {
                result = professionalReAudit.Disagree(commentId, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.DeleteComment)
            {
                result = professionalReAudit.DeleteComment(commentId, string.Empty, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.CancelDelete)
            {
                result = professionalReAudit.UndoComment(commentId, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.AgreeDesignReject)
            {
                result = professionalReAudit.AgreeDesignCompanyNoRepairComment(commentId, string.Empty, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.DisagreeDesignReject)
            {
                result = professionalReAudit.DisagreeDesignCompanyNoRepairComment(commentId, "", operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.AgreeRepaired)
            {
                result = professionalReAudit.AgreeRepair(commentId, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.DisagreeRepaired)
            {
                result = professionalReAudit.DisagreeRepair(commentId, reason, operationType.GetDescription());
                
            }
            else if (operationType == ReAduitCommentOperationType.Back)
            {
                result = professionalReAudit.UndoComment(commentId, reason, operationType.GetDescription());
                
            }

            return Json(new { result = result, errmsg = "" });
        }

        [HttpPost]
        public ActionResult DesignManagerOperateComment(int commentId, DesignManagerCommentOperationType operationType)
        {
            var comment = new CommentService().Get(e => e.Id == commentId);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            if (!new PermissionService().CanManagerProject(CurrentCustomer, comment.EngineeringFile.Engineering.Project))
                return NoAuthorityJson();

            var designCompanyProjectManager = new DesignCompanyProjectManager(CurrentCustomer, comment.EngineeringFile, comment.EngineeringFile.FileVersions.OrderByDescending(e => e.Id).FirstOrDefault());

            CurrentCustomer.CurrentRole = Role.DesignCompanyManager;

            var result = false;

            if (operationType == DesignManagerCommentOperationType.Agree)
            {
                result = designCompanyProjectManager.Agree(commentId, "", operationType.GetDescription());
                
            }
            else if (operationType == DesignManagerCommentOperationType.Return)
            {
                result = designCompanyProjectManager.Disagree(commentId, "", operationType.GetDescription());
                
            }
            else if (operationType == DesignManagerCommentOperationType.Repaired)
            {
                var dic = new Dictionary<int, bool>();
                dic.Add(commentId, true);
                result = designCompanyProjectManager.CommentsRepair(dic, "", operationType.GetDescription());
                
            }
            else if (operationType == DesignManagerCommentOperationType.UnRepaired)
            {
                var dic = new Dictionary<int, bool>();
                dic.Add(commentId, false);
                result = designCompanyProjectManager.CommentsRepair(dic, "", operationType.GetDescription());
                
            }
            else if (operationType == DesignManagerCommentOperationType.Back)
            {
                result = designCompanyProjectManager.Undo(commentId, comment.Status, "", operationType.GetDescription());
                
            }

            return Json(new { result = result, errmsg = "" });
        }

        public ActionResult GetBulkOperationButton(int modelVersionId,int professionId = 0)
        {
            var modelVersion = new FileVersionService().GetById(modelVersionId);
            if (modelVersion == null)
                throw new ArgumentNullException(nameof(modelVersion));

            ModelProfession modelProfession = null;
            if (professionId != 0)
            {
                modelProfession = modelVersion.ModelProfessions.OrderByDescending(e => e.Id).FirstOrDefault();
            }

            var model = new CommentBulkOperationButtonModel()
            {
                ModelStatus = modelVersion.Status,
                ProfessionStatus = modelProfession?.Status,
            };

            if (CurrentCustomer.Organization.OrganizationType == OrganizationType.DesignCompany)
            {
                model.CustomerRole = Role.DesignCompanyManager;
            }
            else
            {
                if (professionId != 0)
                {
                    model.CustomerRole = CurrentCustomer.CustomerRoles.FirstOrDefault(e => e.EngineeringId == modelVersion.EngineeringFile.EngineeringId && e.ProfessionId == professionId)?.Role;
                }
            }

            return Json(new { htmlStr = this.RenderPartialViewToString("_BulkOperationButton", model) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult BulkOperation(string strCommentIds, CommentBulkOperationType type)
        {
            var commentIds = strCommentIds.Split(',').Select(e => { return Convert.ToInt32(e); }).ToArray();
            
            if (commentIds.Count() == 0)
                return Json(new { result = false, errmsg = "未选择意见" });

            var first = commentIds[0];
            var comment = new CommentService().Get(e => e.Id == first);
            if (comment == null)
                throw new ArgumentNullException(nameof(comment));

            var result = true;
            string errmsg = "";

            var designCompanyProjectManager = new DesignCompanyProjectManager(CurrentCustomer, comment.EngineeringFile, comment.EngineeringFile.FileVersions.OrderByDescending(e => e.Id).FirstOrDefault());
            var professionalAudit = new ProfessionalAudit(CurrentCustomer, comment.EngineeringFile, comment.EngineeringFile.FileVersions.OrderByDescending(e => e.Id).FirstOrDefault(), comment.ProfessionId);
            var professionalReAudit = new ProfessionalReAudit(CurrentCustomer, comment.EngineeringFile, comment.EngineeringFile.FileVersions.OrderByDescending(e => e.Id).FirstOrDefault(), comment.ProfessionId);

            if (type == CommentBulkOperationType.DesignManagerRepaired)
            {
                if (!new PermissionService().CanManagerProject(CurrentCustomer, comment.EngineeringFile.Engineering.Project))
                    return NoAuthorityJson();
                CurrentCustomer.CurrentRole = Role.DesignCompanyManager;

                foreach (var id in commentIds)
                {
                    var dic = new Dictionary<int, bool>();
                    dic.Add(id, true);
                    if (!designCompanyProjectManager.CommentsRepair(dic, "", type.GetDescription()))
                    {
                        result = false;
                        errmsg = SetBulkOperationErrsmg(errmsg, id);
                    }
                }
            }
            else if (type == CommentBulkOperationType.DesignManagerUnrepaired)
            {
                if (!new PermissionService().CanManagerProject(CurrentCustomer, comment.EngineeringFile.Engineering.Project))
                    return NoAuthorityJson();
                CurrentCustomer.CurrentRole = Role.DesignCompanyManager;

                foreach (var id in commentIds)
                {
                    var dic = new Dictionary<int, bool>();
                    dic.Add(id, false);
                    if (!designCompanyProjectManager.CommentsRepair(dic, "", type.GetDescription()))
                    {
                        result = false;
                        errmsg = SetBulkOperationErrsmg(errmsg, id);
                    }
                }
            }
            else if (type == CommentBulkOperationType.CheckerAgreeRepaired)
            {
                if (!new PermissionService().CanCheckProject(CurrentCustomer, comment.EngineeringFile.Engineering, comment.ProfessionId))
                    return NoAuthorityJson();
                CurrentCustomer.CurrentRole = Role.Checker;

                foreach (var id in commentIds)
                {
                    if (!professionalAudit.AgreeRepari(id, string.Empty, type.GetDescription()))
                    {
                        result = false;
                        errmsg = SetBulkOperationErrsmg(errmsg, id);
                    }
                }
            }
            else if (type == CommentBulkOperationType.ChecherDisagreeRepaired)
            {
                if (!new PermissionService().CanCheckProject(CurrentCustomer, comment.EngineeringFile.Engineering, comment.ProfessionId))
                    return NoAuthorityJson();
                CurrentCustomer.CurrentRole = Role.Checker;

                foreach (var id in commentIds)
                {
                    if (!professionalAudit.DisagreeRepari(id, string.Empty, type.GetDescription()))
                    {
                        result = false;
                        errmsg = SetBulkOperationErrsmg(errmsg, id);
                    }
                }
            }
            else if (type == CommentBulkOperationType.ReviewerAgreeRepaired)
            {
                if (!new PermissionService().CanReviewProject(CurrentCustomer, comment.EngineeringFile.Engineering, comment.ProfessionId))
                    return NoAuthorityJson();
                CurrentCustomer.CurrentRole = Role.Reviewer;

                foreach (var id in commentIds)
                {
                    if (!professionalReAudit.AgreeRepair(id, string.Empty, type.GetDescription()))
                    {
                        result = false;
                        errmsg = SetBulkOperationErrsmg(errmsg, id);
                    }
                }
            }
            else if (type == CommentBulkOperationType.ReviewerDisagreeRepaired)
            {
                if (!new PermissionService().CanReviewProject(CurrentCustomer, comment.EngineeringFile.Engineering, comment.ProfessionId))
                    return NoAuthorityJson();
                CurrentCustomer.CurrentRole = Role.Reviewer;

                foreach (var id in commentIds)
                {
                    if (!professionalReAudit.DisagreeRepair(id, string.Empty, type.GetDescription()))
                    {
                        result = false;
                        errmsg = SetBulkOperationErrsmg(errmsg, id);
                    }
                }
            }

            return Json(new { result = result, errmsg = errmsg });
        }
        #endregion
    }
}