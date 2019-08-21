using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NEL.Comm;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

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


        [HttpPost("demo")]
        public JsonResult demo(IFormFile file)
        {
            try
            {
                //var file = Request.Form.Files[0];
                string filename = file.FileName;
                using (FileStream fs = System.IO.File.Create(filename))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }
                return Json(toRes("", filename));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
                return Json(toFail(ex));
            }
        }

        // 
        [HttpPost("testnet")]
        public JsonResult uploadTestnet()
        {
            try
            {
                var file = Request.Form.Files[0];
                using (var stream = file.OpenReadStream())
                {
                    var fileName = file.FileName.toRandomFileName().toTemp();
                    var ossUrl = apiTest.PutTestStream(fileName, stream);
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