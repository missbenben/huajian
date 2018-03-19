using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core;
using TS.Core.Domain.Customers;
using TS.Core.Domain.Organizations;
using TS.Data.Extensions;
using TS.Service.DataAccess.Customers;
using TS.Service.DataAccess.Organizetions;
using TS.Web.Models.CustomerLogin;
using TS.Web.Models.SuperAdmin;
using TS.Web.Filters;
using System.Net.Mail;
using System.Text.RegularExpressions;
using TS.Service.BusinessLogic.CustomerLogic;

namespace TS.Web.Controllers
{
    public class OrganizationController : BaseController
    {
        #region init
        private OrganizationService organizationService;
        private CustomerService customerService;
        private OrganizationService orgService;
        private OrganizationInfoUnCheckedService orgUnCheckedService;

        public OrganizationController()
        {
            organizationService = new OrganizationService();
            customerService = new CustomerService();
            orgService = new OrganizationService();
            orgUnCheckedService = new OrganizationInfoUnCheckedService();
        }
        #endregion

        #region CompanyInfo
        /// <summary>
        /// 企业信息页面
        /// </summary>
        /// <returns></returns>
        public ActionResult EnterpriseInfo()
        {
            if (CurrentCustomer.CustomerType != Core.Domain.Customers.CustomerType.Admin)
            {
                return new HttpUnauthorizedResult();
            }

            var organization = organizationService.Get(s => s.Id == CurrentCustomer.OrganizationId);
            var companyRegisterModel = new CompanyRegisterModel()
            {
                CompanyID = organization.OrganizationNumber.ToString(),
                CompanyType = organization.OrganizationType.GetDescription(),
                CompanyName = organization.Name,
                Address = organization.OrganizationAddress,
                ZipCode = organization.ZipCode,
                CompanyPhone = organization.OrganizationTelephone,
                BusinessLicence = organization.BusinessLicence,
                UploadLicenceUri = organization.BusinessLicensePicUri
            };

            return View(companyRegisterModel);
        }

