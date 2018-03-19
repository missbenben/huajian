using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TS.Web.Models.EngineeringFiles
{
    public class ModelEvaluateModel
    {
        public ModelEvaluateModel()
        {
            AvailableEngineering = new List<SelectListItem>();
        }
        public string ProjectName { get; set; }
        public List<SelectListItem> AvailableEngineering { get; set; }

        public class ProfessionEvaluateModel
        {
            public string ProfessionName { get; set; }
            public string Grade { get; set; }
            public string CheckerName { get; set; }
            public string ReviewerName { get; set; }
            public string Result { get; set; }
        }

        public class EChartsCommentLevel
        {
            public EChartsCommentLevel()
            {
                Comments = new List<EchartsProfession>();
            }
            public string CommentType { get; set; }
            public List<EchartsProfession> Comments { get; set; }
        }

        public class EchartsProfession
        {
            public string Profession { get; set; }
            public int Amount { get; set; }
        }
    }
}