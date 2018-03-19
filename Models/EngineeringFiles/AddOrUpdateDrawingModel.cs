using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TS.Web.Models.EngineeringFiles
{
    public class AddOrUpdateDrawingModel
    {
        public AddOrUpdateDrawingModel()
        {
            AvaliableDrawingCatalogs = new List<SelectListItem>();
            AvaliableDrawingProfessions = new List<SelectListItem>();
        }

        public List<SelectListItem> AvaliableDrawingCatalogs { get; set; }
        public List<SelectListItem> AvaliableDrawingProfessions { get; set; }
    }
}