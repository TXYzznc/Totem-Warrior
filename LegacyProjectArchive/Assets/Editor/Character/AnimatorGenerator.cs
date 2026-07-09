using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Tattoo.EditorTools.Character
{
    /// <summary>
    /// 菜单：Tools/Character/Generate Animator from Sprite Folder
    ///
    /// 扫描 Assets/Resources/Sprite/Character/<Name>/<Action>/<Dir>.png，
    /// 批量生成 AnimationClip、AnimatorController、Prefab。
    /// 支持 4 个角色：Player1 / Player2 / Player3 / Boss1。
    /// </summary>
    public sealed class AnimatorGenerator : EditorWindow
    {
        // ─── 常量 ───────────────────────────────────────────────────────────
        const string SpriteRoot   = "Assets/Resources/Sprite/Character";
        const string AnimRoot     = "Assets/Resources/Anim/Character";
        const string PrefabRoot   = "Assets/Resources/Prefab/Character";

        static readonly string[] Characters = { "Player1", "Player2", "Player3", "Boss1" };
        static readonly string[] Actions    = { "Idle", "Walk", "Attack", "Death" };
        // 顺序与 PlayerAnimatorBridge.ComputeDirection 保持一致：Down=0 / Up=1 / Left=2 / Right=3
        static readonly string[] Directions = { "Down", "Up", "Left", "Right" };

        // Direction 参数值：与 Directions 数组下标对应
        // Down=0 Up=1 Left=2 Right=3
        const int FrameCount   = 4;
        const float FrameRate  = 8f;      // 8 fps
        const float FrameTime  = 1f / FrameRate;

        // ─── Window ─────────────────────────────────────────────────────────
        int _selectedCharIndex = 0; // 0 = 全部
        bool _generateAll = true;

        [MenuItem("Tools/Character/Generate Animator from Sprite Folder")]
        static void Open() => GetWindow<AnimatorGenerator>("角色 Animator 生成器");

        /// <summary>headless 入口：直接对全部 4 个角色批量生成，方便 editor_execute_menu 远程驱动。</summary>
        [MenuItem("Tools/Character/Generate All (Headless)")]
        static void GenerateAllHeadless()
        {
            GenerateAll();
            Debug.Log("[AnimatorGenerator] Headless Action=GenerateAll Done");
        }

        /// <summary>一键：先 ForceReimport 把 PPU/pivot/spritesheet 应用到现有 PNG，再 GenerateAll 出 anim+controller+prefab。</summary>
        [MenuItem("Tools/Character/Reimport Then Generate All")]
        static void ReimportThenGenerateAll()
        {
            ForceReimportSprites();
            GenerateAll();
            // BUG-17-01: 同帧 SaveAsPrefabAsset + Reimport 后立即 Play 会导致 runtimeAnimatorController=null，
            // 末尾再走一次 SaveAssets+Refresh 确保 asset DB 把 Animator → Prefab 引用回写稳定。
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AnimatorGenerator] ReimportThenGenerateAll done");
        }

        /// <summary>强制重导 Sprite/Character 下全部贴图，保证 PPU/pivot/spritesheet 设置生效。</summary>
        [MenuItem("Tools/Character/Force Reimport Sprites")]
        static void ForceReimportSprites()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteRoot });
            int n = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                n++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AnimatorGenerator] ForceReimport count={n} root={SpriteRoot}");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("角色 Animator 生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _generateAll = EditorGUILayout.Toggle("处理全部角色", _generateAll);

            if (!_generateAll)
            {
                _selectedCharIndex = EditorGUILayout.Popup("角色", _selectedCharIndex, Characters);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("生成"))
            {
                if (_generateAll)
                    GenerateAll();
                else
                    GenerateForCharacter(Characters[_selectedCharIndex]);
            }
        }

        // ─── 入口 ────────────────────────────────────────────────────────────
        static void GenerateAll()
        {
            foreach (var name in Characters)
                GenerateForCharacter(name);
        }

        static void GenerateForCharacter(string charName)
        {
            EnsureFolder(AnimRoot);
            EnsureFolder(PrefabRoot);

            string charAnimDir = $"{AnimRoot}/{charName}";
            EnsureFolder(charAnimDir);

            int generated = 0;
            int skipped   = 0;

            // 1. 生成 AnimationClip
            var clips = new Dictionary<string, AnimationClip>(); // key = "Action_Dir"

            foreach (var action in Actions)
            {
                foreach (var dir in Directions)
                {
                    string spritePath = $"{SpriteRoot}/{charName}/{action}/{dir}.png";
                    var sprites = LoadSprites(spritePath);

                    if (sprites == null || sprites.Length == 0)
                    {
                        skipped++;
                        continue;
                    }

                    string clipKey  = $"{action}_{dir}";
                    string clipPath = $"{charAnimDir}/{clipKey}.anim";

                    var clip = BuildClip(clipKey, action, sprites);
                    AssetDatabase.CreateAsset(clip, clipPath);
                    clips[clipKey] = clip;
                    generated++;
                }
            }

            // 2. 生成 AnimatorController
            string controllerPath = $"{charAnimDir}/Controller.controller";
            var controller = BuildController(controllerPath, clips);

            // 3. 生成 Prefab
            BuildPrefab(charName, controller, clips);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AnimatorGenerator] {charName}：已生成 {generated} 个 clip，跳过 {skipped} 个缺失文件。");
        }

        // ─── AnimationClip ───────────────────────────────────────────────────
        static AnimationClip BuildClip(string clipName, string action, Sprite[] sprites)
        {
            var clip = new AnimationClip
            {
                name      = clipName,
                frameRate = FrameRate,
            };

            // Loop 设置
            bool isLoop = action == "Idle" || action == "Walk";
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // SpriteRenderer.sprite 的 Object Reference Curve
            var binding = EditorCurveBinding.PPtrCurve(
                "",
                typeof(SpriteRenderer),
                "m_Sprite"
            );

            int frameCount = Mathf.Min(sprites.Length, FrameCount);
            var keyframes  = new ObjectReferenceKeyframe[frameCount + (isLoop ? 0 : 1)];

            for (int i = 0; i < frameCount; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time  = i * FrameTime,
                    value = sprites[i],
                };
            }

            // 非循环动画：末帧停在最后一帧（避免 Unity 自动插值到空）
            if (!isLoop)
            {
                keyframes[frameCount] = new ObjectReferenceKeyframe
                {
                    time  = frameCount * FrameTime,
                    value = sprites[frameCount - 1],
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            return clip;
        }

        // ─── AnimatorController ──────────────────────────────────────────────
        static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips)
        {
            // 删除旧文件（如果存在）
            if (File.Exists(Path.GetFullPath(path)))
                AssetDatabase.DeleteAsset(path);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var root       = controller.layers[0].stateMachine;

            // 参数
            controller.AddParameter("Direction",     AnimatorControllerParameterType.Int);
            controller.AddParameter("IsMoving",      AnimatorControllerParameterType.Bool);
            controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die",           AnimatorControllerParameterType.Trigger);
            // Dead 用于把 Death_<Dir> 状态钉住：所有 AnyState→Idle/Walk 转换都要求 Dead==false
            controller.AddParameter("Dead",          AnimatorControllerParameterType.Bool);

            // ── 4 个顶层 State，每个 State 有 4 个按 Direction 切换的子 State ──
            // 简单方案：顶层 4 状态（Idle / Walk / Attack / Death），
            // 每个顶层状态内用 AnyState→Idle_Dir 处理方向，
            // 这里选更易读的方式：每个顶层 State motion 用 BlendTree（1D，parameter=Direction）
            // 但 BlendTree 在 AnimatorController 脚本 API 里用 ChildMotion 数组支持，
            // 这里直接建：
            //   顶层 State = Idle，默认 motion = Idle_Down（兜底）
            //   再建 4 个 AnyState → Idle_Dir transition（Direction Equals N）
            // 按任务描述的"第一版简单方案"：顶层 4 State + AnyState Transitions
            // ────────────────────────────────────────────────────────────────

            // 辅助：拿 clip，找不到返回 null
            AnimationClip GetClip(string action, string dir)
            {
                clips.TryGetValue($"{action}_{dir}", out var c);
                return c;
            }

            // 默认方向是 Down
            AnimationClip DefaultClip(string action) =>
                GetClip(action, "Down") ?? GetClip(action, "Right") ?? GetClip(action, "Left") ?? GetClip(action, "Up");

            // 建顶层 4 State
            var stateIdle   = root.AddState("Idle");
            var stateWalk   = root.AddState("Walk");
            var stateAttack = root.AddState("Attack");
            var stateDeath  = root.AddState("Death");

            stateIdle.motion   = DefaultClip("Idle");
            stateWalk.motion   = DefaultClip("Walk");
            stateAttack.motion = DefaultClip("Attack");
            stateDeath.motion  = DefaultClip("Death");

            // 默认状态 = Idle
            root.defaultState = stateIdle;

            // ── 方向 Sub-State：AnyState → <Action>_<Dir> ──────────────────
            // 为节省状态数，方向切换直接改顶层 State 的 motion（运行时用 Animator.SetInteger）；
            // 但纯 Editor 脚本 API 不支持运行时换 motion；
            // 标准做法：每个 Action 建 4 个方向 Sub-State，由 AnyState + Direction 参数切换。
            // 任务要求"第一版用简单方案，可读性优先"——执行如下：
            //   为每个 (Action, Dir) 建 State，AnyState 按 (ActionParam, Direction) 转入。
            // 不引入 State 参数；Bridge 只设置 IsMoving / Direction / 2 个 trigger。

            string[] dirNames = Directions; // Up/Down/Left/Right
            // 删掉之前建的无方向 State，改为有方向的细化状态
            root.RemoveState(stateIdle);
            root.RemoveState(stateWalk);
            root.RemoveState(stateAttack);
            root.RemoveState(stateDeath);

            // 重建：Action × Dir
            var stateMap = new Dictionary<string, AnimatorState>();

            for (int ai = 0; ai < Actions.Length; ai++)
            {
                string action = Actions[ai];
                for (int di = 0; di < dirNames.Length; di++)
                {
                    string dir       = dirNames[di];
                    string key       = $"{action}_{dir}";
                    var   motion     = GetClip(action, dir);

                    var state        = root.AddState(key);
                    state.motion     = motion; // null 时 Unity 显示空 motion，不报错
                    stateMap[key]    = state;
                }
            }

            // 默认状态 = Idle_Down
            if (stateMap.TryGetValue("Idle_Down", out var defaultState))
                root.defaultState = defaultState;

            // ── Transitions ─────────────────────────────────────────────────
            // Idle_<Dir>: Direction==di && !IsMoving
            // Walk_<Dir>: Direction==di && IsMoving
            // Attack_<Dir>: Direction==di && AttackTrigger
            // Death_<Dir>: Direction==di && Die
            // —— 不引入 State 参数；Idle/Walk 仅靠 IsMoving 区分，Attack/Death 仅靠 trigger。

            // Idle_<Dir>: !Dead && !IsMoving && Direction==di
            for (int di = 0; di < dirNames.Length; di++)
            {
                string key = $"Idle_{dirNames[di]}";
                if (!stateMap.TryGetValue(key, out var targetState)) continue;
                var t = root.AddAnyStateTransition(targetState);
                t.hasExitTime         = false;
                t.duration            = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.IfNot,   0, "Dead");
                t.AddCondition(AnimatorConditionMode.Equals,  di, "Direction");
                t.AddCondition(AnimatorConditionMode.IfNot,   0, "IsMoving");
            }

            // Walk_<Dir>: !Dead && IsMoving && Direction==di
            for (int di = 0; di < dirNames.Length; di++)
            {
                string key = $"Walk_{dirNames[di]}";
                if (!stateMap.TryGetValue(key, out var targetState)) continue;
                var t = root.AddAnyStateTransition(targetState);
                t.hasExitTime         = false;
                t.duration            = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.IfNot,   0, "Dead");
                t.AddCondition(AnimatorConditionMode.Equals,  di, "Direction");
                t.AddCondition(AnimatorConditionMode.If,      0, "IsMoving");
            }

            // Death：任意 → Death_<Dir>（Die Trigger），按当前 Direction
            for (int di = 0; di < dirNames.Length; di++)
            {
                string key = $"Death_{dirNames[di]}";
                if (!stateMap.TryGetValue(key, out var deathState)) continue;
                var t = root.AddAnyStateTransition(deathState);
                t.hasExitTime         = false;
                t.duration            = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If,      0, "Die");
                t.AddCondition(AnimatorConditionMode.Equals,  di, "Direction");
            }

            // Attack：任意 → Attack_<Dir>（AttackTrigger），按当前 Direction；Dead 时禁止
            for (int di = 0; di < dirNames.Length; di++)
            {
                string key = $"Attack_{dirNames[di]}";
                if (!stateMap.TryGetValue(key, out var attackState)) continue;
                var t = root.AddAnyStateTransition(attackState);
                t.hasExitTime         = false;
                t.duration            = 0f;
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.IfNot,   0, "Dead");
                t.AddCondition(AnimatorConditionMode.If,      0, "AttackTrigger");
                t.AddCondition(AnimatorConditionMode.Equals,  di, "Direction");
            }

            // Attack / Death → Idle（Attack 退出时回 Idle）
            for (int di = 0; di < dirNames.Length; di++)
            {
                string dir = dirNames[di];
                if (!stateMap.TryGetValue($"Attack_{dir}", out var attackState)) continue;
                if (!stateMap.TryGetValue($"Idle_{dir}",   out var idleState))   continue;

                var t = attackState.AddTransition(idleState);
                t.hasExitTime = true;
                t.exitTime    = 1f;
                t.duration    = 0f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        // ─── Prefab ──────────────────────────────────────────────────────────
        static void BuildPrefab(string charName, AnimatorController controller, Dictionary<string, AnimationClip> clips)
        {
            string prefabPath = $"{PrefabRoot}/{charName}.prefab";
            EnsureFolder(PrefabRoot);

            // 找 Idle_Down 第 1 帧作为默认 sprite
            Sprite defaultSprite = null;
            var idleDownSprites = LoadSprites($"{SpriteRoot}/{charName}/Idle/Down.png");
            if (idleDownSprites != null && idleDownSprites.Length > 0)
                defaultSprite = idleDownSprites[0];

            var go = new GameObject(charName);

            var sr        = go.AddComponent<SpriteRenderer>();
            sr.sprite     = defaultSprite;

            var animator  = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            go.AddComponent<EntityRef>();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
        }

        // ─── 工具方法 ────────────────────────────────────────────────────────
        /// <summary>加载 sprite sheet 中所有子 Sprite（Multiple 切分后的）。</summary>
        static Sprite[] LoadSprites(string path)
        {
            if (!File.Exists(Path.GetFullPath(path))) return null;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            var result = new List<Sprite>();
            foreach (var a in all)
            {
                if (a is Sprite s) result.Add(s);
            }
            // 按 name 排序（_0 _1 _2 _3）
            result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return result.ToArray();
        }

        /// <summary>确保目录存在，不存在则递归创建。</summary>
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            // 递归确保父目录
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            string folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
