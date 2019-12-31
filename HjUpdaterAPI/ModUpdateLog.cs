using Newtonsoft.Json;

namespace HjUpdaterAPI
{
    internal class ModUpdateLog
    {
        public string packageName { get; set; }

        [JsonIgnore]
        public System.Version lastVersion { get; set; }

        [JsonIgnore]
        public System.Version newVersion { get; set; }

        [JsonPropertyAttribute("lastVersion")]
        public string _lastVersionString => lastVersion.ToString();

        [JsonPropertyAttribute("newVersion")]
        public string _newVersionString => newVersion.ToString();

        public ModUpdateLog(string packageName, System.Version lastVersion, System.Version newVersion)
        {
            this.packageName = packageName;
            this.lastVersion = lastVersion;
            this.newVersion = newVersion;
        }
    }
}