        [RoleFilter(CustomerType.Admin)]
        public ActionResult AdminEditCompany(FormCollection form)
        {
            if (CurrentCustomer.OrganizationId == null)
            {
                return Json(new
                {
                    Result = true,
                    Message = "异常！"
                }, JsonRequestBehavior.AllowGet);
            }

            var orgId = CurrentCustomer.OrganizationId.Value;

            string companyNumber = form["orgNumber"];
            string companyName = form["name"];
            string companyAddress = form["address"];
            string tel = form["tel"];
            string licence = form["licence"];
            string licenceuri = form["licenceuri"];
            OrganizationInfoUnchecked orgInfoUnChecked = new OrganizationInfoUnchecked();
            orgInfoUnChecked.AdminId = CurrentCustomer.Id;
            orgInfoUnChecked.BusinessLicence = licence;
            orgInfoUnChecked.BusinessLicensePicUri = licenceuri;
            orgInfoUnChecked.Name = companyName;
            orgInfoUnChecked.OrganizationAddress = companyAddress;
            orgInfoUnChecked.OrganizationId = orgId;
            orgInfoUnChecked.OrganizationInfoStatus = OrganizationInfoStatus.UnChecked;
            orgInfoUnChecked.OrganizationNumber = companyNumber;
            orgInfoUnChecked.OrganizationTelephone = tel;
            DateTime localDate = DateTime.Now;
            orgInfoUnChecked.CreateTime = localDate;
            orgUnCheckedService.Insert(orgInfoUnChecked);

            return Json(new
            {
                Result = true,
                Message = "已提交审核！"
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region AdminCheck
        //管理员待审核首页
        [RoleFilter(CustomerType.Admin)]
        public ActionResult AdminCheckList()
        {
            //Customer adminModel = this.CurrentCustomer;

            return View();
        }

        //返回待审核数据List
        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetAdminUserList(int pageIndex, int pageSize, int checkType, string searchFuzzyInput)
        {
            if (CurrentCustomer.OrganizationId == null)
            {
                return Json(new { listHtml = string.Empty, total = 0 }, JsonRequestBehavior.AllowGet);
            }

            var checkstatus = checkType != 1 ? CustomerStatus.Available : CustomerStatus.Unaudited;
            var pageList = customerService.GetCheckOrUnCheckUserList(checkstatus, CurrentCustomer.OrganizationId.Value, searchFuzzyInput);
            var pageLists = PrepareUnCheckedUserData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("UnCheckedUser", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
        }
        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetAdminUnCheckedData(int pageIndex, int pageSize)
        {
            Customer admin = this.CurrentCustomer;
            var pageList = customerService.GetUnauditedUserByOrgId(admin.OrganizationId);
            var pageLists = PrepareUnCheckedUserData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("UnCheckedUser", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
            // return View();
        }
        [RoleFilter(CustomerType.Admin)]
        public ActionResult GetAdminCheckedData(int pageIndex, int pageSize)
        {
            Customer admin = this.CurrentCustomer;
            var pageList = customerService.GetCheckedUserByOrgId(admin.OrganizationId);
            var pageLists = PrepareUnCheckedUserData(pageList, pageIndex, pageSize);
            return Json(new { listHtml = this.RenderPartialViewToString("UnCheckedUser", pageLists), total = pageLists.TotalCount }, JsonRequestBehavior.AllowGet);
            // return View();
        }

        [RoleFilter(CustomerType.Admin)]
        public PagedList<AdminAndCompanyModel> PrepareUnCheckedUserData(IQueryable<Customer> query, int pageIndex, int pageSize)
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

        [RoleFilter("SuperAdmin,Admin")]
        public ActionResult ApproveUser(int adminId)
        {
            customerService.setUserAvailable(adminId);

            return Json(new
            {
                Result = true,
                Message = "已通过该用户"
            });
        }
        [RoleFilter("SuperAdmin,Admin")]
        public ActionResult DisApproveUser(int adminId)
        {
            customerService.DeleteUser(adminId);

            return Json(new
            {
                Result = true,
                Message = "已拒绝该用户"
            });
        }

        [RoleFilter("SuperAdmin,Admin")]
        public ActionResult FreezeUser(int userId)
        {

            customerService.setUserFreeze(userId);
            return Json(new
            {
                Result = true,
                Message = "已冻结该用户"
            });
        }

        [RoleFilter("SuperAdmin,Admin")]
        public ActionResult UnFreezeUser(int userId)
        {

            customerService.setUserAvailable(userId);

            return Json(new
            {
                Result = true,
                Message = "已解冻该用户"
            });
        }



        #endregion

        #region CommonFuncton
        [RoleFilter(CustomerType.Admin)]
        public ActionResult InviteColleague(string colleagueEmail, string colleagueName)
        {
            Customer admin = this.CurrentCustomer;
            string adminName = admin.Name;

            var sendingEmail = System.Configuration.ConfigurationManager.AppSettings["SendingEmail"];
            var emailAuthCode = System.Configuration.ConfigurationManager.AppSettings["EmailAuthCode"];
            var emailService = System.Configuration.ConfigurationManager.AppSettings["EmailService"];
            var emailPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["EmailPort"]);

            //发邮件
            System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient(emailService, emailPort);

            client.EnableSsl = true;
            //client.Host = "smtp.163.com";
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(sendingEmail, emailAuthCode);
            MailAddress from = new MailAddress(sendingEmail, "华建数字化审图平台", System.Text.Encoding.UTF8);
            MailAddress to = new MailAddress(colleagueEmail, "", System.Text.Encoding.UTF8);
            MailMessage message = new MailMessage(from, to);

            message.Body = "您的同事" + adminName + "邀请您加入华建数字化审图平台，请登陆http://"+ System.Configuration.ConfigurationManager.AppSettings["DomainName"] + "/Customer/Register  注册使用";
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.Subject = "您的同事" + adminName + "邀请您加入华建数字化审图平台";
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = true;
            //  client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;





            //添加附件  
            //Attachment data = new Attachment(@"附件路径", System.Net.Mime.MediaTypeNames.Application.Octet);
            //message.Attachments.Add(data);

            try
            {
                client.Send(message);
                return Json(new
                {
                    Result = true,
                    Message = "已发送邀请邮件至您的同事！"
                }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {

                //throw e;
                return Json(new
                {
                    Result = true,
                    Message = "该邮箱不存在！"
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [RoleFilter("SuperAdmin,Admin")]
        public ActionResult EditCompanyUserInfo(string newName, string newEmail,string newPhone, int userId)
        {
            if (newName!=null&&newEmail != null && newPhone != null)
            {
                if (newName != null && newName != "")
                {
                    customerService.UpdateUserName(userId, newName);

                }

                if (newEmail != null&&newEmail!="")
                {
                    //验证邮箱格式
                    string emailsStr = @"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+$";
                    //邮箱正则表达式对象
                    Regex emailsReg = new Regex(emailsStr);
                    if (!emailsReg.IsMatch(newEmail))
                    {
                        return Json(new
                        {
                            Result = false,
                            Message = "邮箱格式有误！"
                        }, JsonRequestBehavior.AllowGet);
                    }

                    customerService.UpdateUserEmail(userId, newEmail);

                }

                if (newPhone != null&&newPhone!="")
                {
                    Regex phoneReg = new Regex("^[0-9]{11,11}$");

                    if (!phoneReg.IsMatch(newPhone))
                    {
                        return Json(new
                        {
                            Result = false,
                            Message = "手机号应为11位数字！"
                        }, JsonRequestBehavior.AllowGet);
                    }
                    customerService.UpdateUserPhone(userId, newPhone);

                }
                return Json(new
                {
                    Result = true,
                    Message = "修改成功"
                }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "修改信息不能为空"
                }, JsonRequestBehavior.AllowGet);
            }


        }

        [RoleFilter("Admin,SuperAdmin")]
        public ActionResult GetUserInfoInit(int userId)
        {
            var cus = customerService.GetById(userId);
            if (cus != null)
            {
                return Json(new
                {
                    Result = true,
                    UserName =cus.Name ,
                    UserEmail =cus.Email,
                    Tel =cus.Telephone
            }, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(new
                {
                    Result = false,
                    UserName ="" ,
                    UserEmail ="",
                    Tel =""
            }, JsonRequestBehavior.AllowGet);

        }

        [RoleFilter(CustomerType.Admin)]
        public ActionResult AdminCreateUser()
        {
            CompanyRegisterModel crModel = new CompanyRegisterModel();
            return View(crModel);
        }

        [RoleFilter(CustomerType.Admin)]
        public ActionResult AdminCreateUserFunc(CompanyRegisterModel companyRM)
        {
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

            int orgId=CurrentCustomer.OrganizationId.Value;

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
            return RedirectToAction("AdminCheckList");

        }
        #endregion




    }
}
