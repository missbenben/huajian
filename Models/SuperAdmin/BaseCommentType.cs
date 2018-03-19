using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.SuperAdmin
{
    public enum BaseCommentType
    {
        /// <summary>
        /// 完整性意见
        /// </summary>
        [Description("完整性意见")]
        IntegralityCommentType = 1,
        /// <summary>
        /// 普通意见
        /// </summary>
        [Description("分专业意见")]
        DesignCommentType = 2,
    }
}