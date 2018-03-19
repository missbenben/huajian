using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Domain.Customers;
using TS.Service.DataAccess.Customers;
using System.Web.Security;
using TS.Web.Filters;
using TS.Service.BusinessLogic.CustomerLogic;
using TS.Core.Domain.Organizations;
using TS.Service.DataAccess.Organizetions;
using TS.Web.Models.CustomerLogin;
using System.Text.RegularExpressions;
using TS.Web.Models.SuperAdmin;
using TS.Core;
using TS.Data.Extensions;
using static TS.Web.Models.SuperAdmin.InformationManagerModel;
using TS.Service.DataAccess.Dictionaries;
using TS.Core.Domain.Dictionaries;

namespace TS.Web.Controllers
{
    [RoleFilter(CustomerType.SuperAdmin)]
    public class SuperAdminController : BaseController
    {
        #region ini
        private CustomerService customerService;
        private OrganizationService orgService;
        private OrganizationInfoUnCheckedService orgUnCheckedInFoService;
        public SuperAdminController()
        {
            customerService = new CustomerService();
            orgService = new OrganizationService();
            orgUnCheckedInFoService = new OrganizationInfoUnCheckedService();

        }
        #endregion

        #region SuperAdminCheck
        //超级管理员待审核首页

        public ActionResult SuperAdminCheckList()
        {
            // Customer superModel = this.CurrentCustomer;

            return View();
        }

        //返回待审核数据List

