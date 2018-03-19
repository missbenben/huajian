using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Schema;
using TS.Core.Domain.EngineeringFiles;
using TS.Core.Domain.Projects;
using TS.Service.BusinessLogic.BIMModelOperation;
using TS.Service.DataAccess.Customers;
using TS.Service.DataAccess.Dictionaries;
using TS.Service.DataAccess.EngineeringFiles;
using TS.Web.Helper;
using TS.Web.Models.EngineeringFiles;

namespace TS.Web.Controllers
{
    public class BlobFileController : BaseController
    {
        #region Fields

        private readonly FileVersionService _fileVersionService;
        private readonly EngineeringFileService _engineeringFileService;

        #endregion

        public BlobFileController()
        {
            _fileVersionService = new FileVersionService();
            _engineeringFileService = new EngineeringFileService();
        }

        #region Utilities
        /// <summary>
        /// 预置用户登录和用户角色状态的Cookie
        /// </summary>
        private void SetCookieAndCurrentField()
        {
            var customer = new CustomerService().Get(c => c.Id == 1);
            SetTestCurrentCustomer(customer);//伪造用户登录cookie
        }

        #endregion

        #region Methods

        /// <summary>
        /// 获取添加图纸模态框的分页数据
        /// </summary>
        /// <returns></returns>
        public ActionResult AddOrUpdateDrawing()
        {
            var model = new AddOrUpdateDrawingModel();
            //添加可选的图纸专业和类目
            foreach (var item in DictionaryService.DrawingCatalogDictionary)
            {
                model.AvaliableDrawingCatalogs.Add(new SelectListItem()
                {
                    Text = item.DisplayName,
                    Value = item.Id.ToString()
                });
            }
            foreach (var item in DictionaryService.DrawingProfessionDictionary)
            {
                model.AvaliableDrawingProfessions.Add(new SelectListItem()
                {
                    Text = item.DisplayName,
                    Value = item.Id.ToString()
                });
            }

            return View(model);
        }

        /// <summary>
        /// 新建图纸系列，并更新第一个版本图纸
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateDrawingSeries(FormCollection form)
        {
            try
            {
                int engineeringId = int.Parse(form["engineeringId"]);
                int drawingCatalogValue = int.Parse(form["drawingCatalogValue"]);
                int drawingProfessionValue = int.Parse(form["drawingProfessionValue"]);
                var blobFile = Request.Files["blobFile"];
                string drawingSeriesName = form["drawingSeriesName"];
                string fileSize = form["fileSize"];
                string fileDescription = form["fileDescription"];

                if (_engineeringFileService.Get(ef => ef.FileName == drawingSeriesName && ef.EngineeringId == engineeringId && ef.DrawingProfessionId == drawingProfessionValue) != null)
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "图纸名称重复"
                    });
                }

