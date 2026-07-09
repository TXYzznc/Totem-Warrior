using System;
using AttackSystem.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 监听 WeaponEquippedEvent，在玩家手部挂点上卸旧装新武器 prefab。
    /// 由 SpawnerModule 在玩家 GameObject 上 AddComponent 并调用 Init。
    /// </summary>
    public sealed class PlayerWeaponMounter : MonoBehaviour
    {
        [SerializeField] Transform _weaponSlot; // 武器挂点，Inspector 可指定手部骨骼

        EventBus _bus;
        ModuleRunner _runner;
        GameObject _currentWeapon;
        IDisposable _sub;

        // 缓存 gameObject 引用，避免在事件回调中访问被销毁对象
        GameObject _cachedGameObject;

        public void Init(EventBus bus, ModuleRunner runner)
        {
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _cachedGameObject = gameObject;

            _sub = _bus.Subscribe<WeaponEquippedEvent>(OnWeaponEquipped);

            // 挂点 fallback：递归找 WeaponSlot 节点，找不到就用自身 transform
            if (_weaponSlot == null)
                _weaponSlot = FindRecursive(transform, "WeaponSlot") ?? transform;
        }

        void OnWeaponEquipped(WeaponEquippedEvent e)
        {
            // 只处理绑定到玩家 Target 的事件（Mounter 仅挂在玩家上，PlayerTarget 引用唯一）
            if (e.Actor == null) return;
            var spawner = _runner.GetModule<SpawnerModule>();
            if (spawner == null || !ReferenceEquals(spawner.PlayerTarget, e.Actor)) return;

            // 卸载当前武器
            if (_currentWeapon != null)
            {
                Destroy(_currentWeapon);
                _currentWeapon = null;
            }

            if (string.IsNullOrEmpty(e.WeaponPrefabPath))
            {
                FrameworkLogger.Warn("PlayerWeaponMounter",
                    $"Actor={e.Actor.Name} WeaponId={e.WeaponId} WeaponPrefabPath is empty, skip mount");
                return;
            }

            var prefab = Resources.Load<GameObject>(e.WeaponPrefabPath);
            if (prefab != null)
            {
                _currentWeapon = Instantiate(prefab, _weaponSlot);
                _currentWeapon.transform.localPosition = Vector3.zero;
                _currentWeapon.transform.localRotation = Quaternion.identity;
                FrameworkLogger.Info("PlayerWeaponMounter",
                    $"Action=Mount WeaponId={e.WeaponId} Prefab={e.WeaponPrefabPath}");
            }
            else
            {
                // prefab 缺失时 fallback Cube + Warn，不阻断运行
                FrameworkLogger.Warn("PlayerWeaponMounter",
                    $"Action=FallbackWeapon WeaponId={e.WeaponId} Prefab={e.WeaponPrefabPath} missing");
                _currentWeapon = BuildFallbackWeapon(e.WeaponId);
                _currentWeapon.transform.SetParent(_weaponSlot, worldPositionStays: false);
                _currentWeapon.transform.localPosition = Vector3.zero;
                _currentWeapon.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            }
        }

        void OnDestroy()
        {
            _sub?.Dispose();
        }

        // 深度优先递归查找指定名称的 Transform；找不到返回 null
        static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static GameObject BuildFallbackWeapon(string weaponId)
        {
            var root = new GameObject("FallbackWeapon_" + weaponId);
            var grip = AddPart(root.transform, "Grip", PrimitiveType.Cube,
                new Vector3(0f, -0.08f, 0f), new Vector3(0.08f, 0.24f, 0.08f),
                new Color(0.16f, 0.11f, 0.07f));
            var blade = AddPart(root.transform, "Blade", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 0f), new Vector3(0.12f, 0.38f, 0.04f),
                new Color(0.75f, 0.82f, 0.9f));
            var guard = AddPart(root.transform, "Guard", PrimitiveType.Cube,
                new Vector3(0f, 0.01f, 0f), new Vector3(0.26f, 0.05f, 0.06f),
                new Color(0.95f, 0.68f, 0.18f));

            DisableCollider(grip);
            DisableCollider(blade);
            DisableCollider(guard);
            return root;
        }

        static GameObject AddPart(Transform parent, string name, PrimitiveType type,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = color;
            }
            return go;
        }

        static void DisableCollider(GameObject go)
        {
            var collider = go != null ? go.GetComponent<Collider>() : null;
            if (collider != null) collider.enabled = false;
        }
    }
}
