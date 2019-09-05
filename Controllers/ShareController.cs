using Microsoft.AspNetCore.Mvc;
using NEL.Comm;
using NEL.NNS.lib;
using NEL_FutureDao_API.lib;
using Newtonsoft.Json.Linq;
using System;

namespace NEL_FutureDao_API.Controllers
{
    [Route("api/[controller]")]
    public class ShareController : Controller
    {
        [HttpGet("{projId}/{net}/{type}")]
        public void Get(string projId, int net, int type)
        {
            try
            {
                var res = GetShareReturnHtml(projId, net, type); ;
                Response.ContentType = "text/html; charset=UTF-8";
                Response.Body.Write(res.toBytes());
                Response.Body.Flush();
            } catch (Exception ex)
            {
                string errMsg = "Internal error:" + ex.Message;
                Response.Body.Write(errMsg.toBytes());
                Response.Body.Flush();
            }
        }

        private string GetShareReturnHtml(string projId, int net, int type)
        {
            getProjInfo(projId, net, out string projBrief, out string projConverUrl);
            var callback = getCallback(projId, net);
            //
            if (type == 1)
            {
                return string.Format(shareReturnHtmlFmtOg, "summary_large_image", "FutureDao", projBrief, projConverUrl, callback);
            }
            return string.Format(shareReturnHtmlFmt, "website", "FutureDao", projBrief, projConverUrl, callback);

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
            + "\r\n<meta data-rh=\"true\" name=\"twitter:card\" content=\"{0}\" />"
            + "\r\n<meta data-rh=\"true\" name=\"twitter:title\" content=\"{1}\" />"
            + "\r\n<meta data-rh=\"true\" name=\"twitter:description\" content=\"{2}\" />"
            + "\r\n<meta data-rh=\"true\" name=\"twitter:image\" content=\"{3}\" />"
            + "\r\n</head>"
            + "\r\n<script>"
            + "\r\nwindow.location.href = decodeURIComponent(decodeURI(\"{4}\"));"
            + "\r\n</script>"
            + "\r\n<body>"
            + "\r\n</body>"
            + "\r\n</html>"
        ;
        private string shareReturnHtmlFmtOg = ""
            + "<!DOCTYPE html>"
            + "\r\n<html lang=\"en\">"
            + "\r\n<head>"
            + "\r\n<meta data-rh=\"true\" property=\"og:type\" content=\"{0}\" />"
            + "\r\n<meta data-rh=\"true\" property=\"og:title\" content=\"{1}\" />"
            + "\r\n<meta data-rh=\"true\" property=\"og:description\" content=\"{2}\" />"
            + "\r\n<meta data-rh=\"true\" property=\"og:image\" content=\"{3}\" />"
            + "\r\n</head>"
            + "\r\n<script>"
            + "\r\nwindow.location.href = decodeURIComponent(decodeURI(\"{4}\"));"
            + "\r\n</script>"
            + "\r\n<body>"
            + "\r\n</body>"
            + "\r\n</html>"
        ;
        private string testnetShareReturnCallbackUrlPrefix = "https://futuredao.nel.group/test/projectinfo/";
        private string mainnetShareReturnCallbackUrlPrefix = "https://futuredao.nel.group/projectinfo/";

    }
    static class ShareHelper
    {
        public static byte[] toBytes(this string data)
        {
            return System.Text.Encoding.UTF8.GetBytes(data);
        }
    }
}