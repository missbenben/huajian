using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Data.Extensions;
using static TS.Web.Models.SuperAdmin.InformationManagerModel;

namespace TS.Web.Models.SuperAdmin
{
    public class CommentTypeModel
    {
        public CommentTypeModel()
        {
            SecondCommentTypes = new List<DictionaryModel>();
        }

        public int commentId { get; set; }
        public DictionaryType type { get; set; }
        public BaseCommentType BaseCommentType { get; set; }
        public string BaseCommentTypeDes { get { return BaseCommentType.GetDescription(); } }
        public string FirstCommentTypeName { get; set; }
 
        public List<DictionaryModel> SecondCommentTypes { get; set; }
    }
}