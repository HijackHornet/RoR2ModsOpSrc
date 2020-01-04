namespace Hj
{
    using System;
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using static Hj.HjUpdaterAPI;

    internal class ThunderAPI
    {
        internal const string BASEAPIURL = "thunderstore.io/api/v1";

        internal static IEnumerator GetPackages(Action callback)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(ThunderAPI.BASEAPIURL + "/package");
            webRequest.SetRequestHeader("accept", "application/json");
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.LogError(LOG + "The fetching of all packages failed with error : " + webRequest.error);
                yield break;
            }
            else
            {
                Package.packages = Package.FromJson(webRequest.downloadHandler.text);
                if (Package.packages.Length <= 0)
                {
                    Debug.LogError(LOG + "Package list seems empty. Please try to restart the game. If the error persist check out this mod page for details and contact infos.");
                    yield break;
                }
                else
                {
                    Debug.Log(LOG + "Mods list fetched.");
                    //Process Queue
                    callback();
                }
            }
        }

        internal static IEnumerator DownloadUpdate(ModUpdateRequest modUpdateRequest, Package pk, Action<ModUpdateRequest> callback)
        {
            Debug.Log(LOG + "Downloading package for update...");
            UnityWebRequest web = UnityWebRequest.Get(pk.Versions[0].DownloadUrl);
            yield return web.SendWebRequest();
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
                    callback(modUpdateRequest);
                }
            }
        }

        private static bool ByteArrayToFile(string fileName, byte[] byteArray)
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
    }
}