using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Economy;
using Tattoo.Data;
using Tattoo;

// Runtime dependencies are injected by WeaponSpawnerModule; no direct GetModule calls here.

/// <summary>
/// 商人交互触发器。挂在商人 GO 上。
/// Slots 由 WeaponSpawnerModule Spawn 时注入（CONTRACT §C / §G）。
/// MonoBehaviour 内零 GetModule 调用。
/// </summary>
public sealed class MerchantTrigger : MonoBehaviour
{
    // ── 由 WeaponSpawnerModule 注入 ────────────────────────────────────
    /// <summary>商人展示槽位（0~2），Spawn 时注入，每局固定。</summary>
    public IReadOnlyList<MerchantConfigRow> Slots;

    /// <summary>EventBus 引用，Spawn 时注入。</summary>
    public EventBus Bus;

    /// <summary>玩家 Target，OnTriggerEnter 时从 collider 取，或 Spawn 时注入。</summary>
    public Target PlayerTarget;

    /// <summary>输入入口。由 WeaponSpawnerModule 注入，保证商人购买按键走 InputModule。</summary>
    public InputModule Input;
    public EconomyModule Economy;
    public Actor PlayerActor;
    public Transform PlayerTransform;

    // ── 内部状态 ───────────────────────────────────────────────────────
    bool _playerInRange;
    const float InteractRadius = 2.2f;

    /// <summary>每个槽位是否已购买（一局内每槽只能购买一次）。</summary>
    readonly bool[] _purchased = new bool[3];

    // ── 世界 UI 提示 ───────────────────────────────────────────────────
    GameObject _hintGO;
    TextMeshPro[] _slotTexts;   // 每个槽位一行文本

    static readonly KeyCode[] SlotKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };

    void Awake()
    {
        BuildHintUI();
    }

    void OnDestroy()
    {
        if (_hintGO != null)
            Destroy(_hintGO);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        if (PlayerTarget == null)
            PlayerTarget = other.GetComponentInParent<EntityRef>()?.Target;

        _playerInRange = true;
        RefreshHintUI();
        DisplayHint(true);
        FrameworkLogger.Info("MerchantTrigger", "Action=PlayerEntered");
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        _playerInRange = false;
        DisplayHint(false);
        FrameworkLogger.Info("MerchantTrigger", "Action=PlayerExited");
    }

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        var entity = other.GetComponentInParent<EntityRef>();
        if (entity != null) return entity.IsPlayer;
        return other.CompareTag("Player");
    }

    void Update()
    {
        RefreshRangeByDistance();
        if (!_playerInRange) return;

        // 监听 1/2/3 键对应槽位购买
        for (int i = 0; i < 3; i++)
        {
            if (Input == null || !Input.IsKeyPressed(SlotKeys[i])) continue;
            TryPurchase(i);
        }

        // Billboarding
        if (_hintGO != null && Camera.main != null)
            _hintGO.transform.rotation = Camera.main.transform.rotation;
    }

    void RefreshRangeByDistance()
    {
        if (PlayerTransform == null) return;
        Vector3 delta = PlayerTransform.position - transform.position;
        delta.y = 0f;
        bool inRange = delta.sqrMagnitude <= InteractRadius * InteractRadius;
        if (inRange == _playerInRange) return;

        _playerInRange = inRange;
        if (inRange) RefreshHintUI();
        DisplayHint(inRange);
        FrameworkLogger.Info("MerchantTrigger",
            inRange ? "Action=PlayerEntered Source=Distance" : "Action=PlayerExited Source=Distance");
    }

    void TryPurchase(int slotIndex)
    {
        if (Slots == null || slotIndex >= Slots.Count)
        {
            FrameworkLogger.Warn("MerchantTrigger", $"Action=PurchaseFailed Reason=SlotOutOfRange SlotIndex={slotIndex}");
            return;
        }

        if (_purchased[slotIndex])
        {
            FrameworkLogger.Info("MerchantTrigger", $"Action=PurchaseSkipped Reason=AlreadyPurchased SlotIndex={slotIndex}");
            return;
        }

        if (Bus == null)
        {
            FrameworkLogger.Error("MerchantTrigger", $"Action=PurchaseFailed Reason=BusNull SlotIndex={slotIndex}");
            return;
        }

        if (PlayerTarget == null)
        {
            FrameworkLogger.Error("MerchantTrigger", $"Action=PurchaseFailed Reason=PlayerTargetNull SlotIndex={slotIndex}");
            return;
        }

        var slot = Slots[slotIndex];
        if (Economy == null || PlayerActor == null)
        {
            FrameworkLogger.Error("MerchantTrigger", $"Action=PurchaseFailed Reason=EconomyMissing SlotIndex={slotIndex}");
            return;
        }

        int coins = Economy.GetInventory(PlayerActor).Coins;
        if (coins < slot.GoldCost)
        {
            FrameworkLogger.Warn("MerchantTrigger",
                $"Action=PurchaseRejected Reason=InsufficientCoins SlotIndex={slotIndex} WeaponId={slot.WeaponId} Coins={coins} GoldCost={slot.GoldCost}");
            return;
        }

        FrameworkLogger.Info("MerchantTrigger",
            $"Action=Purchase SlotIndex={slotIndex} WeaponId={slot.WeaponId} GoldCost={slot.GoldCost}");

        Bus.Publish(new MerchantPurchaseEvent(PlayerTarget, slot.WeaponId, slot.GoldCost));

        _purchased[slotIndex] = true;
        RefreshHintUI();   // 灰显已购槽位
    }

    // ── 世界 UI 构造 ──────────────────────────────────────────────────

    void BuildHintUI()
    {
        _hintGO = new GameObject("MerchantHintUI");
        _hintGO.transform.SetParent(transform);
        _hintGO.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        _hintGO.transform.localRotation = Quaternion.identity;

        var canvas = _hintGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(4f, 2f);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        _hintGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        _slotTexts = new TextMeshPro[3];
        for (int i = 0; i < 3; i++)
        {
            var textGO = new GameObject($"SlotText_{i}");
            textGO.transform.SetParent(_hintGO.transform, false);

            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = $"[{i + 1}] —";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 2.5f;
            tmp.color = Color.white;

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.sizeDelta = new Vector2(400f, 40f);
            textRT.localPosition = new Vector3(0f, (1 - i) * 45f, 0f);

            _slotTexts[i] = tmp;
        }

        _hintGO.SetActive(false);
    }

    /// <summary>刷新 UI 文本（槽位 WeaponId + GoldCost；已购则灰显）。</summary>
    void RefreshHintUI()
    {
        if (_slotTexts == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (_slotTexts[i] == null) continue;

            if (Slots != null && i < Slots.Count)
            {
                var s = Slots[i];
                _slotTexts[i].text = $"[{i + 1}] {s.WeaponId} {s.GoldCost}g";
                _slotTexts[i].color = _purchased[i] ? Color.gray : Color.white;
            }
            else
            {
                _slotTexts[i].text = $"[{i + 1}] —";
                _slotTexts[i].color = Color.gray;
            }
        }
    }

    void DisplayHint(bool show)
    {
        if (_hintGO != null)
            _hintGO.SetActive(show);
    }
}
