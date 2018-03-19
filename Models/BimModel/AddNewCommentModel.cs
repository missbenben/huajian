using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Comments;

namespace TS.Web.Models.BimModel
{
    public class AddNewCommentModel
    {
        public AddNewCommentModel()
        {
            Comment = new Comment();
            BIMElements = new List<BIMElement>();
        }

        public int RoleId { get; set; }

        public Comment Comment { get; set; }

        public List<BIMElement> BIMElements { get; set; }

    }
}