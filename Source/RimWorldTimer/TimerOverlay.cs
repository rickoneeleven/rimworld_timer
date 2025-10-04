using System;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorldTimer
{
    // Keeps the timer window alive and persists basic state
    public class TimerGameComponent : GameComponent
    {
        public static TimerGameComponent? Instance;

        public float RemainingSeconds;
        public bool AlarmActive;
        public float FlashAccum;
        public bool FlashOn;
        public float LastRealtime;

        private const float FlashInterval = 0.25f;

        public TimerGameComponent(Game game) { }

        public override void FinalizeInit()
        {
            Instance = this;
            EnsureWindow();
            if (RemainingSeconds <= 0f)
            {
                ResetTimerFull();
            }
            LastRealtime = Time.realtimeSinceStartup;
        }

        public override void GameComponentTick()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            EnsureWindow();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref RemainingSeconds, "RemainingSeconds", 0f);
            Scribe_Values.Look(ref AlarmActive, "AlarmActive", false);
        }

        public void ResetTimerFull()
        {
            var settings = TimerMod.Settings ?? LoadedModManager.GetMod<TimerMod>().GetSettings<TimerSettings>();
            RemainingSeconds = Mathf.Max(1, settings.IntervalSeconds);
            AlarmActive = false;
            FlashAccum = 0f;
            FlashOn = false;
            LastRealtime = Time.realtimeSinceStartup;
        }

        private static void EnsureWindow()
        {
            if (Find.WindowStack != null && TimerWindow.Instance == null)
            {
                Find.WindowStack.Add(new TimerWindow());
            }
        }
    }

    public partial class TimerWindow : Window
    {
        public static TimerWindow? Instance;
        private bool _loggedDrawOnce;
        private static Texture2D? _texCog;

        public TimerWindow()
        {
            doWindowBackground = false;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            focusWhenOpened = false;
            preventCameraMotion = false;
            layer = WindowLayer.GameUI;
            draggable = false;
            drawShadow = false;
            onlyOneOfTypeAllowed = true;
            resizeable = false;
            closeOnAccept = false;
            closeOnCancel = false;
            Instance = this;
            TryLoadTextures();
        }

        public override void PostClose()
        {
            base.PostClose();
            Instance = null;
        }

        public override Vector2 InitialSize
        {
            get
            {
                var s = TimerMod.Settings ?? LoadedModManager.GetMod<TimerMod>().GetSettings<TimerSettings>();
                // Slightly reduce internal content to account for Window padding
                return new Vector2(Mathf.Clamp(s.PanelWidth, 100, 600), Mathf.Clamp(s.PanelHeight, 40, 300));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var gc = TimerGameComponent.Instance;
            if (gc == null) return;
            var settings = TimerMod.Settings ?? LoadedModManager.GetMod<TimerMod>().GetSettings<TimerSettings>();

            if (!_loggedDrawOnce)
            {
                _loggedDrawOnce = true;
                Log.Message("[RimWorldTimer] TimerWindow drawing");
            }

            float now = Time.realtimeSinceStartup;
            float dt = Mathf.Max(0f, now - gc.LastRealtime);
            gc.LastRealtime = now;

            bool paused = true;
            var tickManager = Find.TickManager;
            if (tickManager != null)
            {
                try
                {
                    // Covers normal pause and force-pause windows (e.g., trade, mod settings)
                    paused = tickManager.Paused;
                }
                catch
                {
                    // Fallback for older APIs
                    paused = tickManager.CurTimeSpeed == TimeSpeed.Paused;
                }
            }
            // Extra guard: if Unity is effectively paused, treat as paused
            if (Time.timeScale <= 0f) paused = true;
            if (!gc.AlarmActive && !paused)
            {
                gc.RemainingSeconds -= dt;
                if (gc.RemainingSeconds <= 0f)
                {
                    gc.AlarmActive = true;
                    gc.RemainingSeconds = 0f;
                    if (settings.BeepEnabled)
                    {
                        try { SoundDefOf.Click.PlayOneShotOnCamera(Find.CurrentMap); }
                        catch (Exception e) { Log.Warning("[RimWorldTimer] Beep failed: " + e.Message); }
                    }
                    try { Find.TickManager.CurTimeSpeed = TimeSpeed.Paused; } catch { }
                }
            }

            const float flashInterval = 0.25f;
            if (gc.AlarmActive && settings.FlashEnabled)
            {
                gc.FlashAccum += dt;
                if (gc.FlashAccum >= flashInterval)
                {
                    gc.FlashAccum -= flashInterval;
                    gc.FlashOn = !gc.FlashOn;
                }
            }
            else
            {
                gc.FlashOn = false;
            }

            const float margin = 12f;
            // Keep window anchored top-right by updating its size and position each frame
            windowRect.width = Mathf.Clamp(settings.PanelWidth, 100, 600);
            windowRect.height = Mathf.Clamp(settings.PanelHeight, 40, 300);
            windowRect.x = UI.screenWidth - windowRect.width - margin - settings.OffsetX;
            windowRect.y = margin + settings.OffsetY;

            Color old = GUI.color;
            Color bg = (gc.AlarmActive && settings.FlashEnabled && gc.FlashOn)
                ? new Color(0.95f, 0.2f, 0.2f, 0.85f)
                : new Color(0f, 0f, 0f, 0.70f);
            // Draw inside inRect for proper clipping
            var drawRect = new Rect(0f, 0f, inRect.width, inRect.height);
            Widgets.DrawBoxSolid(drawRect, bg);
            GUI.color = Color.white;
            Widgets.DrawBox(drawRect);
            GUI.color = old;

            var inner = drawRect.ContractedBy(6f);
            // Simple horizontal layout: time left, button right; keep safe padding
            float buttonWidth = Mathf.Min(72f, inner.width * 0.35f);
            float buttonHeight = Mathf.Clamp(inner.height - 12f, 22f, 28f);
            float cogSize = buttonHeight; // square
            float gap = 6f;
            var resetRect = new Rect(inner.xMax - buttonWidth, inner.y + (inner.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
            var cogRect = new Rect(resetRect.x - gap - cogSize, inner.y + (inner.height - cogSize) / 2f, cogSize, cogSize);
            var timeRect = new Rect(inner.x, inner.y, inner.width - buttonWidth - cogSize - 2 * gap, inner.height);

            DrawLabelOutlined(timeRect, FormatTime(gc.RemainingSeconds), Color.white);

            // Settings button (icon if available, else small text)
            bool clickedSettings;
            if (_texCog != null)
            {
                clickedSettings = Widgets.ButtonImage(cogRect, _texCog, true);
            }
            else
            {
                clickedSettings = Widgets.ButtonText(cogRect, "Cfg");
            }
            if (clickedSettings) OpenModSettings();
            TooltipHandler.TipRegion(cogRect, "Settings");

            if (gc.AlarmActive)
            {
                if (Widgets.ButtonText(resetRect, "Reset"))
                {
                    gc.ResetTimerFull();
                    try { Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; } catch { }
                }
            }
            else
            {
                if (Widgets.ButtonText(resetRect, "Reset"))
                {
                    gc.ResetTimerFull();
                }
            }
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0) seconds = 0;
            int total = Mathf.CeilToInt(seconds);
            int mm = total / 60;
            int ss = total % 60;
            // Show minutes:seconds for readability, but accept any seconds interval
            return mm.ToString("00") + ":" + ss.ToString("00");
        }
        private static void DrawLabelOutlined(Rect rect, string text, Color textColor)
        {
            var prevFont = Text.Font;
            var prevAnchor = Text.Anchor;
            var prevColor = GUI.color;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            float o = 1f;
            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            Widgets.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), text);
            Widgets.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), text);
            Widgets.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), text);
            Widgets.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), text);

            GUI.color = textColor;
            Widgets.Label(rect, text);

            GUI.color = prevColor;
            Text.Font = prevFont;
            Text.Anchor = prevAnchor;
        }
    }

    internal static class SettingsLauncher
    {
        public static void OpenModSettings()
        {
            try
            {
                var mod = LoadedModManager.GetMod<TimerMod>();
                // Try to find Dialog_ModSettings via Harmony AccessTools for compatibility
                var type = AccessTools.TypeByName("RimWorld.Dialog_ModSettings")
                           ?? AccessTools.TypeByName("Verse.Dialog_ModSettings")
                           ?? AccessTools.TypeByName("Dialog_ModSettings");
                if (type != null)
                {
                    object? dlg = null;
                    // Prefer ctor(Mod)
                    var withMod = AccessTools.Constructor(type, new[] { typeof(Mod) });
                    if (withMod != null)
                    {
                        dlg = Activator.CreateInstance(type, mod);
                    }
                    else
                    {
                        // Fallback to parameterless dialog
                        var noArgs = AccessTools.Constructor(type, Type.EmptyTypes);
                        if (noArgs != null)
                        {
                            dlg = Activator.CreateInstance(type);
                        }
                    }
                    if (dlg is Window w)
                    {
                        Find.WindowStack.Add(w);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[RimWorldTimer] Failed to open settings directly: " + e.Message);
            }
            // Final fallback: open the standard settings list if available via Defs
            try
            {
                var pageType = AccessTools.TypeByName("RimWorld.Dialog_ModSettings")
                                ?? AccessTools.TypeByName("Verse.Dialog_ModSettings");
                if (pageType != null)
                {
                    var dlg = Activator.CreateInstance(pageType);
                    if (dlg is Window w) { Find.WindowStack.Add(w); }
                }
            }
            catch { }
        }
    }

    // Local helpers
    public partial class TimerWindow
    {
        private static void TryLoadTextures()
        {
            if (_texCog != null) return;
            // Try a few likely built-in paths; fallback to default bad texture
            _texCog = ContentFinder<Texture2D>.Get("UI/Buttons/Options", false)
                      ?? ContentFinder<Texture2D>.Get("UI/Buttons/Settings", false)
                      ?? ContentFinder<Texture2D>.Get("UI/Buttons/Config", false)
                      ?? ContentFinder<Texture2D>.Get("UI/Widgets/Gear", false);
        }

        private static void OpenModSettings()
        {
            SettingsLauncher.OpenModSettings();
        }
    }
}
