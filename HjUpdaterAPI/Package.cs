using Newtonsoft.Json;
using System;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace HjUpdaterAPI
{
    public class Package
    {
        #region Properties

        [J("date_created")] public DateTimeOffset DateCreated { get; set; }

        [J("date_updated")] public DateTimeOffset DateUpdated { get; set; }

        [J("full_name")] public string FullName { get; set; }

        [J("is_deprecated")] public bool IsDeprecated { get; set; }

        [J("is_pinned")] public bool IsPinned { get; set; }

        [J("name")] public string Name { get; set; }

        [J("owner")] public string Owner { get; set; }

        [J("package_url")] public Uri PackageUrl { get; set; }

        [J("rating_score")] public long RatingScore { get; set; }

        [J("uuid4")] public Guid Uuid4 { get; set; }

        [J("versions")] public Version[] Versions { get; set; }

        #endregion Properties

        #region Methods

        public static Package[] FromJson(string json) => JsonConvert.DeserializeObject<Package[]>(json, Converter.Settings);

        #endregion Methods
    }

    public class Version
    {
        #region Properties

        [J("date_created")] public DateTimeOffset DateCreated { get; set; }

        [J("dependencies")] public string[] Dependencies { get; set; }

        [J("description")] public string Description { get; set; }

        [J("downloads")] public long Downloads { get; set; }

        [J("download_url")] public Uri DownloadUrl { get; set; }

        [J("full_name")] public string FullName { get; set; }

        [J("icon")] public Uri Icon { get; set; }

        [J("is_active")] public bool IsActive { get; set; }

        [J("name")] public string Name { get; set; }

        [J("uuid4")] public Guid Uuid4 { get; set; }

        [J("version_number")] public System.Version VersionNumber { get; set; }

        [J("website_url")] public string WebsiteUrl { get; set; }

        #endregion Properties
    }

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

    public static class Serialize
    {
        #region Methods

        public static string ToJson(Package[] self) => JsonConvert.SerializeObject(self, Converter.Settings);

        #endregion Methods
    }
}