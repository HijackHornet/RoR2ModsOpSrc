﻿namespace Hj
{
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using UnityEngine;
    using J = Newtonsoft.Json.JsonPropertyAttribute;
    using static Hj.HjUpdaterAPI;

    internal class Package
    {
        #region Properties

        internal static Package[] packages = null;
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

        internal static bool EqualsDependecy(Package pk, System.Version version)
        {
            for (int i = 0; i < pk.Versions.Length; i++)
            {
                if (pk.Versions[i].VersionNumber == version)
                {
                    string[] a = pk.Versions[i].Dependencies.Where<string>(x => { return !x.Contains("HjUpdaterAPI"); }).OrderBy(y => y).ToArray();
                    string[] b = pk.Versions[0].Dependencies.Where<string>(x => { return !x.Contains("HjUpdaterAPI"); }).OrderBy(y => y).ToArray();

                    if (a.Length == b.Length)
                    {
                        bool eq = true;
                        for (int j = 0; j < a.Length; j++)
                        {
                            if (a[j] != b[j])
                            {
                                eq = false;
                            }
                        }
                        return eq;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            Debug.LogError(LOG + "Unable to find the dependencies for that mod version. Please contact the modder of that mod that you encountered this issue.");
            throw new Exception("");
        }

        internal static Package GetPackage(string modName)
        {
            if (packages != null)
            {
                for (int i = 0; i < packages.Length; i++)
                {
                    if (packages[i].FullName == modName)
                    {
                        return packages[i];
                    }
                }

                for (int i = 0; i < packages.Length; i++)
                {
                    if (packages[i].Name == modName)
                    {
                        return packages[i];
                    }
                }
            }
            Debug.LogWarning(LOG + "Couldnt find a package named '" + modName + "' in the package list. Update check will not be performed for that mod.");
            throw new Exception("");
        }

        #endregion Methods
    }

    internal class Version
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

    internal static class Serialize
    {
        #region Methods

        public static string ToJson(Package[] self) => JsonConvert.SerializeObject(self, Converter.Settings);

        #endregion Methods
    }
}