        public ActionResult GetSuperAdminUnCheckedData(int pageIndex, int pageSize,string searchFuzzyInput,int typeOfCompany)
        {
            var pageList = customerService.GetUnauditedAdmin(pageIndex, pageSize, searchFuzzyInput, typeOfCompany);
            var pageLists = PrepareUnCheckedData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("UnCheckedCompany", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
            // return View();
        }


        public PagedList<AdminAndCompanyModel> PrepareUnCheckedData(IQueryable<Customer> query, int pageIndex, int pageSize)
        {
            Organization org = new Organization();
            IList<AdminAndCompanyModel> aacModels = new List<AdminAndCompanyModel>();
            foreach (var cus in query)
            {

                AdminAndCompanyModel aacItem = new AdminAndCompanyModel();
                aacItem.Account = cus.UserAccount;
                aacItem.AdminId = cus.Id;
                aacItem.AdminName = cus.Name;
                aacItem.ApplyTime = cus.CreateTime.ToString();
                aacItem.CompanyId = cus.OrganizationId;
                if (cus.OrganizationId != null)
                {
                    org = orgService.GetOrgById(cus.OrganizationId);
                    aacItem.CompanyName = org.Name;
                    switch (org.OrganizationType)
                    {
                        case OrganizationType.BuildingCompany:
                            aacItem.CompanyType = "建设公司"; break;
                        case OrganizationType.Censorship:
                            aacItem.CompanyType = "审查机构"; break;
                        case OrganizationType.DesignCompany:
                            aacItem.CompanyType = "设计公司"; break;
                        default:
                            aacItem.CompanyType = ""; break;
                    }
                    aacModels.Add(aacItem);
                }
            }
            // var totalCount = aacModels.Count();
            return new PagedList<AdminAndCompanyModel>(aacModels, pageIndex, pageSize);
        }

        public ActionResult UnCheckedDetail(int UnCheckedAdminId)
        {
            //if (CurrentCustomer.CustomerType != Core.Domain.Customers.CustomerType.SuperAdmin)
            //{
            //    return new HttpUnauthorizedResult();
            //}
            var adminUser = customerService.Get(e => e.Id == UnCheckedAdminId);
            int? orgIdnow = adminUser.OrganizationId;
            var organization = orgService.Get(s => s.Id == orgIdnow);
            var companyRegisterModel = new DetailModel()
            {
                CompanyID = organization.OrganizationNumber.ToString(),
                CompanyType = organization.OrganizationType.GetDescription(),
                CompanyName = organization.Name,
                Address = organization.OrganizationAddress,
                ZipCode = organization.ZipCode,
                CompanyPhone = organization.OrganizationTelephone,
                BusinessLicence = organization.BusinessLicence,
                UploadLicenceUri = organization.BusinessLicensePicUri,
                comId = organization.Id,
                AdminId = UnCheckedAdminId
            };
            if (organization.OrganizationStatus != OrganizationStatus.Unaudited)
            {
                return View("CheckedDetail", companyRegisterModel);
            }
            return View(companyRegisterModel);
        }


        public ActionResult IsApprove(string isapp, int orgId, int adminId)
        {

            if (isapp == "approve")
            {

                customerService.setUserAvailable(adminId);
                orgService.setCompanyAvailable(orgId);
                return Json(new
                {
                    Result = true,
                    Message = "已通过该申请"
                });
            }
            else if (isapp == "disapprove")
            {
                customerService.DeleteUser(adminId);
                orgService.DeleteCompany(orgId);
                return Json(new
                {
                    Result = false,
                    Message = "已拒绝该申请"
                });
            }
            else
                return Json(new
                {
                    Result = false,
                    Message = "异常信息"
                });
        }

        public ActionResult SuperAdminHasCheckedList()
        {


            return View();
        }

        public ActionResult GetSuperAdminHasCheckedData(int pageIndex, int pageSize, string searchFuzzyInput, int typeOfCompany)
        {
            var pageList = customerService.GetauditedAdmin(pageIndex, pageSize, searchFuzzyInput, typeOfCompany);
            var pageLists = PrepareUnCheckedData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("CheckedCompany", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
            // return View();
        }

        public ActionResult CheckedDetail(int CheckedAdminId)
        {
            //if (CurrentCustomer.CustomerType != Core.Domain.Customers.CustomerType.SuperAdmin)
            //{
            //    return new HttpUnauthorizedResult();
            //}
            var adminUser = customerService.Get(e => e.Id == CheckedAdminId);
            int? orgIdnow = adminUser.OrganizationId;
            var organization = orgService.Get(s => s.Id == orgIdnow);
            string freeze = "";
            switch (organization.OrganizationStatus)
            {
                case OrganizationStatus.Available:
                    freeze = "unfreeze"; break;
                case OrganizationStatus.freeze:
                    freeze = "freeze"; break;
            }
            var companyRegisterModel = new DetailModel()
            {
                CompanyID = organization.OrganizationNumber.ToString(),
                CompanyType = organization.OrganizationType.GetDescription(),
                CompanyName = organization.Name,
                Address = organization.OrganizationAddress,
                ZipCode = organization.ZipCode,
                CompanyPhone = organization.OrganizationTelephone,
                BusinessLicence = organization.BusinessLicence,
                UploadLicenceUri = organization.BusinessLicensePicUri,
                comId = organization.Id,
                AdminId = CheckedAdminId,
                IsFreeze = freeze
            };
            if (organization.OrganizationStatus == OrganizationStatus.Unaudited)
            {
                return View("UnCheckedDetail", companyRegisterModel);
            }
            return View(companyRegisterModel);
        }

        public ActionResult ChangeCompanyStatus(string isapp, int orgId, int adminId)
        {

            if (isapp == "freeze")
            {
                var customers = customerService.GetUsersByOrgId(orgId).ToList();
                foreach (Customer cus in customers)
                {
                    customerService.setUserFreeze(cus.Id);
                }
                orgService.setCompanyFreeze(orgId);
                return Json(new
                {
                    Result = true,
                    Message = "已冻结该公司"
                });
            }
            else if (isapp == "unfreeze")
            {
                List<Customer> customers = customerService.GetUsersByOrgId(orgId).ToList();
                foreach (Customer cus in customers)
                {
                    customerService.setUserAvailable(cus.Id);
                }
                orgService.setCompanyAvailable(orgId);
                return Json(new
                {
                    Result = true,
                    Message = "已解冻该公司"
                });
            }
            else
                return Json(new
                {
                    Result = false,
                    Message = "异常信息"
                });
        }
        #endregion

        #region SuperAdminCreateCompany
        [RoleFilter(CustomerType.SuperAdmin)]
        public ActionResult SuperAdminCreateCompany()
        {
            var sd = new CompanyRegisterModel();
            sd.companys = orgService.GetAvaiOrganizations();
            return View(sd);
        }

        [RoleFilter(CustomerType.SuperAdmin)]
        public ActionResult SuperAdminCreateUser(CompanyRegisterModel companyRM)
        {

            OrganizationService orService = new OrganizationService();
            companyRM.companys = orService.GetAvaiOrganizations();

            companyRM.WhichToShow = "user";
            if (companyRM.UserAccount != "" && companyRM.UserAccount != null)
            {
                //验证用户名是否存在
                if (RegisterBusinessLayer.CheckAccountExist(companyRM.UserAccount))
                {
                    companyRM.AccountError = "已存在的用户名，请使用其他的用户名";
                    return View("Register", companyRM);
                }
            }
            else
            {
                companyRM.AccountError = "用户名不能为空";
                return View("Register", companyRM);
            }

            if (companyRM.UserEmail != "" && companyRM.UserEmail != null)
            {
                //验证邮箱格式
                string emailStr = @"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+$";
                //邮箱正则表达式对象
                Regex emailReg = new Regex(emailStr);
                if (!emailReg.IsMatch(companyRM.UserEmail))
                {
                    companyRM.EmailError = "输入的邮件格式有误";
                    return View("Register", companyRM);
                }
            }
            else
            {
                companyRM.EmailError = "邮箱不能为空";
                return View("Register", companyRM);
            }

            if (companyRM.UserMobile != "" && companyRM.UserMobile != null)
            {
                Regex mobileReg = new Regex("^[0-9]{11,11}$");

                if (!mobileReg.IsMatch(companyRM.UserMobile))
                {
                    companyRM.MobileError = "手机格式有误";
                    return View("Register", companyRM);
                }

            }
            else
            {
                companyRM.MobileError = "手机号不能为空";
                return View("Register", companyRM);
            }

            string userCompanyId = companyRM.UserCompanyId;
            int orgId;
            if (userCompanyId != null && userCompanyId != "")
            {
                orgId = Convert.ToInt32(userCompanyId);
            }
            else
            {
                companyRM.CompanyIdAndNameError = "公司名不能为空";
                return View("Register", companyRM);
            }
            //保存



            //OrganizationType companyType;
            //string comtype = companyRM.CompanyType;

            //switch (comtype)
            //{
            //    case "设计公司": companyType = OrganizationType.DesignCompany; break;
            //    case "建设公司": companyType = OrganizationType.BuildingCompany; break;
            //    case "审查机构": companyType = OrganizationType.Censorship; break;
            //    default: companyType = OrganizationType.DesignCompany; break;
            //}

            //new OrganizationService();

            Customer cus = new Customer();
            DateTime localDate = DateTime.Now;
            cus.CreateTime = localDate;
            cus.LastVisitTime = localDate;

            //need to add
            cus.EmployeeId = "P0802";
            System.Guid guid = System.Guid.NewGuid();
            cus.CustomerGuid = guid;
            cus.CustomerStatus = CustomerStatus.Available;
            cus.CustomerType = CustomerType.User;
            cus.Email = companyRM.UserEmail;
            cus.Mobile = companyRM.UserMobile;

            //need to add
            cus.Telephone = companyRM.UserMobile;
            //need to add
            cus.Department = "liuliu";

            cus.Name = companyRM.UserName;




            cus.OrganizationId = orgId;

            //cus.Organization = orService.GetOrgById(orgId);

            cus.Password = companyRM.UserPassword;
            cus.UserAccount = companyRM.UserAccount;

            new CustomerService().Insert(cus);

            //设置登陆状态并跳转到对应功能页
            return RedirectToAction("SuperAdminchecklist");

        }



        [HttpPost]
        [RoleFilter(CustomerType.SuperAdmin)]
        public ActionResult SuperAdminCreateCompany(CompanyRegisterModel companyRM)
        {
            OrganizationService orService = new OrganizationService();
            companyRM.companys = orService.GetAvaiOrganizations();

            companyRM.WhichToShow = "company";
            string uploadLicenceUri = "";
            if (companyRM.CompanyID == "" || companyRM.CompanyID == null)
            {
                companyRM.CompanyIdError = "企业组织机构代码应为15位数字";
                return View(companyRM);
            }

            if (companyRM.ZipCode != null && companyRM.ZipCode != null)
            {
                Regex zipcodeReg = new Regex("^\\d{6}$");
                if ((!zipcodeReg.IsMatch(companyRM.ZipCode)))
                {
                    companyRM.ZipCodeError = "邮编应为6位数字";
                    return View(companyRM);
                }
            }
            else
            {
                companyRM.ZipCodeError = "邮编应为6位数字";
                return View(companyRM);
            }


            //Regex companyPhoneReg = new Regex("^[-\\d]{7-20}$");
            //if ((!companyPhoneReg.IsMatch(companyRM.CompanyPhone)))
            //{
            //    companyRM.CompanyPhoneError = "公司电话格式有误";
            //    return View(companyRM);
            //}

            //验证用户名是否存在
            if (companyRM.Account != null && companyRM.Account != "")
            {
                if (RegisterBusinessLayer.CheckAccountExist(companyRM.Account))
                {
                    companyRM.AccountError = "已存在的用户名，请使用其他的用户名";
                    return View(companyRM);
                }
            }
            else
            {
                companyRM.AccountError = "账号不能为空";
                return View(companyRM);
            }
            //验证公司名是否存在
            if (companyRM.CompanyName != null && companyRM.CompanyName != "")
            {
                if (RegisterBusinessLayer.CheckCompanyExist(companyRM.CompanyName))
                {
                    companyRM.CompanyNameError = "已存在的公司名，请使用其他的公司名";
                    return View(companyRM);
                }
            }
            else
            {
                companyRM.CompanyNameError = "公司名称不能为空";
                return View(companyRM);
            }

            //验证邮箱格式
            if (companyRM.ApplyEmail != null && companyRM.ApplyEmail != "")
            {
                string emailStr = @"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+$";
                //邮箱正则表达式对象
                Regex emailReg = new Regex(emailStr);
                if (!emailReg.IsMatch(companyRM.ApplyEmail))
                {
                    companyRM.EmailError = "请填写正确的邮箱格式";
                    return View(companyRM);
                }
            }
            else
            {
                companyRM.EmailError = "请填写正确的邮箱格式";
                return View(companyRM);
            }

            if (companyRM.ApplyPhone != null && companyRM.ApplyPhone != "")
            {
                Regex mobileReg = new Regex("^[0-9]{11,11}$");

                if (!mobileReg.IsMatch(companyRM.ApplyPhone))
                {
                    companyRM.MobileError = "手机号码应为11位数字";
                    return View(companyRM);
                }
            }
            else
            {
                companyRM.MobileError = "手机号码应为11位数字";
                return View(companyRM);
            }



            /*
                            var file = Request.Files[0];                
                            if (file != null && file.ContentLength > 0)
                            {


                                //文件名的key和value
                                string savePath = Server.MapPath("~/upload/BusinessLicencePicture");
                                if (!System.IO.Directory.Exists(savePath))
                                {
                                    System.IO.Directory.CreateDirectory(savePath);
                                }
                                Guid addToTheEnd = new Guid();
                                string filepath = savePath + "\\" + file.FileName + addToTheEnd;
                                file.SaveAs(filepath);
                                uploadLicenceUri = filepath;

                            }
                            else
                            {
                                //上传失败返回到注册页面
                                return View(companyRM);

                                //InfoAllRight = false;
                                //backMessage = backMessage + "上传图片有误/n";
                            }
                            */
            //uploadLicenceUri = "asd/asd/asd";
            if (companyRM.UploadLicenceUri != null && companyRM.UploadLicenceUri != "")
            {
                uploadLicenceUri = companyRM.UploadLicenceUri;
            }
            else
            {
                companyRM.UriError = "图片不能为空！";
                return View(companyRM);
            }


            //保存
            Organization org = new Organization();
            org.BusinessLicence = companyRM.BusinessLicence;
            //get uri by last step
            org.BusinessLicensePicUri = uploadLicenceUri;
            org.ContacterName = companyRM.ApplyName;
            org.ContacterPhone = companyRM.ApplyPhone;
            org.Name = companyRM.CompanyName;
            org.OrganizationAddress = companyRM.Address;
            org.OrganizationNumber = companyRM.CompanyID;
            org.OrganizationTelephone = companyRM.CompanyPhone;
            org.OrganizationStatus = OrganizationStatus.Available;
            OrganizationType companyType;
            string comtype = companyRM.CompanyType;

            switch (comtype)
            {
                case "设计公司": companyType = OrganizationType.DesignCompany; break;
                case "建设公司": companyType = OrganizationType.BuildingCompany; break;
                case "审查机构": companyType = OrganizationType.Censorship; break;
                default: companyType = OrganizationType.DesignCompany; break;
            }
            org.OrganizationType = companyType;

            org.ProposerEmail = companyRM.ApplyEmail;
            org.ProposerName = companyRM.ApplyName;
            org.ProposerPhone = companyRM.ApplyPhone;
            org.ZipCode = companyRM.ZipCode;
            //new OrganizationService();

            Customer cus = new Customer();
            DateTime localDate = DateTime.Now;
            cus.CreateTime = localDate;
            cus.LastVisitTime = localDate;

            //need to add
            cus.EmployeeId = "P0802";

            System.Guid guid = System.Guid.NewGuid();
            cus.CustomerGuid = guid;

            cus.CustomerStatus = CustomerStatus.Available;
            cus.CustomerType = CustomerType.Admin;
            cus.Email = companyRM.ApplyEmail;
            cus.Mobile = companyRM.ApplyPhone;

            //need to add
            cus.Telephone = companyRM.ApplyPhone;
            //need to add
            cus.Department = "liuliu";

            cus.Name = companyRM.ApplyName;
            //cus.Organization = org;
            cus.OrganizationId = org.Id;
            cus.Password = companyRM.Password;
            cus.UserAccount = companyRM.Account;

            bool isSuccess = new OrganizationService().InsertOrganizationAndUser(org, cus);
            //new CustomerService().Insert(cus);

            //设置登陆状态并跳转到对应功能页
            // LoginCheck.PasswordSignIn(companyRM.Account, companyRM.Password, this.SetCustomerCookie);
            return RedirectToAction("SuperAdminchecklist");
        }
        #endregion

        #region Super Admin Information Manager

        [RoleFilter(CustomerType.SuperAdmin)]
        public ActionResult InformationManager()
        {          
            return View(PrepareInformationManagerModel());
        }

        protected InformationManagerModel PrepareInformationManagerModel()
        {
            InformationManagerModel model = new InformationManagerModel();

            model.AvaliableBaseCommentTypes = Helper.EnumHelper.EnumToSelectListItem<BaseCommentType>();
            model.CommentLevels = DictionaryService.CommentLevelDictionary.Where(e => e.IsDeleted == false).Select(dic => {
                return new DictionaryModel()
                {
                    DictionaryId = dic.Id,
                    DisplayName = dic.DisplayName,
                    Type = DictionaryType.CommentLevel,
                };
            }).ToList();
            model.CommentProfessions = DictionaryService.CommentProfessionDictionary.Where(e => e.IsDeleted == false).Select(dic =>
            {
                return new DictionaryModel()
                {
                    DictionaryId = dic.Id,
                    DisplayName = dic.DisplayName,
                    Type = DictionaryType.CommentProfession,
                };
            }).ToList();
            model.DrawingCatalogs = DictionaryService.DrawingCatalogDictionary.Where(e => e.IsDeleted == false).Select(dic =>
            {
                return new DictionaryModel()
                {
                    DictionaryId = dic.Id,
                    DisplayName = dic.DisplayName,
                    Type = DictionaryType.DrawingCatalog,
                };
            }).ToList();
            model.DrawingProfessions = DictionaryService.DrawingProfessionDictionary.Where(e => e.IsDeleted == false).Select(dic =>
            {
                return new DictionaryModel()
                {
                    DictionaryId = dic.Id,
                    DisplayName = dic.DisplayName,
                    Type = DictionaryType.DrawingProfession,
                };
            }).ToList();
            DictionaryService.IntegralityCommentTypeDictionary.Where(e => e.IsDeleted == false).ToList().ForEach(dic =>
            {
                model.CommentTypes.Add(new CommentTypeModel()
                {
                    commentId = dic.Id,
                    BaseCommentType = BaseCommentType.IntegralityCommentType,
                    type = DictionaryType.FirstCommentType,
                    FirstCommentTypeName =dic.DisplayName,
                });
            });
            DictionaryService.DesignCommentTypeDictionary.Where(e => e.IsDeleted == false && e.SecondSystemName == DictionaryService.category).ToList().ForEach(dic => {
                model.CommentTypes.Add(new CommentTypeModel()
                {
                    commentId = dic.Id,
                    BaseCommentType = BaseCommentType.DesignCommentType,
                    FirstCommentTypeName = dic.DisplayName,
                    type = DictionaryType.FirstCommentType,
                    SecondCommentTypes = DictionaryService.DesignCommentTypeDictionary.Where(e => e.IsDeleted == false && e.ParentId == dic.Id).Select(sen => {
                        return new DictionaryModel()
                        {
                            DictionaryId = sen.Id,
                            DisplayName = sen.DisplayName,
                            Type = DictionaryType.DesignCommentType,
                        };
                    }).ToList(),
                });
            });

            return model;
        }

        [HttpPost]
        public ActionResult AddDictionary(DictionaryModel model)
        {
            if (!CurrentCustomer.IsSuperAdmin())
                return NoAuthorityJson();

            var dictionary = DictionaryModelToEntity(model);

            new DictionaryService().InsertOrUpdateDictionary(dictionary);

            var htmlStr = PrepareDictionaryAddHtmlStr(dictionary, model);

            return Json(new { result = true, htmlStr = htmlStr, dictionary = model });
        }

        protected Dictionary DictionaryModelToEntity(DictionaryModel model)
        {
            Dictionary dictionay = new Dictionary();

            if (model.Type != DictionaryType.FirstCommentType)
            {
                dictionay.FirstSystemName = model.Type.GetDescription();
                if (model.Type == DictionaryType.DesignCommentType)
                {
                    dictionay.SecondSystemName = DictionaryService.value;
                    dictionay.ParentId = model.ParentId;
                }
            }
            else
            {
                if (model.BaseCommentType.Value == BaseCommentType.DesignCommentType)
                {
                    dictionay.FirstSystemName = DictionaryService.designCommentType;
                    dictionay.SecondSystemName = DictionaryService.category;
                }
                else
                {
                    dictionay.FirstSystemName = DictionaryService.integralityCommentType;
                }
            }

            dictionay.DisplayName = model.DisplayName;
            dictionay.ExtraValue1 = model.ExtraValue1;
            dictionay.ExtraValue2 = model.ExtraValue2;
            dictionay.ExtraValue3 = model.ExtraValue3;
            return dictionay;
        }

        protected string PrepareDictionaryAddHtmlStr(Dictionary dic, DictionaryModel model)
        {

            model.DictionaryId = dic.Id;

            if (model.Type != DictionaryType.FirstCommentType)
            {
                var list = new List<DictionaryModel>();
                list.Add(model);
                return base.RenderPartialViewToString("_DictionaryItem", list);
            }
            else
            {
                CommentTypeModel commentType = new CommentTypeModel()
                {
                    BaseCommentType = model.BaseCommentType.Value,
                    FirstCommentTypeName = model.DisplayName,
                    commentId = dic.Id,
                    type = DictionaryType.FirstCommentType,
                };
                var list = new List<CommentTypeModel>();
                list.Add(commentType);
                return base.RenderPartialViewToString("_DictionaryCommentType", list);
            }
            
        }

        [HttpPost]
        public ActionResult DeleteDictionary(DictionaryType type, int dictionaryId)
        {
            if (!CurrentCustomer.IsSuperAdmin())
                return NoAuthorityJson();

            Dictionary dictionary;
            if (type != DictionaryType.FirstCommentType)
            {
                var list = DictionaryService.GetDictionaryList(type.GetDescription());
                dictionary = list.Find(e => e.Id == dictionaryId);

                new DictionaryService().DeleteDictionary(dictionary);
            }
            else
            {
                dictionary = DictionaryService.DesignCommentTypeDictionary.FirstOrDefault(e => e.Id == dictionaryId);
                if (dictionary == null)
                    dictionary = DictionaryService.IntegralityCommentTypeDictionary.Find(e => e.Id == dictionaryId);

                new DictionaryService().DeleteDictionary(dictionary);
            }

            return Json(new { result = true, dictionaryId = dictionaryId, type = type, level = dictionary.DisplayName });
        }
        #endregion

        #region SuperAdminModifyInfo
        public ActionResult SuperAdminEditCompany(FormCollection form)
        {
            if (form["companyId"] == "" || form["companyId"] == null)
            {
                return Json(new
                {
                    Result = true,
                    Message = "异常！"
                }, JsonRequestBehavior.AllowGet);
            }
            int companyId = Convert.ToInt32(form["companyId"]);
            string companyNumber = form["orgNumber"];
            string companyName = form["name"];
            string companyAddress = form["address"];
            string tel = form["tel"];
            string licence = form["licence"];
            string licenceuri = form["licenceuri"];

            Organization org = orgService.GetOrgById(companyId);
            if (companyNumber != "" && companyNumber != null)
            {
                org.OrganizationNumber = companyNumber;
            }

            if (companyName != "" && companyName != null)
            {
                org.Name = companyName;
            }
            if (companyAddress != "" && companyAddress != null)
            {
                org.OrganizationAddress = companyAddress;
            }
            if (tel != "" && tel != null)
            {
                org.OrganizationTelephone = tel;
            }

            if (licence != "" && licence != null)
            {
                org.BusinessLicence = licence;
            }
            if (licenceuri != "" && licenceuri != null)
            {
                org.BusinessLicensePicUri = licenceuri;
            }
            orgService.UpdateCompany(org);
            return Json(new
            {
                Result = true,
                Message = "更新成功！"
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region SuperAdminCheckModifyInfo

        public ActionResult GetSuperAdminCheckModifyInfoData(int pageIndex, int pageSize, string searchFuzzyInput, int typeOfCompany)
        {
            var pageList = orgUnCheckedInFoService.GetUnauditedOrgInfo(pageIndex, pageSize, searchFuzzyInput, typeOfCompany);
            var pageLists = PrepareUnCheckedInFoData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("UnCheckedCompanyModifyInfo", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
            // return View();
        }

        public PagedList<AdminAndCompanyModel> PrepareUnCheckedInFoData(IQueryable<OrganizationInfoUnchecked> query, int pageIndex, int pageSize)
        {
            Organization org = new Organization();
            IList<AdminAndCompanyModel> aacModels = new List<AdminAndCompanyModel>();
            foreach (var orgInfo in query)
            {
                Customer cus = customerService.Get(e=>e.Id==orgInfo.AdminId);//orgInfo.Customer;
                AdminAndCompanyModel aacItem = new AdminAndCompanyModel();
                aacItem.UnCheckedInfoId = orgInfo.Id;
                aacItem.Account = cus.UserAccount;
                aacItem.AdminId = cus.Id;
                aacItem.AdminName = cus.Name;
                aacItem.ApplyTime = orgInfo.CreateTime.ToString();//cus.CreateTime.ToString();
                aacItem.CompanyId = cus.OrganizationId;
                if (cus.OrganizationId != null)
                {
                    org = orgService.GetOrgById(cus.OrganizationId);
                    aacItem.CompanyName = org.Name;
                    switch (org.OrganizationType)
                    {
                        case OrganizationType.BuildingCompany:
                            aacItem.CompanyType = "建设公司"; break;
                        case OrganizationType.Censorship:
                            aacItem.CompanyType = "审查机构"; break;
                        case OrganizationType.DesignCompany:
                            aacItem.CompanyType = "设计公司"; break;
                        default:
                            aacItem.CompanyType = ""; break;
                    }
                    aacModels.Add(aacItem);
                }
            }
            // var totalCount = aacModels.Count();
            return new PagedList<AdminAndCompanyModel>(aacModels, pageIndex, pageSize);
        }


        //未审核公司更新信息详情页
        public ActionResult SuperAdminCheckModifyInfoDetail(int UnCheckedInfoId)
        {
            var unCheckedInfo = orgUnCheckedInFoService.Get(e => e.Id == UnCheckedInfoId);
            var org = orgService.Get(e => e.Id == unCheckedInfo.OrganizationId);
            var oldAndNewModel = new OldAndNewCompanyInfoModel()
            {
                UnCheckedInfoId = UnCheckedInfoId,
                OldNumber = org.OrganizationNumber,
                OldType = org.OrganizationType.GetDescription(),
                OldName = org.Name,
                OldPhone = org.OrganizationTelephone,
                OldAddress = org.OrganizationAddress,
                OldBusinessLicence = org.BusinessLicence,
                OldUploadLicenceUri = org.BusinessLicensePicUri,
                NewNumber = unCheckedInfo.OrganizationNumber,
                //不改类型
                NewType = org.OrganizationType.GetDescription(),
                NewName =unCheckedInfo.Name,
                NewPhone=unCheckedInfo.OrganizationTelephone,
                NewAddress=unCheckedInfo.OrganizationAddress,
                NewBusinessLicence=unCheckedInfo.BusinessLicence,
                NewUploadLicenceUri=unCheckedInfo.BusinessLicensePicUri
            };

            return View(oldAndNewModel);
        }

        //更新信息是否审核通过
        public ActionResult IsCompanyInfoApprove(string isapp, int unCheckedInfoId)
        {
            var unCheckedInfo = orgUnCheckedInFoService.Get(e => e.Id == unCheckedInfoId);

            if (isapp == "approve")
            {
                var org = orgService.Get(e => e.Id == unCheckedInfo.OrganizationId);//unCheckedInfo.Organization;
                org.OrganizationNumber = unCheckedInfo.OrganizationNumber;
                org.Name = unCheckedInfo.Name;
                org.OrganizationTelephone = unCheckedInfo.OrganizationTelephone;
                org.OrganizationAddress = unCheckedInfo.OrganizationAddress;
                org.BusinessLicence = unCheckedInfo.BusinessLicence;
                org.BusinessLicensePicUri = unCheckedInfo.BusinessLicensePicUri;
                orgService.Update(org);
                unCheckedInfo.OrganizationInfoStatus = OrganizationInfoStatus.Checked;
                orgUnCheckedInFoService.Update(unCheckedInfo);

                return Json(new
                {
                    Result = true,
                    Message = "已通过该申请"
                });
            }
            else if (isapp == "disapprove")
            {
                unCheckedInfo.OrganizationInfoStatus = OrganizationInfoStatus.refuse;
                orgUnCheckedInFoService.Update(unCheckedInfo);
                return Json(new
                {
                    Result = false,
                    Message = "已拒绝该申请"
                });
            }
            else
                return Json(new
                {
                    Result = false,
                    Message = "异常信息"
                });
        }
        #endregion



        #region HRManager
        public ActionResult GetSuperAdminUserList(int pageIndex, int pageSize, int checkType, string searchFuzzyInput)
        {
            var checkstatus = checkType != 1 ? CustomerStatus.Available : CustomerStatus.Unaudited;
            var pageList = customerService.GetAdminCheckOrUnCheckUserList(checkstatus,  searchFuzzyInput);
            var pageLists = SupPrepareUnCheckedUserData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("AdminUserMana", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
        }

        public PagedList<AdminAndCompanyModel> SupPrepareUnCheckedUserData(IQueryable<Customer> query, int pageIndex, int pageSize)
        {
            Organization org = new Organization();
            IList<AdminAndCompanyModel> aacModels = new List<AdminAndCompanyModel>();
            foreach (var cus in query)
            {

                AdminAndCompanyModel aacItem = new AdminAndCompanyModel();
                aacItem.Account = cus.UserAccount;
                aacItem.AdminId = cus.Id;
                aacItem.AdminName = cus.Name;
                aacItem.ApplyEmail = cus.Email;
                aacItem.ApplyPhone = cus.Mobile;
                switch (cus.CustomerStatus)
                {
                    case CustomerStatus.Available: aacItem.PageType = "Checked"; break;
                    case CustomerStatus.Unaudited: aacItem.PageType = "UnChecked"; break;
                    case CustomerStatus.freeze: aacItem.PageType = "Freeze"; break;
                }
                aacItem.CompanyId = cus.OrganizationId;
                if (cus.OrganizationId != null)
                {
                    org = orgService.GetOrgById(cus.OrganizationId);
                    aacItem.CompanyName = org.Name;
                    switch (org.OrganizationType)
                    {
                        case OrganizationType.BuildingCompany:
                            aacItem.CompanyType = "建设公司"; break;
                        case OrganizationType.Censorship:
                            aacItem.CompanyType = "审查机构"; break;
                        case OrganizationType.DesignCompany:
                            aacItem.CompanyType = "设计公司"; break;
                        default:
                            aacItem.CompanyType = ""; break;
                    }
                    aacModels.Add(aacItem);
                }
            }
            // var totalCount = aacModels.Count();
            return new PagedList<AdminAndCompanyModel>(aacModels, pageIndex, pageSize);
        }
        #endregion
    }
}
