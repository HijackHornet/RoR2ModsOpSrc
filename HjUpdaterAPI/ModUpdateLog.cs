namespace Hj
{
    using Newtonsoft.Json;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using static Hj.HjUpdaterAPI;

    internal class ModUpdateLog
    {
        private static List<ModUpdateLog> oldModUpdateLogs = new List<ModUpdateLog>();
        private static List<ModUpdateLog> newModUpdateLogs = new List<ModUpdateLog>();
        public string packageName { get; set; }

        [JsonIgnore]
        public System.Version lastVersion { get; set; }

        [JsonIgnore]
        public System.Version newVersion { get; set; }

        [JsonPropertyAttribute("lastVersion")]
        public string _lastVersionString => lastVersion.ToString();

        [JsonPropertyAttribute("newVersion")]
        public string _newVersionString => newVersion.ToString();

        internal ModUpdateLog(string packageName, System.Version lastVersion, System.Version newVersion)
        {
            this.packageName = packageName;
            this.lastVersion = lastVersion;
            this.newVersion = newVersion;
        }

        //Parse the local file into List<ModUpdateLog> oldModUpdateLogs;
        internal static void readModUpdateLogFile()
        {
            var updateFileLocation = Path.Combine(workingDirectory, BACKUPFOLDER, "updateLog.json");
            if (File.Exists(updateFileLocation))
            {
                string jsonModUpdateLog = File.ReadAllText(updateFileLocation);
                try
                {
                    oldModUpdateLogs = JsonConvert.DeserializeObject<List<ModUpdateLog>>(jsonModUpdateLog, Converter.Settings);
                }
                catch (Exception e)
                {
                    Debug.Log(LOG + "ModUpdateLog contains incorrect formatting. Error :" + e);
                    File.Delete(updateFileLocation);
                }
            }
        }

        //Parse List<ModUpdateLog> newModUpdateLogs; and write it to the local file (overide)
        internal static void writeModUpdateLogFile()
        {
            var updateFileLocation = Path.Combine(workingDirectory, BACKUPFOLDER, "updateLog.json");
            string newJson = JsonConvert.SerializeObject(newModUpdateLogs, Converter.Settings);
            try
            {
                File.WriteAllText(updateFileLocation, newJson);
            }
            catch (Exception e)
            {
                Debug.Log(LOG + "Json file couldn't be written. Error :" + e);
            }
        }

        internal static bool checkIfSimilarUpdateAlreadyProceed(ModUpdateLog modUpdateLog)
        {
            return oldModUpdateLogs.Contains(modUpdateLog);
        }

        internal static void logModUpdate(ModUpdateLog modUpdateLog)
        {
            newModUpdateLogs.Add(modUpdateLog);
        }
    }
}