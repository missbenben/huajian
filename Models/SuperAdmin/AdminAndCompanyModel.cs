using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Customers;
namespace TS.Web.Models.SuperAdmin
{
    public class AdminAndCompanyModel
    {
        public int AdminId { get; set; }

        public int? CompanyId { get; set; }

        public int UnCheckedInfoId { get; set; }

        public string Account { get; set; }

        public string AdminName { get; set; }

        public string CompanyName { get; set; }

        public string CompanyType { get; set; }

        public string ApplyTime { get; set; }

        public string ApplyEmail { get; set; }

        public string ApplyPhone { get; set; }

        public string PageType { get; set; }



      //  public string OrganizationNumber { get; set; }

      //  public string CompanyPhone { get; set; }

      //  public string CompanyAddress { get; set; }

      //  public string BusinessLicence { get; set; }

      //  public string BusinessLicensePicUri { get; set; }

     //   public OrganizationStatus Status { get; set; }

      //  public CustomerStatus CusStatus { get; set; }
    }
}