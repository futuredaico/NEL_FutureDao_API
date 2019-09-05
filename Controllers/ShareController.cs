using Microsoft.AspNetCore.Mvc;
using NEL.Comm;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;

namespace NEL_FutureDao_API.Controllers
{
    [Route("api/[controller]")]
    public class ShareController : Controller
    {
        [HttpGet("{projId}/{net}")]
        public ActionResult<string> Get(string projId, int net)
        {
            //return GetShareReturnHtml(projId, net);
            var res = GetShareReturnHtml(projId, net);
            Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(res));
            Response.Body.Flush();
            return "succ";
        }

        private string GetShareReturnHtml(string projId, int net)
        {
            var callback = getCallback(projId, net);
            getProjInfo(projId, net, out string projBrief, out string projConverUrl);
            return string.Format(shareReturnHtmlFmt, "summary_large_image", "FutureDao", projBrief, projConverUrl, callback, methodStr);

        }
        private void getProjInfo(string projId, int net, out string projBrief, out string projConverUrl)
        {
            //projName = "";
            projBrief = "";
            projConverUrl = "";
            var mh = Api.getTestApi().getMongoDB();
            var mongodbConnStr = mh.dao_mongodbConnStr_testnet;
            var mongodbDatabase = mh.dao_mongodbDatabase_testnet;
            if (net == 1)
            {
                mh = Api.getMainApi().getMongoDB();
                mongodbConnStr = mh.dao_mongodbConnStr_mainnet;
                mongodbDatabase = mh.dao_mongodbDatabase_mainnet;
            }
            projId = projId.toNormal();
            string findStr = new JObject { { "projId", projId } }.ToString();
            string fieldStr = MongoFieldHelper.toReturn(new string[] { "projName", "projBrief", "projConverUrl" }).ToString();
            var queryRes = mh.GetData(mongodbConnStr, mongodbDatabase, "daoProjInfo", findStr, fieldStr);
            if (queryRes.Count > 0)
            {
                //projName = queryRes[0]["projName"].ToString();
                projBrief = queryRes[0]["projBrief"].ToString();
                projConverUrl = queryRes[0]["projConverUrl"].ToString();
            }
        }
        private string getCallback(string projId, int net)
        {
            var callback = testnetShareReturnCallbackUrlPrefix;
            if (net == 1) callback = mainnetShareReturnCallbackUrlPrefix;
            return callback + projId;
        }
        private string shareReturnHtmlFmt = ""
            + "<!DOCTYPE html>"
            + "\r\n<html lang=\"en\">"
            + "\r\n<head>"
            + "\r\n<meta name=\"twitter:card\" content=\"{0}\" />"
            + "\r\n<meta name=\"twitter:title\" content=\"{1}\" />"
            + "\r\n<meta name=\"twitter:description\" content=\"{2}\" />"
            + "\r\n<meta name=\"twitter:image.src\" content=\"{3}\" />"
            + "\r\n</head>"
            + "\r\n<script>"
            + "\r\nwindow.location.href = decodeURIComponent(decodeURI(\"{4}\"));"
            //+ "\r\n{5}"
            + "\r\n</script>"
            + "\r\n<body>"
            + "\r\n</body>"
            + "\r\n</html>"
        ;
        private string methodStr = ""
            + "\r\nfunction GetQueryString(name) {"
            + "\r\n     var reg = new RegExp(\"(^| &)\" + name + \"=([^&]*)(&|$)\");"
            + "\r\n     var r = window.location.search.substr(1).match(reg);"
            + "\r\n     if (r != null) return decodeURI(r[2]);"
            + "\r\n         return null;"
            + "\r\n}"
            ;
        private string testnetShareReturnCallbackUrlPrefix = "https://futuredao.nel.group/test/projectinfo/";
        private string mainnetShareReturnCallbackUrlPrefix = "https://futuredao.nel.group/projectinfo/";

    }
}