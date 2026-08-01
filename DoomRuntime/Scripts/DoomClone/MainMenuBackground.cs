using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DoomArcade.Scripts.DoomClone
{
    public class MainMenuBackground : MonoBehaviour
    {
        [SerializeField] GameObject staticBackground;
        [SerializeField] VideoClip[] videoClips;
        [SerializeField] float introDelay = 5f;
        [SerializeField] float stripWidth = 4f;
        [SerializeField] float meltDuration = 5f;
        [SerializeField] float startDelayVariance = 0.3f;
        [SerializeField] private Canvas scalerCanvas;

        private List<RectTransform> strips = new List<RectTransform>();

        void Awake()
        {
            if (staticBackground == null) staticBackground = gameObject;
        }

        public void PlayIntro()
        {
            StartCoroutine(IntroSequence());
        }

        IEnumerator IntroSequence()
        {
            staticBackground.SetActive(true);
            var staticCG = staticBackground.GetComponent<CanvasGroup>() ?? staticBackground.AddComponent<CanvasGroup>();
            staticCG.alpha = 1f;

            yield return new WaitForSeconds(introDelay);

            yield return StartCoroutine(MeltStrips(staticBackground.GetComponent<RectTransform>()));
        }

        public IEnumerator MeltStrips(RectTransform targetArea = null, bool disableTargetAfterCapture = true,
            Action onComplete = null, Texture preCapturedTexture = null)
        {
            if (targetArea == null)
            {
                if (staticBackground == null) yield break;
                targetArea = staticBackground.GetComponent<RectTransform>();
                if (targetArea == null) yield break;
            }

            Canvas.ForceUpdateCanvases();

            for (int i = strips.Count - 1; i >= 0; i--)
            {
                if (strips[i]) DestroyImmediate(strips[i].gameObject);
            }

            strips.Clear();

            Texture capturedTex = preCapturedTexture ?? CaptureCanvasTexture(targetArea);
            if (capturedTex == null) yield break;

            if (scalerCanvas == null) yield break;

            if (disableTargetAfterCapture && targetArea.gameObject != null)
                targetArea.gameObject.SetActive(false);

            float areaWidth = targetArea.rect.width;
            float areaHeight = targetArea.rect.height;
            int texWidthPx = capturedTex.width;

            int pixelStripWidth = Mathf.Max(1, Mathf.RoundToInt(stripWidth));
            int numStrips = Mathf.CeilToInt((float)texWidthPx / pixelStripWidth);

            GameObject parentObj = new GameObject("MeltStripsParent", typeof(RectTransform));
            RectTransform stripParent = parentObj.GetComponent<RectTransform>();
            stripParent.SetParent(targetArea, false);

            stripParent.anchorMin = Vector2.zero;
            stripParent.anchorMax = Vector2.one;
            stripParent.offsetMin = Vector2.zero;
            stripParent.offsetMax = Vector2.zero;
            stripParent.pivot = targetArea.pivot;
            stripParent.SetAsLastSibling();

            var startDelays = new float[numStrips];
            for (int i = 0; i < numStrips; i++)
                startDelays[i] = UnityEngine.Random.Range(0f, startDelayVariance);

            for (int stripIdx = 0; stripIdx < numStrips; stripIdx++)
            {
                float uStart = (float)(stripIdx * pixelStripWidth) / texWidthPx;
                float uEnd = (float)Mathf.Min((stripIdx + 1) * pixelStripWidth, texWidthPx) / texWidthPx;
                float uWidth = uEnd - uStart;

                GameObject stripObj = new GameObject($"MeltStrip_{stripIdx}", typeof(RectTransform), typeof(RawImage));
                RectTransform stripRT = stripObj.GetComponent<RectTransform>();
                stripRT.SetParent(stripParent, false);

                stripRT.anchorMin = new Vector2(uStart, 0f);
                stripRT.anchorMax = new Vector2(uEnd, 1f);
                stripRT.offsetMin = Vector2.zero;
                stripRT.offsetMax = Vector2.zero;
                stripRT.pivot = new Vector2(0.5f, 1f);

                RawImage stripRaw = stripObj.GetComponent<RawImage>();
                stripRaw.raycastTarget = false;
                stripRaw.texture = capturedTex;
                stripRaw.uvRect = new Rect(uStart, 0f, uWidth, 1f);

                strips.Add(stripRT);
            }

            float elapsed = 0f;
            float duration = meltDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < strips.Count; i++)
                {
                    if (!strips[i]) continue;

                    float delay = startDelays[i];
                    if (elapsed < delay) continue;

                    float localDuration = Mathf.Max(0.0001f, duration - delay);
                    float t = Mathf.Clamp01((elapsed - delay) / localDuration);
                    float fallT = 1f - Mathf.Pow(1f - t, 3f);

                    float y = Mathf.Lerp(0f, -areaHeight * 2f, fallT);
                    strips[i].anchoredPosition = new Vector2(0f, y);
                }

                yield return null;
            }

            if (stripParent) DestroyImmediate(stripParent.gameObject);
            strips.Clear();

            if (capturedTex != null) DestroyImmediate(capturedTex);

            onComplete?.Invoke();
        }

        public Texture CaptureUITexture(RectTransform targetArea)
        {
            return CaptureCanvasTexture(targetArea);
        }

        Texture CaptureCanvasTexture(RectTransform targetArea)
        {
            Canvas rootCanvas = scalerCanvas.rootCanvas;
            if (rootCanvas == null) return null;

            Vector3[] corners = new Vector3[4];
            targetArea.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];
            Vector3 center = (bottomLeft + topRight) / 2f;

            GameObject tempCamGO = new GameObject("TempCaptureCamera");
            Camera tempCam = tempCamGO.AddComponent<Camera>();

            Camera refCam = rootCanvas.worldCamera ?? Camera.main;

            if (refCam != null)
            {
                tempCam.CopyFrom(refCam);
                tempCam.clearFlags = CameraClearFlags.SolidColor;
                tempCam.backgroundColor = Color.clear;
                tempCam.cullingMask = 1 << rootCanvas.gameObject.layer;
                tempCam.orthographic = true;
            }
            else
            {
                tempCam.clearFlags = CameraClearFlags.SolidColor;
                tempCam.backgroundColor = Color.clear;
                tempCam.cullingMask = 1 << rootCanvas.gameObject.layer;
                tempCam.orthographic = true;
            }

            float heightWorld = topRight.y - bottomLeft.y;
            tempCam.orthographicSize = heightWorld / 2f;

            Vector3 forward = refCam != null ? refCam.transform.forward : Vector3.forward;
            tempCam.transform.position = center - forward * 10f;
            tempCam.transform.LookAt(center);

            int texWidth = (int)(targetArea.rect.width * rootCanvas.scaleFactor);
            int texHeight = (int)(targetArea.rect.height * rootCanvas.scaleFactor);
            texWidth = Mathf.Clamp(texWidth, 64, 4096);
            texHeight = Mathf.Clamp(texHeight, 64, 4096);

            RenderTexture rt = RenderTexture.GetTemporary(texWidth, texHeight, 24);
            tempCam.targetTexture = rt;
            tempCam.Render();

            Texture2D result = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            tempCam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            DestroyImmediate(tempCamGO);

            return result;
        }
    }
}