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
                    $"Action=FallbackCube WeaponId={e.WeaponId} Prefab={e.WeaponPrefabPath} missing");
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(_weaponSlot, worldPositionStays: false);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale    = new Vector3(0.15f, 0.4f, 0.08f);
                _currentWeapon = cube;
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
    }
}
