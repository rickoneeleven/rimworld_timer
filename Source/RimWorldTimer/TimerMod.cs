using System;
using UnityEngine;
using Verse;

namespace RimWorldTimer
{
    public class TimerSettings : ModSettings
    {
        public int IntervalSeconds = 600;
        public int OffsetX = 0; // Positive moves left from right edge
        public int OffsetY = 0; // Positive moves down from top edge
        public bool FlashEnabled = true;
        public bool BeepEnabled = true;
        public int PanelWidth = 200;
        public int PanelHeight = 70;

        public override void ExposeData()
        {
            // New seconds-based interval
            Scribe_Values.Look(ref IntervalSeconds, "IntervalSeconds", 600);
            // Backward-compat for earlier minutes field
            int legacyMinutes = -1;
            Scribe_Values.Look(ref legacyMinutes, "IntervalMinutes", -1);
            if (Scribe.mode == LoadSaveMode.LoadingVars && legacyMinutes >= 0)
            {
                IntervalSeconds = Mathf.Max(1, legacyMinutes * 60);
            }
            Scribe_Values.Look(ref OffsetX, "OffsetX", 0);
            Scribe_Values.Look(ref OffsetY, "OffsetY", 0);
            Scribe_Values.Look(ref FlashEnabled, "FlashEnabled", true);
            Scribe_Values.Look(ref BeepEnabled, "BeepEnabled", true);
            Scribe_Values.Look(ref PanelWidth, "PanelWidth", 200);
            Scribe_Values.Look(ref PanelHeight, "PanelHeight", 70);
        }
    }

    public class TimerMod : Mod
    {
        public static TimerSettings? Settings;
        private string _secondsBuffer = string.Empty;
        private string _widthBuffer = string.Empty;
        private string _heightBuffer = string.Empty;

        public TimerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TimerSettings>();
        }

        public override string SettingsCategory() => "Timer";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard { ColumnWidth = inRect.width };
            listing.Begin(inRect);

            listing.Label("Interval (seconds)");
            int min = 1, max = 24 * 60 * 60;
            if (string.IsNullOrEmpty(_secondsBuffer)) _secondsBuffer = Settings!.IntervalSeconds.ToString();
            var intervalRect = listing.GetRect(28f);
            _secondsBuffer = Widgets.TextField(intervalRect, _secondsBuffer);
            if (int.TryParse(_secondsBuffer, out var intVal))
            {
                Settings.IntervalSeconds = Mathf.Clamp(intVal, min, max);
            }
            else if (!string.IsNullOrEmpty(_secondsBuffer))
            {
                // restore buffer to current value if invalid
                _secondsBuffer = Settings.IntervalSeconds.ToString();
            }

            listing.GapLine();
            listing.Label("Position offset from top-right (pixels)");
            listing.Label($"X (right→left): {Settings.OffsetX}");
            Settings.OffsetX = (int)listing.Slider(Settings.OffsetX, -600, 600);
            listing.Label($"Y (top→down): {Settings.OffsetY}");
            Settings.OffsetY = (int)listing.Slider(Settings.OffsetY, -400, 400);

            listing.GapLine();
            listing.Label("Panel size (pixels)");
            if (string.IsNullOrEmpty(_widthBuffer)) _widthBuffer = Settings.PanelWidth.ToString();
            if (string.IsNullOrEmpty(_heightBuffer)) _heightBuffer = Settings.PanelHeight.ToString();
            var wr = listing.GetRect(28f);
            var hr = listing.GetRect(28f);
            Widgets.Label(wr.LeftHalf(), "Width");
            _widthBuffer = Widgets.TextField(wr.RightHalf(), _widthBuffer);
            Widgets.Label(hr.LeftHalf(), "Height");
            _heightBuffer = Widgets.TextField(hr.RightHalf(), _heightBuffer);
            if (int.TryParse(_widthBuffer, out var w)) Settings.PanelWidth = Mathf.Clamp(w, 100, 600); else _widthBuffer = Settings.PanelWidth.ToString();
            if (int.TryParse(_heightBuffer, out var h)) Settings.PanelHeight = Mathf.Clamp(h, 40, 300); else _heightBuffer = Settings.PanelHeight.ToString();

            listing.Gap();
            listing.CheckboxLabeled("Flash when alarmed", ref Settings.FlashEnabled);
            listing.CheckboxLabeled("Beep when alarmed", ref Settings.BeepEnabled);

            listing.Gap();
            if (listing.ButtonText("Reset Timer Now"))
            {
                TimerGameComponent.Instance?.ResetTimerFull();
            }

            listing.End();
            Settings.Write();
        }
    }
}
