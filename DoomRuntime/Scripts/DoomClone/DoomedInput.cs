using ManagedDoom;
using ManagedDoom.Unity;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public static class DoomedInputInitializer
    {
        public static void ApplyConfigToDoomedInput(Config config)
        {
            DoomedInput.MenuUp   = PickKey(config.key_forward, KeyCode.W);
            DoomedInput.MenuDown = PickKey(config.key_backward, KeyCode.S);

            var left  = PickKey(config.key_strafeleft,  KeyCode.A);
            var right = PickKey(config.key_straferight, KeyCode.D);

            DoomedInput.MoveLeftKey  = left;
            DoomedInput.MoveRightKey = right;

            DoomedInput.Sprint = PickKey(config.key_run, KeyCode.LeftShift);

            DoomedInput.Use = PickKey(config.key_use, KeyCode.E);

            DoomedInput.PauseOrBack = KeyCode.Escape;

            DoomedInput.FireMouseButton = 0;

            DoomedInput.UseMouseLookForTurn = config.input_mouse_turn;
        }

        private static KeyCode PickKey(KeyBinding binding, KeyCode fallback)
        {
            if (binding == null || binding.Keys == null || binding.Keys.Count == 0)
                return fallback;

            KeyCode candidate = KeyCode.None;
            foreach (var doomKey in binding.Keys)
            {
                var kc = UnityUserInput.DoomToUnityKey(doomKey);
                if (kc == KeyCode.None)
                    continue;

                if (kc == KeyCode.W || kc == KeyCode.S || kc == KeyCode.A || kc == KeyCode.D)
                    return kc;

                if (kc == KeyCode.UpArrow || kc == KeyCode.DownArrow || kc == KeyCode.LeftArrow || kc == KeyCode.RightArrow)
                    candidate = candidate == KeyCode.None ? kc : candidate;

                if (candidate == KeyCode.None)
                    candidate = kc;
            }

            return candidate != KeyCode.None ? candidate : fallback;
        }
    }
    public static class DoomedInput
    {
        public const string MoveHorizontal = "Horizontal";
        public const string MoveVertical   = "Vertical";
        public const string LookHorizontal = "Mouse X";

        public static KeyCode Sprint      = KeyCode.LeftShift;
        public static int FireMouseButton = 0;

        public static readonly KeyCode[] WeaponKeys = {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        };

        public static KeyCode MoveLeftKey  = KeyCode.A;
        public static KeyCode MoveRightKey = KeyCode.D;

        public static KeyCode Use           = KeyCode.E;
        public static KeyCode CabinetToggle = KeyCode.F;

        public static KeyCode MenuUp       = KeyCode.W;
        public static KeyCode MenuDown     = KeyCode.S;
        public static KeyCode MenuUpAlt    = KeyCode.UpArrow;
        public static KeyCode MenuDownAlt  = KeyCode.DownArrow;
        public static KeyCode MenuConfirm  = KeyCode.Return;
        public static KeyCode MenuConfirm2 = KeyCode.KeypadEnter;

        public static KeyCode PauseOrBack = KeyCode.Escape;
        
        public static KeyCode MenuConfirmYes = KeyCode.Y;
        public static KeyCode MenuConfirmNo  = KeyCode.N;

        public static bool UseMouseLookForTurn = true;
    }
}