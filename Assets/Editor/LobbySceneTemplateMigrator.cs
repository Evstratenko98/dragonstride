using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LobbySceneTemplateMigrator
{
    private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
    private const float MinimumSlotHeight = 136f;

    public static void Migrate()
    {
        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        LobbySceneController controller = Object.FindFirstObjectByType<LobbySceneController>();
        if (controller == null)
        {
            throw new UnityException("[LobbySceneTemplateMigrator] LobbySceneController was not found in LobbyScene.");
        }

        SerializedObject serializedController = new(controller);
        SerializedProperty slotViewsProperty = serializedController.FindProperty("slotViews");
        if (slotViewsProperty == null || slotViewsProperty.arraySize == 0)
        {
            throw new UnityException("[LobbySceneTemplateMigrator] slotViews was not found or is empty.");
        }

        SerializedProperty firstSlotProperty = slotViewsProperty.GetArrayElementAtIndex(0);
        GameObject firstSlotRoot = firstSlotProperty.FindPropertyRelative("Root").objectReferenceValue as GameObject;
        Button firstRemoveButton = firstSlotProperty.FindPropertyRelative("RemoveButton").objectReferenceValue as Button;
        if (firstSlotRoot == null)
        {
            throw new UnityException("[LobbySceneTemplateMigrator] The first slot root is missing.");
        }

        ExpandFirstSlot(firstSlotRoot);
        AlignRemoveButton(firstSlotRoot, firstRemoveButton);

        for (int i = slotViewsProperty.arraySize - 1; i >= 1; i--)
        {
            SerializedProperty slotProperty = slotViewsProperty.GetArrayElementAtIndex(i);
            GameObject slotRoot = slotProperty.FindPropertyRelative("Root").objectReferenceValue as GameObject;
            if (slotRoot != null)
            {
                Object.DestroyImmediate(slotRoot);
            }
        }

        slotViewsProperty.arraySize = 1;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LobbySceneTemplateMigrator] LobbyScene was migrated to a single template slot.");
    }

    private static void ExpandFirstSlot(GameObject firstSlotRoot)
    {
        RectTransform rootRect = firstSlotRoot.GetComponent<RectTransform>();
        LayoutElement layoutElement = firstSlotRoot.GetComponent<LayoutElement>();
        if (rootRect == null || layoutElement == null)
        {
            return;
        }

        float targetHeight = Mathf.Max(MinimumSlotHeight, layoutElement.preferredHeight);
        layoutElement.preferredHeight = targetHeight;
        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        EditorUtility.SetDirty(layoutElement);
        EditorUtility.SetDirty(rootRect);
    }

    private static void AlignRemoveButton(GameObject firstSlotRoot, Button removeButton)
    {
        if (removeButton == null)
        {
            return;
        }

        RectTransform content = firstSlotRoot.transform.Find("Content") as RectTransform;
        RectTransform classColumn = content != null ? content.Find("ClassColumn") as RectTransform : null;
        if (content == null || classColumn == null)
        {
            return;
        }

        float topOffset = CalculateControlTopOffset(classColumn);
        float classColumnHeight = GetPreferredHeight(classColumn);
        RectTransform removeButtonRect = removeButton.GetComponent<RectTransform>();
        LayoutElement removeButtonLayout = removeButton.GetComponent<LayoutElement>();
        float removeButtonWidth = removeButtonLayout != null && removeButtonLayout.preferredWidth > 0f
            ? removeButtonLayout.preferredWidth
            : removeButtonRect.rect.width;
        float removeButtonHeight = removeButtonLayout != null && removeButtonLayout.preferredHeight > 0f
            ? removeButtonLayout.preferredHeight
            : removeButtonRect.rect.height;

        RectTransform actionColumn = removeButtonRect.parent != null && removeButtonRect.parent != content
            ? removeButtonRect.parent as RectTransform
            : CreateActionColumn(content);

        LayoutElement actionLayout = actionColumn.GetComponent<LayoutElement>();
        actionLayout.preferredWidth = removeButtonWidth;
        actionLayout.preferredHeight = Mathf.Max(classColumnHeight, topOffset + removeButtonHeight);

        VerticalLayoutGroup actionLayoutGroup = actionColumn.GetComponent<VerticalLayoutGroup>();
        actionLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        actionLayoutGroup.spacing = 0f;
        actionLayoutGroup.childControlWidth = true;
        actionLayoutGroup.childControlHeight = true;
        actionLayoutGroup.childForceExpandWidth = false;
        actionLayoutGroup.childForceExpandHeight = false;

        LayoutElement spacerLayout = EnsureSpacer(actionColumn);
        spacerLayout.preferredHeight = topOffset;

        if (removeButtonRect.parent != actionColumn)
        {
            removeButtonRect.SetParent(actionColumn, false);
        }

        removeButtonRect.SetAsLastSibling();

        EditorUtility.SetDirty(actionLayout);
        EditorUtility.SetDirty(actionLayoutGroup);
        EditorUtility.SetDirty(spacerLayout);
        EditorUtility.SetDirty(removeButtonRect);
    }

    private static RectTransform CreateActionColumn(RectTransform content)
    {
        GameObject actionColumnObject = new(
            "ActionColumn",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(VerticalLayoutGroup));
        actionColumnObject.transform.SetParent(content, false);
        actionColumnObject.transform.SetAsLastSibling();
        return actionColumnObject.GetComponent<RectTransform>();
    }

    private static LayoutElement EnsureSpacer(RectTransform actionColumn)
    {
        Transform spacer = actionColumn.Find("TopSpacer");
        if (spacer == null)
        {
            GameObject spacerObject = new("TopSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacerObject.transform.SetParent(actionColumn, false);
            spacerObject.transform.SetAsFirstSibling();
            spacer = spacerObject.transform;
        }

        LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
        spacer.SetAsFirstSibling();
        return spacerLayout;
    }

    private static float CalculateControlTopOffset(RectTransform classColumn)
    {
        float topOffset = 0f;
        VerticalLayoutGroup layoutGroup = classColumn.GetComponent<VerticalLayoutGroup>();
        if (classColumn.childCount > 0)
        {
            topOffset += GetPreferredHeight(classColumn.GetChild(0) as RectTransform);
        }

        if (layoutGroup != null)
        {
            topOffset += layoutGroup.spacing;
        }

        return topOffset;
    }

    private static float GetPreferredHeight(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return 0f;
        }

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.preferredHeight > 0f)
        {
            return layoutElement.preferredHeight;
        }

        return rectTransform.rect.height;
    }
}
