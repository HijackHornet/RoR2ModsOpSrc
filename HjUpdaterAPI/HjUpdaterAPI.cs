namespace Hj
{
    using BepInEx;
    using BepInEx.Configuration;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Debug = UnityEngine.Debug;

    [BepInPlugin(GUID, NAME, VERSION)]
    public class HjUpdaterAPI : BaseUnityPlugin
    {
        public const string
            NAME = "HjUpdaterAPI",
            GUID = "com.hijackhornet." + NAME,
            VERSION = "1.4.0";

        #region Constants

        internal const string BACKUPFOLDER = "BackupMods";
        internal const string LOG = "[HjUpdaterAPI] ";
        internal const string MODFOLDERCONTAINER = "HijackHornet-HjUpdaterAPI";

        #endregion Constants

        #region Fields

        internal static string workingDirectory;
        private static Queue<ModUpdateRequest> modRegisteredForLateUpdateQueue = new Queue<ModUpdateRequest>();
        private static Queue<ModUpdateRequest> modRegisteredQueue = new Queue<ModUpdateRequest>();

        public enum Flag
        {
            UpdateAlways,
            UpdateIfSameDependencyOnlyElseWarnOnly,
            UpdateIfSameDependencyOnlyElseWarnAndDeactivate,
            WarnOnly,
            WarnAndDeactivate
        };

        public static ConfigEntry<bool> ConfigDeactivateDeactivateonUpdate { get; set; }
        public static ConfigEntry<bool> ConfigDeactivateThis { get; set; }
        public static ConfigEntry<bool> ConfigDeactivateUpdateAlways { get; set; }
        public static ConfigEntry<bool> ConfigDeactivateUpdateIfSameDependencies { get; set; }
        public static ConfigEntry<bool> ConfigDeactivateUpdateIfSameDependenciesElseDeactivate { get; set; }
        public static ConfigEntry<bool> ConfigPerformDepreactedCheckAndRemove { get; set; }

        #endregion Fields

        #region Methods

        private void Awake()
        {
            //Config base
            ConfigDeactivateThis = Config.Bind<bool>(
                "config",
                "deactivate_completly",
                false,
                "Change this to true if you want to deactivate all kinds of update check."
                );
            ConfigPerformDepreactedCheckAndRemove = Config.Bind<bool>(
                "config",
                "deprecated_check",
                true,
                "Choose if you want deprecated (not working) mods to be deactivate if detected"
                );
            //Overwrites configs
            ConfigDeactivateUpdateAlways = Config.Bind<bool>(
                "Overwrite",
                "overwrite_update_always",
                false,
                "If true, all mods flaged as 'update always' will be replaced by warn only"
            );
            ConfigDeactivateUpdateIfSameDependencies = Config.Bind<bool>(
                "Overwrite",
                "overwrite_update_type_update_if_same_dependencies",
                false,
                "If true, all mods flaged as 'update if same dependencies else warn only' will be replaced by warn only"
            );
            ConfigDeactivateUpdateIfSameDependenciesElseDeactivate = Config.Bind<bool>(
                 "Overwrite",
                 "overwrite_update_type_update_if_same_dependencies_else_deactivate",
                 false,
                 "If true, all mods flaged as 'update if same dependencies else deactivate' will be replaced by warn only"
            );
            ConfigDeactivateDeactivateonUpdate = Config.Bind<bool>(
                "Overwrite",
                "overwrite_warn_and_deactivate",
                false,
                "If true, all mods flaged as 'warn and deactivate on update found' will be replaced by warn only"
            );

            List<string> filesPath = new List<string>();
            filesPath.Add("Newtonsoft.Json.dll");

            Register("HjUpdaterAPI", Flag.UpdateAlways, filesPath);
            PerformAwake();
        }

        internal void Start()
        {
            if ((modRegisteredQueue.Count > 0) && this.enabled && !ConfigDeactivateThis.Value)
            {
                ModUpdateLog.readModUpdateLogFile();
                Debug.Log(LOG + "Checking updates for " + modRegisteredQueue.Count + " mod(s)...");
                StartCoroutine(ThunderAPI.GetPackages(LaunchQueueProcess));
            }
        }

        private void OnDestroy()
        {
            if ((modRegisteredForLateUpdateQueue.Count > 0) && this.enabled)
            {
                Debug.Log(LOG + "Starting late mod update deployement.");
                PerformLateUpdates();
            }
            ModUpdateLog.writeModUpdateLogFile();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register(string packageName, Flag flag = Flag.UpdateIfSameDependencyOnlyElseWarnOnly, List<string> otherFilesLocationRelativeToTheDll = null, bool modUseRuntimeRessourceLoading = false)
        {
            StackFrame frame = new StackFrame(1);
            modRegisteredQueue.Enqueue(new ModUpdateRequest(packageName, MetadataHelper.GetMetadata(frame.GetMethod().DeclaringType).Version, Assembly.GetCallingAssembly().Location, ReturnFlagAccordingToConfig(flag), otherFilesLocationRelativeToTheDll, modUseRuntimeRessourceLoading));
        }

        internal void LaunchQueueProcess()
        {
            StartCoroutine(QueueProcess());
        }

        internal IEnumerator QueueProcess()
        {
            while (modRegisteredQueue.Count > 0)
            {
                yield return ProcessQueueElement(modRegisteredQueue.Dequeue());
            }
            Debug.Log(LOG + "All registered mod have been checked for newer versions. This update process is now complete.");
            ModUpdateLog.writeModUpdateLogFile();
        }

        internal IEnumerator ProcessQueueElement(ModUpdateRequest modUpdateRequest)
        {
            Package pk;
            try { pk = Package.GetPackage(modUpdateRequest.packageName); }
            catch { yield break; }

            if ((pk.Versions == null) || pk.Versions.Length <= 0)
            {
                Debug.LogWarning(LOG + "Couldnt find versions for the package named '" + modUpdateRequest.packageName + "' in the package list. Update check will not be performed for that mod.");
                yield break;
            }
            else if (modUpdateRequest.currentVersion < pk.Versions[0].VersionNumber)
            {
                ModUpdateLog mul = new ModUpdateLog(modUpdateRequest.packageName, modUpdateRequest.currentVersion, pk.Versions[0].VersionNumber);

                if (ModUpdateLog.checkIfSimilarUpdateAlreadyProceed(mul))
                {
                    Debug.LogWarning(LOG + "Similar update already proceed last time. Maybe the modder forgot to change the version number ?");
                }
                else
                {
                    if (modUpdateRequest.flag.Equals(Flag.UpdateAlways))
                    {
                        yield return StartPerformUpdate(modUpdateRequest, pk);
                    }
                    else if (modUpdateRequest.flag.Equals(Flag.UpdateIfSameDependencyOnlyElseWarnOnly))
                    {
                        bool sameDependencies;
                        try { sameDependencies = Package.EqualsDependecy(pk, modUpdateRequest.currentVersion); }
                        catch { yield break; }

                        if (!sameDependencies)
                        {
                            Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                            + System.Environment.NewLine + "However, the newest version uses a different dependency version. This mod specifie not to update automaticly in that case. Please go to " + pk.PackageUrl + " and update manually.");
                        }
                        else
                        {
                            yield return StartPerformUpdate(modUpdateRequest, pk);
                        }
                    }
                    else if (modUpdateRequest.flag.Equals(Flag.UpdateIfSameDependencyOnlyElseWarnAndDeactivate))
                    {
                        if (Package.EqualsDependecy(pk, modUpdateRequest.currentVersion))
                        {
                            yield return StartPerformUpdate(modUpdateRequest, pk);
                        }
                        else
                        {
                            Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                           + System.Environment.NewLine + "However, the newest version uses a different dependency version. This mod specifie not to update automaticly in that case and to deactivate the mod at the next game start. Please go to " + pk.PackageUrl + " and update manually.");

                            DeactivateMod(modUpdateRequest);
                        }
                    }
                    else if (modUpdateRequest.flag.Equals(Flag.WarnOnly))
                    {
                        Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                            + System.Environment.NewLine + "This mod specifie not to update automaticly. Please go to " + pk.PackageUrl + " and update manually.");
                    }
                    else if (modUpdateRequest.flag.Equals(Flag.WarnAndDeactivate))
                    {
                        Debug.LogWarning(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ")."
                            + System.Environment.NewLine + "This mod specifie to deactivate the mod when you will close the game. Please go to " + pk.PackageUrl + " and reinstall manually.");
                        DeactivateMod(modUpdateRequest);
                    }
                }
                ModUpdateLog.logModUpdate(mul);
            }
            else if (pk.IsDeprecated)
            {
                Debug.LogWarning(LOG + pk.Name + "Has been flagged as deprecated. This means it doesnt work anymore. The mod will be deactivate when you will close the game except if you specified otherwise in the config file.");
                if (ConfigPerformDepreactedCheckAndRemove.Value)
                {
                    DeactivateMod(modUpdateRequest);
                }
            }
            else
            {
                Debug.Log(LOG + "The package (mod) named '" + modUpdateRequest.packageName + "' (" + modUpdateRequest.currentVersion.ToString() + ") is up to date.");
            }
        }

        private static Flag ReturnFlagAccordingToConfig(Flag flag)
        {
            switch (flag)
            {
                case Flag.UpdateAlways when ConfigDeactivateUpdateAlways.Value:
                    return Flag.WarnOnly;
                case Flag.UpdateAlways:
                    return flag;
                case Flag.UpdateIfSameDependencyOnlyElseWarnOnly when ConfigDeactivateUpdateIfSameDependencies.Value:
                    return Flag.WarnOnly;
                case Flag.UpdateIfSameDependencyOnlyElseWarnOnly:
                    return flag;
                case Flag.UpdateIfSameDependencyOnlyElseWarnAndDeactivate when ConfigDeactivateUpdateIfSameDependenciesElseDeactivate.Value:
                    return Flag.WarnOnly;
                case Flag.UpdateIfSameDependencyOnlyElseWarnAndDeactivate:
                    return flag;
                case Flag.WarnAndDeactivate when ConfigDeactivateDeactivateonUpdate.Value:
                    return Flag.WarnOnly;
                case Flag.WarnAndDeactivate:
                    return flag;
                case Flag.WarnOnly:
                    return flag;
                default:
                    return flag;
            }
        }

        private byte DeactivateMod(ModUpdateRequest modUpdateRequest)
        {
            if (modUpdateRequest.modUseRuntimeRessourceLoading)
            {
                /*This is badly coded, but it was a shortcute i took. Basically if a mod is late update, we add it to another queue and
                 remove the flag so that we we use the perform update on the second queue at game closure, it doesnt block here again*/
                modUpdateRequest.modUseRuntimeRessourceLoading = false;
                modRegisteredForLateUpdateQueue.Enqueue(modUpdateRequest);
                return 2;
            }
            else
            {
                try
                {
                    DirectoryInfo backupFolder = Directory.CreateDirectory(Path.Combine(workingDirectory, BACKUPFOLDER,
                        $"{modUpdateRequest.packageName}-{modUpdateRequest.currentVersion}-{ DateTime.Now.Day}.{DateTime.Now.Month}.{DateTime.Now.Year}.{DateTime.Now.Hour}.{DateTime.Now.Minute}.{DateTime.Now.Second}"));

                    string path = Directory.GetParent(modUpdateRequest.currentDllFileLocation).FullName;
                    Debug.Log(modUpdateRequest.currentDllFileLocation);
                    File.Move(modUpdateRequest.currentDllFileLocation, Path.Combine(backupFolder.FullName, Path.GetFileName(modUpdateRequest.currentDllFileLocation) + ".old"));
                    foreach (string fileLocation in modUpdateRequest.otherFilesLocationRelativeToTheDll)
                    {
                        if (File.Exists(Path.Combine(path, fileLocation)))
                        {
                            Debug.Log(Path.Combine(path, fileLocation));
                            if (fileLocation.Contains(".."))
                            {
                                throw new Exception("One or multiple files used as ressource by the mod is in a parent folder to its assembly (dll). Hj-UpdaterAPI isnt able (for security reasons) to perform this type of update. Please make the update manually and contact this mod owner so that (s)he know (s)he uses Hj-UpdaterAPI outside of its defined limitations.");
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

        private void DeployModUpdate(ModUpdateRequest modUpdateRequest, string modZipFilePath)
        {
            ZipFile.ExtractToDirectory(modZipFilePath, Directory.GetParent(modUpdateRequest.currentDllFileLocation).FullName);
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

        private void PerformLateUpdates()
        {
            while (modRegisteredForLateUpdateQueue.Count > 0)
            {
                ModUpdateRequest modUpdateRequest = modRegisteredForLateUpdateQueue.Dequeue();
                DeactivateMod(modUpdateRequest);
                DeployModUpdate(modUpdateRequest, Path.Combine(Path.GetTempPath(), modUpdateRequest.packageName + ".zip"));
            }
        }

        private IEnumerator StartPerformUpdate(ModUpdateRequest modUpdateRequest, Package pk)
        {
            Debug.Log(LOG + "An update for " + modUpdateRequest.packageName + " is available. Current version(" + modUpdateRequest.currentVersion.ToString() + "). Newest version (" + pk.Versions[0].VersionNumber.ToString() + ").");
            yield return ThunderAPI.DownloadUpdate(modUpdateRequest, pk, PerformUpdate);
        }

        internal void PerformUpdate(ModUpdateRequest modUpdateRequest)
        {
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

        #endregion Methods
    }
}