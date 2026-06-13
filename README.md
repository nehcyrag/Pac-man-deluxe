# Pac-Man Deluxe

![Pac-Man Deluxe](Assets/Resources/Menu/pacman_deluxe.png)

以 Unity 製作的 2D Pac-Man 延伸作品。在經典迷宮追逐玩法上加入雙人合作、不同追蹤策略的鬼魂、關卡難度成長，以及多種特殊道具。

## 遊戲特色

- 單人與本機雙人模式
- 程式化生成迷宮、豆子、鬼屋與傳送通道
- 多種鬼魂 AI：直接追蹤、預判路線、向量夾擊及隨機轉向
- 關卡推進與逐關加速
- 生命、分數、倒數、暫停與結算介面
- 背景音樂與遊戲音效
- 特殊道具會定時出現在迷宮中

## 操作方式

| 功能 | 單人／玩家 1 | 玩家 2 |
| --- | --- | --- |
| 移動 | 方向鍵 | `W` `A` `S` `D` |
| 使用能量彈／炸彈 | `Enter` | `Space` |

其他快捷鍵：

| 按鍵 | 功能 |
| --- | --- |
| `T` | 暫停／繼續 |
| `R` | 重新開始 |
| `Q` | 結束遊戲並顯示結果 |

## 特殊道具

| 道具 | 效果 |
| --- | --- |
| Power Pellet | 讓鬼魂暫時進入可被吃掉的驚嚇狀態 |
| Energy Pellet | 獲得一發能量彈，可擊倒路徑上的鬼魂 |
| Bomb Pellet | 獲得一枚炸彈，可放置並產生十字爆炸 |
| Phantom Pellet | 生成幻影誘餌，暫時干擾鬼魂追蹤 |
| Lightning Pellet | 觸發閃電連結攻擊鬼魂；雙人模式中才會隨機生成 |

能量彈與炸彈一次只能持有其中一種，使用後即消耗。

## 執行專案

### 環境需求

- Unity `6000.3.15f1`
- Universal Render Pipeline `17.3.0`
- 支援 Windows、macOS 或 Linux 的 Unity Editor

### 開啟與遊玩

1. 使用 Unity Hub 加入此專案資料夾。
2. 以 Unity `6000.3.15f1` 開啟專案。
3. 開啟 `Assets/Scenes/SampleScene.unity`。
4. 按下 Unity Editor 上方的 Play。
5. 在主選單選擇單人或雙人模式。

首次開啟時 Unity 會依照 `Packages/manifest.json` 自動安裝所需套件。

## 建置遊戲

1. 在 Unity 開啟 `File > Build Profiles`。
2. 選擇目標平台並切換平台。
3. 確認 `Assets/Scenes/SampleScene.unity` 已加入場景清單。
4. 選擇 `Build` 或 `Build And Run`。

## 專案結構

```text
Assets/
├─ Editor/       # 場景建置工具
├─ Resources/    # 選單圖片、道具圖片與音效
├─ Scenes/       # 主要遊戲場景
├─ Scripts/      # 遊戲流程、玩家、鬼魂、迷宮與道具邏輯
├─ Sprites/      # 執行時使用的 Sprite 資源
├─ UI/           # 主選單、HUD、倒數與結算畫面
└─ Settings/     # URP 與 2D Renderer 設定
```

主要程式：

- `GameManager.cs`：遊戲模式、關卡、生命、分數與特殊效果
- `MazeGenerator.cs`：迷宮、牆壁、豆子、鬼屋與傳送通道
- `PlayerController.cs`：玩家移動、碰撞、重生與道具使用
- `GhostController.cs`：鬼魂狀態、尋路與追蹤策略
- `BasicSceneBuilder.cs`：從 Unity 選單重建基本場景

如需重建基本場景，請先離開 Play Mode，再執行：

```text
Tools > Pac-Man Deluxe > Build Basic Scene
```

## 開發用快捷鍵

下列按鍵主要供測試使用：

| 按鍵 | 功能 |
| --- | --- |
| `N` | 跳至下一關 |
| `K` 或 `F9` | 切換無敵模式 |

## 聲明

本專案為學習與非官方同人用途，與 Bandai Namco Entertainment 無關。Pac-Man 名稱及相關角色權利屬於其各自權利人；發布或再利用專案內圖片、音訊等素材前，請自行確認授權狀態。
