using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class TotemStartupSelectForm : TotemUIFormBase
{
    private static readonly Color NormalTint = Color.white;
    private static readonly Color SelectedTint = new Color(0.42f, 0.92f, 0.62f, 1f);

    private static readonly IdLabelOption[] ColorOptions =
    {
        new IdLabelOption(1, "Red"),
        new IdLabelOption(2, "Yellow"),
        new IdLabelOption(3, "Green"),
        new IdLabelOption(4, "Blue"),
        new IdLabelOption(5, "Purple"),
        new IdLabelOption(6, "Gold"),
        new IdLabelOption(7, "White"),
    };

    private static readonly StringLabelOption[] WeaponOptions =
    {
        new StringLabelOption("knife_basic", "Knife"),
        new StringLabelOption("hammer_heavy", "Hammer"),
        new StringLabelOption("pistol_basic", "Pistol"),
        new StringLabelOption("bow_charge", "Bow"),
        new StringLabelOption("energy_fist", "Energy Fist"),
    };

    private static readonly IdLabelOption[] PatternOptions =
    {
        new IdLabelOption(1, "Line", "pattern_line"),
        new IdLabelOption(2, "Ring", "pattern_ring"),
        new IdLabelOption(3, "Spiral", "pattern_spiral"),
        new IdLabelOption(4, "Zigzag", "pattern_zigzag"),
        new IdLabelOption(5, "Bolt", "pattern_bolt"),
        new IdLabelOption(6, "Star", "pattern_star"),
        new IdLabelOption(7, "Stream", "pattern_stream"),
        new IdLabelOption(8, "Beast", "pattern_beast"),
    };

    [SerializeField] private Transform colorRoot;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform patternRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private readonly List<GameObject> colorCards = new List<GameObject>(3);
    private readonly List<GameObject> weaponCards = new List<GameObject>(5);
    private readonly List<GameObject> patternCards = new List<GameObject>(2);
    private readonly List<int> colorIds = new List<int>(3);
    private readonly List<string> weaponIds = new List<string>(5);
    private readonly List<int> patternIds = new List<int>(2);
    private readonly List<int> selectedPatternIds = new List<int>(2);

    private int selectedColorId = -1;
    private string selectedWeaponId;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (colorRoot == null)
        {
            colorRoot = FindChildTransform("ColorRoot");
        }

        if (weaponRoot == null)
        {
            weaponRoot = FindChildTransform("WeaponRoot");
        }

        if (patternRoot == null)
        {
            patternRoot = FindChildTransform("PatternRoot");
        }

        confirmButton = BindButton(confirmButton, "ConfirmButton", OnConfirmClicked);
        cancelButton = BindButton(cancelButton, "CancelButton", OnCancelClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        FlowService?.EnterStartupSelect();
        BuildOptions();
        GFTrace.Success("TotemUI", "StartupSelect.Open");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearOptions();
        base.OnClose(isShutdown, userData);
    }

    public void SelectColor(int colorId)
    {
        selectedColorId = colorId;
        ApplyHighlight(colorCards, colorIds, colorId);
        RefreshConfirmButton();
        GFTrace.Info("TotemUI", "StartupSelect.ColorSelected", null, GFTrace.Data("colorId", colorId.ToString()));
    }

    public void SelectWeapon(string weaponId)
    {
        selectedWeaponId = weaponId;
        ApplyHighlight(weaponCards, weaponIds, weaponId);
        RefreshConfirmButton();
        GFTrace.Info("TotemUI", "StartupSelect.WeaponSelected", null, GFTrace.Data("weaponId", weaponId));
    }

    public void TogglePattern(int patternId)
    {
        if (selectedPatternIds.Contains(patternId))
        {
            selectedPatternIds.Remove(patternId);
        }
        else if (selectedPatternIds.Count < 2)
        {
            selectedPatternIds.Add(patternId);
        }

        ApplyPatternHighlight();
        RefreshConfirmButton();
        GFTrace.Info("TotemUI", "StartupSelect.PatternToggled", null, GFTrace.Data("patternId", patternId.ToString()));
    }

    public void OnConfirmClicked()
    {
        if (selectedColorId <= 0 || string.IsNullOrWhiteSpace(selectedWeaponId) || selectedPatternIds.Count == 0)
        {
            GFTrace.Warning("TotemUI", "StartupSelect.ConfirmRejected");
            return;
        }

        FlowService?.ConfirmStartup(selectedColorId, selectedWeaponId, selectedPatternIds.ToArray());
    }

    private void OnCancelClicked()
    {
        GFTrace.Info("TotemUI", "StartupSelect.CancelClicked");
        UIService?.OpenCharacterSelect();
    }

    private void BuildOptions()
    {
        ClearOptions();
        selectedColorId = -1;
        selectedWeaponId = null;
        selectedPatternIds.Clear();

        BuildColorCards();
        BuildWeaponCards();
        BuildPatternCards();
        RefreshConfirmButton();
    }

    private void BuildColorCards()
    {
        if (colorRoot == null)
        {
            GFTrace.Warning("TotemUI", "StartupSelect.ColorRootMissing");
            return;
        }

        for (int i = 0; i < ColorOptions.Length; i++)
        {
            var option = ColorOptions[i];
            int capturedId = option.Id;
            var card = CreateCard($"Color_{option.Id}", option.Label, colorRoot, () => SelectColor(capturedId));
            colorCards.Add(card);
            colorIds.Add(option.Id);
        }
    }

    private void BuildWeaponCards()
    {
        if (weaponRoot == null)
        {
            GFTrace.Warning("TotemUI", "StartupSelect.WeaponRootMissing");
            return;
        }

        for (int i = 0; i < WeaponOptions.Length; i++)
        {
            var option = WeaponOptions[i];
            string capturedId = option.Id;
            TryLoadRuntimeSprite(GetWeaponAssetKey(option.Id), out var sprite);
            var card = CreateCard($"Weapon_{option.Id}", option.Label, weaponRoot, () => SelectWeapon(capturedId), sprite);
            weaponCards.Add(card);
            weaponIds.Add(option.Id);
        }
    }

    private void BuildPatternCards()
    {
        if (patternRoot == null)
        {
            GFTrace.Warning("TotemUI", "StartupSelect.PatternRootMissing");
            return;
        }

        var unlockedPatternIds = GetUnlockedPatternOptionIds(MetaProgressService?.CaptureSnapshot());
        for (int i = 0; i < PatternOptions.Length; i++)
        {
            var option = PatternOptions[i];
            if (!Contains(unlockedPatternIds, option.Id))
            {
                continue;
            }

            int capturedId = option.Id;
            var card = CreateCard($"Pattern_{option.Id}", option.Label, patternRoot, () => TogglePattern(capturedId));
            patternCards.Add(card);
            patternIds.Add(option.Id);
        }
    }

    public static int[] GetUnlockedPatternOptionIds(TotemMetaProgressSnapshot snapshot)
    {
        var result = new List<int>(PatternOptions.Length);
        for (int i = 0; i < PatternOptions.Length; i++)
        {
            var option = PatternOptions[i];
            if (IsPatternOptionUnlocked(snapshot, option.MetaId))
            {
                result.Add(option.Id);
            }
        }

        if (result.Count == 0)
        {
            result.Add(1);
            result.Add(2);
        }

        return result.ToArray();
    }

    private static bool IsPatternOptionUnlocked(TotemMetaProgressSnapshot snapshot, string patternId)
    {
        if (snapshot?.patternUnlocks == null || string.IsNullOrWhiteSpace(patternId))
        {
            return false;
        }

        string normalizedPatternId = patternId.Trim();
        for (int i = 0; i < snapshot.patternUnlocks.Length; i++)
        {
            var entry = snapshot.patternUnlocks[i];
            if (entry == null || !string.Equals(entry.patternId?.Trim(), normalizedPatternId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (HasAnyUnlockedSlot(entry.slots))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnyUnlockedSlot(bool[] slots)
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i])
            {
                return true;
            }
        }

        return false;
    }

    private static bool Contains(int[] values, int target)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearOptions()
    {
        DestroyCards(colorCards);
        DestroyCards(weaponCards);
        DestroyCards(patternCards);
        colorIds.Clear();
        weaponIds.Clear();
        patternIds.Clear();
        selectedPatternIds.Clear();
    }

    private static void DestroyCards(List<GameObject> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
            {
                Destroy(cards[i]);
            }
        }

        cards.Clear();
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton == null)
        {
            return;
        }

        confirmButton.interactable = selectedColorId > 0 &&
                                     !string.IsNullOrWhiteSpace(selectedWeaponId) &&
                                     selectedPatternIds.Count > 0;
    }

    private static void ApplyHighlight(List<GameObject> cards, List<int> ids, int selectedId)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var image = cards[i] == null ? null : cards[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i < ids.Count && ids[i] == selectedId ? SelectedTint : NormalTint;
            }
        }
    }

    private static void ApplyHighlight(List<GameObject> cards, List<string> ids, string selectedId)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            var image = cards[i] == null ? null : cards[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i < ids.Count && ids[i] == selectedId ? SelectedTint : NormalTint;
            }
        }
    }

    private void ApplyPatternHighlight()
    {
        for (int i = 0; i < patternCards.Count; i++)
        {
            var image = patternCards[i] == null ? null : patternCards[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i < patternIds.Count && selectedPatternIds.Contains(patternIds[i]) ? SelectedTint : NormalTint;
            }
        }
    }

    private static GameObject CreateCard(string cardName, string label, Transform parent, UnityAction onClick, Sprite iconSprite = null)
    {
        var go = new GameObject(cardName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = iconSprite == null ? new Vector2(160f, 88f) : new Vector2(160f, 112f);

        var image = go.AddComponent<Image>();
        image.color = NormalTint;
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        if (iconSprite != null)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -10f);
            iconRect.sizeDelta = new Vector2(56f, 56f);

            var icon = iconGo.AddComponent<Image>();
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        if (iconSprite == null)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        else
        {
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.offsetMin = new Vector2(8f, 8f);
            textRect.offsetMax = new Vector2(-8f, 42f);
        }

        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 18;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;

        return go;
    }

    private static string GetWeaponAssetKey(string weaponId)
    {
        return string.IsNullOrWhiteSpace(weaponId) ? string.Empty : $"weapon.{weaponId}";
    }

    private readonly struct IdLabelOption
    {
        public readonly int Id;
        public readonly string Label;
        public readonly string MetaId;

        public IdLabelOption(int id, string label, string metaId = null)
        {
            Id = id;
            Label = label;
            MetaId = metaId ?? string.Empty;
        }
    }

    private readonly struct StringLabelOption
    {
        public readonly string Id;
        public readonly string Label;

        public StringLabelOption(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }
}
