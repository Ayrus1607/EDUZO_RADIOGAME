using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// One-shot Editor script to restructure the EDUZO_RADIOGAME project
/// to match the Eduzo team folder convention.
/// 
/// Run from: Tools > Eduzo > Restructure Project
/// After running successfully, DELETE this script.
/// </summary>
public class ProjectRestructure : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> log = new List<string>();
    private bool isDone = false;

    [MenuItem("Tools/Eduzo/Restructure Project")]
    static void ShowWindow()
    {
        var window = GetWindow<ProjectRestructure>("Project Restructure");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("EDUZO Project Restructure", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (!isDone)
        {
            GUILayout.Label("This will reorganize the project to match the Eduzo team convention:");
            GUILayout.Label("  Assets/Games/radio/     (Audio, Font, Game UI, Prefabs, Scenes, Scripts)");
            GUILayout.Label("  Assets/Games/data-handling/ (Audio, Font, Game UI, Prefabs, Scenes, Scripts)");
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "IMPORTANT: Make sure Unity is not in Play mode.\n" +
                "This operation uses AssetDatabase.MoveAsset() so all GUID references are preserved.\n" +
                "A git commit before running is recommended.",
                MessageType.Warning);

            GUILayout.Space(10);

            if (GUILayout.Button("Run Restructure", GUILayout.Height(40)))
            {
                log.Clear();
                RunRestructure();
                isDone = true;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Restructure complete! Review the log below.\nYou can now delete this script (Assets/Editor/ProjectRestructure.cs).", MessageType.Info);
        }

        GUILayout.Space(10);
        GUILayout.Label("Log:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (string entry in log)
        {
            if (entry.StartsWith("[ERROR]"))
                EditorGUILayout.HelpBox(entry, MessageType.Error);
            else if (entry.StartsWith("[WARN]"))
                EditorGUILayout.HelpBox(entry, MessageType.Warning);
            else
                GUILayout.Label(entry);
        }
        EditorGUILayout.EndScrollView();
    }

    void RunRestructure()
    {
        Log("========== STARTING PROJECT RESTRUCTURE ==========");

        try
        {
            // ===================================================
            // PHASE 1: Create the target folder structure
            // ===================================================
            Log("\n--- Phase 1: Creating folder structure ---");

            CreateFolder("Assets", "Games");
            CreateFolder("Assets/Games", "radio");
            CreateFolder("Assets/Games/radio", "Audio");
            CreateFolder("Assets/Games/radio", "Font");
            CreateFolder("Assets/Games/radio", "Game UI");
            CreateFolder("Assets/Games/radio", "Prefabs");
            CreateFolder("Assets/Games/radio", "Scenes");
            CreateFolder("Assets/Games/radio", "Scripts");

            CreateFolder("Assets/Games", "data-handling");
            CreateFolder("Assets/Games/data-handling", "Audio");
            CreateFolder("Assets/Games/data-handling", "Font");
            CreateFolder("Assets/Games/data-handling", "Game UI");
            CreateFolder("Assets/Games/data-handling", "Prefabs");
            CreateFolder("Assets/Games/data-handling", "Scenes");
            CreateFolder("Assets/Games/data-handling", "Scripts");

            CreateFolder("Assets/_Common", "Game UI");

            // CRITICAL: Refresh so the new folders are registered before moves
            AssetDatabase.Refresh();

            // ===================================================
            // PHASE 2: Move Radio Game files
            // ===================================================
            Log("\n--- Phase 2: Moving Radio Game files ---");

            // Scene (with rename)
            MoveAsset("Assets/Scenes/SampleScene.unity",
                       "Assets/Games/radio/Scenes/RadioGame_Scene.unity");

            // Scripts
            MoveAsset("Assets/Scripts/Radio/RadioFormController.cs",
                       "Assets/Games/radio/Scripts/RadioFormController.cs");
            MoveAsset("Assets/Scripts/Radio/RadioGameManager.cs",
                       "Assets/Games/radio/Scripts/RadioGameManager.cs");
            MoveAsset("Assets/Scripts/Radio/RadioPulseEffect.cs",
                       "Assets/Games/radio/Scripts/RadioPulseEffect.cs");
            MoveAsset("Assets/Scripts/Radio/RadioQuestionData.cs",
                       "Assets/Games/radio/Scripts/RadioQuestionData.cs");

            // UI Art
            MoveAllAssets("Assets/UI/RadioGame_assets", "Assets/Games/radio/Game UI");

            // Font (Oxanium)
            MoveAllAssets("Assets/Font", "Assets/Games/radio/Font");

            // ===================================================
            // PHASE 3: Move Data Handling Game files
            // ===================================================
            Log("\n--- Phase 3: Moving Data Handling Game files ---");

            // Scene
            MoveAsset("Assets/Scenes/DataHandling_Scene.unity",
                       "Assets/Games/data-handling/Scenes/DataHandling_Scene.unity");

            // Scripts
            MoveAsset("Assets/Scripts/DataHandling/BarGraphManager.cs",
                       "Assets/Games/data-handling/Scripts/BarGraphManager.cs");
            MoveAsset("Assets/Scripts/DataHandling/DataFormController.cs",
                       "Assets/Games/data-handling/Scripts/DataFormController.cs");
            MoveAsset("Assets/Scripts/DataHandling/DataGameManager.cs",
                       "Assets/Games/data-handling/Scripts/DataGameManager.cs");
            MoveAsset("Assets/Scripts/DataHandling/DataTableManager.cs",
                       "Assets/Games/data-handling/Scripts/DataTableManager.cs");
            MoveAsset("Assets/Scripts/DataHandling/PieChartManager.cs",
                       "Assets/Games/data-handling/Scripts/PieChartManager.cs");
            MoveAsset("Assets/Scripts/DataHandling/TallyMarkManager.cs",
                       "Assets/Games/data-handling/Scripts/TallyMarkManager.cs");

            // UI Art
            MoveAllAssets("Assets/UI/DataHandlingGame_Assets", "Assets/Games/data-handling/Game UI");

            // Prefabs (game-specific)
            MoveAsset("Assets/Prefabs/Graph_Bar.prefab",
                       "Assets/Games/data-handling/Prefabs/Graph_Bar.prefab");
            MoveAsset("Assets/Prefabs/Pie_Slice.prefab",
                       "Assets/Games/data-handling/Prefabs/Pie_Slice.prefab");
            MoveAsset("Assets/Prefabs/Tally_Bundle.prefab",
                       "Assets/Games/data-handling/Prefabs/Tally_Bundle.prefab");
            MoveAsset("Assets/Prefabs/Tally_Row.prefab",
                       "Assets/Games/data-handling/Prefabs/Tally_Row.prefab");
            MoveAsset("Assets/Prefabs/Tally_Single.prefab",
                       "Assets/Games/data-handling/Prefabs/Tally_Single.prefab");
            MoveAsset("Assets/Prefabs/Table_Row.prefab",
                       "Assets/Games/data-handling/Prefabs/Table_Row.prefab");
            MoveAsset("Assets/Prefabs/Clipboard_Row.prefab",
                       "Assets/Games/data-handling/Prefabs/Clipboard_Row.prefab");
            MoveAsset("Assets/Prefabs/Legend_Item.prefab",
                       "Assets/Games/data-handling/Prefabs/Legend_Item.prefab");

            // ===================================================
            // PHASE 4: Move Shared UI to _Common/Game UI
            // ===================================================
            Log("\n--- Phase 4: Moving shared UI to _Common/Game UI ---");

            MoveAllAssets("Assets/UI/Common Assets", "Assets/_Common/Game UI");

            // ===================================================
            // PHASE 5: Cleanup - Delete duplicates & junk
            // ===================================================
            Log("\n--- Phase 5: Cleanup ---");

            // Delete duplicate audio folder
            DeleteAsset("Assets/Audio");

            // Delete _Recovery backup scenes
            DeleteAsset("Assets/_Recovery");

            // Delete the 75MB unitypackage.gz if it ended up in _Common/Game UI
            DeleteAsset("Assets/_Common/Game UI/Epic Toon & Lana Studio.unitypackage.gz");

            // Delete macOS junk files
            DeleteFileIfExists("Assets/_Common/.DS_Store");
            DeleteFileIfExists("Assets/QRcode/.DS_Store");

            // Delete the QRcode document.pdf (not needed at runtime)
            // Keeping it - might be documentation

            // ===================================================
            // PHASE 6: Clean up empty source folders
            // ===================================================
            Log("\n--- Phase 6: Cleaning up empty folders ---");

            DeleteFolderIfEmpty("Assets/Scripts/Radio");
            DeleteFolderIfEmpty("Assets/Scripts/DataHandling");
            DeleteFolderIfEmpty("Assets/Scripts");
            DeleteFolderIfEmpty("Assets/Scenes");
            DeleteFolderIfEmpty("Assets/UI/RadioGame_assets");
            DeleteFolderIfEmpty("Assets/UI/DataHandlingGame_Assets");
            DeleteFolderIfEmpty("Assets/UI/Common Assets");
            DeleteFolderIfEmpty("Assets/UI");
            DeleteFolderIfEmpty("Assets/Font");
            DeleteFolderIfEmpty("Assets/Prefabs");

            // ===================================================
            // PHASE 7: Create README files
            // ===================================================
            Log("\n--- Phase 7: Creating README files ---");

            CreateReadme("Assets/Games/radio/README.md",
                "# Radio Game\n\n" +
                "Teacher creates True/False questions → Student picks A/B on a radio-themed UI.\n\n" +
                "## Flow\n" +
                "Form → Mode Selection → Player Data → Gameplay → Score Screen\n\n" +
                "## Modes\n" +
                "- **Practice Mode**: No timer, no lives, unlimited attempts\n" +
                "- **Test Mode**: Countdown timer, 3 lives, score tracking\n\n" +
                "## Scripts\n" +
                "- `RadioGameManager.cs` — Core game loop, state machine, scoring\n" +
                "- `RadioFormController.cs` — Teacher question input form\n" +
                "- `RadioQuestionData.cs` — Data model for a single question\n" +
                "- `RadioPulseEffect.cs` — UI breathing/pulse animation\n");

            CreateReadme("Assets/Games/data-handling/README.md",
                "# Data Handling Game\n\n" +
                "Teacher inputs category data → Student scans QR flashcards to answer data representation questions.\n\n" +
                "## Flow\n" +
                "Form → Mode Selection → Player Data → Gameplay → Score Screen\n\n" +
                "## Modes\n" +
                "- **Practice Mode**: No timer, no lives, unlimited attempts\n" +
                "- **Test Mode**: Countdown timer, 3 lives, score tracking\n\n" +
                "## Visualization Types\n" +
                "- **Bar Graph** (Mode 0) — Dynamic bar chart with chalk colors\n" +
                "- **Pie Chart** (Mode 1) — Percentage-based pie chart with legends\n" +
                "- **Tally Chart** (Mode 2) — Tally marks grouped in bundles of 5\n" +
                "- **Look & Count** (Mode 3) — Custom image-based counting\n\n" +
                "## Scripts\n" +
                "- `DataGameManager.cs` — Core game loop, QR scanning integration, scoring\n" +
                "- `DataFormController.cs` — Teacher data input form with validation\n" +
                "- `BarGraphManager.cs` — Dynamic bar chart generation\n" +
                "- `PieChartManager.cs` — Pie chart slice/legend generation\n" +
                "- `TallyMarkManager.cs` — Tally mark row generation\n" +
                "- `DataTableManager.cs` — Data table display\n");
        }
        catch (System.Exception e)
        {
            Log($"[ERROR] Exception: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            AssetDatabase.Refresh();
        }

        Log("\n========== RESTRUCTURE COMPLETE ==========");
        Log("Please verify both scenes open correctly.");
        Log("Then delete: Assets/Editor/ProjectRestructure.cs");
    }

    // ===================== HELPER METHODS =====================

    void CreateFolder(string parent, string folderName)
    {
        string fullPath = parent + "/" + folderName;
        if (AssetDatabase.IsValidFolder(fullPath))
        {
            Log($"  [SKIP] Folder already exists: {fullPath}");
            return;
        }

        string guid = AssetDatabase.CreateFolder(parent, folderName);
        if (string.IsNullOrEmpty(guid))
            Log($"[ERROR] Failed to create folder: {fullPath}");
        else
            Log($"  [CREATE] {fullPath}");
    }

    void MoveAsset(string from, string to)
    {
        if (!AssetExists(from))
        {
            Log($"  [SKIP] Source not found: {from}");
            return;
        }
        if (AssetExists(to))
        {
            Log($"  [SKIP] Destination already exists: {to}");
            return;
        }

        string error = AssetDatabase.MoveAsset(from, to);
        if (string.IsNullOrEmpty(error))
            Log($"  [MOVE] {from}  →  {to}");
        else
            Log($"[ERROR] Move failed ({from} → {to}): {error}");
    }

    void MoveAllAssets(string sourceFolder, string destFolder)
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            Log($"  [SKIP] Source folder not found: {sourceFolder}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("", new[] { sourceFolder });
        HashSet<string> movedPaths = new HashSet<string>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // Only move direct children, not items in subfolders
            string relativePath = assetPath.Substring(sourceFolder.Length + 1);
            if (relativePath.Contains("/")) continue;

            // Skip .meta files (handled automatically)
            if (assetPath.EndsWith(".meta")) continue;

            // Skip folders
            if (AssetDatabase.IsValidFolder(assetPath)) continue;

            if (movedPaths.Contains(assetPath)) continue;
            movedPaths.Add(assetPath);

            string fileName = Path.GetFileName(assetPath);
            string destPath = destFolder + "/" + fileName;
            MoveAsset(assetPath, destPath);
        }
    }

    void DeleteAsset(string path)
    {
        if (!AssetExists(path) && !AssetDatabase.IsValidFolder(path))
        {
            Log($"  [SKIP] Not found for deletion: {path}");
            return;
        }

        if (AssetDatabase.DeleteAsset(path))
            Log($"  [DELETE] {path}");
        else
            Log($"[ERROR] Failed to delete: {path}");
    }

    void DeleteFileIfExists(string path)
    {
        // .DS_Store files aren't tracked by AssetDatabase, delete via filesystem
        string fullPath = Path.Combine(Application.dataPath, "..", path).Replace("Assets/../Assets", "Assets");
        // Construct the actual filesystem path
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fsPath = Path.Combine(projectRoot, path.Replace("/", "\\"));

        if (File.Exists(fsPath))
        {
            File.Delete(fsPath);
            // Also delete .meta if it exists
            if (File.Exists(fsPath + ".meta"))
                File.Delete(fsPath + ".meta");
            Log($"  [DELETE] {path} (filesystem)");
        }
        else
        {
            Log($"  [SKIP] File not found: {path}");
        }
    }

    void DeleteFolderIfEmpty(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            Log($"  [SKIP] Folder not found: {path}");
            return;
        }

        // Check if folder has any remaining assets
        string[] remaining = AssetDatabase.FindAssets("", new[] { path });
        if (remaining.Length == 0)
        {
            AssetDatabase.DeleteAsset(path);
            Log($"  [DELETE] Empty folder: {path}");
        }
        else
        {
            Log($"  [WARN] Folder not empty ({remaining.Length} items), keeping: {path}");
        }
    }

    bool AssetExists(string path)
    {
        return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
    }

    void CreateReadme(string assetPath, string content)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fsPath = Path.Combine(projectRoot, assetPath.Replace("/", "\\"));

        if (File.Exists(fsPath))
        {
            Log($"  [SKIP] README already exists: {assetPath}");
            return;
        }

        File.WriteAllText(fsPath, content);
        Log($"  [CREATE] {assetPath}");
    }

    void Log(string message)
    {
        log.Add(message);
        Debug.Log("[Restructure] " + message);
    }
}
