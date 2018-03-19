using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.Comments
{
    public class CommentBulkOperationButtonModel
    {
        public FlowCode ModelStatus { get; set; }
        public FlowCode? ProfessionStatus { get; set; }
        public Role? CustomerRole { get; set; }
    }
}