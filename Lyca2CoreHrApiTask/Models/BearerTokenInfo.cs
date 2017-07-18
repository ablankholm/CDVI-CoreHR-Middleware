using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyca2CoreHrApiTask.Models
{
    public class BearerTokenInfo
    {
        public BearerToken  Token { get; }  = new BearerToken();
        public DateTime     Issued { get; } = DateTime.MinValue;

        public BearerTokenInfo()
        {
        }

        public BearerTokenInfo(BearerToken token, DateTime issued)
        {
            Token   = token;
            Issued  = issued;
        }


        public bool IsExpired()
        {
            return (DateTime.Now >= Issued.AddSeconds(Token.expires_in));
        }

        public bool WillExpireWithin(int seconds)
        {
            return (DateTime.Now.AddSeconds(seconds) >= Issued.AddSeconds(Token.expires_in));
        }
    }
}
