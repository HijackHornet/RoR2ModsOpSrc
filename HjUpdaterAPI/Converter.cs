using Newtonsoft.Json;

namespace HjUpdaterAPI
{
    internal class Converter
    {
        #region Fields

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None
        };

        #endregion Fields
    }
}