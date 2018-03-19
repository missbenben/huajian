using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TS.Web.Models.SuperAdmin
{
    public class InformationManagerModel
    {
        public InformationManagerModel()
        {
            CommentProfessions= new List<DictionaryModel>();
            DrawingCatalogs = new List<DictionaryModel>();
            DrawingProfessions = new List<DictionaryModel>();
            CommentLevels = new List<DictionaryModel>();
            CommentTypes = new List<CommentTypeModel>();
        }

        public List<DictionaryModel> CommentProfessions { get; set; }
        public List<DictionaryModel> DrawingCatalogs { get; set; }
        public List<DictionaryModel> DrawingProfessions { get; set; }
        public List<DictionaryModel> CommentLevels { get; set; }
        public List<CommentTypeModel> CommentTypes { get; set; }

        public List<SelectListItem> AvaliableBaseCommentTypes { get;set;}

        public class DictionaryModel
        {
            public int DictionaryId { get; set; }
            public int ParentId { get; set; }
            public string FirstSystemName { get; set; }
            public string SecondSystemName { get; set; }
            public string DisplayName { get; set; }
            public string ExtraValue1 { get; set; }
            public string ExtraValue2 { get; set; }
            public string ExtraValue3 { get; set; }
            public DictionaryType Type { get; set; }
            public BaseCommentType? BaseCommentType { get; set; }
        }
    }
}