                if (blobFile != null && blobFile.ContentLength > 0)
                {
                    var parameter = new UploadProjectFileParameterType();

                    var engineeringFile = new EngineeringFile();
                    engineeringFile.DrawingProfessionId = drawingProfessionValue;
                    engineeringFile.DrawingCatalogId = drawingCatalogValue;
                    engineeringFile.FileType = FileType.Drawing;
                    //更新文件版本后来纪录最新的文件url
                    engineeringFile.Description = fileDescription;
                    engineeringFile.EngineeringId = engineeringId;
                    engineeringFile.FileName = drawingSeriesName;
                    engineeringFile.IsDistribution = false;
                    engineeringFile.Status = FlowCode.Pre_DesignCompany_Uploaded;
                    engineeringFile.UpLoadTime = DateTime.Now;
                    engineeringFile.UploaderId = CurrentCustomer.Id;
                    new EngineeringFileService().Insert(engineeringFile);

                    parameter.Description = fileDescription;
                    parameter.FileNameKey = "blobFile";
                    parameter.FileNameValue = blobFile.FileName;
                    parameter.UploadStream = blobFile.InputStream;
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("versionNo", "1"));
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("fileSize", fileSize));
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("drawingProfession", engineeringFile.DrawingProfessionId.ToString()));
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("drawingCatalog", engineeringFile.DrawingCatalogId.ToString()));

                    FileVersion currentFile = new FileVersion();
                    DesignCompanyProjectManager designCompanyProjectManager = new DesignCompanyProjectManager(CurrentCustomer, engineeringFile, currentFile);
                    designCompanyProjectManager.UploadBlob(parameter);

                    return Json(new
                    {
                        Result = true,
                        Message = "上传成功"
                    });
                }

                return Json(new
                {
                    Result = false,
                    Message = "表单数据异常"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Result = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 更新已存在的图纸系列版本
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UpdateDrawingSeries(FormCollection form)
        {
            try
            {
                int engineeringFileId = int.Parse(form["engineeringfileid"]);
                int drawingCatalogId = int.Parse(form["drawingCatalogValue"]);
                int drawingProfessionId = int.Parse(form["drawingProfessionValue"]);
                var blobFile = Request.Files["blobFile"];
                string drawingSeriesName = form["drawingSeriesName"];
                string fileSize = form["fileSize"];
                string fileDescription = form["fileDescription"];

                var engineeringFile = _engineeringFileService.Get(ef => ef.Id == engineeringFileId);
                if (engineeringFile == null)
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "未找到项目文件"
                    });
                }

                if (engineeringFile.FileName != drawingSeriesName && _engineeringFileService.Get(ef => ef.FileName == drawingSeriesName && ef.EngineeringId == engineeringFile.Engineering.Id && ef.DrawingProfessionId == drawingProfessionId) != null)
                {
                    return Json(new
                    {
                        Result = false,
                        Message = "图纸名称重复"
                    });
                }

                //更新图纸系列
                engineeringFile.FileName = drawingSeriesName;
                engineeringFile.DrawingCatalogId = drawingCatalogId;
                engineeringFile.DrawingProfessionId = drawingProfessionId;
                _engineeringFileService.Update(engineeringFile);

                if (blobFile != null && blobFile.ContentLength > 0)
                {
                    var parameter = new UploadProjectFileParameterType();

                    parameter.Description = fileDescription;
                    parameter.FileNameKey = "blobFile";
                    parameter.FileNameValue = blobFile.FileName;
                    parameter.UploadStream = blobFile.InputStream;
                    var lastestDrawing = engineeringFile.FileVersions.OrderByDescending(fv => fv.UpLoadeTime).ToList()
                        .FirstOrDefault();
                    string versionNo = lastestDrawing == null ? "1" : (int.Parse(lastestDrawing.VersionNo) + 1).ToString();
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("versionNo", versionNo));
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("fileSize", fileSize));
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("drawingProfession", engineeringFile.DrawingProfessionId.ToString()));
                    parameter.PostParameters.Add(new KeyValuePair<string, string>("drawingCatalog", engineeringFile.DrawingCatalogId.ToString()));

                    FileVersion currentFile = new FileVersion();
                    DesignCompanyProjectManager designCompanyProjectManager = new DesignCompanyProjectManager(CurrentCustomer, engineeringFile, currentFile);
                    designCompanyProjectManager.UploadBlob(parameter);

                    return Json(new
                    {
                        Result = true,
                        Message = "更新，并上传成功"
                    });
                }

                return Json(new
                {
                    Result = true,
                    Message = "更新成功"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Result = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 查看Blob已上传文件的目录
        /// </summary>
        /// <returns></returns>
        public ActionResult GetBlobList()
        {
            string containername = ConfigurationManager.AppSettings["ContainerName"].ToString();
            var container = AzureBlobHelper.GetContainer(containername);

            StringBuilder sb = new StringBuilder();
            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    sb = sb.AppendFormat("Block blob of length {0}: {1}\n", blob.Properties.Length, blob.Uri);

                }
                else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob pageBlob = (CloudPageBlob)item;

                    sb = sb.AppendFormat("Page blob of length {0}: {1}\n", pageBlob.Properties.Length, pageBlob.Uri);

                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory directory = (CloudBlobDirectory)item;

                    sb = sb.AppendFormat("Directory: {0}\n", directory.Uri);
                }
            }

            return Content(sb.ToString());
        }

        /// <summary>
        /// 查看BlobFile内容
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public ActionResult ViewBlob(string blobName)
        {
            if (blobName == "")
            {
                return Content("无内容");
            }

            ViewBag.FileUrl = AzureBlobHelper.GetSAS(blobName);
            //ViewBag.FileType = blobName.Split('.').LastOrDefault().ToLower() == "pdf" ? "pdf": "img";

            return View();
            //可以下载文本形式
            //string text;
            //using (var memoryStream = new MemoryStream())
            //{
            //    blockBlob2.DownloadToStream(memoryStream);
            //    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            //}
        }

        /// <summary>
        /// 删除blobFile
        /// </summary>
        /// <param name="drawingBlobId"></param>
        /// <returns></returns>
        public ActionResult DeleteBlob(int drawingBlobId)
        {
            var drawingFile = _fileVersionService.Get(f => f.Id == drawingBlobId);

            if (drawingFile == null)
            {
                return Json(new
                {
                    result = false,
                    errmsg = "文件未找到"
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                string containername = ConfigurationManager.AppSettings["ContainerName"].ToString();
                var container = AzureBlobHelper.GetContainer(containername);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(drawingFile.BIMFileId);

                // Delete the blob.
                blockBlob.Delete();
                _fileVersionService.Delete(drawingFile);

                return Json(new
                {
                    result = true,
                    errmsg = "已删除"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    result = true,
                    errmsg = $"已删除，无需再删除:{ex.Message}"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

    }
}