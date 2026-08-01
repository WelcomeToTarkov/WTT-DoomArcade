using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.FrameCapture
{
    public class FrameCapture : MonoBehaviour
    {
        private Camera camRender;
        private Camera camPlayer;
        private bool recording = false;
        private int frameCount = 0;
        private Dictionary<Transform, TransformCapture> transformsToCapture;
        public List<Transform> miscTransforms;

        List<Texture2D> frames;
        RenderTexture buffer;

        private void Start()
        {
            camPlayer = Camera.main;
            transformsToCapture = new Dictionary<Transform, TransformCapture>();

            camRender = GetComponent<Camera>();
            buffer = new RenderTexture(320, 240, 24);
            frames = new List<Texture2D>();
        }

        private void Update()
        {
            if (recording)
            {
                frameCount++;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (!recording)
                {
                    Debug.Log("Recording started");

                    frameCount = 0;

                    transformsToCapture = new Dictionary<Transform, TransformCapture>();
                    Transform cameraTransform = camPlayer.transform;
                    transformsToCapture.Add(cameraTransform, new TransformCapture(cameraTransform));

                    foreach (Transform t in miscTransforms)
                    {
                        transformsToCapture.Add(t, new TransformCapture(t));
                    }

                    recording = true;
                }
                else
                {
                    recording = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                recording = false;

                WriteToDisk();
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (recording)
            {
                Graphics.Blit(source, buffer);
                Texture2D texture = new Texture2D(buffer.width, buffer.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, buffer.width, buffer.height), 0, 0);
                frames.Add(texture);

                foreach (var kvp in transformsToCapture)
                {
                    kvp.Value.Capture(frameCount, kvp.Key, camPlayer.fieldOfView);
                }
            }

            Graphics.Blit(source, destination);
        }

        void WriteToDisk()
        {
            string captureName = "depth_capture_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second;
            string dirPath = Directory.CreateDirectory(Application.persistentDataPath + "/" + captureName).FullName;

            List<TransformCapture> capturesTransform = transformsToCapture.Values.ToList();
            string json = JsonConvert.SerializeObject(capturesTransform, Formatting.Indented);
            File.WriteAllText(dirPath + "/" + captureName + "_trs.json", json);

            transformsToCapture.Clear();

            for (int i = 0; i < frames.Count; i++)
            {
                byte[] bytes = frames[i].EncodeToPNG();
                File.WriteAllBytes(dirPath + "/" + i + ".png", bytes);
                Destroy(frames[i]);
            }
            frames.Clear();

            Debug.Log("Capture saved to " + dirPath);
        }
    }
}
