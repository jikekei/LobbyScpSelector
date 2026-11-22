<div align="center">

# LobbyScpSelector

**SCP:SL（LabAPI）开箱即用的开始大厅 + 硬币 SCP 自选插件**

[简体中文](README.md) · [English](README_EN.md)

</div>

![大厅与 SCP 自选实机效果](assets/lobby-preview.png)

## 功能

- 内置默认大厅出生点、7 个 SCP 硬币位置和文字朝向，装完重启就能直接用，不改配置也行。
- 支持开局前大厅等待与硬币自选 SCP，游戏内用指令就能自由设置开始大厅出生点和硬币位置。
- 每个硬币上方会自动生成 `TextToy` 角色名牌；用指令挪位置时，文字朝向会跟着你的视角走，旧位置也会自动删掉并写回 `config.yml`。
- 开局会优先给选了硬币的玩家分配对应 SCP，同局不重复随机，7 人以下默认不能选 `SCP-079`。

---

## 快速安装

仅需 **2 个运行前提**：服务端自带 `LabApi >= 1.1.7` 与 `0Harmony.dll`。

1. 下载 `LobbyScpSelector.dll`（或从源码编译），放入服务端 `LabAPI/plugins/global/`（或指定端口插件目录）。
2. 确保 `0Harmony.dll` 已放在服务端 LabAPI 的 `dependencies/` 目录。
3. 重启服务端即可生效；首次启动会在配置目录自动生成英文默认 `config.yml` 和 `translations.yml`。

### 切换为简体中文

插件默认生成的 `config.yml`（注释）和 `translations.yml`（游戏内提示文本）均为英文。如需切换为中文界面与中文配置注释，将仓库中的 [translations/zh-CN/translations.yml](translations/zh-CN/translations.yml)（以及可选的 [translations/zh-CN/config.yml](translations/zh-CN/config.yml)）覆盖复制到服务端同插件配置目录后重启即可。

---

## 可选：游戏内调整点位

默认坐标已可直接开玩；如需微调位置，在管理控制台（RA）输入以下指令即可**自动生效并保存**，无需手动编辑 YAML：

| 指令 | 别名 | 权限 | 说明 |
| --- | --- | --- | --- |
| `scpcoin <scp>` | `scpc`, `scpfoot` | `scpselection.config` | 在脚下地面放置对应 SCP 硬币与上方文字（自动删除旧位置、对齐玩家朝向并保存） |
| `scpcoin <scp> <x> <y> <z>` | `scpc`, `scpfoot` | `scpselection.config` | 在指定世界坐标放置或移动对应 SCP 硬币 |
| `scpcoin remove <scp>` | `scpc`, `scpfoot` | `scpselection.config` | 删除该 SCP 硬币配置并移除实体 |
| `scpcoin list` / `reload` | `scpc`, `scpfoot` | `scpselection.config` | 查看全部配置坐标 / 重载当前大厅硬币 |
| `lobbyspawn set [x y z]` | `setlobbyspawn`, `lspawn` | `lobby.spawn` | 将大厅出生点设为当前位置或指定坐标并保存 |
| `lobbyspawn tp` / `get` | `setlobbyspawn`, `lspawn` | `lobby.spawn` | 传送至大厅出生点验证 / 查看当前坐标 |
| `scpgetpos` | `scpcoord` | `scpselection.position` | 查看自身当前世界坐标 |

> 支持的 `<scp>` 参数：`173`、`049`、`096`、`079`、`3114`、`106`、`939`（不区分大小写，可带 `scp` 前缀）。

---

<details>
<summary>查看默认 config.yml 预设与源码构建命令</summary>

```yaml
is_enabled: true
debug: false
lobby_spawn_position:
  x: -0.09920526
  y: 295.3805
  z: -8.435011
coin_scale: 17
min_players_for_scp079: 7
allow_scp_overflow: true
use_custom_start_queue: true
scp_selection_coin_positions:
  Scp173: { x: -2.656249, y: 294.4705, z: -12.48828 }
  Scp049: { x: 2.300781, y: 294.7761, z: -12.60156 }
  Scp096: { x: 8.140626, y: 294.7, z: -12.27734 }
  Scp079: { x: 3.214844, y: 294.4705, z: -3.140625 }
  Scp3114: { x: -2.425783, y: 294.4705, z: -3.070313 }
  Scp106: { x: -7.792969, y: 294.4705, z: -3.53125 }
  Scp939: { x: 7.660157, y: 295.0731, z: -3.160156 }
scp_selection_coin_label_rotations:
  Scp106: { x: 0, y: 2.469223, z: 0 }
  Scp3114: { x: 0, y: 0.2279663, z: 0 }
  Scp079: { x: 0, y: 6.880295, z: 0 }
  Scp939: { x: 0, y: 7.347214, z: 0 }
  Scp096: { x: 0, y: 180.401, z: 0 }
  Scp049: { x: 0, y: 180.2856, z: 0 }
  Scp173: { x: 0, y: 182.7411, z: 0 }
coin_label:
  enabled: true
  offset: { x: 0, y: 1.2, z: 0 }
  scale: { x: 0.5, y: 0.5, z: 0.5 }
  display_size: { x: 1000, y: 1000 }
  face_lobby_spawn: true
  default_rotation: { x: 0, y: 0, z: 0 }
  text_format: ""
```

从源码构建（`.NET Framework 4.8.1` / `x64`）：

```powershell
dotnet build LobbyScpSelector.csproj -c Release `
  -p:GameManagedDirectory="你的_SCPSL_Data\Managed_路径" `
  -p:PluginDependencyDirectory="你的_0Harmony与Assembly-CSharp-Publicized_目录"
```

> 若此前运行过旧版本，已有 `config.yml` 不会被自动覆盖；删除旧配置后重启即可重新生成上述默认预设。

</details>

---

基于 [MIT License](LICENSE) 开源。

