using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Tiny helpers so the card UIs can build themselves in code (no prefabs or manual wiring).</summary>
public static class UIFactory
{
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>(); // the project uses the new Input System
    }

    public static RectTransform MakeGroup(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<UnityEngine.UI.Image>().color = color;
        return (RectTransform)go.transform;
    }

    public static TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    public static UnityEngine.UI.Button MakeButton(Transform parent, string name, Color color, UnityAction onClick)
    {
        var rt = MakePanel(parent, name, color);
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = rt.GetComponent<UnityEngine.UI.Image>();
        if (onClick != null) button.onClick.AddListener(onClick);
        return button;
    }

    public static UnityEngine.UI.Image MakeImageIcon(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(AspectRatioFitter));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.raycastTarget = false;

        var fitter = go.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1.0f;

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;

        return img;
    }

    /// <summary>Anchors to one point and sets size + position (pivot is where anchoredPosition is measured).</summary>
    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }

    public static void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    public static Color CategoryColor(CardCategory category)
    {
        switch (category)
        {
            case CardCategory.Paddle: return new Color(0.20f, 0.45f, 0.85f);
            case CardCategory.Ball:   return new Color(0.85f, 0.45f, 0.15f);
            default:                  return new Color(0.50f, 0.30f, 0.75f);
        }
    }

    public static string CategoryName(CardCategory category)
    {
        switch (category)
        {
            case CardCategory.Paddle: return "Platform";
            case CardCategory.Ball:   return "Ball";
            default:                  return "Arena";
        }
    }

    public static TMP_InputField MakeInputField(Transform parent, string name, string placeholder, bool isPassword)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);

        var bg = go.GetComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.15f, 0.18f, 0.25f, 1f);

        // Text Area (viewport)
        var textAreaGO = new GameObject("Text Area", typeof(RectTransform));
        textAreaGO.transform.SetParent(go.transform, false);
        var textArea = (RectTransform)textAreaGO.transform;
        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = Vector2.one;
        textArea.offsetMin = new Vector2(16, 8);
        textArea.offsetMax = new Vector2(-16, -8);

        // Placeholder
        var phGO = new GameObject("Placeholder", typeof(RectTransform));
        phGO.transform.SetParent(textArea, false);
        Stretch((RectTransform)phGO.transform);
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text = placeholder;
        ph.fontSize = 30;
        ph.color = new Color(0.55f, 0.55f, 0.6f);
        ph.alignment = TextAlignmentOptions.Left;

        // Text
        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(textArea, false);
        Stretch((RectTransform)textGO.transform);
        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 30;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Left;

        var input = go.GetComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = txt;
        input.placeholder = ph;
        input.targetGraphic = bg;
        input.lineType = TMP_InputField.LineType.SingleLine;
        if (isPassword) input.contentType = TMP_InputField.ContentType.Password;

        return input;
    }
}
