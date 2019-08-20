using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using NEL.Comm;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace NEL_FutureDao_API.Controllers
{
    [Route("api/[controller]")]
    public class FileController : Controller
    {
        private Api apiTest = Api.getTestApi();
        private Api apiMain = Api.getMainApi();
        private ILog log = LogHelper.GetLogger(typeof(FileController));

        public IActionResult Index()
        {
            return View();
        }

        #region snippet_UploadPhysical
        [HttpPost("testnet")]
        [DisableFormValueModelBinding]
        //[ValidateAntiForgeryToken]
        public async Task<JsonResult> UploadPhysical()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return Json(toFail(new Exception("not support type:"+ Request.ContentType)));
            }

            try
            {
                var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                1000000);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);
                var section = await reader.ReadNextSectionAsync();

                var contentDisposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
                var fileName = contentDisposition.FileName.Value.toRandomFileName();
                var stream = section.Body;
                var ossUrl = apiTest.PutTestStream(fileName.toTemp(), stream);
                return Json(toRes(ossUrl, fileName));
            } catch(Exception ex)
            {
                log.Error(ex.Message, ex);
                return Json(toFail(ex));
            }
        }
        #endregion

        // ***********************************************************
        [HttpPost("testneto")]
        public JsonResult uploadTestnet()
        {
            try
            {
                var file = Request.Form.Files[0];
                using (var stream = file.OpenReadStream())
                {
                    var fileName = file.FileName.toRandomFileName();
                    var ossUrl = apiTest.PutTestStream(fileName.toTemp(), stream);
                    return Json(toRes(ossUrl, fileName));
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                return Json(toFail(ex));
            }
        }

        [HttpPost("mainnet")]
        public JsonResult uploadMainnet()
        {
            try
            {
                var file = Request.Form.Files[0];
                using (var stream = file.OpenReadStream())
                {
                    var fileName = file.FileName.toRandomFileName();
                    var ossUrl = apiMain.PutMainStream(fileName.toTemp(), stream);
                    return Json(toRes(ossUrl, fileName));
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                return Json(toFail(ex));
            }
        }

        private JObject toRes(string ossUrl, string fileName)
        {
            if (!ossUrl.EndsWith("/")) ossUrl += "/";
            ossUrl += fileName;

            return new JObject {
                { "jsonrpc", "2.0" } ,
                { "id", "1" } ,
                { "result", ossUrl }
            };
        }
        private JObject toFail(Exception ex)
        {
            return new JObject {
                { "jsonrpc", "2.0" } ,
                { "id", "1" } ,
                { "error", ex.Message }
            };
        }
    }
}