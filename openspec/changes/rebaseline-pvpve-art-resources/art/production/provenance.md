# 资源来源与可复现性

## 确定性生成资源

UI 面板、按钮、图标、HUD 纹理及 VFX 通用纹理由 `art/tools/generate_offline_art.py` 生成。脚本使用固定规则和固定随机种子，允许重复生成与追溯。

## AI 辅助背景 v02（当前候选）

- 文件：`ui/png/backgrounds/T_UI_FP_MainMenu_Background_Oasis_v02.png`
- 生成日期：2026-08-11
- 工具：OpenAI 内置图像生成
- 生成记录：`exec-d30fccaf-ecc3-40aa-8ea4-1a054c385abb`
- 参考：绿洲新城高位透视鸟瞰图、浅水盆和多肉花坛现有资源。
- 用途：首轮可玩版主菜单背景候选；不作为环境模型、结构尺寸或关卡布局依据。
- 约束：左侧低细节留白，右侧暖砂岩/石灰泥城市与青绿运河；不含人物、敌人、武器、文字、Logo、水印和 UI。
