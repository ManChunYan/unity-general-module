using System.IO;
using UnityEditor;

namespace General.Editor
{
    public static class DeleteSaveData
    {
        [MenuItem("Tools/SaveData/Clear")]
        public static void DeleteData()
        {
            if (!EditorUtility.DisplayDialog(
                    "Clear Save Data",
                    $"Delete all {General.MainBase.FileFormat} files from {General.MainBase.SavePath}?",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            if (Directory.Exists(General.MainBase.SavePath))
            {
                var files = Directory.GetFiles(General.MainBase.SavePath, "*" + General.MainBase.FileFormat);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
        }
    }
}
