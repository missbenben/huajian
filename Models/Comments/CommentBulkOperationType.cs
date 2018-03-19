using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.Comments
{
    public enum CommentBulkOperationType
    {
        [Description("设计公司已修复意见")]
        DesignManagerRepaired = 1,
        [Description("设计公司未修复意见")]
        DesignManagerUnrepaired = 2,
        [Description("审核人同意意见已修复")]
        CheckerAgreeRepaired = 3,
        [Description("审核人不同意意见已修复")]
        ChecherDisagreeRepaired = 4,
        [Description("复核人同意意见已修复")]
        ReviewerAgreeRepaired = 5,
        [Description("复核人不同意意见已修复")]
        ReviewerDisagreeRepaired = 6,
    }
}