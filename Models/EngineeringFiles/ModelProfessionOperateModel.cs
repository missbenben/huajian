using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.EngineeringFiles
{
    public class ModelProfessionOperateModel
    {
        public ModelProfessionOperateModel()
        {
            Roles = new List<Role>();
        }

        public List<Role> Roles { get; set; }
        public int ModelProfessionId { get; set; }
        public FlowCode Status { get; set; }
        public bool CanCheckerSubmit { get; set; }
        public bool CanReviewerSubmit { get; set; }      
        public bool CanReviewerReturn { get; set; }
    }
}