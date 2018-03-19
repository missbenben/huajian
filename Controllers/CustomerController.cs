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
using System.Net.Mail;
using System.IO;
using System.Security.AccessControl;
using TS.Service.BusinessLogic.BIMModelOperation;
using TS.Core.Domain.EngineeringFiles;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TS.Web.Controllers
{
    public class CustomerController : BaseController
    {
        private CustomerService customerService;
        private OrganizationService orgService;

        #region Utilities
        public CustomerController()
        {
            customerService = new CustomerService();
            orgService = new OrganizationService();
        }
        public ActionResult GetUser()
        {
            AdminAndCompanyModel acModel = new AdminAndCompanyModel();
            acModel.AdminName = this.CurrentCustomer.Name;
            if (this.CurrentCustomer.OrganizationId != null)
            {
                acModel.CompanyName = orgService.GetOrgById(this.CurrentCustomer.OrganizationId).Name;
            }
            else
            {
                acModel.CompanyName = "超级管理员";
            }

            return View("_UserInfo", acModel);
        }

        public ActionResult IsAccountExist(string account)
        {
            if (account != null && account != "")
            {
                if (RegisterBusinessLayer.CheckAccountExist(account))
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "已存在的用户名，请使用其他的用户名"
                    });
                }
                return Json(new
                {
                    Result = true,
                    Message = "success"
                });
            }
            else
            {
                return Json(new
                {
                    Result = true,
                    Message = "success"
                });
            }
        }

        public ActionResult IsCompanyNameExist(string comName)
        {
            if (comName != null && comName != "")
            {
                if (RegisterBusinessLayer.CheckCompanyExist(comName))
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "已存在的公司名，请使用其他的公司名"
                    });
                }
                return Json(new
                {
                    Result = true,
                    Message = "success"
                });
            }
            else
            {
                return Json(new
                {
                    Result = true,
                    Message = "success"
                });
            }
        }

        public ActionResult GetExistCompanyList(string inputData)
        {
            List<Organization> companys = new List<Organization>();
            companys= orgService.GetShaiXuanAvaiOrganizations(inputData);
            List<CompanyAndId> nameAndIds = new List<CompanyAndId>();
            foreach(var item in companys)
            {
                nameAndIds.Add(new CompanyAndId { CompanyId = item.Id, CompanyName = item.Name } );
            }
            return Json(new
            {
                Result = true,
                Message = nameAndIds
            });
        }
        #endregion

        #region Login
        [IsLoginFilter]
        public ActionResult Login()
        {
            LoginModel lgmodel = new LoginModel();
            lgmodel.JustRegister = "notshow";
            return View(lgmodel);
        }



        [HttpPost]
        [IsLoginFilter]
        public ActionResult PostLogin(FormCollection form)
        {
            string username = form["username"];
            string password = form["password"];
            bool isremember = Convert.ToBoolean(form["isremember"]);

            if (username == "" || password == "")
            {
                return Json(new
                {
                    Result = false,
                    Message = "账号密码不能为空！"
                });
            }
            //result success Failure LockedOut
            LoginCheckResult result = LoginCheck.PasswordSignIn(username, password, this.SetCustomerCookie);

            if (result == LoginCheckResult.Success)
            {
                Customer cus = this.CurrentCustomer;
                var userRole = cus.CustomerType;//getrolebyid
                HttpCookie userRoleCookie = new HttpCookie("Role");
                switch (userRole)
                {
                    case CustomerType.User:
                        {
                            userRoleCookie.Value = "User";
                            if (isremember)
                            {
                                //加上remember password
                                HttpCookie rolelongCookie = Request.Cookies[".ASPXAUTH"];
                                rolelongCookie.Expires = DateTime.Now.AddDays(7);
                                Response.Cookies.Add(rolelongCookie);
                                userRoleCookie.Expires = DateTime.Now.AddDays(7);

                            }
                            Response.SetCookie(userRoleCookie);
                            return Json(new
                            {
                                Result = true,
                                Message = "User"
                            });
                        }
                    case CustomerType.SuperAdmin:
                        {
                            userRoleCookie.Value = "SuperAdmin";
                            if (isremember)
                            {
                                //加上remember password
                                HttpCookie rolelongCookie = Request.Cookies[".ASPXAUTH"];
                                rolelongCookie.Expires = DateTime.Now.AddDays(7);
                                Response.Cookies.Add(rolelongCookie);
                                userRoleCookie.Expires = DateTime.Now.AddDays(7);

                            }
                            Response.SetCookie(userRoleCookie);
                            return Json(new
                            {
                                Result = true,
                                Message = "SuperAdmin"
                            });
                        }
                    case CustomerType.Admin:
                        {
                            userRoleCookie.Value = "Admin";
                            if (isremember)
                            {
                                //加上remember password
                                HttpCookie rolelongCookie = Request.Cookies[".ASPXAUTH"];
                                rolelongCookie.Expires = DateTime.Now.AddDays(7);
                                Response.Cookies.Add(rolelongCookie);
                                userRoleCookie.Expires = DateTime.Now.AddDays(7);

                            }
                            Response.SetCookie(userRoleCookie);
                            return Json(new
                            {
                                Result = true,
                                Message = "Admin"
                            });
                        }
                    default: throw new ArgumentNullException("Can not find user type by id");
                }





            }
            else if (result == LoginCheckResult.LockedOut)
            {
                return Json(new
                {
                    Result = false,
                    Message = "未经审核的用户或此账户已被冻结，请联系管理员！"
                });
            }

            else if (result == LoginCheckResult.Failure)
            {
                return Json(new
                {
                    Result = false,
                    Message = "用户名或密码错误"
                });
            }
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "some error happened when login"
                });
            }
        }

        ////跳转到用户页面
        //[IsUserFilter]
        //public String ToUser()//ActionResult ToUser()
        //{
        //    return "u";// View();
        //}
        //[IsSuperAdminFilter]
        //public String ToSuperAdmin()//ActionResult ToSuperAdmin()
        //{
        //    return "sa";//View();
        //}
        //[IsAdminFilter]
        //public String ToAdmin()//ActionResult ToAdmin()
        //{
        //    return "a";//View();
        //}

        //用户登出
        public ActionResult UserExit()
        {
            HttpCookie cookie = Request.Cookies[".ASPXAUTH"];
            HttpCookie cookieRole = Request.Cookies["Role"];
            if (cookie != null)
            {
                TimeSpan ts = new TimeSpan(-1, 0, 0, 0);
                cookie.Expires = DateTime.Now.Add(ts);
                Response.AppendCookie(cookie);
            }
            if (cookieRole != null)
            {
                TimeSpan ts = new TimeSpan(-1, 0, 0, 0);
                cookieRole.Expires = DateTime.Now.Add(ts);
                Response.AppendCookie(cookieRole);
                return Json(new
                {
                    Result = true,
                    Message = "登出成功！"
                });
            }
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "已处于登出状态"
                });
            }
        }
        #endregion 

        #region Register
        //company register
        public ActionResult Register()
        {
            var sd = new CompanyRegisterModel();
            sd.WhichToShow = "user";
            OrganizationService orService = new OrganizationService();
            sd.companys = orService.GetAvaiOrganizations();
            return View(sd);
        }
        #endregion

        #region CompanyRegister
        [HttpPost]
        public ActionResult Register(CompanyRegisterModel companyRM)
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
            org.OrganizationStatus = OrganizationStatus.Unaudited;
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

            cus.CustomerStatus = CustomerStatus.Unaudited;
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
            return View("Login", new LoginModel { JustRegister = "show" });


        }

        [HttpPost]
        public ActionResult PostPicture(FormCollection form)
        {
            //向BIM上传model
            string uploadLicenceUri = "";
            var file = Request.Files["licencePicture"];

            //var file = Request.Files[0];
            if (file != null && file.ContentLength > 0)
            {
                string filehouzhui = file.FileName.Substring(file.FileName.LastIndexOf('.') + 1, file.FileName.Length - file.FileName.LastIndexOf('.') - 1);
                if (filehouzhui.ToLower() == "jpg" || filehouzhui.ToLower() == "png" || filehouzhui.ToLower() == "jpeg")
                {
                    System.Guid addToTheEnd = System.Guid.NewGuid();
                    string fileRealName = file.FileName.Substring(0, file.FileName.LastIndexOf('.'));

                    string fileNameValue = fileRealName + addToTheEnd + "." + filehouzhui;
                    Stream uploadStream = file.InputStream;


                    //上传至Azure

                    string containername = ConfigurationManager.AppSettings["ContainerName"].ToString();
                    var container = AzureBlobHelper.GetContainer(containername);

                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileNameValue);
                    blockBlob.Properties.ContentType = "application/octet-stream";
                    blockBlob.UploadFromStream(uploadStream);

                    var fileBlobUrl = AzureBlobHelper.GetSAS(fileNameValue).Split('?').FirstOrDefault();

                    ////文件名的key和value
                    //string savePath = Server.MapPath("~/upload/BusinessLicencePicture");
                    //if (!System.IO.Directory.Exists(savePath))
                    //{
                    //    System.IO.Directory.CreateDirectory(savePath);
                    //    //加访问权限
                    //   // AddpathPower(savePath, "Users");
                    //}
                    ////防止文件名重复
                    //System.Guid addToTheEnd = System.Guid.NewGuid();
                    //string fileRealName = file.FileName.Substring(0, file.FileName.LastIndexOf('.'));

                    //string filepath = savePath + "\\" + fileRealName + addToTheEnd + "." + filehouzhui;
                    //file.SaveAs(filepath);
                    uploadLicenceUri = fileBlobUrl;
                    return Json(new
                    {
                        Result = true,
                        Message = uploadLicenceUri
                    });

                }
                else
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "请上传正确格式的图片文件！"
                    });
                }


            }
            else
            {

                return Json(new
                {
                    Result = false,
                    Message = "请上传jpg或jpeg格式的图片！"
                });

                //InfoAllRight = false;
                //backMessage = backMessage + "上传图片有误/n";
            }
        }
        #endregion

        #region UserRegister
        public ActionResult PostUserRegister(CompanyRegisterModel companyRM)
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
            cus.CustomerStatus = CustomerStatus.Unaudited;
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
            return View("Login", new LoginModel { JustRegister = "show" });

        }


        #endregion

        #region UserEdit
        //用户信息修改页面
        public ActionResult UserInfoEdit()
        {
            Organization org = new Organization();
            Customer cus = this.CurrentCustomer;
            AdminAndCompanyModel aacItem = new AdminAndCompanyModel();
            aacItem.Account = cus.UserAccount;
            aacItem.AdminName = cus.Name;
            aacItem.ApplyPhone = cus.Mobile;
            aacItem.ApplyEmail = cus.Email;

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

            }

            return View(aacItem);
        }

        public ActionResult ModifyPhone(string newphonenum)
        {
            if (newphonenum != null && newphonenum != "")
            {
                Regex phoneReg = new Regex("^[0-9]{11,11}$");

                if (!phoneReg.IsMatch(newphonenum))
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "手机号格式有误！"
                    }, JsonRequestBehavior.AllowGet);
                }
                Customer cus = this.CurrentCustomer;
                customerService.UpdateUserPhone(cus.Id, newphonenum);
                return Json(new
                {
                    Result = true,
                    Message = "更新成功！"
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "不能为空！"
                }, JsonRequestBehavior.AllowGet);
            }


        }

        public ActionResult ModifyEmail(string newemailval)
        {
            if (newemailval != null && newemailval != "")
            {
                //验证邮箱格式
                string emailsStr = @"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+$";
                //邮箱正则表达式对象
                Regex emailsReg = new Regex(emailsStr);
                if (!emailsReg.IsMatch(newemailval))
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "邮箱格式有误！"
                    }, JsonRequestBehavior.AllowGet);
                }
                Customer cus = this.CurrentCustomer;
                customerService.UpdateUserEmail(cus.Id, newemailval);
                return Json(new
                {
                    Result = true,
                    Message = "更新成功！"
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "不能为空！"
                }, JsonRequestBehavior.AllowGet);
            }

        }

        [RoleFilter("Admin,SuperAdmin,User")]
        public ActionResult ModifyPassword()
        {
            return View();
        }

        [RoleFilter("Admin,SuperAdmin,User")]
        public ActionResult ModifyPasswords(string password)
        {
            if (password != null && password != "")
            {
                Customer cus = this.CurrentCustomer;
                customerService.UpdateUserPassword(cus.Id, password);
                UserExit();
                return Json(new
                {
                    Result = true,
                    Message = "更新成功！"
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "不能为空！"
                }, JsonRequestBehavior.AllowGet);
            }

        }

        #endregion

        #region forgetpassword
        public ActionResult ForgetPassword()
        {

            return View();
        }

        public ActionResult ChangePasswordAndSendEmail(string userAccount, string userEmail)
        {
            bool isAccountMatchEmail = customerService.IsAccountMatchEmail(userAccount, userEmail);
            if (isAccountMatchEmail)
            {
                Customer cus = customerService.GetCustomerByAccountAndEmail(userAccount, userEmail);
                string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

                Random randrom = new Random((int)DateTime.Now.Ticks);

                string newPassword = "";
                for (int i = 0; i < 10; i++)
                {
                    newPassword += chars[randrom.Next(chars.Length)];
                }
                //更新数据库中密码为随机生成的密码
                customerService.UpdateUserPassword(cus.Id, newPassword);


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
                MailAddress to = new MailAddress(userEmail, "", System.Text.Encoding.UTF8);
                MailMessage message = new MailMessage(from, to);

                message.Body = "已为您将密码重置为：" + newPassword + " 。请尽快登陆系统修改密码！";
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.Subject = "华建数字化审图平台找回密码";
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
                        Message = "已发送邮件至您的邮箱，请查看并修改密码！"
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
            else
            {
                return Json(new
                {
                    Result = false,
                    Message = "账号与邮箱不匹配！"
                }, JsonRequestBehavior.AllowGet);

            }



        }
        #endregion

        //public bool AddpathPower(string pathname, string username)
        //{

        //    DirectoryInfo dirinfo = new DirectoryInfo(pathname);

        //    if ((dirinfo.Attributes & FileAttributes.ReadOnly) != 0)
        //    {
        //        dirinfo.Attributes = FileAttributes.Normal;
        //    }

        //    //取得访问控制列表   
        //    DirectorySecurity dirsecurity = dirinfo.GetAccessControl();

        //    //switch (power)
        //    //{
        //    //    case "FullControl":
        //    try
        //    {
        //        dirsecurity.AddAccessRule(new FileSystemAccessRule(username, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.InheritOnly, AccessControlType.Allow));
        //        dirinfo.SetAccessControl(dirsecurity);
        //        return true;

        //    }
        //    catch (Exception e)
        //    {
        //        return false;
        //    }
        //    //case "ReadOnly":
        //    //    dirsecurity.AddAccessRule(new FileSystemAccessRule(username, FileSystemRights.Read, AccessControlType.Allow));
        //    //    break;
        //    //case "Write":
        //    //    dirsecurity.AddAccessRule(new FileSystemAccessRule(username, FileSystemRights.Write, AccessControlType.Allow));
        //    //    break;
        //    //case "Modify":
        //    //    dirsecurity.AddAccessRule(new FileSystemAccessRule(username, FileSystemRights.Modify, AccessControlType.Allow));
        //    //    break;
        //    //}

        //}
    }
}