using System.Collections.Generic;
using UnityEngine;

namespace DoomArcade.Scripts.DoomClone.FrameCapture
{
    internal class TransformCapture
    {
        public int id;
        public string name;

        public int id_parent;
        public int[] id_children;

        public bool absolute;
        public bool mesh;
        public int meshVertexAmount;

        // posx, posy, posz,
        // rotx, roty, rotz, rotw
        // sclx, scly, sclz
        public List<float[]> capture;

        public TransformCapture(UnityEngine.Transform t, bool absolute = true)
        {
            name = t.name;
            id = t.GetInstanceID();
            id_parent = t.parent == null ? 0 : t.parent.GetInstanceID();
            id_children = new int[t.childCount];
            for (int i = 0; i < t.childCount; i++)
            {
                id_children[i] = t.GetChild(i).GetInstanceID();
            }

            this.absolute = absolute;

            var meshFilter = t.GetComponent<MeshFilter>();
            mesh = meshFilter != null && meshFilter.sharedMesh != null;
            if (mesh)
                meshVertexAmount = meshFilter.sharedMesh.vertexCount;
            else
            {
                var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
                mesh = skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null;
                if (mesh)
                    meshVertexAmount = skinnedMeshRenderer.sharedMesh.vertexCount;
            }

            capture = new List<float[]>();

            if (mesh)
                Capture(0, t);
        }

        public void Capture(int frame, Transform trs, float fov = 0)
        {
            if (absolute)
                capture.Add(new float[12]
                {
                    trs.position.x, trs.position.y, trs.position.z,
                    trs.rotation.w, trs.rotation.x, trs.rotation.y, trs.rotation.z,
                    trs.lossyScale.x, trs.lossyScale.y, trs.lossyScale.z,
                    frame,
                    fov
                });
            else
                capture.Add(new float[12]
                {
                    trs.localPosition.x, trs.localPosition.y, trs.localPosition.z,
                    trs.localRotation.w, trs.localRotation.x, trs.localRotation.y, trs.localRotation.z,
                    trs.localScale.x, trs.localScale.y, trs.localScale.z,
                    frame,
                    fov
                });
        }
    }
}