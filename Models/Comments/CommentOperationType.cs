using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.Comments
{
    public enum AduitCommentOperationType
    {
        [Description("审核人作废意见")]
        Pre_AuditDeleteComment = 10,
        [Description("作废意见")]
        DeleteComment = 1,
        [Description("同意意见已修复")]
        AgreeRepaired = 2,
        [Description("不同意意见已修复")]
        DisagreeRepaired = 3,
        [Description("撤销作废")]
        Cancel = 4,
        [Description("撤销")]
        Back = 5,
    }

    public enum ReAduitCommentOperationType
    {
        [Description("同意审查人的完整性意见")]
        Pre_AgreeComment = 10,
        [Description("驳回审查人的完整性意见")]
        Pre_DisAgreeComment = 20,
        [Description("撤销对审查人的完整性意见的操作")]
        Pre_Back = 30,
        [Description("复核人作废意见")]
        Pre_ReauditDeleteComment = 40,

        [Description("作废意见")]
        DeleteComment = 1,
        [Description("同意意见已修复")]
        AgreeRepaired = 2,
        [Description("不同意意见已修复")]
        DisagreeRepaired = 3,
        [Description("重审意见")]
        ReturnComment = 4,
        [Description("通过意见")]
        PassComment = 5,
        [Description("撤销作废意见")]
        CancelDelete = 6,
        [Description("同意设计公司驳回")]
        AgreeDesignReject = 7,
        [Description("不同意设计公司驳回")]
        DisagreeDesignReject = 8,
        [Description("撤销")]
        Back = 9,
    }

    public enum DesignManagerCommentOperationType
    {
        [Description("同意")]
        Agree = 1,
        [Description("驳回")]
        Return = 2,
        [Description("已修复")]
        Repaired = 3,
        [Description("未修复")]
        UnRepaired = 4,
        [Description("撤回")]
        Back = 5,          
    }
}