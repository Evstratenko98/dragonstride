using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneUiLayoutMigrator
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";

    [MenuItem("Tools/UI/Rebuild GameScene UI Layout")]
    public static void RebuildFromMenu()
    {
        Migrate();
    }

    public static void Migrate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameScreenView gameScreenView = UnityEngine.Object.FindFirstObjectByType<GameScreenView>(FindObjectsInactive.Include);
        CharacterRosterPanelView rosterPanelView = UnityEngine.Object.FindFirstObjectByType<CharacterRosterPanelView>(FindObjectsInactive.Include);

        if (gameScreenView == null)
        {
            throw new InvalidOperationException("GameScreenView was not found in GameScene.");
        }

        if (rosterPanelView == null)
        {
            throw new InvalidOperationException("CharacterRosterPanelView was not found in GameScene.");
        }

        gameScreenView.RebuildSceneLayout();
        rosterPanelView.RebuildSceneLayout();

        EditorUtility.SetDirty(gameScreenView);
        EditorUtility.SetDirty(rosterPanelView);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[GameSceneUiLayoutMigrator] GameScene UI layout rebuilt successfully.");
    }
}
