using System.Reflection;
using EFT.UI;
using UnityEngine;

namespace DoomArcadeClient.Utils
{
    public static class CursorSettings
    {
        private static readonly MethodInfo SetCursorMethod;
        private static readonly MethodInfo SetCursorLockMethod;

        static CursorSettings()
        {
            var cursorType = typeof(CursorSwitcher);

            SetCursorMethod = cursorType.GetMethod(
                nameof(CursorSwitcher.SetCursor),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(ECursorType) },
                null);

            SetCursorLockMethod = cursorType.GetMethod(
                nameof(CursorSwitcher.SetCursorLockMode),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(bool), typeof(FullScreenMode) },
                null);
        }

        public static void SetCursor(ECursorType type)
        {
            SetCursorMethod?.Invoke(null, new object[] { type });
        }

        public static void SetCursorLockMode(bool visible, FullScreenMode fullscreenMode)
        {
            SetCursorLockMethod?.Invoke(null, new object[] { visible, fullscreenMode });
        }
    }
}