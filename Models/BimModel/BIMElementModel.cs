using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TS.Core.Domain.Comments;

namespace TS.Web.Models.BimModel
{
    public class BIMElementModel
    {
        public int Id { get; set; }

        public bool IsDelete { get; set; }

        public int CommentId { get; set; }

        /// <summary>
        /// 指示是批注还是构件
        /// </summary>
        public BIMType BIMElementType { get; set; }

        /// <summary>
        /// BIM的标识
        /// </summary>
        public string BIMId { get; set; }

        /// <summary>
        /// 构件的名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 批注的缩略图
        /// </summary>
        public string BimThumb { get; set; }

        public string ModelGuid { get; set; }

        public DateTime CreateTime { get; set; }
    }
}