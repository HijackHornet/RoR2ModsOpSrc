using System.Collections.Generic;
using static Hj.HjUpdaterAPI;

namespace HjUpdaterAPI
{
    internal class ModUpdateRequest
    {
        #region Fields

        private List<string> _otherFilesLocationRelativeToTheDll = null;

        #endregion Fields

        #region Properties

        public string currentDllFileLocation { get; set; }
        public System.Version currentVersion { get; set; }
        public Flag flag { get; set; }
        public bool modUseRuntimeRessourceLoading { get; set; }

        public List<string> otherFilesLocationRelativeToTheDll
        {
            get => _otherFilesLocationRelativeToTheDll;
            set
            {
                if (value == null)
                {
                    _otherFilesLocationRelativeToTheDll = new List<string>();
                    _otherFilesLocationRelativeToTheDll.Add("README.md");
                    _otherFilesLocationRelativeToTheDll.Add("manifest.json");
                    _otherFilesLocationRelativeToTheDll.Add("icon.png");
                }
                else
                {
                    if (!value.Contains("README.md")) { value.Add("README.md"); }
                    if (!value.Contains("manifest.json")) { value.Add("manifest.json"); }
                    if (!value.Contains("icon.png")) { value.Add("icon.png"); }
                    _otherFilesLocationRelativeToTheDll = value;
                }
            }
        }

        public string packageName { get; set; }

        #endregion Properties

        #region Constructors

        public ModUpdateRequest(string packageName, System.Version currentVersion, string currentDllFileLocation, Flag flag, List<string> otherFilesLocationRelativeToTheDll, bool modUseRuntimeRessourceLoading)
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