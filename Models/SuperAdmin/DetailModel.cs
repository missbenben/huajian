using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TS.Web.Models.SuperAdmin
{
    public class DetailModel
    {
        public int comId { get; set; }

        public int AdminId { get; set; }

        public string CompanyID { get; set; }


        public string CompanyType { get; set; }


        public string CompanyName { get; set; }


        public string Address { get; set; }

        public string ZipCode { get; set; }


        public string CompanyPhone { get; set; }

        public string BusinessLicence { get; set; }

        public string UploadLicenceUri { get; set; }

        public string IsFreeze { get; set; }
    }
}