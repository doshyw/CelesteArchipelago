using Microsoft.Xna.Framework;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedLevel : IPatchable
    {
        public void Load()
        {
            Everest.Events.Level.OnTransitionTo += OnTransitionTo;
            Everest.Events.Level.OnExit += OnExit;
        }

        public void Unload()
        {
            Everest.Events.Level.OnTransitionTo -= OnTransitionTo;
            Everest.Events.Level.OnExit -= OnExit;
        }

        private static void OnTransitionTo(Level level, LevelData next, Vector2 direction)
        {
            var state = new PlayState(false, level.Session.Area, next.Name);
            Logger.Log("CelesteArchipelago", $"Transitioning level. Setting PlayState to {state}");
            ArchipelagoController.Instance.PlayState = state;
        }

        private static void OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            if(mode != LevelExit.Mode.SaveAndQuit)
            {
                var state = new PlayState(true, level.Session.Area, "overworld");
                Logger.Log("CelesteArchipelago", $"Exiting level. Setting PlayState to {state}");
                ArchipelagoController.Instance.PlayState = state;
            }
        }
    }
}
