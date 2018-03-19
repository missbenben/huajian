using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Web.Filters;

namespace TS.Web.Models.CustomerLogin
{
    public class CompanyRegisterModel
    {
        public CompanyRegisterModel()
        {
            companys = new List<Organization>();
        }
        #region Company

        public string CompanyID { get; set; }


        public string CompanyType{ get; set; }


        public string CompanyName { get; set; }


        public string Address { get; set; }

        public string ZipCode { get; set; }


        public string CompanyPhone { get; set; }


        public string BusinessLicence { get; set; }


        public string ApplyName { get; set; }


        public string ApplyEmail { get; set; }

 
        public string ApplyPhone { get; set; }


        public string Account { get; set; }


        public string Password { get; set; }


        public string UploadLicenceUri { get; set; }
        // public string UploadLicenceUri { get; set; }
        public string EmailError { get; set; }

        public string MobileError { get; set; }

        public string AccountError { get; set; }

        public string CompanyNameError { get; set; }

        public string  CompanyIdError { get; set; }

        public string  ZipCodeError { get; set; }

        public string UriError { get; set; }

        //public string CompanyPhoneError { get; set; }
        public string  CompanyIdAndNameError { get; set; }
        #endregion
        #region User
        //    public int companyId { get; set; }
        public List<Organization> companys { get; set; }
        public string UsersCompanyName { get; set; }
        public string UserAccount { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPassword { get; set; }
        public string UserMobile { get; set; }
        
        public string UserCompanyId { get; set; }
        #endregion


        public string WhichToShow { get; set; }

    }
}