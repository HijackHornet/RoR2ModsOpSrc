using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HjUpdaterAPI
{
    public static class Serialize
    {
        #region Methods

        public static string ToJson(Package[] self) => JsonConvert.SerializeObject(self, Converter.Settings);

        #endregion Methods
    }
}
