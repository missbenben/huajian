using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace TS.Web.Models.Projects
{
    public enum ProjectPageType
    {
        [Description("我管理的项目")]
        ManageredProject,       
        [Description("我审查的项目")]
        CheckedProject,
        [Description("我复核的项目")]
        ReviewedProject,
        [Description("已归档的项目")]
        FiledProject,
    }
}