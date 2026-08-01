using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DoomArcade.Scripts.Arcade
{
    public class WadLibrary : MonoBehaviour
    {
        [Header("Build folder")]
        public string buildWadFolderRelative = "WADs";

        public static string OverrideBaseDir;
        public string[] WadPaths { get; private set; } = System.Array.Empty<string>();

        public void Scan()
        {
            var folder = GetWadFolder();

            if (!Directory.Exists(folder))
            {
                WadPaths = System.Array.Empty<string>();
                return;
            }

            WadPaths = Directory.GetFiles(folder, "*.WAD", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName)
                .ToArray();

        }

        private string GetWadFolder()
        {
            if (!string.IsNullOrEmpty(OverrideBaseDir))
                return Path.Combine(OverrideBaseDir, buildWadFolderRelative);

            var asmLocation = Assembly.GetExecutingAssembly().Location;
            var asmDir      = Path.GetDirectoryName(asmLocation) ?? string.Empty;
            return Path.Combine(asmDir, buildWadFolderRelative);
        }
    }
}