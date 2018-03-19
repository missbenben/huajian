using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.Comments
{
    public enum CommentType
    {
        [Description("所有")]
        All = 0,
        [Description("完整性意见")]
        Integrality = 1,
        [Description("专业性意见")]
        Profession = 2,
    }
}