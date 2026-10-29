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
| `Near clip plane` | 0.001 | 范围 0.001 ~ 1.0。默认 0.001 最贴近；0.01 ~ 0.05 更保守 |
| `Koikatu.exe` | 开 | 在游戏本体里生效 |
| `KoikatuVR.exe` | 关 | 在本体 VR 启动器里生效 |
| `CharaStudio.exe` | 关 | 在工作室里生效 |

- KoikatuVR插件已经有修改近裁剪值的功能，默认关闭
- CharaStudio那边已经有修改裁剪值的插件，默认关闭
- 如果远景出现闪烁这在0.001数值下的正常现象，这是为了获得近距离不穿模的代价，改成0.003可以缓解

## 实现说明

- 只依赖 `UnityEngine` + `BepInEx`，不引用任何游戏程序集，同一个 DLL 可在三个 exe 中使用。
- 目标相机取 `Camera.main`（游戏自身也用这个），拿不到时退化为"面积最大的启用相机"。
- 游戏自身从不修改主相机的 near，所以设置一次即长期有效；相机被重建时插件会自动重设。
- 主相机没有自定义投影矩阵，因此改 `nearClipPlane` 有效。

## 许可

MIT，见 `LICENSE`。
