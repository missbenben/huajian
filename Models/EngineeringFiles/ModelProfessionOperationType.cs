using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.EngineeringFiles
{
    public enum ModelProfessionOperationType
    {
        /// <summary>
        /// 审核人申请复核
        /// </summary>
        [Description("申请复核")]
        CheckerApplyForReview = 1,
        /// <summary>
        /// 复核人退回重审
        /// </summary>
        [Description("退回重审")]
        ReviewerSendBackCheckerApply = 2,
        /// <summary>
        /// 复核人通过审核人提交
        /// </summary>
        [Description("通过提交")]
        ReviewerPassCheckerApply = 3,
        /// <summary>
        /// 复核人处理完设计公司驳回的意见，提交给项目经理
        /// </summary>
        [Description("已处理设计公司驳回意见")]
        ReviewerHandledDesignManagerReject = 4,
    }
}