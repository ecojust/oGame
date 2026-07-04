# oGame — 拼豆游戏 (Perler Bead Game)

基于 Unity 2D Platformer Microgame 模板改造的拼豆（Perler Beads / Fuse Beads）游戏。

## 运行

| 环境     | 版本                                   |
| -------- | -------------------------------------- |
| Unity    | 6000.5.2f1 (Unity 6)                   |
| 渲染管线 | Universal Render Pipeline (URP) 17.4.0 |
| 输入系统 | Input System 1.19.0                    |

打开 `Assets/Scenes/SampleScene.unity`，点击 Play 即可。游戏启动后自动进入拼豆模式，旧的平台跳跃 UI 会被隐藏。

## 操作方式

| 操作                 | 说明     |
| -------------------- | -------- |
| 左键点击色板         | 选择颜色 |
| 左键点击画布格子     | 放置拼豆 |
| 右键点击画布上的拼豆 | 移除拼豆 |

## 项目结构

```
Assets/
├── Scenes/
│   └── SampleScene.unity          # 唯一场景，原平台跳跃关卡
│
├── Scripts/
│   ├── BeadGame/                  # ★ 拼豆游戏核心代码（新增）
│   │   ├── BeadGameManager.cs     # 主控：运行时创建 UI 层次，管理状态
│   │   ├── BeadCell.cs            # 单个格子：左键放置、右键移除
│   │   └── BeadGameSetup.cs       # 自动初始化（RuntimeInitializeOnLoadMethod）
│   │
│   ├── Core/                      # 仿真事件系统（原平台跳跃框架）
│   │   ├── Simulation.cs          #   事件调度器
│   │   ├── Simulation.Event.cs    #   事件基类
│   │   ├── Simulation.InstanceRegister.cs
│   │   ├── HeapQueue.cs           #   优先队列
│   │   └── Fuzzy.cs               #   模糊比较工具
│   │
│   ├── Gameplay/                  # 仿真事件实现（原平台跳跃）
│   │   ├── PlayerDeath.cs, PlayerJumped.cs, PlayerLanded.cs 等 12 个
│   │   └── EnemyDeath.cs, PlayerEnemyCollision.cs 等
│   │
│   ├── Mechanics/                 # 游戏组件和物理（原平台跳跃）
│   │   ├── PlayerController.cs    #   玩家控制
│   │   ├── KinematicObject.cs     #   2D 运动学物理
│   │   ├── EnemyController.cs     #   敌人 AI
│   │   ├── GameController.cs      #   全局游戏控制器
│   │   └── Health.cs, DeathZone.cs, VictoryZone.cs 等
│   │
│   ├── Model/
│   │   └── PlatformerModel.cs     # 数据模型
│   │
│   ├── UI/
│   │   ├── MetaGameController.cs  # 主菜单/游戏切换（当前被隐藏）
│   │   └── MainUIController.cs    # UI 面板切换
│   │
│   └── View/
│       ├── ParallaxLayer.cs       # 视差滚动
│       └── AnimatedTile.cs        # 瓦片动画
│
├── Prefabs/
│   ├── UI Canvas.prefab           # 原 UI Canvas（被拼豆覆盖）
│   ├── Player.prefab, Enemy.prefab, Button.prefab 等
│   └── CinemachineConfiner.prefab
│
├── Character/                     # 角色动画 & 精灵图
├── Environment/                   # 环境贴图 & 瓦片
├── Audio/                         # 音效（跳跃、死亡、收集等）
├── Mod Assets/                    # 可选的扩展资源
├── Rendering/                     # URP 管线配置
├── Settings/                      # 输入配置、渲染配置
├── TextMesh Pro/                  # TMP 字体和着色器
├── Tiles/                         # Tilemap 瓦片资源
└── Tutorials/                     # Unity Learn 交互教程
```

## 拼豆模块说明

### 启动流程

1. `BeadGameSetup.AutoSetup()` 通过 `[RuntimeInitializeOnLoadMethod]` 在场景加载后自动执行
2. 检查场景中是否已有 `BeadGameManager`，没有则创建
3. `BeadGameManager.Start()` 运行时构建完整 UI

### UI 层次（运行时动态创建）

```
BeadCanvas (ScreenSpaceOverlay)
├── Bg                              # 全屏深色背景
├── PegboardPanel (左 65%)          # 拼豆画板
│   └── Cell_0_0 ~ Cell_15_15      # 16×16 圆形拼豆格子
└── SidePanel (右 35%)
    ├── PatternPanel (上 50%)       # 图案预览（红色心形）
    └── PalettePanel (下 50%)       # 12 色调色板
```

### 核心配置

`BeadGameManager` 的 Inspector 可调参数：

| 参数                       | 默认值 | 说明                                |
| -------------------------- | ------ | ----------------------------------- |
| `gridWidth` / `gridHeight` | 16     | 画布格子数                          |
| `paletteColors`            | 12 色  | 调色板颜色数组                      |
| `paletteColumns`           | 4      | 色板列数                            |
| `patternCsv`               | (空)   | 自定义图案，逗号分隔的矩阵，-1 为空 |

### 图案格式

`patternCsv` 使用逗号分隔的数字矩阵，每行一个换行：

```
0,0,0,0,0
0,1,1,1,0
0,0,1,0,0
```

数字对应 `paletteColors` 的索引，`-1` 表示空白。留空则自动生成心形图案。

## 原平台跳跃代码

原 Platformer 的完整代码保留在 `Scripts/Core/`、`Scripts/Gameplay/`、`Scripts/Mechanics/` 中。这些代码在运行时会随旧 Canvas 一起被隐藏（`BeadGameManager.Start()` 会禁用所有已有的 Canvas），不会影响拼豆游戏。如需恢复平台跳跃功能，移除或禁用 `BeadGameSetup.cs` 即可。
