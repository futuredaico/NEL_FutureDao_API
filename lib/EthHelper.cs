using Nethereum.Signer;
using Nethereum.Util;
using System.Text;

namespace NEL_FutureDao_API.lib
{
    public class EthHelper
    {
        public static bool verify(string dataStr, string signStr)
        {
            try
            {
                var sign = EthECDSASignatureFactory.ExtractECDSASignature(signStr);
                var dataHash = new Sha3Keccack().CalculateHash(Encoding.UTF8.GetBytes(dataStr));
                var pubKey = EthECKey.RecoverFromSignature(sign, dataHash);
                return pubKey.Verify(dataHash, sign);
            }catch
            {
                return false;
            }
            
        }
    }
}
