using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;

[InitializeOnLoad]
public static class ToolbarButtons
{
    const string REFRESH_DOMAIN_ICON_PATH = "Assets/_RefreshDomain/Editor/Icons/Reload.png";
    const string REFRESH_PLAY_ICON_PATH = "Assets/_RefreshDomain/Editor/Icons/ReloadAndPlay.png";
    static ToolbarButtons()
    {
        EditorApplication.delayCall += TryAttach;
    }

    static void TryAttach()
    {
        Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(toolbarType);

        if (toolbars.Length == 0) return;

        UnityEngine.Object toolbar = toolbars[0];

        VisualElement root = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(toolbar) as VisualElement;

        if (root == null) return;

        VisualElement playModeContainer = root.Q("PlayMode");

        if (playModeContainer == null) return;

        Texture2D refreshButtonIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(REFRESH_DOMAIN_ICON_PATH);
        Texture2D refreshAndPlayButtonIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(REFRESH_PLAY_ICON_PATH);
       
        CreateToolbarButton(playModeContainer, refreshButtonIcon, () => Debug.Log("Button 1 pressed"), 0);
        CreateToolbarButton(playModeContainer, refreshAndPlayButtonIcon, () => Debug.Log("Button 2 pressed"), 1);
    }

    static void CreateToolbarButton(VisualElement parent, Texture2D icon, Action onClick, int? index = null)
    {
        Button button = new Button(() => onClick());
        button.AddToClassList("unity-toolbar-button");

        button.style.width = 24;
        button.style.height = 22;
        button.style.marginRight = 6;

        var image = new Image();
        image.image = icon;
        image.scaleMode = ScaleMode.ScaleToFit;

        button.Add(image);

        if (index.HasValue) parent.Insert(index.Value, button);
        else                parent.Add(button);
    }
}