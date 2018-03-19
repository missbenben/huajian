using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TS.Core.Domain.Comments;
using TS.Core.Domain.Customers;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Projects;
using TS.Data.Extensions;

namespace TS.Web.Models.BimModel
{
    public class CommentModel
    {
        public bool MustOperate { get; set; }

        public int Id { get; set; }

        public int EngineeringFileCommentId { get; set; }

        public int ProfessionId { get; set; }

        public string Description { get; set; }

        public string Floor { get; set; }

        public int CommentTypeId { get; set; }

        public string CommentType { get; set; }

        public int ModelProfessionId { get; set; }

        public DateTime CreateTime { get; set; }

        public int CreatorId { get; set; }

        public int UpdatorId { get; set; }

        public bool? IsRepaired { get; set; }

        public FlowCode Status { get; set; }
        public string StatusDisplayName => Status.GetDescription();

        public List<BIMElementModel> BimElements { get; set; }

        public Customer Creator { get; set; }

        public Customer Updator { get; set; }

        public Comments.CommentDetailModel.CommentButtom commentButtom { get; set; }

        public RejectModel RejectDetail { get; set; }
    }
}