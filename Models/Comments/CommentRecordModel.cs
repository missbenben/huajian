using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Comments;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;

namespace TS.Web.Models.Comments
{
    public class CommentRecordModel
    {
        public string OperatorName { get; set; }
        public FlowCode Status { get; set; }
        public string StatusDes { get{ return Status.GetDescription(); } }
        public string UpdateTime { get; set; }
        public string Reason { get; set; }
        public string Operation { get; set; }
        public ChangeType ChangeType { get; set; }
    }
}