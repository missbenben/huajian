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
using TS.Service.DataAccess.Permissions;
using TS.Service.DataAccess.Projects;
using TS.Service.Facade;
using TS.Web.Helper;
using TS.Web.Models.BimModel;
using TS.Web.Models.Comments;

namespace TS.Web.Controllers
{
    /// <summary>
    /// EngineeringModelController
    /// </summary>
    public class BimModelController : BaseController
    {
        #region Fields

        private readonly EngineeringService _engineeringService = new EngineeringService();
        private readonly EngineeringFileService _engineeringFileService = new EngineeringFileService();
        private readonly FileVersionService _fileVersionService = new FileVersionService();
        private readonly CommentService _commentService = new CommentService();
        private readonly BIMElementService _bIMElementService = new BIMElementService();
        private readonly ModelProfessionService _modelProfessionService = new ModelProfessionService();

        #endregion

        #region Utilities

        /// <summary>
        /// 是否必须处理该意见
        /// </summary>
        private bool MustOperate(FlowCode commentStatus, FlowCode modelProfessionStatus, Role role)
        {
            if (role == Role.Checker)
            {
                //审核人修改复核人退回的意见
                if ((modelProfessionStatus == FlowCode.Pre_AuditCompany_ProfessionReAudit_Back &&
                    commentStatus == FlowCode.Pre_AuditCompany_ProfessionReaudit_Reject_ProfessionalPerfect) ||
                    (modelProfessionStatus == FlowCode.AuditCompany_ProfessionReaudit_Reject_ProfessionAudit &&
                     commentStatus == FlowCode.AuditCompany_ProfessionReaudit_Disagree_ProfessionAuditComment))
                {
                    return true;
                }
                //审核人校对设计公司对意见的反馈
                if (modelProfessionStatus == FlowCode.AuditCompany_ProjectManager_Sign_BuildCompany &&
                    commentStatus == FlowCode.DesignCompany_Repair_Comment)
                {
                    return true;
                }
            }
            else if (role == Role.Reviewer)
            {
                //复核人审核审核人的意见
                if ((modelProfessionStatus == FlowCode.Pre_AuditCompany_ProfessionAudit_Submit &&
                     commentStatus == FlowCode.Pre_AuditCompany_ProfessionAudit_ProfessionalPerfectComment) ||
                    (modelProfessionStatus == FlowCode.AuditCompany_ProfessionAudit_Submit && (commentStatus == FlowCode.AuditCompany_ProfessionAudit_Create_Comment ||
                                                                                               commentStatus == FlowCode.AuditCompany_ProfessionAudit_Update_Comment ||
                                                                                               commentStatus == FlowCode.AuditCompany_ProfessionAudit_Undelete_Comment)))
                {
                    return true;
                }
                //复核人校对设计公司对意见的反馈
                if (modelProfessionStatus == FlowCode.AuditCompany_ProfessionAudit_Submit &&
                    (commentStatus == FlowCode.AuditCompany_ProfessionAudit_Agrees_DesignCompany_RePairComment ||
                     commentStatus == FlowCode.AuditCompany_ProfessionAudit_Disagrees_DesignCompany_RePairComment))
                {
                    return true;
                }
                //复核人再次处理设计公司对意见的反馈
                if (modelProfessionStatus == FlowCode.DesignCompany_Submit_AuditCompany_ProfessionReaudit &&
                    commentStatus == FlowCode.DesignCompany_Disagree_AuditCompany_Comment)
                {
                    return true;
                }
            }
            else if (role == Role.DesignCompanyManager)
            {
                //设计公司处理反馈意见
                if (modelProfessionStatus == FlowCode.BuildCompany_Submit_DesignCompany &&
                    (commentStatus == FlowCode.AuditCompany_ProjectManager_Reject_NoRepair_Comment ||
                     commentStatus == FlowCode.DesignCompany_Undo_ArgeementOpration_AuditCompany_Comment))
                {
                    return true;
                }
                //设计公司重新上传模型
                if (modelProfessionStatus == FlowCode.AuditCompany_ProjectManager_Confrim_CurrentProcess_End &&
                    (commentStatus == FlowCode.AuditCompany_ProjectManager_Comprim_NeedRepair_Comment ||
                     commentStatus == FlowCode.DesignCompany_Undo_RepairAtiveOpration_Comment))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MustOperate(Comment comment, Role role)
        {
            switch (role)
            {
                case Role.DesignCompanyManager:
                    if (comment.NeedDesignPmOperate)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case Role.Checker:
                    if (comment.NeedCheckerOperate)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case Role.Reviewer:
                    if (comment.NeedReviewerOperate)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default: return false;
            }
        }

        /// <summary>
        /// 获取当前角色（不确定是什么角色进来的时候）
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="engineeringId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        private CustomerRoleMapping GetCurrentRole(int projectId, int? engineeringId = null, int professionId = 0)
        {
            var role = (GetRole(projectId, engineeringId, professionId) ?? GetRole(projectId, engineeringId)) ?? GetRole(projectId);

            return role;
        }

        private List<Role> GetAvailableRoles(int projectId, int? engineeringId = null, int professionId = 0)
        {
            var list = new List<Role>();

            //项目经理？
            var managerRole = CurrentCustomer.CustomerRoles.FirstOrDefault(r => r.ProjectId == projectId &&
                                                                (r.EngineeringId == null || r.EngineeringId == engineeringId) &&
                                                                r.ProfessionId == 0);
            if(managerRole != null)
            list.Add(managerRole.Role);

            //审核人复核人？
            foreach(var r in CurrentCustomer.CustomerRoles.Where(r => r.ProjectId == projectId &&
                                                                r.EngineeringId == engineeringId &&
                                                                r.ProfessionId == professionId))
            {
                list.Add(r.Role);
            }

            list = list.OrderBy(r => (int) r).ToList();

            return list;
        }

        /// <summary>
        /// 获取角色:
        /// 只传入projectId：项目级经理
        /// 传入engineeringId：子项目级别的经理
        /// 传入engineeringId，professionId：子项目专业级审查复查人
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="engineeringId">子项目级别的权限</param>
        /// <param name="professionId">子项目专业级权限</param>
        /// <returns></returns>
        private CustomerRoleMapping GetRole(int projectId, int? engineeringId = null, int professionId = 0)
        {

                var role = CurrentCustomer.CustomerRoles.Where(r => r.ProjectId == projectId &&
                                                                    r.EngineeringId == engineeringId &&
                                                                    r.ProfessionId == professionId).ToList().FirstOrDefault();
                return role;
        }

        private void PrepareModelDetailModel(FileVersion fileVersion, ModelDetailModel model)
        {
            model.CurrentFileVersion = fileVersion;
            model.CurrentFileVersionId = fileVersion.Id;
            model.ModelGuid = fileVersion.BIMFileId;

            var engineeringFile = fileVersion.EngineeringFile;
            model.ProjectGuid = engineeringFile.Engineering.Project.BIMProjectId;
            model.ProjectName =
                $"{engineeringFile.Engineering.Project.ProjectName} - {engineeringFile.Engineering.Name}";
            var manager = new CustomerRoleMappingService().Get(r => r.ProjectId == fileVersion.EngineeringFile.Engineering.ProjectId && r.EngineeringId == fileVersion.EngineeringFile.EngineeringId);
            if(manager == null)
                new CustomerRoleMappingService().Get(r => r.ProjectId == fileVersion.EngineeringFile.Engineering.ProjectId);

            model.ProjectManagerName = manager?.CustomerName;

            model.EngineeringFile = engineeringFile;
            model.CurrentCustomer = CurrentCustomer;

            for (int i = 1; i <= fileVersion.EngineeringFile.Engineering.FloorsAboveGround; i++)
            {
                model.AvailableFloors.Insert(0, new SelectListItem()
                {
                    Text = $"地上{i}层",
                    Value = $"地上{i}层"
                });
            }

            model.AvailableFloors.Insert(0, new SelectListItem()
            {
                Text = $"通用",
                Value = $"通用"
            });

            for (int i = 1; i <= fileVersion.EngineeringFile.Engineering.FloorsUnderGround; i++)
            {
                model.AvailableFloors.Add(new SelectListItem()
                {
                    Text = $"地下{i}层",
                    Value = $"地下{i}层"
                });
            }

            foreach (var p in DictionaryService.CommentProfessionDictionary)
            {
                model.ModelProfessions.Add(new SelectListItem
                {
                    Text = p.DisplayName,
                    Value = p.Id.ToString(),
                    Selected = false
                });
            }

            if (fileVersion.Status == FlowCode.Pre_AuditCompany_ProjectManager_Sign ||
                fileVersion.Status == FlowCode.Pre_AuditCompany_ProfessionAudit_Submit)
            {
                foreach (var type in DictionaryService.IntegralityCommentTypeDictionary)
                {
                    model.CommentTypes.Add(new SelectListItem()
                    {
                        Text = type.DisplayName,
                        Value = type.Id.ToString()
                    });
                }
            }
            else
            {
                var commentTypes =
                    DictionaryService.DesignCommentTypeDictionary.Where(
                        e => e.SecondSystemName == DictionaryService.category);
                foreach (var ct in commentTypes)
                {
                    foreach (var secct in DictionaryService.DesignCommentTypeDictionary.Where(
                        e => e.ParentId == ct.Id && e.SecondSystemName == DictionaryService.value))
                    {
                        model.CommentTypes.Add(new SelectListItem()
                        {
                            Text = $"{ct.DisplayName}-{secct.DisplayName}",
                            Value = secct.Id.ToString()
                        });

                    }
                }
            }
        }

        private CommentModel PrepareCommentModel(Comment comment,Role? role,FlowCode professionStatus,int modelVersionId)
        {
            var model = new CommentModel();
            model.Id = comment.Id;
            model.EngineeringFileCommentId = comment.EngineeringFileCommentId;
            model.Floor = comment.Floor;
            model.CommentTypeId = comment.CommentTypeId;
            model.CommentType = new DictionaryService().Get(ct => ct.Id == comment.CommentTypeId).DisplayName;
            model.Description = comment.Description;
            model.CreatorId = comment.CreatorId;
            model.Creator = comment.Creator;
            model.CreateTime = comment.CreateTime;
            model.ModelProfessionId = comment.ProfessionId;
            model.ProfessionId = comment.ProfessionId;
            model.UpdatorId = comment.UpdatorId;
            model.Updator = comment.Updator;
            model.Status = comment.Status;

            //model.MustOperate = MustOperate(comment.Status, professionStatus, role.GetValueOrDefault());
            model.MustOperate = MustOperate(comment, role.GetValueOrDefault());

            model.BimElements = comment.BIMElements.Where(b => b.IsDelete == false).Select(b => new BIMElementModel()
            {
                Id = b.Id,
                IsDelete = b.IsDelete,
                BIMElementType = b.BIMElementType,
                BIMId = b.BIMId,
                Name = b.Name,
                BimThumb = b.BimThumb
            }).ToList();

            model.commentButtom = new Models.Comments.CommentDetailModel.CommentButtom()
            {
                CommentId = comment.Id,
                Status = comment.Status,
                ProfessionId = comment.ProfessionId,
                ProfessionStatus = professionStatus,
                Roles = new List<Role>() { CurrentCustomer.CurrentRole.GetValueOrDefault() },
                CurrentModelVersionId = modelVersionId,
                ModelStatus = comment.EngineeringFile.Status,
            };

            //驳回详情
            if (comment.ChildCommentId.HasValue)
            {
                var rejectDetail = _commentService.Get(c => c.Id == comment.ChildCommentId);
                model.RejectDetail = new RejectModel
                {
                    Id = rejectDetail.Id,
                    Description = rejectDetail.Description,
                    BimElements = rejectDetail.BIMElements.Where(b => b.IsDelete == false).Select(
                        b => new BIMElementModel()
                        {
                            Id = b.Id,
                            IsDelete = b.IsDelete,
                            BIMElementType = b.BIMElementType,
                            BIMId = b.BIMId,
                            Name = b.Name,
                            BimThumb = b.BimThumb
                        }).ToList()
                };
            }

            return model;
        }

        protected CommentDataListModel PrepareCommentListModel(FileVersion fileVersion, int searchProfessionId, string keyword, int pageIndex = 1, int pageSize = 20)
        {
            var model = new CommentDataListModel();
            model.CurrentModelVersionId = fileVersion.Id;

            //获取角色
            model.CustomerRole = CurrentCustomer.CurrentRole;

            //获取专业状态，如果当前版本找不到，就找上一版本的
            model.ProfessionStatus = _modelProfessionService.GetLatestModelProfession(fileVersion, searchProfessionId).Status;
            //model.ProfessionStatus = _modelProfessionService.Get(p => p.EngineeringFileId == fileVersion.EngineeringFileId 
                                                                        //&& p.ModelVersionId == fileVersion.Id && p.ProfessionId == searchProfessionId).Status;

            PagedList<Comment> comments;
            //TODO 根据版本筛选？

            comments = GetCurrentVersionComments(fileVersion, searchProfessionId, keyword, pageIndex, pageSize);

            foreach (var c in comments)
            {
                model.Comments.Add(PrepareCommentModel(c,model.CustomerRole,model.ProfessionStatus,model.CurrentModelVersionId));
            }

            //需要操作的置顶
            model.Comments = model.Comments.OrderByDescending(c => c.MustOperate).ToList();

            return model;
        }

        protected PagedList<Comment> GetCurrentVersionComments(FileVersion fileVersion, int professionId, string keywords, int pageIndex = 1, int pageSize = 20)
        {
            var comments = _commentService.GetCommentByRoleAndEngineeringFileId(CurrentCustomer.CurrentRole.GetValueOrDefault(), fileVersion.Id, true);
            //审核阶段看不到初审阶段的意见
            if (fileVersion.Status == FlowCode.Pre_AuditCompany_ProjectManager_ConformComplate ||
                fileVersion.Status == FlowCode.AuditCompany_ProfessionAudit_Submit ||
                fileVersion.Status == FlowCode.AuditCompany_ProfessionReaudit_Reject_ProfessionAudit)
            {
                comments = comments.Where(c => !((int)c.Status).ToString().StartsWith("1"));
            }
            
            //模型界面看意见必有版本
            var lastesModelVersion = fileVersion.EngineeringFile.FileVersions.OrderByDescending(e => e.UpLoadeTime).FirstOrDefault();
            if (lastesModelVersion == null)
                throw new ArgumentException(nameof(lastesModelVersion));
            if (lastesModelVersion.Id != fileVersion.Id)
            {
                comments = comments.Where(c => c.CreateVersionId == fileVersion.Id);
            }
            else
            {
                var role = CurrentCustomer.CurrentRole;
                if (((int)lastesModelVersion.Status).ToString().StartsWith("1"))
                {
                    if (role.HasValue && (role == Role.CensorshipManager || role == Role.CensorshipEngreeingManager || role == Role.Checker || role == Role.Reviewer))
                    {
                        comments = comments.Where(c => c.CreateVersionId == fileVersion.Id);
                    }
                }
            }

            //模型界面看意见必有专业
            comments = comments.Where(c => c.ProfessionId == professionId);

            //模型界面看意见可以搜索
            comments = comments.Where(c => c.Id.ToString().Contains(keywords));

            //分页
            return new PagedList<Comment>(comments.OrderByDescending(e => e.CreateTime), pageIndex, pageSize);
        }

        #endregion

        #region Comments

        /// <summary>
        /// 获取待修改意见的数据
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetComment(FormCollection form)
        {
            int id = int.Parse(form["CommentId"]);

            var comment = _commentService.Get(c => c.Id == id);
            if (comment == null)
            {
                return Json(new
                {
                    Result = false,
                    Message = "传入参数异常，未找到意见"
                });
            }

            var model = new CommentModel();
            model.Id = comment.Id;
            model.CommentTypeId = comment.CommentTypeId;
            model.Floor = comment.Floor;
            model.Description = comment.Description;
            model.ModelProfessionId = comment.ProfessionId;
            model.BimElements = comment.BIMElements.Where(b => b.IsDelete == false).Select(b => new BIMElementModel()
            {
                Id = b.Id,
                IsDelete = b.IsDelete,
                BIMElementType = b.BIMElementType,
                BIMId = b.BIMId,
                Name = b.Name,
                BimThumb = b.BimThumb
            }).ToList();

            return Json(new
            {
                Result = true,
                Comment = model
            });
        }

        /// <summary>
        /// 新建意见
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateComment(AddNewCommentModel model)
        {
            var fileVersion = _fileVersionService.Get(fv => fv.Id == model.Comment.CreateVersionId);
            if (fileVersion == null)
            {
                return Json(new
                {
                    Result = false,
                    Message = "传入参数异常，未找到模型文件"
                });
            }

            //获取角色
            //CurrentCustomer.CurrentRole = GetCurrentRole(fileVersion.EngineeringFile.Engineering.ProjectId, fileVersion.EngineeringFile.EngineeringId, model.Comment.ProfessionId)?.Role;
            CurrentCustomer.CurrentRole = (Role) model.RoleId;

            if (CurrentCustomer.CurrentRole != Role.Checker && CurrentCustomer.CurrentRole != Role.Reviewer)
            {
                return Json(new
                {
                    Result = false,
                    Message = "当前角色不可新建意见"
                });
            }

            var modelProfession = new ModelProfessionService().Get(mp => mp.ModelVersionId == fileVersion.Id && mp.ProfessionId == model.Comment.ProfessionId);
            if (modelProfession == null)
            {
                return Json(new
                {
                    Result = false,
                    Message = "模型专业未找到"
                });
            }

            //两个初审阶段审核人的可操作状态
            //一个初审阶段审核人的可操作状态
            //三个审查阶段审核人的可操作状态
            //两个审查阶段复核人的可操作状态
            if (!(CurrentCustomer.CurrentRole == Role.Checker 
                  && (modelProfession.Status == FlowCode.Pre_AuditCompany_ProjectManager_Sign
                  || modelProfession.Status == FlowCode.Pre_AuditCompany_ProfessionReAudit_Back
                  || modelProfession.Status == FlowCode.Pre_AuditCompany_ProjectManager_ConformComplate
                  || modelProfession.Status == FlowCode.AuditCompany_ProfessionReaudit_Reject_ProfessionAudit
                  || modelProfession.Status == FlowCode.AuditCompany_ProjectManager_Sign_BuildCompany )
                  || (CurrentCustomer.CurrentRole == Role.Reviewer
                  && (modelProfession.Status == FlowCode.Pre_AuditCompany_ProfessionAudit_Submit 
                  || modelProfession.Status == FlowCode.AuditCompany_ProfessionAudit_Submit 
                  || modelProfession.Status == FlowCode.DesignCompany_Submit_AuditCompany_ProfessionReaudit)))) 
            {
                return Json(new
                {
                    Result = false,
                    Message = "当前模型专业状态不允许新建意见"
                });
            }

            try
            {
                var commentType = new DictionaryService().Get(d => d.Id == model.Comment.CommentTypeId);
                //区分意见类型
                if (commentType != null && commentType.FirstSystemName == DictionaryService.designCommentType)
                {
                    var result = false;
                    model.Comment.Operation = "新增审查意见";
                    if (CurrentCustomer.CurrentRole == Role.Checker)
                    {
                        model.Comment.ProfessionId = model.Comment.ProfessionId;
                        if (model.Comment.Id == 0)
                        {
                            result = new ProfessionalAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, model.Comment.ProfessionId)
                                .AddComment(model.Comment);
                        }
                        else
                        {
                            result = new ProfessionalAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, model.Comment.ProfessionId)
                                .UpdateComment(model.Comment);
                        }
                    }
                    else if (CurrentCustomer.CurrentRole == Role.Reviewer)
                    {
                        model.Comment.ProfessionId = model.Comment.ProfessionId;
                        if (model.Comment.Id == 0)
                        {
                            result = new ProfessionalReAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, model.Comment.ProfessionId)
                                .AddComment(model.Comment);
                        }
                        else
                        {
                            result = new ProfessionalReAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, model.Comment.ProfessionId)
                                .UpdateComment(model.Comment);
                        }
                    }
                    if (result)
                    {
                        //新建BIM构件和批注纪录
                        foreach (var e in model.BIMElements)
                        {
                            e.CreateTime = DateTime.Now;
                            e.CommentId = model.Comment.Id;
                            if (e.Id == 0)
                            {
                                _bIMElementService.Insert(e);
                            }
                            else
                            {
                                _bIMElementService.Update(e);
                            }
                        }

                        return Json(new
                        {
                            Result = result,
                            Message = "操作成功"
                        });
                    }
                    return Json(new
                    {
                        Result = result,
                        Message = model.Comment.Id == 0 ? "新建意见失败" : "更新意见失败"
                    });
                }
                else
                {
                    //新建完整性意见
                    model.Comment.Operation = "新增完整性意见";
                    if (CurrentCustomer.CurrentRole == Role.Checker)
                    {
                        model.Comment.Status = FlowCode
                            .Pre_AuditCompany_ProfessionAudit_ProfessionalPerfectComment;
                    }
                    else
                    {
                        model.Comment.Status = FlowCode
                            .Pre_AuditCompany_ProfessionReaudit_ProfessionalPerfectComment;
                    }
                    model.Comment.ProfessionId = model.Comment.ProfessionId;

                    model.Comment.CreateTime = DateTime.Now;
                    model.Comment.UpdateTime = DateTime.Now;
                    model.Comment.CreatorId = CurrentCustomer.Id;
                    model.Comment.UpdatorId = CurrentCustomer.Id;
                    if (model.Comment.Id == 0)
                    {
                        _commentService.InsertComment(model.Comment, CurrentCustomer);
                    }
                    else
                    {
                        _commentService.UpdateComment(model.Comment, CurrentCustomer);
                    }

                    foreach (var e in model.BIMElements)
                    {
                        e.CreateTime = DateTime.Now;
                        e.CommentId = model.Comment.Id;
                        if (e.Id == 0)
                        {
                            _bIMElementService.Insert(e);
                        }
                        else
                        {
                            _bIMElementService.Update(e);
                        }
                    }

                    return Json(new
                    {
                        Result = true,
                        Message = "操作成功"
                    });
                }
            }
            catch (Exception ex)
            {
                //错误页
                return Json(new
                {
                    Result = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 驳回意见
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult RejectComment(AddNewCommentModel model)
        {
            var comment = _commentService.Get(c => c.Id == model.Comment.Id);
            if (comment == null)
            {
                return Json(new
                {
                    Result = false,
                    Message = "传入参数异常，未找到意见"
                });
            }

            var fileVersion = _fileVersionService.Get(fv => fv.Id == model.Comment.CreateVersionId);
            if (fileVersion == null)
            {
                return Json(new
                {
                    Result = false,
                    Message = "传入参数异常，未找到模型文件"
                });
            }

            //获取角色
            //CurrentCustomer.CurrentRole = GetCurrentRole(fileVersion.EngineeringFile.Engineering.ProjectId, fileVersion.EngineeringFile.EngineeringId, comment.ProfessionId)?.Role;
            CurrentCustomer.CurrentRole = (Role)model.RoleId;

            if (CurrentCustomer.CurrentRole != Role.Checker && CurrentCustomer.CurrentRole != Role.Reviewer && CurrentCustomer.CurrentRole != Role.DesignCompanyManager)
            {
                return Json(new
                {
                    Result = false,
                    Message = "当前角色不可驳回"
                });
            }

            var modelProfession = new ModelProfessionService().Get(mp => mp.ModelVersionId == fileVersion.Id && mp.ProfessionId == comment.ProfessionId);
            if (modelProfession == null)
            {
                return Json(new
                {
                    Result = false,
                    Message = "模型专业未找到"
                });
            }

            try
            {
                //新建意见
                comment.Operation = "驳回并附加描述";
                if (modelProfession.Status == FlowCode.Pre_AuditCompany_ProfessionAudit_Submit)
                {
                    comment.UpdateTime = DateTime.Now;
                    comment.UpdatorId = CurrentCustomer.Id;
                    comment.Status = FlowCode.Pre_AuditCompany_ProfessionReaudit_Reject_ProfessionalPerfect;
                }
                else if (modelProfession.Status == FlowCode.BuildCompany_Submit_DesignCompany)
                {
                    comment.UpdateTime = DateTime.Now;
                    comment.UpdatorId = CurrentCustomer.Id;
                    comment.Status = FlowCode.DesignCompany_Disagree_AuditCompany_Comment;
                }

                var rejectComment = new Comment();
                rejectComment.Description = model.Comment.Description;//这个是传入的参数
                rejectComment.CreateVersionId = fileVersion.Id;
                var result = _commentService.InsertRejectComment(comment, rejectComment, CurrentCustomer) > 0;

                if (result)
                {
                    comment.ChildCommentId = rejectComment.Id;
                    _commentService.UpdateComment(comment, CurrentCustomer, "驳回意见");

                    //新建BIM构件和批注纪录
                    foreach (var e in model.BIMElements)
                    {
                        e.CreateTime = DateTime.Now;
                        e.CommentId = rejectComment.Id;
                        if (e.Id == 0)
                        {
                            _bIMElementService.Insert(e);
                        }
                        else
                        {
                            _bIMElementService.Update(e);
                        }
                    }
                }

                return Json(new
                {
                    Result = result,
                    Message = result? "操作成功" : "操作失败"
                });

            }
            catch (Exception ex)
            {
                //错误页
                return Json(new
                {
                    Result = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取意见数据
        /// </summary>
        /// <param name="searchModelFileVersionId"></param>
        /// <param name="searchProfessionId"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public ActionResult GetComments(int roleId, int searchModelFileVersionId, int searchProfessionId, string keyword)
        {
            //TODO 没有加分页
            var fileVersion = _fileVersionService.Get(f => f.Id == searchModelFileVersionId);
            if (fileVersion == null)
                throw new ArgumentException(nameof(fileVersion));

            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, fileVersion.EngineeringFile.Engineering.ProjectId))
                return NoAuthorityJson();

            //CurrentCustomer.CurrentRole = GetCurrentRole(fileVersion.EngineeringFile.Engineering.ProjectId, fileVersion.EngineeringFile.Engineering.Id, searchProfessionId)?.Role;
            CurrentCustomer.CurrentRole = (Role)roleId;

            var model = PrepareCommentListModel(fileVersion, searchProfessionId, keyword);
            return Json(new { result = true, listHtml = this.RenderPartialViewToString("_CommentList", model) }, 
                JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Model

        /// <summary>
        /// 进入模型页面
        /// </summary>
        /// <param name="modelVersionId">模型文件版本</param>
        /// <param name="annotationId">批注的id</param>
        /// <returns></returns>
        public ActionResult ModelDetail(int modelVersionId, string annotationId = "", int commentId = 0, int professionId = 1)
        {
            //取出模型对应model的engineeringFile
            var fileVersion = new FileVersionService().Get(e => e.Id == modelVersionId);
            if (fileVersion == null)
                return Content("模型文件未找到");

            //权限
            if (!new PermissionService().CanUserVisitProject(CurrentCustomer, fileVersion.EngineeringFile.Engineering.ProjectId))
                return new HttpUnauthorizedResult();

            //CurrentCustomer.CurrentRole = GetCurrentRole(fileVersion.EngineeringFile.Engineering.ProjectId, fileVersion.EngineeringFile.EngineeringId, 1)?.Role;

            //构件model
            var model = new ModelDetailModel();
            model.TargetAnnotationId = annotationId;
            model.CommentId = commentId;
            model.CurrentProfessionId = professionId;
            foreach (var r in GetAvailableRoles(fileVersion.EngineeringFile.Engineering.ProjectId,
                fileVersion.EngineeringFile.EngineeringId, 1))
            {
                model.AvailableRoles.Add(new SelectListItem
                {
                    Value = ((int)r).ToString(),
                    Text = r.GetDescription()
                });
            }

            PrepareModelDetailModel(fileVersion, model);

            return View(model);
        }

        /// <summary>
        /// 重新获取活动角色项
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        public ActionResult GetAvailableRoles(int fileVersionId, int professionId)
        {
            var fileVersion = new FileVersionService().Get(e => e.Id == fileVersionId);
            if (fileVersion == null)
                return Content("模型文件未找到");

            var availableRoles = new List<SelectListItem>();
            foreach (var r in GetAvailableRoles(fileVersion.EngineeringFile.Engineering.ProjectId,
                fileVersion.EngineeringFile.EngineeringId, professionId))
            {
                availableRoles.Add(new SelectListItem
                {
                    Value = ((int)r).ToString(),
                    Text = r.GetDescription()
                });
            }

            return Json(availableRoles, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 模型上传
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ModelUpLoad(FormCollection form)
        {
            try
            {
                //set page timeout to 5 minutes
                this.Server.ScriptTimeout = 300;

                string userName = form["userName"];
                string phase = form["phase"];
                int engineeringId = int.Parse(form["engineeringId"]);
                var file = Request.Files["bimModelFile"];
                string fileSize = form["fileSize"];
                string description = form["fileDescription"];

                var engineering = _engineeringService.Get(e => e.Id == engineeringId);
                var project = engineering.Project;
                //找下有没有历史版本
                var engineeringFile =
                    _engineeringFileService.Get(
                        ef => ef.EngineeringId == engineeringId && ef.FileType == FileType.Model);
                if (engineeringFile == null)
                {
                    //没有的话新建一个EngineeringFile，状态110
                    engineeringFile = new EngineeringFile();
                    engineeringFile.Status = FlowCode.Pre_DesignCompany_AwaitUpload;
                    engineeringFile.EngineeringId = engineeringId;
                    engineeringFile.FileType = FileType.Model;
                    engineeringFile.FileName = engineering.Name + "Model";
                    engineeringFile.Description = description;
                    engineeringFile.UploaderId = CurrentCustomer.Id;
                    engineeringFile.UpLoadTime = DateTime.Now;
                    engineeringFile.IsDistribution = false;
                    _engineeringFileService.Insert(engineeringFile);
                }

                //if (engineeringFile.Status != FlowCode.Pre_DesignCompany_AwaitUpload && engineeringFile.Status != FlowCode.Await_DesignCompany_ReUpload)
                //{
                //    return Json(new
                //    {
                //        Result = false,
                //        Message = "当前不允许上传新模型！"
                //    });
                //}

                if (file != null && file.ContentLength > 0)
                {
                    UploadProjectFileParameterType uploadParameterType = new UploadProjectFileParameterType(@"http://40.125.208.83:81/api/prj/UploadModel");
                    //上传文件需要附带的参数
                    uploadParameterType.PostParameters.Add("ProjectID", project.BIMProjectId);
                    uploadParameterType.PostParameters.Add("UserName", userName);
                    uploadParameterType.PostParameters.Add("Phase", phase);
                    uploadParameterType.PostParameters.Add("FileSize", fileSize);
                    //文件名的key和value
                    uploadParameterType.FileNameKey = "bimFile";
                    uploadParameterType.FileNameValue = file.FileName;
                    //文件本体Stream
                    uploadParameterType.UploadStream = file.InputStream;
                    uploadParameterType.Description = description;

                    FileVersion currentModel = new FileVersion();

                    //调用上传服务
                    DesignCompanyProjectManager designCompanyProjectManager = new DesignCompanyProjectManager(CurrentCustomer, engineeringFile, currentModel);
                    var uploadResult = designCompanyProjectManager.UploadModel(uploadParameterType);

                    return Json(new
                    {
                        Result = uploadResult.Succeed,
                        Message = uploadResult.MessageValue
                    });
                }

                return Json(new
                {
                    Result = false,
                    Message = "表单数据异常"
                });

            }
            catch (Exception exc)
            {
                //错误页
                return Json(new
                {
                    Result = false,
                    Message = exc.Message
                });
            }
        }
        
        #endregion

        #region Operation

        /// <summary>
        /// 获取模型页面操作按钮的局部页数据
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        public ActionResult GetOperationButton(int roleId, int fileVersionId, int professionId)
        {
            //取出模型对应model的engineeringFile
            var fileVersion = new FileVersionService().Get(e => e.Id == fileVersionId);
            if (fileVersion == null)
                return Content("模型文件未找到");

            //构件model
            var model = new ModelDetailModel();
            PrepareModelDetailModel(fileVersion, model);
            //model.CurrentModelProfession = new ModelProfessionService().Get(mf => mf.ModelVersionId == fileVersion.Id && mf.ProfessionId == professionId);
            model.CurrentModelProfession = _modelProfessionService.GetLatestModelProfession(fileVersion, professionId);

            if (model.CurrentModelProfession != null)
            {
                //完整性审查阶段
                if (model.CurrentModelProfession.Status == FlowCode.Pre_AuditCompany_ProjectManager_Sign ||
                    model.CurrentModelProfession.Status == FlowCode.Pre_AuditCompany_ProfessionReAudit_Back)
                {
                    model.Pre_CanCheckerSubmit =
                        !_commentService.HasAudior_Pre_NoAuditComment(model.CurrentModelProfession.ProfessionId, fileVersion.Id);
                }
                if (model.CurrentModelProfession.Status == FlowCode.Pre_AuditCompany_ProfessionAudit_Submit)
                {
                    model.Pre_CanReviewerSubmit =
                        _commentService.Pre_Reaudit_CanSubmit(model.CurrentModelProfession.ProfessionId, fileVersion.Id);
                }
                if (model.CurrentModelProfession.Status == FlowCode.Pre_AuditCompany_ProfessionAudit_Submit)
                {
                    model.Pre_CanReviewerReturn = _commentService.Reaudor_Pre_HasNoOperationToProfessionAudit(model.CurrentModelProfession.ProfessionId, fileVersion.Id);
                }

                //设计审查阶段
                if (model.CurrentModelProfession.Status == FlowCode.Pre_AuditCompany_ProjectManager_ConformComplate || 
                    model.CurrentModelProfession.Status == FlowCode.AuditCompany_ProfessionReaudit_Reject_ProfessionAudit || 
                    model.CurrentModelProfession.Status == FlowCode.AuditCompany_ProjectManager_Sign_BuildCompany)
                {
                    model.CanCheckerSubmit = 
                        !_commentService.HasAuditorNoAuditComment(model.CurrentModelProfession.ProfessionId, model.CurrentModelProfession.EngineeringFileId);
                }
                if (model.CurrentModelProfession.Status == FlowCode.AuditCompany_ProfessionAudit_Submit)
                {
                    model.CanReviewerSubmit = 
                        _commentService.HasReaudorCanSubmitToProjectManagerComment(model.CurrentModelProfession.ProfessionId, model.CurrentModelProfession.EngineeringFileId);
                    model.CanReviewerReturn = 
                        !_commentService.ReaudorHasNoOperationToProfessionAudit(model.CurrentModelProfession.ProfessionId, model.CurrentModelProfession.EngineeringFileId);
                }
                if (model.CurrentModelProfession.Status == FlowCode.DesignCompany_Submit_AuditCompany_ProfessionReaudit)
                {
                    model.CanReviewerSubmit = 
                        _commentService.HasReaudorCanSubmitToProjectManagerComment(model.CurrentModelProfession.ProfessionId, model.CurrentModelProfession.EngineeringFileId);
                }
            }

            //CurrentCustomer.CurrentRole = GetCurrentRole(fileVersion.EngineeringFile.Engineering.ProjectId, fileVersion.EngineeringFile.EngineeringId, professionId)?.Role;
            CurrentCustomer.CurrentRole = (Role) roleId;

            if (CurrentCustomer.CurrentRole == null)
            {
                return Json(new
                    {
                        result = true,
                        listHtml = this.RenderPartialViewToString("_OperationButtonBar", model),
                        roleName = $"当前角色：{CurrentCustomer.Name}（无可操作角色）",
                        currentStatus = $"当前状态：{model.CurrentModelProfession.Status.GetDescription()}",
                    },
                    JsonRequestBehavior.AllowGet);
            }

            return Json(new {
                result = true,
                listHtml = this.RenderPartialViewToString("_OperationButtonBar", model),
                roleName = $"当前角色：{CurrentCustomer.Name}（{CurrentCustomer.CurrentRole.GetDescription()}）",
                currentStatus = $"当前状态：{model.CurrentModelProfession.Status.GetDescription()}",
                },
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 审查人提交完整性意见给复核人
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CommitIntegralityCommentToReviewer(int fileVersionId, int professionId)
        {
            try
            {
                var fileVersion = _fileVersionService.Get(f => f.Id == fileVersionId);
                if (fileVersion == null)
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "传入参数异常，未找到模型文件"
                    }, JsonRequestBehavior.AllowGet);
                }

                var professionalAudit = new ProfessionalAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, professionId);
                var result = professionalAudit.CommitIntegralityCommentToReviewer();

                return Json(new
                {
                    Result = result.Succeed,
                    Message = result.MessageValue
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    Result = false,
                    Message = e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 复核人驳回审查人意见
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ReturnIntegralityComment(int fileVersionId, int professionId)
        {
            try
            {
                var fileVersion = _fileVersionService.Get(f => f.Id == fileVersionId);
                if (fileVersion == null)
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "传入参数异常，未找到模型文件"
                    }, JsonRequestBehavior.AllowGet);
                }

                var professionalReAudit = new ProfessionalReAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, professionId);
                var result = professionalReAudit.ModelOperateReject();

                return Json(new
                {
                    Result = result.Succeed,
                    Message = result.MessageValue
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    Result = false,
                    Message = e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 复查人提交完整性意见给经理
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ConfirmIntegralityComment(int fileVersionId, int professionId)
        {
            try
            {
                var fileVersion = _fileVersionService.Get(f => f.Id == fileVersionId);
                if (fileVersion == null)
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "传入参数异常，未找到模型文件"
                    }, JsonRequestBehavior.AllowGet);
                }

                //更新工程状态时插用户角色信息需要这个值
                //CurrentCustomer.CurrentRole = GetCurrentRole(fileVersion.EngineeringFile.Engineering.ProjectId, fileVersion.EngineeringFile.EngineeringId, professionId).Role;
                CurrentCustomer.CurrentRole = Role.Reviewer;

                var professionalReAudit = new ProfessionalReAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, professionId);
                var result = professionalReAudit.ModelOperateSubmit();

                return Json(new
                {
                    Result = result.Succeed,
                    Message = result.MessageValue
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    Result = false,
                    Message = e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 审核人申请复核
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ApplyReAudit(int fileVersionId, int professionId)
        {
            try
            {
                var fileVersion = _fileVersionService.Get(e => e.Id == fileVersionId);

                if (fileVersion == null)
                    throw new ArgumentNullException(nameof(fileVersion));

                CurrentCustomer.CurrentRole = Role.Checker;
                //TODO 这里返回的结果应该是OperationResultCode
                var result = new ProfessionalAudit(CurrentCustomer, fileVersion.EngineeringFile, fileVersion, professionId).Submit();

                return Json(new
                {
                    Result = result,
                    Message = result ? "操作成功" : "当前无需或不可进行此操作。"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    Result = true,
                    Message = e.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 复核人提交给项目经理
        /// </summary>
        /// <param name="fileVersionId"></param>
        /// <param name="professionId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ReviewerSubmit(int fileVersionId, int professionId)
        {
            var modelProfession = new ModelProfessionService().Get(e => e.ModelVersionId == fileVersionId && e.ProfessionId == professionId);
            if (modelProfession == null)
                throw new ArgumentNullException(nameof(modelProfession));

            if (!new PermissionService().CanReviewProject(CurrentCustomer, modelProfession.EngineerFile.Engineering, modelProfession.ProfessionId))
                return NoAuthorityJson();

            CurrentCustomer.CurrentRole = Role.Reviewer;
            var result = new ProfessionalReAudit(CurrentCustomer, modelProfession.EngineerFile, modelProfession.ModelVersion, modelProfession.ProfessionId).Submit();

            return Json(new
            {
                Result = true,
                Message = result ? "操作成功": "操作失败"
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 复核人驳回给审核人
        /// </summary>
        /// <param name="modelProfessionId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ReviewerReturn(int fileVersionId, int professionId)
        {
            var modelProfession = new ModelProfessionService().Get(e => e.ModelVersionId == fileVersionId && e.ProfessionId == professionId);
            if (modelProfession == null)
                throw new ArgumentNullException(nameof(modelProfession));

            if (!new PermissionService().CanReviewProject(CurrentCustomer, modelProfession.EngineerFile.Engineering, modelProfession.ProfessionId))
                return NoAuthorityJson();

            CurrentCustomer.CurrentRole = Role.Reviewer;
            var result = new ProfessionalReAudit(CurrentCustomer, modelProfession.EngineerFile, modelProfession.ModelVersion, modelProfession.ProfessionId).Reject();

            return Json(new { result = result, errmsg = "" });
        }

        #endregion
    }
}