using UnityEngine;
using TMPro;

// TODO: 后续接 InputModule.GetKeyDown("Pickup")，当前回退到 Unity 原生 Input
// 因 MonoBehaviour 不许 GetModule，InputModule 访问点暂用 Input.GetKeyDown(KeyCode.F)

/// <summary>
/// 宝箱交互触发器。挂在宝箱 GO 上。
/// ChestConfig 由 WeaponSpawnerModule Spawn 时注入（CONTRACT §C / §G）。
/// MonoBehaviour 内零 GetModule 调用。
/// </summary>
public sealed class ChestInteractTrigger : MonoBehaviour
{
    // ── 由 WeaponSpawnerModule 注入 ────────────────────────────────────
    /// <summary>宝箱类型 ID，对应 ChestConfig.ChestId。Spawn 时注入。</summary>
    public string ChestId;

    /// <summary>EventBus 引用，Spawn 时注入。</summary>
    public EventBus Bus;

    /// <summary>ChestConfig 引用，Spawn 时注入（MonoBehaviour 不许 GetModule）。</summary>
    public ChestConfig Cfg;

    // ── 内部状态 ───────────────────────────────────────────────────────
    bool _playerInRange;
    bool _isOpened;

    // ── 世界 UI 提示 ───────────────────────────────────────────────────
    GameObject _hintGO;
    TextMeshPro _hintText;

    // ── 已开宝箱的材质颜色 ─────────────────────────────────────────────
    static readonly Color OpenedColor = new Color(0.25f, 0.25f, 0.25f);

    void Awake()
    {
        BuildHintUI("[F] 开启宝箱");
    }

    void OnDestroy()
    {
        if (_hintGO != null)
            Destroy(_hintGO);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_isOpened) return;

        _playerInRange = true;
        DisplayHint(true);
        FrameworkLogger.Info("ChestInteractTrigger", $"Action=PlayerEntered ChestId={ChestId}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;
        DisplayHint(false);
        FrameworkLogger.Info("ChestInteractTrigger", $"Action=PlayerExited ChestId={ChestId}");
    }

    void Update()
    {
        if (!_playerInRange || _isOpened) return;

        // TODO: 后续改为 InputModule.IsPickupPressed()
        if (Input.GetKeyDown(KeyCode.F))
        {
            OnInteract();
        }

        // Billboarding
        if (_hintGO != null && Camera.main != null)
            _hintGO.transform.rotation = Camera.main.transform.rotation;
    }

    void OnInteract()
    {
        if (Bus == null)
        {
            FrameworkLogger.Error("ChestInteractTrigger", $"Action=OpenFailed Reason=BusNull ChestId={ChestId}");
            return;
        }

        if (Cfg == null)
        {
            FrameworkLogger.Error("ChestInteractTrigger", $"Action=OpenFailed Reason=CfgNull ChestId={ChestId}");
            return;
        }

        // 按 Probability 加权随机选一行
        var rows = Cfg.GetByChestId(ChestId);
        if (rows == null || rows.Count == 0)
        {
            FrameworkLogger.Error("ChestInteractTrigger", $"Action=OpenFailed Reason=NoRows ChestId={ChestId}");
            return;
        }

        var row = WeightedRandom(rows);
        if (row == null)
        {
            FrameworkLogger.Error("ChestInteractTrigger", $"Action=OpenFailed Reason=WeightedRandomFailed ChestId={ChestId}");
            return;
        }

        FrameworkLogger.Info("ChestInteractTrigger",
            $"Action=Open ChestId={ChestId} RewardType={row.RewardType} RewardId={row.RewardId} Amount={row.RewardAmount}");

        Bus.Publish(new ChestOpenedEvent(ChestId, row.RewardType, row.RewardId, row.RewardAmount, transform.position));

        // 标记已开，隐藏 UI，改材质颜色
        _isOpened = true;
        _playerInRange = false;
        DisplayHint(false);
        ApplyOpenedVisual();
    }

    /// <summary>Probability 加权随机（整数权重，同 ChestId 和必须 = 100）。</summary>
    static ChestConfigRow WeightedRandom(System.Collections.Generic.IReadOnlyList<ChestConfigRow> rows)
    {
        int total = 0;
        foreach (var r in rows) total += r.Probability;

        int roll = Random.Range(0, total);
        int cumulative = 0;
        foreach (var r in rows)
        {
            cumulative += r.Probability;
            if (roll < cumulative) return r;
        }
        return rows[rows.Count - 1];
    }

    void ApplyOpenedVisual()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            renderer.material.color = OpenedColor;
    }

    // ── 世界 UI 构造 ──────────────────────────────────────────────────

    void BuildHintUI(string text)
    {
        _hintGO = new GameObject("ChestHintUI");
        _hintGO.transform.SetParent(transform);
        _hintGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        _hintGO.transform.localRotation = Quaternion.identity;

        var canvas = _hintGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 0.5f);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        _hintGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var textGO = new GameObject("HintText");
        textGO.transform.SetParent(_hintGO.transform, false);

        _hintText = textGO.AddComponent<TextMeshPro>();
        _hintText.text = text;
        _hintText.alignment = TextAlignmentOptions.Center;
        _hintText.fontSize = 3f;
        _hintText.color = Color.yellow;

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
