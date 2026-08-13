using UnityEngine;

[DisallowMultipleComponent]
public sealed class TotemMapSceneAuthoring : MonoBehaviour
{
    [SerializeField] private Vector2 worldMin = new Vector2(-256f, -384f);
    [SerializeField] private Vector2 worldMax = new Vector2(256f, 384f);

    public Vector2 WorldMin => worldMin;
    public Vector2 WorldMax => worldMax;
    public Vector2 WorldSize => worldMax - worldMin;
    public Vector2 WorldCenter => (worldMin + worldMax) * 0.5f;

    public void Configure(Vector2 min, Vector2 max)
    {
        worldMin = Vector2.Min(min, max);
        worldMax = Vector2.Max(min, max);
    }
}
