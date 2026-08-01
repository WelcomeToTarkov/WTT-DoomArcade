using DoomArcade.Scripts.DoomClone;
using UnityEngine;

namespace DoomArcade.Scripts.Arcade
{
    public class TarkovDoomExitHook : MonoBehaviour
    {
        private DoomGameController controller;
        public static TarkovDoomExitHook Instance;

        public void Init(DoomGameController owner)
        {
            Instance = this;
            controller = owner;
        }

        public void ExitToArcade()
        {
            if (controller != null)
                controller.ExitToArcade();
        }
    }

}