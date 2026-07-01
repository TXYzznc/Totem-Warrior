using UnityEngine;
using TMPro;
using Tattoo.Data;

// TODO: 后续接 InputModule.GetKeyDown("Pickup")，当前回退到 Unity 原生 Input
// 因 MonoBehaviour 不许 GetModule，InputModule 访问点暂用 Input.GetKeyDown(KeyCode.F)

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

    // ── 内部状态 ───────────────────────────────────────────────────────
    bool _playerInRange;

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
        if (!other.CompareTag("Player")) return;

        // 若未注入 PlayerTarget，尝试从 collider 取
        if (PlayerTarget == null)
            PlayerTarget = other.GetComponent<Target>();

        _playerInRange = true;
        DisplayHint(true);
        FrameworkLogger.Info("WeaponPickupTrigger", $"Action=PlayerEntered WeaponId={WeaponId}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        DisplayHint(false);
        FrameworkLogger.Info("WeaponPickupTrigger", $"Action=PlayerExited WeaponId={WeaponId}");
    }

    void Update()
    {
        if (!_playerInRange) return;

        // TODO: 后续改为 InputModule.IsPickupPressed()
        if (Input.GetKeyDown(KeyCode.F))
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
