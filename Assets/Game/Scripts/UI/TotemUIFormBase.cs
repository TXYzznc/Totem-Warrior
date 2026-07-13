using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class TotemUIFormBase : UIFormBase
{
    protected TotemGameRuntime Runtime => TotemGameRuntime.Instance;

    protected TotemGameFlowService FlowService => Runtime?.GetService<TotemGameFlowService>();

    protected TotemUIService UIService => Runtime?.GetService<TotemUIService>();

    protected TotemActorService ActorService => Runtime?.GetService<TotemActorService>();

    protected TotemInputService InputService => Runtime?.GetService<TotemInputService>();

    protected TotemParticipantReadinessService ReadinessService => Runtime?.GetService<TotemParticipantReadinessService>();

    protected TotemCombatService CombatService => Runtime?.GetService<TotemCombatService>();

    protected TotemAssetService AssetService => Runtime?.GetService<TotemAssetService>();

    protected TotemEnemyService EnemyService => Runtime?.GetService<TotemEnemyService>();

    protected TotemZoneService ZoneService => Runtime?.GetService<TotemZoneService>();

    protected TotemWeaponService WeaponService => Runtime?.GetService<TotemWeaponService>();

    protected TotemSkillService SkillService => Runtime?.GetService<TotemSkillService>();

    protected TotemStatusService StatusService => Runtime?.GetService<TotemStatusService>();

    protected TotemTattooService TattooService => Runtime?.GetService<TotemTattooService>();

    protected TotemInteractionService InteractionService => Runtime?.GetService<TotemInteractionService>();

    protected TotemMetaProgressService MetaProgressService => Runtime?.GetService<TotemMetaProgressService>();

    protected TotemMapService MapService => Runtime?.GetService<TotemMapService>();

    protected T FindChildComponent<T>(string childName) where T : Component
    {
        if (string.IsNullOrEmpty(childName))
        {
            return null;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != childName)
            {
                continue;
            }

            if (transforms[i].TryGetComponent<T>(out var component))
            {
                return component;
            }
        }

        return null;
    }

    protected Transform FindChildTransform(string childName)
    {
        if (string.IsNullOrEmpty(childName))
        {
            return null;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == childName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    protected Button BindButton(Button button, string childName, UnityAction callback)
    {
        if (button == null)
        {
            button = FindChildComponent<Button>(childName);
        }

        if (button == null)
        {
            GFTrace.Warning("TotemUI", "Button.Missing", null, GFTrace.Data("form", GetType().Name, "button", childName));
            return null;
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
        return button;
    }

    protected void SetText(Component component, string value)
    {
        if (component == null)
        {
            return;
        }

        if (component is TMP_Text tmpText)
        {
            tmpText.SetText(value);
            return;
        }

        if (component is Text text)
        {
            text.text = value;
        }
    }

    protected void SetChildText(string childName, string value)
    {
        var tmpText = FindChildComponent<TMP_Text>(childName);
        if (tmpText != null)
        {
            tmpText.SetText(value);
            return;
        }

        var text = FindChildComponent<Text>(childName);
        if (text != null)
        {
            text.text = value;
        }
    }

    protected bool TryLoadRuntimeSprite(string assetKey, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(assetKey))
        {
            return false;
        }

        return AssetService != null && AssetService.TryLoadSprite(assetKey, out sprite) && sprite != null;
    }
}
