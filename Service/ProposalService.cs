using NEL.NNS.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NEL_FutureDao_API.Service
{
    public class ProposalService
    {
        public MongoHelper mh { get; set; }
        public string dao_mongodbConnStr {get;set;}
        public string dao_mongodbDatabase { get; set; }

    }

    class VoteState
    {                               // 投票选择     投票状态
        public string Not = "0";    // 反对         投票中
        public string Yes = "1";    // 赞成         
    }
    class VoteKey
    {

    }

}
