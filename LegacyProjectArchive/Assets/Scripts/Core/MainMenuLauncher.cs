using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tattoo
{
    /// <summary>
    /// MainMenu 场景启动器（不依赖 GameApp / ModuleRunner）。
    /// 极简：点击 "开始游戏" 按钮 → 加载 Launch 场景 → GameApp 接管。
    ///
    /// 设计意图：
    ///   - MainMenu 场景纯展示与导航，没有 21 模块的开销
    ///   - 玩家不在主菜单时不消耗战斗系统资源
    ///   - 切到 Launch 后 GameApp.Start 完整初始化，进入战斗
    /// </summary>
    public sealed class MainMenuLauncher : MonoBehaviour
    {
        [SerializeField] Button _startBtn;
        [SerializeField] Button _quitBtn;
        [SerializeField] string _launchSceneName = "Launch";

        void Start()
        {
            if (_startBtn != null) _startBtn.onClick.AddListener(OnStartClicked);
            if (_quitBtn  != null) _quitBtn.onClick.AddListener(OnQuitClicked);
            Debug.Log("[MainMenuLauncher] Action=Ready");
        }

        public void OnStartClicked()
        {
            Debug.Log($"[MainMenuLauncher] Action=StartClicked LoadScene={_launchSceneName}");
            SceneManager.LoadScene(_launchSceneName);
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
