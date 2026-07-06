using UnityEngine;

/// <summary>
/// Lightweight visual-only projectile for basic attacks.
/// Damage and hit resolution stay in WeaponModule; this object only makes the shot readable.
/// </summary>
public sealed class AttackProjectileView : MonoBehaviour
{
    const float ArriveDistanceSq = 0.04f;

    Vector3 _target;
    Vector3 _direction;
    float _speed;
    float _life;
    float _age;
    TrailRenderer _trail;

    public static void Spawn(Vector3 start, Vector3 target, Color color, float speed, float life, float radius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "AttackProjectileView";
        go.transform.position = start;
        go.transform.localScale = Vector3.one * Mathf.Max(0.05f, radius);

        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var view = go.AddComponent<AttackProjectileView>();
        view.Initialize(target, color, speed, life);
    }

    void Initialize(Vector3 target, Color color, float speed, float life)
    {
        _target = target;
        _target.y = Mathf.Max(_target.y, transform.position.y);
        _direction = (_target - transform.position);
        if (_direction.sqrMagnitude < 0.001f)
            _direction = Vector3.forward;
        _direction.Normalize();

        _speed = Mathf.Max(0.1f, speed);
        _life = Mathf.Max(0.05f, life);

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = color;
            renderer.material = mat;
        }

        _trail = gameObject.AddComponent<TrailRenderer>();
        _trail.time = 0.18f;
        _trail.startWidth = 0.12f;
        _trail.endWidth = 0f;
        _trail.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _trail.startColor = color;
        _trail.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _age += dt;
        transform.position += _direction * (_speed * dt);

        if (_age >= _life || (transform.position - _target).sqrMagnitude <= ArriveDistanceSq)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
            Destroy(renderer.material);
        if (_trail != null && _trail.material != null)
            Destroy(_trail.material);
    }
}
