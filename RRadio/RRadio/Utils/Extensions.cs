using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ronnrein.RRadio.Utils {
    public static class Extensions {

        public static string ToSafeString(this JToken token) {
            return token == null ? "" : token.ToString();
        }

    }
}
