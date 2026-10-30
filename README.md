# NearClipEdit

修改 Koikatu 主相机近裁剪面（near clip plane）的独立插件，用于近距离的模型不被裁掉提前穿模。

## 前置依赖

- Koikatu / Koikatsu Party（含 CharaStudio）
- BepInEx 5.4.x（64 位）

## 安装

把 `NearClipEdit.dll` 放进 `BepInEx/plugins/`。

## 配置（F1）

| 配置项 | 默认 | 说明 |
|---|---|---|
| `Enabled` | 开 | 总开关 |
| `Near clip plane` | 0.001 | 范围 0.001 ~ 1.0。默认 0.001 最贴近，0.01 ~ 0.05 更保守 |
| `Koikatu.exe` | 开 | 在游戏本体里生效 |
| `KoikatuVR.exe` | 关 | 在本体 VR 启动器里生效 |
| `CharaStudio.exe` | 关 | 在工作室里生效 |

- KoikatuVR插件已经有修改近裁剪值的功能，默认关闭
- CharaStudio那边已经有修改裁剪值的插件，默认关闭
- 如果远景出现闪烁这在0.001数值下是正常现象，这是为了获得近距离不穿模的代价，改成0.003可以缓解

## 实现说明

- 只依赖 `UnityEngine` + `BepInEx`，不引用任何游戏程序集，同一个 DLL 可在三个 exe 中使用。
- 目标相机取 `Camera.main`（游戏自身也用这个），拿不到时退化为"面积最大的启用相机"。
- 游戏自身从不修改主相机的 near，所以设置一次即长期有效；相机被重建时插件会自动重设。
- 主相机没有自定义投影矩阵，因此改 `nearClipPlane` 有效。

## 许可

MIT，见 `LICENSE`。

------------------------------------------------------------------------------------------------------------------------------------------

# NearClipEdit

A standalone plugin that changes the near clip plane of Koikatu's main camera, so you can get right up close to a model without it getting clipped or poking through.

## Requirements

- Koikatu / Koikatsu Party (with CharaStudio)
- BepInEx 5.4.x (64-bit)

## Install

Drop `NearClipEdit.dll` into `BepInEx/plugins/`.

## Config (F1)

| Option | Default | Notes |
|---|---|---|
| `Enabled` | on | master switch |
| `Near clip plane` | 0.001 | range 0.001 ~ 1.0. Default 0.001 is the closest. 0.01 ~ 0.05 is more conservative |
| `Koikatu.exe` | on | enabled in the base game |
| `KoikatuVR.exe` | off | enabled in the base game's VR launcher |
| `CharaStudio.exe` | off | enabled in Studio |

- The KoikatuVR plugin already has a near clip setting, so it's off by default there
- CharaStudio already has a plugin for this, so it's off by default there too
- Some flickering in the distance is normal at 0.001 — that's the price of getting close without clipping. Bump it to 0.003 if it bothers you.

## How it works

- Only depends on `UnityEngine` + `BepInEx`, no game assemblies — the same DLL works in all three exes.
- Targets `Camera.main` (the game itself uses it too); if that isn't available it falls back to "the enabled camera with the largest area".
- The game never touches the main camera's near plane, so setting it once is enough; if the camera gets rebuilt, the plugin sets it again.
- The main camera doesn't use a custom projection matrix, so changing `nearClipPlane` actually takes effect.

## License

MIT, see `LICENSE`.
