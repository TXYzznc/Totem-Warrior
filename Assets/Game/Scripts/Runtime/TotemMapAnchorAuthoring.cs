using UnityEngine;

[DisallowMultipleComponent]
public sealed class TotemMapAnchorAuthoring : MonoBehaviour
{
    [SerializeField] private string anchorId = string.Empty;
    [SerializeField] private TotemMapAnchorKind kind = TotemMapAnchorKind.Unknown;
    [SerializeField, Min(0.1f)] private float searchRadius = 6f;
    [SerializeField] private bool reachable = true;

    public string AnchorId => anchorId;
    public TotemMapAnchorKind Kind => kind;
    public float SearchRadius => searchRadius;
    public bool IsReachable => reachable;

    public void Configure(string id, TotemMapAnchorKind anchorKind, float radius, bool isReachable = true)
    {
        anchorId = id ?? string.Empty;
        kind = anchorKind;
        searchRadius = Mathf.Max(0.1f, radius);
        reachable = isReachable;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = kind switch
        {
            TotemMapAnchorKind.PlayerSpawn => new Color(0.2f, 0.8f, 1f, 0.8f),
            TotemMapAnchorKind.Resource => new Color(1f, 0.75f, 0.15f, 0.8f),
            TotemMapAnchorKind.Extraction => new Color(0.25f, 1f, 0.35f, 0.8f),
            _ => Color.magenta,
        };
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}
