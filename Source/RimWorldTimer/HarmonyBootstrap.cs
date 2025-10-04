using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimWorldTimer
{
    [StaticConstructorOnStartup]
    public static class HarmonyBootstrap
    {
        static HarmonyBootstrap()
        {
            try
            {
                var harmony = new Harmony("rickoneeleven.rimworldtimer");
                var target = AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI));
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(HarmonyBootstrap), nameof(UIOnGUI)));
            }
            catch (Exception e)
            {
                Log.Error("[RimWorldTimer] Harmony patch failed: " + e);
            }
        }

        public static void UIOnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing) return;
            if (Find.WindowStack == null) return;

            // Ensure game component exists for save/load persistence
            var game = Current.Game;
            if (game != null)
            {
                if (game.components == null) return;
                bool hasComp = game.components.Any(c => c is TimerGameComponent);
                if (!hasComp)
                {
                    try { game.components.Add(new TimerGameComponent(game)); Log.Message("[RimWorldTimer] Added GameComponent"); }
                    catch (Exception e) { Log.Warning("[RimWorldTimer] Failed to add GameComponent: " + e.Message); }
                }
            }

            // Ensure our window is present
            if (TimerWindow.Instance == null)
            {
                try { Find.WindowStack.Add(new TimerWindow()); Log.Message("[RimWorldTimer] Added TimerWindow"); }
                catch (Exception e) { Log.Warning("[RimWorldTimer] Failed to add window: " + e.Message); }
            }
        }
    }
}
