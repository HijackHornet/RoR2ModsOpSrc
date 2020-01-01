using System;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace HjUpdaterAPI
{
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
}