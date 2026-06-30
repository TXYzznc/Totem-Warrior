using System;
using System.Collections.Generic;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 玩家 Animator 桥接器。每帧从 InputModule.GetMoveDirection() 读方向写 Animator 参数；
    /// 攻击/死亡走 EventBus 事件触发 Trigger。
    /// 挂在玩家 GameObject 上，由 SpawnerModule 实例化后调用 Init(bus, runner)。
    /// </summary>
    public sealed class PlayerAnimatorBridge : MonoBehaviour
    {
        Animator     _anim;
        EventBus     _bus;
        ModuleRunner _runner;
        InputModule  _input;
        int          _lastDir = 0; // 0=Down 默认朝下
        bool         _isDead;
        readonly List<IDisposable> _subs = new();

        public void Init(EventBus bus, ModuleRunner runner)
        {
            _anim   = GetComponent<Animator>();
            _bus    = bus;
            _runner = runner;
            if (_anim == null)
            {
                FrameworkLogger.Warn("PlayerAnimatorBridge", "Animator missing on Player GameObject");
                return;
            }
            _subs.Add(_bus.Subscribe<AttackHitEvent>(OnAttack));
            _subs.Add(_bus.Subscribe<PlayerDiedEvent>(OnDie));
        }

        void Update()
        {
            if (_anim == null) return;
            if (_isDead) return; // 死后不再改任何 Animator 参数，避免 AnyState 把 Death 状态打掉
            if (_input == null)
            {
                // InputModule 不在 Bridge 的 Dependencies 中（Bridge 不是 IGameModule），运行时懒查询
                try { _input = _runner?.GetModule<InputModule>(); } catch { return; }
                if (_input == null) return;
            }

            var v = _input.GetMoveDirection();
            bool isMoving = v.sqrMagnitude > 0.0025f;
            _anim.SetBool("IsMoving", isMoving);
            if (isMoving)
            {
                _lastDir = ComputeDirection(v);
                _anim.SetInteger("Direction", _lastDir);
            }
        }

        void OnAttack(AttackHitEvent e)
        {
            if (_anim != null) _anim.SetTrigger("AttackTrigger");
        }

        void OnDie(PlayerDiedEvent e)
        {
            if (_anim != null)
            {
                _anim.SetBool("Dead", true);
                _anim.SetTrigger("Die");
            }
            _isDead = true;
        }

        void OnDestroy()
        {
            foreach (var d in _subs) d.Dispose();
            _subs.Clear();
        }

        static int ComputeDirection(Vector2 v)
        {
            // 0=Down, 1=Up, 2=Left, 3=Right；|y| >= |x| 时走上下
            if (Mathf.Abs(v.y) >= Mathf.Abs(v.x)) return v.y >= 0f ? 1 : 0;
            return v.x >= 0f ? 3 : 2;
        }
    }
}
