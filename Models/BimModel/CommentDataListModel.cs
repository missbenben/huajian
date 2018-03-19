using System.Collections.Generic;
using TS.Core.Domain.Projects;

namespace TS.Web.Models.BimModel
{
    public class CommentDataListModel
    {
        public CommentDataListModel()
        {
            Comments = new List<CommentModel>();
        }

        public int CurrentModelVersionId { get; set; }
        public Role? CustomerRole { get; set; }
        public FlowCode ProfessionStatus { get; set; }

        public List<CommentModel> Comments { get; set; }
    }
}