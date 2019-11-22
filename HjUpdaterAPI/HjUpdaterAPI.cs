﻿namespace Hj
{
    using BepInEx;
    using BepInEx.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Networking;
    using J = Newtonsoft.Json.JsonPropertyAttribute;

    [BepInPlugin("com.hijackhornet.modautoupdaterapi", "HjUpdaterAPI", "1.0.0")]
    public class HjUpdaterAPI : BaseUnityPlugin
    {
        #region Constants

        internal const string BASEAPIURL = "thunderstore.io/api/v1";
        private const string BACKUPFOLDER = "BackupMods";
        private const string MODFOLDERCONTAINER = "HijackHornet-HjUpdaterAPI";
        private const string LOG = "[HjUpdaterAPI] ";

        #endregion Constants

        #region Fields

        public static ConfigEntry<bool> ConfigWrapperBlockUpdates { get; set; }
        public static byte UpdateAlways = 0;
        public static byte UpdateIfSameDependencyOnlyElseWarnOnly = 1;
        public static byte UpdateIfSameDependencyOnlyElseWarnAndDeactivate = 2;
        public static byte WarnOnly = 3;
        public static byte WarnAndDeactivate = 4;

        private static Queue<ModUpdateRequest> modRegisteredQueue = new Queue<ModUpdateRequest>();
        private static Queue<ModUpdateRequest> modRegisteredForLateUpdateQueue = new Queue<ModUpdateRequest>();

        private Package[] packages;

        private string workingDirectory;

        #endregion Fields

        #region Methods

        public static void RegisterForUpdate(string packageName, System.Version currentVersion, string assemblyFileLocation, byte flag = 1, List<string> otherFilesLocationRelativeToTheDll = null, bool modUseRuntimeRessourceLoading = false)
        {
            modRegisteredQueue.Enqueue(new ModUpdateRequest(packageName, currentVersion, assemblyFileLocation, flag, otherFilesLocationRelativeToTheDll, modUseRuntimeRessourceLoading));
        }

        private void Awake()
        {
            ConfigWrapperBlockUpdates = Config.Bind<bool>(
                "config",
                "blockupdates",
                false,
                "Change this to true if you want the updates to be blocked"
                );

            List<string> filesPath = new List<string>();
            filesPath.Add("Newtonsoft.Json.dll");

            RegisterForUpdate("HjUpdaterAPI", MetadataHelper.GetMetadata(this).Version, Assembly.GetExecutingAssembly().Location, UpdateAlways, filesPath);
            PerformAwake();
        }

        private void PerformAwake()
        {
            workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (new DirectoryInfo(workingDirectory).Name == "plugins")
            {
                Debug.LogWarning(LOG + "HjUpdaterAPI has been installed outside of a folder container. Re-installation...");
                Directory.CreateDirectory(Path.Combine(workingDirectory, MODFOLDERCONTAINER));
                File.Move(Path.Combine(workingDirectory, Assembly.GetExecutingAssembly().Location), Path.Combine(workingDirectory, MODFOLDERCONTAINER, "HjUpdaterAPI.dll"));
                bool depE = File.Exists(Path.Combine(workingDirectory, "Newtonsoft.Json.dll"));
                if (depE)
                {
                    File.Move(Path.Combine(workingDirectory, "Newtonsoft.Json.dll"), Path.Combine(workingDirectory, MODFOLDERCONTAINER, "Newtonsoft.Json.dll"));
                    workingDirectory = Path.Combine(workingDirectory, MODFOLDERCONTAINER);
                }
                else
                {
                    Debug.LogError(LOG + "Missing dependency Newtonsoft.Json.dll ! Please add this file to your BepinEx/plugin/" + MODFOLDERCONTAINER + " folder. This dll was included inside the archive of HjUpdaterAPI. Until the problem is resolved, no updates checks nor auto-updates will be performed.");
                    this.enabled = false;
                }
            }
            if (!Directory.Exists(Path.Combine(workingDirectory, BACKUPFOLDER)))
            {
                Directory.CreateDirectory(Path.Combine(workingDirectory, BACKUPFOLDER));
            }
        }

        internal void Start()
        {
            if ((modRegisteredQueue.Count > 0) && this.enabled && ConfigWrapperBlockUpdates.Value)
            {
                Debug.Log(LOG + "Checkink updates for " + modRegisteredQueue.Count + " mod(s)...");
                StartCoroutine(GetPackagesAndLaunchQueueProcess());
            }
        }

        private void OnDestroy()
        {
            if ((modRegisteredForLateUpdateQueue.Count > 0) && this.enabled)
            {
                Debug.Log(LOG + "Starting late mod update deployement.");
                PerformLateUpdates();
            }
        }

        private void PerformLateUpdates()
        {
            while (modRegisteredForLateUpdateQueue.Count > 0)
            {
                ModUpdateRequest modUpdateRequest = modRegisteredForLateUpdateQueue.Dequeue();
                DeployModUpdate(modUpdateRequest, Path.Combine(Path.GetTempPath(), modUpdateRequest.packageName + ".zip"));
            }
        }

        internal IEnumerator GetPackagesAndLaunchQueueProcess()
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(BASEAPIURL + "/package");
            webRequest.SetRequestHeader("accept", "application/json");
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.LogError(LOG + "The fetching of all packages failed with error : " + webRequest.error);
                yield break;
            }
            else
            {
                this.packages = Package.FromJson(webRequest.downloadHandler.text);
                if (this.packages.Length <= 0)
                {
                    Debug.LogError(LOG + "Package list seems empty. Please try to restart the game. If the error persist check out this mod page for details and contact infos.");
                    yield break;
                }
                else
                {
                    Debug.Log(LOG + "Mods list fetched.");
                    //Process Queue
                    while (modRegisteredQueue.Count > 0)
                    {
                        yield return ProcessQueueElement(modRegisteredQueue.Dequeue());
                    }
                    Debug.Log(LOG + "All registered mod have been checked for newer versions. This update process is now complete.");
                }
            }
        }

        internal IEnumerator ProcessQueueElement(ModUpdateRequest modUpdateRequest)
        {
            Package pk;
            try { pk = GetPackage(modUpdateRequest.packageName); }
            catch { yield break; }

            if ((pk.Versions == null) || pk.Versions.Length <= 0)
            {
                Debug.LogWarning(LOG + "Couldnt find versions for the package named '" + modUpdateRequest.packageName + "' in the package list. Update check will not be performed for that mod.");
                yield break;
            }
            else if (modUpdateRequest.currentVersion < pk.Versions[0].VersionNumber)
            {
                if (modUpdateRequest.flag == UpdateAlways)
                {
                    yield return PerformUpdate(modUpdateRequest, pk);
                }
                else if (modUpdateRequest.flag == UpdateIfSameDependencyOnlyElseWarnOnly)
                {
                    bool sameDependencies;
                    try { sameDependencies = EqualsDependecy(pk, modUpdateRequest.currentVersion); }
                    catch { yield break; }

                    if (!sameDependencies)
                    {
                        Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                        + System.Environment.NewLine + "However, the newest version uses a different dependency version. This mod specifie not to update automaticly in that case. Please go to " + pk.PackageUrl + " and update manually.");
                    }
                    else
                    {
                        yield return PerformUpdate(modUpdateRequest, pk);
                    }
                }
                else if (modUpdateRequest.flag == UpdateIfSameDependencyOnlyElseWarnAndDeactivate)
                {
                    if (EqualsDependecy(pk, modUpdateRequest.currentVersion))
                    {
                        yield return PerformUpdate(modUpdateRequest, pk);
                    }
                    else
                    {
                        Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                       + System.Environment.NewLine + "However, the newest version uses a different dependency version. This mod specifie not to update automaticly in that case and to deactivate the mod at the next game start. Please go to " + pk.PackageUrl + " and update manually.");

                        DeactivateMod(modUpdateRequest);
                    }
                }
                else if (modUpdateRequest.flag == WarnOnly)
                {
                    Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                        + System.Environment.NewLine + "This mod specifie not to update automaticly. Please go to " + pk.PackageUrl + " and update manually.");
                }
                else if (modUpdateRequest.flag == WarnAndDeactivate)
                {
                    Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                        + System.Environment.NewLine + "This mod specifie to deactivate the mod when you will close the game. Please go to " + pk.PackageUrl + " and reinstall manually.");
                    DeactivateMod(modUpdateRequest);
                }
            }
            else if (pk.IsDeprecated)
            {
                Debug.LogWarning(LOG + pk.Name + "Has been flagged as deprecated. This means it doesnt work anymore. The mod will re deactivate when you will close the game.");
                DeactivateMod(modUpdateRequest);
            }
            else
            {
                Debug.Log(LOG + "The package (mod) named '" + modUpdateRequest.packageName + "' (" + modUpdateRequest.currentVersion.ToString() + ") is up to date.");
            }
        }

        private IEnumerator PerformUpdate(ModUpdateRequest modUpdateRequest, Package pk)
        {
            Debug.Log(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ").");
            //Download Update
            Debug.Log(LOG + "Downloading package for update...");
            UnityWebRequest web = UnityWebRequest.Get(pk.Versions[0].DownloadUrl);
            yield return web.SendWebRequest(); ;
            if (web.isNetworkError || web.isHttpError)
            {
                Debug.LogError(LOG + "Download failed. Skipping this mod update for now.");
                yield break;
            }
            else
            {
                bool success = ByteArrayToFile(Path.Combine(Path.GetTempPath(), modUpdateRequest.packageName + ".zip"), web.downloadHandler.data);
                if (success)
                {
                    Debug.Log(LOG + "Download complete.");
                    byte a = DeactivateMod(modUpdateRequest);
                    //Backup and deploy
                    if (a == 1)
                    {
                        DeployModUpdate(modUpdateRequest, Path.Combine(Path.GetTempPath(), modUpdateRequest.packageName + ".zip"));
                    }
                    else if (a == 2)
                    {
                        Debug.Log(LOG + "This mod use some files on runtime. This means that the mod will be updated automaticly when you will exit the game.");
                    }
                }
            }
        }

        private void DeployModUpdate(ModUpdateRequest modUpdateRequest, string modZipFilePath)
        {
            ZipFile.ExtractToDirectory(modZipFilePath, Directory.GetParent(modUpdateRequest.currentDllFileLocation).FullName);
        }

        private byte DeactivateMod(ModUpdateRequest modUpdateRequest)
        {
            if (modUpdateRequest.modUseRuntimeRessourceLoading)
            {
                modUpdateRequest.modUseRuntimeRessourceLoading = false;
                modRegisteredForLateUpdateQueue.Enqueue(modUpdateRequest);
                return 2;
            }
            else
            {
                try
                {
                    DirectoryInfo backupFolder = Directory.CreateDirectory(Path.Combine(workingDirectory, BACKUPFOLDER,
                        modUpdateRequest.packageName + '-' + modUpdateRequest.currentVersion.ToString()
                        + "-" + DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year + "." + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second));

                    string path = Directory.GetParent(modUpdateRequest.currentDllFileLocation).FullName;
                    File.Move(modUpdateRequest.currentDllFileLocation, Path.Combine(backupFolder.FullName, Path.GetFileName(modUpdateRequest.currentDllFileLocation) + ".old"));
                    foreach (string fileLocation in modUpdateRequest.otherFilesLocationRelativeToTheDll)
                    {
                        if (File.Exists(Path.Combine(path, fileLocation)))
                        {
                            if (fileLocation.Contains(".."))
                            {
                                throw new Exception("One or multiple files used as ressource by the mod is in a parent folder to its assembly (dll). At the moment, Hj-UpdaterAPI isnt able to perform this type of update. Please make the update manually and contact this mod owner so that (s)he know (s)he uses Hj-UpdaterAPI outside of its defined limitations.");
                            }
                            Directory.Move(Path.Combine(path, fileLocation), Path.Combine(backupFolder.FullName, fileLocation + ".old"));
                        }
                    }
                    Debug.Log(LOG + modUpdateRequest.packageName + " has been updated to the latest version.");
                    return 1;
                }
                catch (Exception e)
                {
                    Debug.LogError(LOG + "An error occured during the deactivation process of the following mod : " + modUpdateRequest.packageName + '-' + modUpdateRequest.currentVersion.ToString()
                        + System.Environment.NewLine + "Details : " + e);
                    return 0;
                }
            }
        }

        private Package GetPackage(string modName)
        {
            for (int i = 0; i < packages.Length; i++)
            {
                if (packages[i].Name == modName)
                {
                    return packages[i];
                }
            }
            Debug.LogWarning(LOG + "Couldnt find a package named '" + modName + "' in the package list. Update check will not be performed for that mod.");
            throw new Exception("");
        }

        private bool EqualsDependecy(Package pk, System.Version version)
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
            Debug.LogError("Unable to find the dependencies for that mod version. Please contact the modder of that mod that you encountered this issue.");
            throw new Exception("");
        }

        private bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(LOG + "Error while writting the dowloaded file to the user temp folder. Details : " + e);
                return false;
            }
        }

        #endregion Methods

        internal class ModUpdateRequest
        {
            #region Fields

            private List<string> _otherFilesLocationRelativeToTheDll = null;

            #endregion Fields

            #region Properties

            public string currentDllFileLocation { get; set; }
            public System.Version currentVersion { get; set; }
            public byte flag { get; set; }
            public bool modUseRuntimeRessourceLoading { get; set; }

            public List<string> otherFilesLocationRelativeToTheDll
            {
                get => _otherFilesLocationRelativeToTheDll;
                set
                {
                    if (!value.Contains("README.md")) { value.Add("README.md"); }
                    if (!value.Contains("manifest.json")) { value.Add("manifest.json"); }
                    if (!value.Contains("icon.png")) { value.Add("icon.png"); }
                    _otherFilesLocationRelativeToTheDll = value;
                }
            }

            public string packageName { get; set; }

            #endregion Properties

            #region Constructors

            public ModUpdateRequest(string packageName, System.Version currentVersion, string currentDllFileLocation, byte flag, List<string> otherFilesLocationRelativeToTheDll, bool modUseRuntimeRessourceLoading)
            {
                this.packageName = packageName;
                this.currentVersion = currentVersion;
                this.currentDllFileLocation = currentDllFileLocation;
                this.otherFilesLocationRelativeToTheDll = otherFilesLocationRelativeToTheDll;
                this.flag = flag;
                this.modUseRuntimeRessourceLoading = modUseRuntimeRessourceLoading;
            }

            #endregion Constructors
        }
    }

    public partial class Package
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
    }

    public partial class Package
    {
        #region Methods

        public static Package[] FromJson(string json) => JsonConvert.DeserializeObject<Package[]>(json, Converter.Settings);

        #endregion Methods
    }

    public static class Serialize
    {
        #region Methods

        public static string ToJson(this Package[] self) => JsonConvert.SerializeObject(self, Converter.Settings);

        #endregion Methods
    }

    public partial class Version
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

    internal static class Converter
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