using NEL.NNS.lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NEL_FutureDao_API.Service
{
    public class RewardService
    {
        private MongoHelper mh { get; set; }

        public JArray applyBuyOrder(
            string userId,
            string token,
            string projId,
            string rewardId,
            string rewardName,
            string price,
            string amt,
            string connectorName,
            string connectorTel,
            string connectorEmail,
            string connectorMessage
            )
        {

            
            return null;
        }
        public JArray queryBuyOrder(string userId, string token)
        {
            return null;
        }

    }
}
