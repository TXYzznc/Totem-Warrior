using UnityEngine;
using TMPro;
using Tattoo.Data;
using Tattoo;

// Runtime dependencies are injected by WeaponSpawnerModule; no direct GetModule calls here.

/// <summary>
/// 武器拾取触发器。挂在武器 Pickup GO 上。
/// 字段由 WeaponSpawnerModule.SpawnPickup 在 Instantiate 后直接注入（CONTRACT §C）。
/// MonoBehaviour 内零 GetModule 调用。
/// </summary>
public sealed class WeaponPickupTrigger : MonoBehaviour
{
    // ── 由 WeaponSpawnerModule 注入 ────────────────────────────────────
    /// <summary>对应 WeaponConfig.WeaponId，Spawn 时由 WeaponSpawnerModule 注入。</summary>
    public string WeaponId;

    /// <summary>EventBus 引用，Spawn 时由 WeaponSpawnerModule 注入。</summary>
    public EventBus Bus;

    /// <summary>玩家 Target，OnTriggerEnter 时从 collider.GetComponent 取，或 Spawn 时注入。</summary>
    public Target PlayerTarget;

    /// <summary>输入入口。由 WeaponSpawnerModule 注入，保证拾取按键走 InputModule。</summary>
    public InputModule Input;
    public Transform PlayerTransform;

    // ── 内部状态 ───────────────────────────────────────────────────────
    bool _playerInRange;
    const float InteractRadius = 1.6f;

    // ── 世界 UI 提示 ───────────────────────────────────────────────────
    GameObject _hintGO;
    TextMeshPro _hintText;

    void Awake()
    {
        BuildHintUI("[F] 拾取");
    }

    void OnDestroy()
    {
        if (_hintGO != null)
            Destroy(_hintGO);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;

        // 若未注入 PlayerTarget，尝试从 collider 取
        if (PlayerTarget == null)
            PlayerTarget = other.GetComponent<EntityRef>()?.Target;

        _playerInRange = true;
        DisplayHint(true);
        FrameworkLogger.Info("WeaponPickupTrigger", $"Action=PlayerEntered WeaponId={WeaponId}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        _playerInRange = false;
        DisplayHint(false);
        FrameworkLogger.Info("WeaponPickupTrigger", $"Action=PlayerExited WeaponId={WeaponId}");
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

        if (Input != null && Input.IsPickupPressed())
        {
            if (Bus == null)
            {
                FrameworkLogger.Error("WeaponPickupTrigger", $"Action=PickupFailed Reason=BusNull WeaponId={WeaponId}");
                return;
            }

            if (PlayerTarget == null)
            {
                FrameworkLogger.Error("WeaponPickupTrigger", $"Action=PickupFailed Reason=PlayerTargetNull WeaponId={WeaponId}");
                return;
            }

            FrameworkLogger.Info("WeaponPickupTrigger", $"Action=Pickup WeaponId={WeaponId} Pos={transform.position}");

            // 由 WeaponSpawnerModule.OnWeaponPickedUp 负责 Destroy 本 GO
            Bus.Publish(new WeaponPickedUpEvent(PlayerTarget, WeaponId, transform.position));

            _playerInRange = false;
            DisplayHint(false);
        }

        // Billboarding：UI 朝向 Camera
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
        DisplayHint(inRange);
        FrameworkLogger.Info("WeaponPickupTrigger",
            inRange
                ? $"Action=PlayerEntered WeaponId={WeaponId} Source=Distance"
                : $"Action=PlayerExited WeaponId={WeaponId} Source=Distance");
    }

    // ── 世界 UI 构造 ──────────────────────────────────────────────────

    void BuildHintUI(string text)
    {
        _hintGO = new GameObject("PickupHintUI");
        _hintGO.transform.SetParent(transform);
        _hintGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        _hintGO.transform.localRotation = Quaternion.identity;

        // Canvas（WorldSpace）
        var canvas = _hintGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 0.5f);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        _hintGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        // TextMeshPro
        var textGO = new GameObject("HintText");
        textGO.transform.SetParent(_hintGO.transform, false);

        _hintText = textGO.AddComponent<TextMeshPro>();
        _hintText.text = text;
        _hintText.alignment = TextAlignmentOptions.Center;
        _hintText.fontSize = 3f;
        _hintText.color = Color.white;

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.sizeDelta = new Vector2(200f, 50f);
        textRT.localPosition = Vector3.zero;

        _hintGO.SetActive(false);
    }

    void DisplayHint(bool show)
    {
        if (_hintGO != null)
            _hintGO.SetActive(show);
    }
}
