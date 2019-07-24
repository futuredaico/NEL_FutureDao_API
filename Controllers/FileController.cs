using Microsoft.AspNetCore.Mvc;

namespace NEL_FutureDao_API.Controllers
{
    [Route("api/[controller]")]
    public class FileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("testnet")]
        public string uploadTestnet()
        {
            var file = Request.Form.Files[0];
            var oss = new OssHelper();
            using(var stream = file.OpenReadStream())
            {
                string cc = oss.PutStream(file.FileName, stream);
                return "fileUrl";
            }
        }

        [HttpPost("mainnet")]
        public void uploadMainnet()
        {
        }

    }
}