using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class TotemCharacterSelectForm : TotemUIFormBase
{
    private static readonly Color NormalTint = Color.white;
    private static readonly Color SelectedTint = new Color(0.42f, 0.92f, 0.62f, 1f);
    private const string UnlockedFrameAssetKey = "ui.character.card.unlocked";

    private static readonly CharacterOption[] CharacterOptions =
    {
        new CharacterOption(1, "角色 1", "ui.character.1"),
        new CharacterOption(2, "角色 2", "ui.character.2"),
        new CharacterOption(3, "角色 3", "ui.character.3"),
    };

    [SerializeField] private Transform characterRoot;
    [SerializeField] private Button nextButton;

    private readonly List<GameObject> cards = new List<GameObject>(3);
    private readonly List<int> cardIds = new List<int>(3);
    private int selectedCharacterId = -1;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (characterRoot == null)
        {
            characterRoot = FindChildTransform("CharacterRoot");
        }

        nextButton = BindButton(nextButton, "NextButton", OnNextClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        FlowService?.EnterCharacterSelect();
        BuildCards();
        GFTrace.Success("TotemUI", "CharacterSelect.Open");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ClearCards();
        base.OnClose(isShutdown, userData);
    }

    public void SetSelectedCharacter(int characterId)
    {
        selectedCharacterId = characterId;
        FlowService?.SelectCharacter(characterId);

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null)
            {
                continue;
            }

            var image = cards[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i < cardIds.Count && cardIds[i] == characterId ? SelectedTint : NormalTint;
            }
        }

        if (nextButton != null)
        {
            nextButton.interactable = true;
        }
    }

    public void OnNextClicked()
    {
        if (selectedCharacterId <= 0)
        {
            GFTrace.Warning("TotemUI", "CharacterSelect.NextRejected", null, GFTrace.Data("reason", "NoCharacterSelected"));
            return;
        }

        GFTrace.Info("TotemUI", "CharacterSelect.NextClicked", null, GFTrace.Data("characterId", selectedCharacterId.ToString()));
        UIService?.OpenStartupSelect();
    }

    private void BuildCards()
    {
        ClearCards();
        selectedCharacterId = -1;
        if (nextButton != null)
        {
            nextButton.interactable = false;
        }

        if (characterRoot == null)
        {
            GFTrace.Warning("TotemUI", "CharacterSelect.RootMissing");
            return;
        }

        TryLoadRuntimeSprite(UnlockedFrameAssetKey, out var frameSprite);
        for (int i = 0; i < CharacterOptions.Length; i++)
        {
            var option = CharacterOptions[i];
            int capturedId = option.Id;
            TryLoadRuntimeSprite(option.AssetKey, out var portraitSprite);
            var card = CreateCard($"CharacterCard_{option.Id}", option.Label, characterRoot, frameSprite, portraitSprite, () => SetSelectedCharacter(capturedId));
            cards.Add(card);
            cardIds.Add(option.Id);
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
            {
                Destroy(cards[i]);
            }
        }

        cards.Clear();
        cardIds.Clear();
    }

    public static string GetCharacterAssetKey(int characterId)
    {
        for (int i = 0; i < CharacterOptions.Length; i++)
        {
            if (CharacterOptions[i].Id == characterId)
            {
                return CharacterOptions[i].AssetKey;
            }
        }

        return string.Empty;
    }

    private static GameObject CreateCard(string cardName, string label, Transform parent, Sprite frameSprite, Sprite portraitSprite, UnityAction onClick)
    {
        var go = new GameObject(cardName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240f, 280f);

        var image = go.AddComponent<Image>();
        image.sprite = frameSprite;
        image.color = NormalTint;
        image.type = frameSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        if (portraitSprite != null)
        {
            var portraitGo = new GameObject("Portrait", typeof(RectTransform));
            portraitGo.transform.SetParent(go.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.sizeDelta = new Vector2(180f, 180f);
            portraitRect.anchoredPosition = new Vector2(0f, 20f);

            var portrait = portraitGo.AddComponent<Image>();
            portrait.sprite = portraitSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
        }

        var textGo = new GameObject("Name", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.sizeDelta = new Vector2(0f, 40f);
        textRect.anchoredPosition = new Vector2(0f, 8f);

        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;

        return go;
    }

    private readonly struct CharacterOption
    {
        public readonly int Id;
        public readonly string Label;
        public readonly string AssetKey;

        public CharacterOption(int id, string label, string assetKey)
        {
            Id = id;
            Label = label;
            AssetKey = assetKey ?? string.Empty;
        }
    }
}
