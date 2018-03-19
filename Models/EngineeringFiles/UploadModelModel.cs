using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TS.Web.Models.EngineeringFiles
{
    public class UploadModelModel
    {
        public int EngineeringFileId { get; set; }

        public int DrawingCatalog { get; set; }

        public int DrawingProfession { get; set; }

        public int DrawingSeriesId { get; set; }

        public string Description { get; set; }
    }
}