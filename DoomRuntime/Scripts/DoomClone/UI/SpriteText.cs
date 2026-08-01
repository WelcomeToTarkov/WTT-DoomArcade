using System.Collections.Generic;
using DoomArcade.Scripts.DoomClone.Scriptables;
using UnityEngine;
using UnityEngine.UI;

namespace DoomArcade.Scripts.DoomClone.UI
{
    [ExecuteAlways]
    public class SpriteText : MonoBehaviour
    {
        public enum Alignment
        {
            Left,
            Center,
            Right
        }
        public enum VerticalAlignment
        {
            Top,
            Center,
            Bottom
        }

        RectTransform rectTransform => GetComponent<RectTransform>();

        public SpriteFont font;

        [TextArea(1, 10)]
        public string targetText;
        public float kerning = 1.0f;
        public float lineSpacing = 1.0f;
        public Alignment textAlignment = Alignment.Left;
        public VerticalAlignment verticalAlignment = VerticalAlignment.Top;
        public Color color = Color.white;

        private Queue<GameObject> pooledObjects = new Queue<GameObject>();
        private int symbolLimit = 36;

        private void OnValidate()
        {
            if (font == null)
                return;

            GenerateText(targetText);
        }

        public void GenerateText(string text)
        {
            targetText = text;
            string[] lines = targetText.Split('\n'); 
            float totalHeight = (lines.Length - 1) * font.LineHeight * lineSpacing;
            float yOffset = 0f;

            switch (verticalAlignment)
            {
                case VerticalAlignment.Center:
                    yOffset = rectTransform.rect.height / 2f;
                    yOffset += totalHeight / 2f;
                    yOffset -= font.LineHeight / 2f;
                    break;
                case VerticalAlignment.Top:
                    yOffset += rectTransform.rect.height;
                    yOffset -= font.LineHeight;
                    break;
                case VerticalAlignment.Bottom:
                    yOffset = totalHeight;
                    break;
            }
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                pooledObjects.Enqueue(child.gameObject);
            }

            foreach (string line in lines)
            {
                Sprite[] sprites = font.StringToSpriteArray(line);

                float totalWidth = 0f;
                for (int i = 0; i < Mathf.Min(sprites.Length, symbolLimit); i++)
                {
                    // avoid adding kerning to the last character
                    if (i == Mathf.Min(sprites.Length, symbolLimit) - 1)
                    {
                        totalWidth += sprites[i].rect.width;
                        break;
                    }
                    totalWidth += sprites[i].rect.width * kerning;
                }

                float xOffset = 0f;
                Vector2 pivotPoint = new Vector2(0, 0);
                Vector2 anchorPoint = new Vector2(0, 0);

                switch (textAlignment)
                {
                    case Alignment.Center:
                        xOffset = -totalWidth / 2f;
                        xOffset += rectTransform.rect.width / 2f;
                        break;
                    case Alignment.Right:
                        xOffset = -totalWidth;
                        xOffset += rectTransform.rect.width;
                        break;
                }

                float runningWidth = 0f;

                for (int i = 0; i < Mathf.Min(sprites.Length, symbolLimit); i++)
                {
                    GameObject go;

                    if (pooledObjects.Count > 0)
                    {
                        go = pooledObjects.Dequeue();
                        go.SetActive(true);
                    }
                    else
                    {
                        go = new GameObject("Letter");
                        go.AddComponent<Image>();
                        go.transform.SetParent(transform, false);
                    }

                    Image image = go.GetComponent<Image>();
                    image.sprite = sprites[i];
                    image.raycastTarget = false;
                    image.color = color;

                    RectTransform rectTransform = go.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(sprites[i].rect.width, sprites[i].rect.height);
                    rectTransform.pivot = pivotPoint;
                    rectTransform.anchorMin = anchorPoint;
                    rectTransform.anchorMax = anchorPoint;
                    rectTransform.anchoredPosition = new Vector2(xOffset + runningWidth, yOffset);

                    runningWidth += sprites[i].rect.width * kerning;
                }

                yOffset -= font.LineHeight * lineSpacing;
            }
        }
    }
}
