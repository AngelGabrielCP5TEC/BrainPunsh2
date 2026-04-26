using System.IO;
using UnityEditor;
using UnityEngine;

// One-click setup for music & end-screen sprites.
//   • Configures end_screens PNGs as Sprite (2D and UI)
//   • Adds AudioManager to the open scene (with all 4 clips wired)
//   • If the open scene has a fighter (i.e. RING), also adds MatchEndOverlay
//     with win/lose sprites wired.
public static class AudioSetupHelper
{
    private const string MENU_MUSIC_PATH      = "Assets/Audio/Screen_Music/MENUMUSIC.mp3";
    private const string RING_MUSIC_PATH      = "Assets/Audio/Screen_Music/RINGMUSIC.mp3";
    private const string WIN_MUSIC_PATH       = "Assets/Audio/Screen_Music/WINSCREEN.mp3";
    private const string LOSE_MUSIC_PATH      = "Assets/Audio/Screen_Music/LOOSESCREEN.mp3";
    private const string BLOCK_SFX_PATH       = "Assets/Audio/Block/block.mp3";
    private const string ROUND_START_SFX_PATH = "Assets/Audio/Round/RoundStartCheer.mp3";
    private const string ROUND_END_SFX_PATH   = "Assets/Audio/Round/RoundEndBell.mp3";
    private const string WIN_SPRITE_PATH      = "Assets/end_screens/you_win.png";
    private const string LOSE_SPRITE_PATH     = "Assets/end_screens/you_lost.png";

    [MenuItem("BrainPunch/Setup Audio + End Screens", priority = 10)]
    public static void Setup()
    {
        EnsureSpriteImporter(WIN_SPRITE_PATH);
        EnsureSpriteImporter(LOSE_SPRITE_PATH);

        var menu       = LoadAudio(MENU_MUSIC_PATH);
        var ring       = LoadAudio(RING_MUSIC_PATH);
        var win        = LoadAudio(WIN_MUSIC_PATH);
        var lose       = LoadAudio(LOSE_MUSIC_PATH);
        var block      = LoadAudio(BLOCK_SFX_PATH);
        var roundStart = LoadAudio(ROUND_START_SFX_PATH);
        var roundEnd   = LoadAudio(ROUND_END_SFX_PATH);

        // ── AudioManager ─────────────────────────────────────────────
        var audio = Object.FindObjectOfType<AudioManager>();
        if (audio == null)
        {
            var go = new GameObject("AudioManager");
            audio = go.AddComponent<AudioManager>();
        }
        var so = new SerializedObject(audio);
        SetClip(so, "_menuMusic",       menu);
        SetClip(so, "_ringMusic",       ring);
        SetClip(so, "_winMusic",        win);
        SetClip(so, "_loseMusic",       lose);
        SetClip(so, "_blockSfx",        block);
        SetClip(so, "_roundStartSfx",   roundStart);
        SetClip(so, "_roundEndBellSfx", roundEnd);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(audio);

        // ── MatchEndOverlay (only in fighting scene) ─────────────────
        bool hasFighters = Object.FindObjectOfType<FighterController>() != null;
        if (hasFighters)
        {
            var winSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(WIN_SPRITE_PATH);
            var loseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LOSE_SPRITE_PATH);

            var overlay = Object.FindObjectOfType<MatchEndOverlay>();
            if (overlay == null)
            {
                var go = new GameObject("MatchEndOverlay");
                overlay = go.AddComponent<MatchEndOverlay>();
            }
            var oso = new SerializedObject(overlay);
            SetSprite(oso, "_winSprite",  winSprite);
            SetSprite(oso, "_loseSprite", loseSprite);
            oso.ApplyModifiedProperties();
            EditorUtility.SetDirty(overlay);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[BrainPunch] Audio + end screens wired. " +
                  $"Menu={menu?.name ?? "MISSING"}, Ring={ring?.name ?? "MISSING"}, " +
                  $"Win={win?.name ?? "MISSING"}, Lose={lose?.name ?? "MISSING"}, " +
                  $"Block={block?.name ?? "MISSING"}, " +
                  $"RoundStart={roundStart?.name ?? "MISSING"}, " +
                  $"RoundEnd={roundEnd?.name ?? "MISSING"}, " +
                  $"OverlayInThisScene={hasFighters}");
    }

    private static AudioClip LoadAudio(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogWarning($"[AudioSetup] Missing audio at {path}");
        return clip;
    }

    private static void EnsureSpriteImporter(string path)
    {
        if (!File.Exists(path)) { Debug.LogWarning($"[AudioSetup] Missing image at {path}"); return; }
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }

    private static void SetClip(SerializedObject so, string field, AudioClip clip)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = clip;
    }

    private static void SetSprite(SerializedObject so, string field, Sprite sprite)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = sprite;
    }
}
