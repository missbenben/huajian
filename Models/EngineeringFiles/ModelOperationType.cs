using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.EngineeringFiles
{
    public enum BuildingModelOperationType
    {

        [Description("转发")]
        Transpond = 1,
        [Description("退回")]
        SendBack = 2,
        [Description("签收")]
        Sign = 3,
    }

    public enum DesignModelOperationType
    {
        [Description("提交")]
        Submit = 1,
        [Description("删除")]
        Delete = 2,
    }

    public enum CensorshipModelOperationType
    {
        [Description("签收")]
        Sign = 1,
        [Description("验收退回")]
        AcceptanceBack = 2,
        [Description("验收")]
        AcceptancePass = 3,
        [Description("发送")]
        Send = 4,
        [Description("审核通过")]
        AduitPass = 5,
        [Description("审核完成")]
        CurrrentAuditCompleted = 6,
    }
}