using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.UI
{
    public class UIArms : MonoBehaviour
    {
        [SerializeField] private SpriteFont font;
        [SerializeField] private Color colorActive;
        [SerializeField] private Color colorDisabled;
        [SerializeField] private Color colorBg;

        private Dictionary<int, SpriteText> kvp;

        private void OnEnable()
        {
            GlobalEventController.OnAnyEvent += RefreshText;
        }

        private void OnDisable()
        {
            GlobalEventController.OnAnyEvent -= RefreshText;
        }

        private void Awake()
        {
            RectTransform bgrect = new GameObject("BG", typeof(RectTransform)).GetComponent<RectTransform>();
            bgrect.SetParent(transform, false);
            bgrect.anchorMin = Vector2.zero;
            bgrect.anchorMax = Vector2.one;
            bgrect.offsetMin = Vector2.zero;
            bgrect.offsetMax = Vector2.zero;
            bgrect.anchoredPosition = new Vector2(1, -1);

            GenerateIndicators(true, bgrect);

            kvp = new Dictionary<int, SpriteText>();
            GenerateIndicators(false, transform as RectTransform);
        }

        void GenerateIndicators(bool bg, RectTransform parent)
        {
            int startAt = 2;

            int cols = 3;
            int rows = 2;

            float width = 1f / cols;
            float height = 1f / rows;

            for (int y = rows - 1; y >= 0; y--)
            {
                for (int x = 0; x < cols; x++)
                {
                    GameObject go = new GameObject("Rect", typeof(RectTransform));
                    go.transform.SetParent(parent, false);
                    RectTransform rect = go.GetComponent<RectTransform>();

                    rect.anchorMin = new Vector2(x * width, y * height);
                    rect.anchorMax = new Vector2((x + 1) * width, (y + 1) * height);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    SpriteText st = go.AddComponent<SpriteText>();
                    st.font = font;
                    st.verticalAlignment = SpriteText.VerticalAlignment.Top;
                    st.color = bg ? colorBg : colorDisabled;
                    st.GenerateText(startAt.ToString());

                    if (!bg)
                    {
                        kvp.Add(startAt, st);
                    }

                    startAt++;
                }
            }
        }

        void RefreshText()
        {
            foreach (KeyValuePair<int, SpriteText> pair in kvp)
            {
                WeaponData weaponData = DataManager.instance.GetWeaponById(pair.Key - 1);
                if (weaponData == null)
                    continue;

                Player.current.weaponUnlocked.TryGetValue(weaponData, out bool unlocked);

                pair.Value.color = unlocked ? colorActive : colorDisabled;
                pair.Value.GenerateText(pair.Value.targetText);
            }
        }
    }
}
