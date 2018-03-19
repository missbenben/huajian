using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Organizations;
using TS.Core.Domain.Customers;
namespace TS.Web.Models.SuperAdmin
{
    public class OldAndNewCompanyInfoModel
    {
        public int UnCheckedInfoId { get; set; }

        public string OldNumber { get; set; }

        public string OldType { get; set; }

        public string OldName { get; set; }

        public string OldPhone { get; set; }

        public string OldAddress { get; set; }

        public string OldBusinessLicence { get; set; }

        public string OldUploadLicenceUri { get; set; }


        public string NewNumber { get; set; }

        public string NewType { get; set; }

        public string NewName { get; set; }

        public string NewPhone { get; set; }

        public string NewAddress { get; set; }

        public string NewBusinessLicence { get; set; }

        public string NewUploadLicenceUri { get; set; }
    }
}
