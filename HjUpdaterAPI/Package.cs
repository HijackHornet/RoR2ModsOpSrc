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
}