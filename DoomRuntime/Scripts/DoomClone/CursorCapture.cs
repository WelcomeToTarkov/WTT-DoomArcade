using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class CursorCapture : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursorCapture(false);
            }
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                ToggleCursorCapture(true);
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                ToggleCursorCapture(true);
            }
        }

        void ToggleCursorCapture(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
    }
}
