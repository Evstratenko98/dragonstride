using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SunnyScenePaletteMigrator
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/MainMenuScene.unity",
        "Assets/Scenes/LobbyScene.unity",
        "Assets/Scenes/FinishScene.unity"
    };

    [MenuItem("Tools/DragonStride/Apply Sunny Scene Palette")]
    public static void Migrate()
    {
        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyPalette(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SunnyScenePaletteMigrator] Sunny palette was applied to menu scenes.");
    }

    private static void ApplyPalette(Scene scene)
    {
        foreach (Camera camera in GetSceneComponents<Camera>(scene))
        {
            camera.backgroundColor = MenuPalette.BackgroundColor;
            EditorUtility.SetDirty(camera);
        }

        foreach (Image image in GetSceneComponents<Image>(scene))
        {
            ApplyImageStyle(image);
        }

        foreach (Button button in GetSceneComponents<Button>(scene))
        {
            ApplyButtonStyle(button);
        }

        foreach (TMP_InputField inputField in GetSceneComponents<TMP_InputField>(scene))
        {
            ApplyInputFieldStyle(inputField);
        }

        foreach (TMP_Dropdown dropdown in GetSceneComponents<TMP_Dropdown>(scene))
        {
            ApplyDropdownStyle(dropdown);
        }

        foreach (TMP_Text text in GetSceneComponents<TMP_Text>(scene))
        {
            ApplyTextStyle(text);
        }

        foreach (LobbySceneController controller in GetSceneComponents<LobbySceneController>(scene))
        {
            SerializedObject serializedObject = new(controller);
            serializedObject.FindProperty("emptySlotColor").colorValue = MenuPalette.SlotEmptyColor;
            serializedObject.FindProperty("activeSlotColor").colorValue = MenuPalette.SlotActiveColor;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }

    private static void ApplyImageStyle(Image image)
    {
        switch (image.gameObject.name)
        {
            case "Backdrop":
                image.color = MenuPalette.BackgroundColor;
                break;
            case "GlowTopRight":
                image.color = MenuPalette.GlowTopRightColor;
                break;
            case "GlowBottomLeft":
                image.color = MenuPalette.GlowBottomLeftColor;
                break;
            case "Stripe":
                image.color = MenuPalette.StripeColor;
                break;
            case "RootPanel":
                image.color = MenuPalette.PanelColor;
                break;
            case "BrandPanel":
            case "MenuPanel":
            case "FinishPanel":
                image.color = MenuPalette.SecondaryPanelColor;
                break;
            case "Template":
                image.color = MenuPalette.DropdownTemplateColor;
                break;
            case "Item Background":
                image.color = MenuPalette.DropdownItemBackgroundColor;
                break;
            case "Arrow":
                image.color = MenuPalette.AccentPressedColor;
                break;
        }

        if (image.gameObject.name.StartsWith("SlotCard_"))
        {
            image.color = MenuPalette.SlotEmptyColor;
        }

        if (image.GetComponent<TMP_InputField>() != null || image.GetComponent<TMP_Dropdown>() != null)
        {
            image.color = MenuPalette.InputBackgroundColor;
        }

        EditorUtility.SetDirty(image);
    }

    private static void ApplyButtonStyle(Button button)
    {
        bool isDanger = button.name.Contains("Quit") || button.name.Contains("Remove");
        bool isSecondary = button.name.Contains("Back") || button.name.Contains("Settings") || button.name.Contains("Collection");

        Color normalColor = isDanger
            ? MenuPalette.DangerButtonColor
            : isSecondary
                ? MenuPalette.ButtonSecondaryColor
                : MenuPalette.AccentColor;

        Color pressedColor = isDanger
            ? MenuPalette.DangerButtonPressedColor
            : isSecondary
                ? MenuPalette.ButtonSecondaryPressedColor
                : MenuPalette.AccentPressedColor;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = normalColor;
            EditorUtility.SetDirty(image);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.12f);
        colors.pressedColor = pressedColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = MenuPalette.DisabledButtonColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = MenuPalette.ButtonLabelColor;
            EditorUtility.SetDirty(label);
        }

        EditorUtility.SetDirty(button);
    }

    private static void ApplyInputFieldStyle(TMP_InputField inputField)
    {
        Image background = inputField.GetComponent<Image>();
        if (background != null)
        {
            background.color = MenuPalette.InputBackgroundColor;
            EditorUtility.SetDirty(background);
        }

        TMP_Text text = inputField.textComponent;
        if (text != null)
        {
            text.color = MenuPalette.TextPrimaryColor;
            EditorUtility.SetDirty(text);
        }

        if (inputField.placeholder is TMP_Text placeholder)
        {
            placeholder.color = new Color(
                MenuPalette.TextSecondaryColor.r,
                MenuPalette.TextSecondaryColor.g,
                MenuPalette.TextSecondaryColor.b,
                0.6f);
            EditorUtility.SetDirty(placeholder);
        }

        EditorUtility.SetDirty(inputField);
    }

    private static void ApplyDropdownStyle(TMP_Dropdown dropdown)
    {
        Image background = dropdown.GetComponent<Image>();
        if (background != null)
        {
            background.color = MenuPalette.InputBackgroundColor;
            EditorUtility.SetDirty(background);
        }

        if (dropdown.captionText != null)
        {
            dropdown.captionText.color = MenuPalette.TextPrimaryColor;
            EditorUtility.SetDirty(dropdown.captionText);
        }

        Transform arrow = dropdown.transform.Find("Arrow");
        if (arrow != null && arrow.TryGetComponent(out Image arrowImage))
        {
            arrowImage.color = MenuPalette.AccentPressedColor;
            EditorUtility.SetDirty(arrowImage);
        }

        if (dropdown.template != null)
        {
            if (dropdown.template.TryGetComponent(out Image templateImage))
            {
                templateImage.color = MenuPalette.DropdownTemplateColor;
                EditorUtility.SetDirty(templateImage);
            }

            Transform item = dropdown.template.Find("Viewport/Content/Item");
            if (item != null)
            {
                Transform itemLabel = item.Find("Item Label");
                if (itemLabel != null && itemLabel.TryGetComponent(out TMP_Text itemLabelText))
                {
                    itemLabelText.color = MenuPalette.TextPrimaryColor;
                    EditorUtility.SetDirty(itemLabelText);
                }

                Transform itemBackground = item.Find("Item Background");
                if (itemBackground != null && itemBackground.TryGetComponent(out Image itemBackgroundImage))
                {
                    itemBackgroundImage.color = MenuPalette.DropdownItemBackgroundColor;
                    EditorUtility.SetDirty(itemBackgroundImage);
                }
            }
        }

        EditorUtility.SetDirty(dropdown);
    }

    private static void ApplyTextStyle(TMP_Text text)
    {
        if (text.GetComponentInParent<Button>() != null || text.GetComponentInParent<TMP_InputField>() != null)
        {
            return;
        }

        switch (text.gameObject.name)
        {
            case "Eyebrow":
            case "WinnerText":
                text.color = MenuPalette.AccentPressedColor;
                break;
            case "Subtitle":
            case "MenuHint":
            case "TopBarText":
            case "HelperText":
            case "Body":
                text.color = MenuPalette.TextSecondaryColor;
                break;
            default:
                if (text.gameObject.name.StartsWith("NameLabel_") ||
                    text.gameObject.name.StartsWith("ClassLabel_") ||
                    text.gameObject.name.StartsWith("SlotState_"))
                {
                    text.color = MenuPalette.TextSecondaryColor;
                }
                else
                {
                    text.color = MenuPalette.TextPrimaryColor;
                }
                break;
        }

        EditorUtility.SetDirty(text);
    }

    private static System.Collections.Generic.IEnumerable<T> GetSceneComponents<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            foreach (T component in roots[i].GetComponentsInChildren<T>(true))
            {
                yield return component;
            }
        }
    }
}
