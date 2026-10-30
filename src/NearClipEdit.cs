using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace NearClipEdit
{
    /// <summary>
    /// 修改游戏主相机的近裁剪面（near clip plane）。
    ///
    /// 本插件不引用任何游戏程序集（只有 UnityEngine + BepInEx），
    /// 所以同一个 DLL 能同时跑 Koikatu.exe / KoikatuVR.exe / CharaStudio.exe；
    /// 具体在哪个程序里生效，由 F1 → [Games] 三个开关决定（默认只有 Koikatu.exe 开）。
    ///
    /// 【日志】本插件**一行日志都不打**（与主插件的精简版同一原则）：
    ///   所有信息都只通过 F1 配置体现，DLL 里没有任何日志文本。
    ///
    /// 【没有"还原原值"功能】用户实测关掉 Enabled 并不会还原（BepInEx 的配置热改不触发
    ///   我们希望的那条路径），反复修不划算 → 整个还原逻辑删掉，代码也因此简单了一大截。
    ///   想恢复原值：把 Near clip plane 调回你原来的值，或重启游戏（相机重新创建）。
    ///
    /// 反编译依据（Koikatu_Data/Managed/Assembly-CSharp.dll）：
    ///   · 全程序集只有 3 处写 nearClipPlane：DebugChaControl(-10)、SaveFrameAssist(-10)、
    ///     MirrorReflection（从主相机拷贝）→ **游戏自己从不改主相机的 near**，
    ///     所以只要设一次就会一直有效（相机被重建时本插件会自动重设）。
    ///   · ADV/ADVScene.cs:75 `scenario.AdvCamera = (advCamera = Camera.main);`
    ///     → 主相机就是 Camera.main，选它是对的。
    ///   · 主相机没有自定义 projectionMatrix（只有 MirrorReflection 给镜像相机设），
    ///     所以改 nearClipPlane 不会被投影矩阵覆盖。
    /// </summary>
    [BepInPlugin(Guid, "Near Clip Edit", Version)]
    [BepInProcess("Koikatu.exe")]
    [BepInProcess("KoikatuVR.exe")]
    [BepInProcess("CharaStudio.exe")]
    public class NearClipEditPlugin : BaseUnityPlugin
    {
        public const string Guid = "bucky.kk.nearclipedit";
        public const string Version = "1.0.0";

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _near;

        private ConfigEntry<bool> _inKoikatu;
        private ConfigEntry<bool> _inKoikatuVr;
        private ConfigEntry<bool> _inCharaStudio;

        /// <summary>当前进程是否属于"被启用"的那一个（Awake 里算一次，之后只看这个 bool）。</summary>
        private bool _gameEnabled;

        /// <summary>当前锁定的相机。Unity 的 fake-null 保证它被销毁后自动变 null → 下帧重找。</summary>
        private Camera _cam;

        /// <summary>兜底找相机失败时的冷却帧（避免每帧调 Camera.allCameras 造成 GC）。</summary>
        private int _retryFrame;

        private void Awake()
        {
            _enabled = Config.Bind("General", "Enabled", true,
                "总开关。");

            _near = Config.Bind("General", "Near clip plane", 0.001f,
                new ConfigDescription(
                    "主相机的近裁剪面。越小 = 能贴得越近看，但太小会出现深度冲突（Z-fighting）。\n"
                    + "范围 0.001 ~ 1.0；默认 0.001（最贴近，实测 VR/平面都正常）；保守些可用 0.05 ~ 0.01。",
                    new AcceptableValueRange<float>(0.001f, 1f)));

            // ---- 按程序启用（同一个 BepInEx 下三个 exe 共用这一份配置）----
            _inKoikatu = Config.Bind("Games", "Koikatu.exe", true,
                "在 Koikatu.exe（游戏本体）里启用。默认开。");
            _inKoikatuVr = Config.Bind("Games", "KoikatuVR.exe", false,
                "在 KoikatuVR.exe（本体 VR 启动器）里启用。默认关。");
            _inCharaStudio = Config.Bind("Games", "CharaStudio.exe", false,
                "在 CharaStudio.exe（工作室）里启用。默认关 —— 工作室那边已有插件改这个值。");

            string exe = "";
            try { exe = Process.GetCurrentProcess().ProcessName; } catch { exe = ""; }

            if (string.Equals(exe, "Koikatu", StringComparison.OrdinalIgnoreCase))
                _gameEnabled = _inKoikatu.Value;
            else if (string.Equals(exe, "KoikatuVR", StringComparison.OrdinalIgnoreCase))
                _gameEnabled = _inKoikatuVr.Value;
            else if (string.Equals(exe, "CharaStudio", StringComparison.OrdinalIgnoreCase))
                _gameEnabled = _inCharaStudio.Value;
            else
                _gameEnabled = false;   // 不认识的程序 → 一律不动
        }

        private void Update()
        {
            if (!_enabled.Value || !_gameEnabled) return;

            Camera cam = ResolveCamera();
            if (cam == null) return;

            float want = _near.Value;
            if (Mathf.Abs(cam.nearClipPlane - want) > 0.00001f)
                cam.nearClipPlane = want;
        }

        // ---------------- 内部 ----------------

        /// <summary>
        /// 取主相机。正常就是 Camera.main（游戏自己也是这么拿的）；
        /// 万一没有 MainCamera 标签，就退化成"面积最大的那个启用相机"。
        /// 只在拿不到时才会枚举相机 —— 而且失败后有 30 帧冷却，
        /// 避免"一直找不到"时每帧调 Camera.allCameras 反复分配数组。
        /// </summary>
        private Camera ResolveCamera()
        {
            if (_cam != null) return _cam;

            try { _cam = Camera.main; } catch { _cam = null; }
            if (_cam != null) return _cam;

            if (Time.frameCount < _retryFrame) return null;
            _retryFrame = Time.frameCount + 30;

            try
            {
                Camera[] all = Camera.allCameras;
                int best = -1;
                long bestArea = -1;
                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.enabled || c.depth < 0f) continue;
                    long area = (long)c.pixelWidth * c.pixelHeight;
                    if (area > bestArea) { bestArea = area; best = i; }
                }
                if (best >= 0) _cam = all[best];
            }
            catch { }

            return _cam;
        }
    }
}
