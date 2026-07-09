using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class TotemOverlayFormBase : TotemUIFormBase
{
    private const string RuntimeRootName = "TotemRuntimeOverlayRoot";

    private static Font cachedFont;
    private GameObject runtimeRoot;

    protected RectTransform RebuildPanel(string title, Vector2 size)
    {
        ClearRuntimeOverlay();

        runtimeRoot = new GameObject(RuntimeRootName, typeof(RectTransform));
        runtimeRoot.transform.SetParent(transform, false);
        var rootRect = runtimeRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var dim = runtimeRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.38f);
        dim.raycastTarget = true;

        var panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(runtimeRoot.transform, false);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = size;
        panelRect.anchoredPosition = Vector2.zero;

        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.09f, 0.10f, 0.12f, 0.96f);
        panelImage.raycastTarget = true;

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 20, 20);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        AddText(panelRect, "Title", title, 24, TextAnchor.MiddleCenter, 42f, FontStyle.Bold);
        return panelRect;
    }

    protected Text AddText(Transform parent, string name, string value, int fontSize, TextAnchor alignment, float preferredHeight, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, preferredHeight);

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;

        var text = go.AddComponent<Text>();
        text.text = value ?? string.Empty;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.font = GetOverlayFont();
        text.raycastTarget = false;
        return text;
    }

    protected Button AddButton(Transform parent, string name, string label, UnityAction onClick, bool interactable = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 44f);

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;
        layout.minHeight = 44f;

        var image = go.AddComponent<Image>();
        image.color = interactable ? new Color(0.25f, 0.46f, 0.72f, 1f) : new Color(0.24f, 0.24f, 0.24f, 1f);
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        var labelText = AddText(go.transform, "Label", label, 17, TextAnchor.MiddleCenter, 40f);
        var labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        return button;
    }

    protected void ClearRuntimeOverlay()
    {
        var existing = transform.Find(RuntimeRootName);
        if (existing == null)
        {
            runtimeRoot = null;
            return;
        }

        Destroy(existing.gameObject);
        runtimeRoot = null;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearRuntimeOverlay();
        UIService?.ForgetOverlay(Id);
        base.OnClose(isShutdown, userData);
    }

    private static Font GetOverlayFont()
    {
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return cachedFont;
    }
}
