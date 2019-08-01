using log4net;
using Microsoft.AspNetCore.Mvc;
using NEL.Comm;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;
using System;

namespace NEL_FutureDao_API.Controllers
{
    [Route("api/[controller]")]
    public class FileController : Controller
    {
        private Api api = Api.getTestApi();
        private ILog log = LogHelper.GetLogger(typeof(FileController));

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("testnet")]
        public JsonResult uploadTestnet()
        {
            try
            {
                var file = Request.Form.Files[0];
                using (var stream = file.OpenReadStream())
                {
                    var fileName = file.FileName.toRandomFileName();
                    var ossUrl = api.PutTestStream(fileName.toTemp(), stream);
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
                    var ossUrl = api.PutMainStream(fileName.toTemp(), stream);
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