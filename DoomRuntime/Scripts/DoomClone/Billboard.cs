using UnityEngine;

namespace DoomArcade.Scripts.DoomClone
{
    public class Billboard : MonoBehaviour
    {
        private static Mesh plane;

        [SerializeField] private Texture2D sprite;
        [SerializeField] private Shader sharedBoxProjectShader; 
        private MeshRenderer meshRenderer;
        private Transform rendererTransform;

        private bool init;

        private void Start()
        {
            if (!init)
                Init();
        }

        private void Init()
        {
            init = true;

            if (plane == null)
            {
                float size = 2;
                plane = new Mesh();
                plane.vertices = new Vector3[]
                {
                    new Vector3(-size/2f, 0, 0),
                    new Vector3(size/2f, 0, 0),
                    new Vector3(size/2f, size, 0),
                    new Vector3(-size/2f, size, 0)
                };
                plane.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            }

            if (sharedBoxProjectShader == null)
            {
                var game = Game.Instance;
                if (game != null && game.boxProjectShader != null)
                {
                    sharedBoxProjectShader = game.boxProjectShader;
                }
                else
                {
                    sharedBoxProjectShader = Shader.Find("Custom/BoxProject");
                }

                if (sharedBoxProjectShader == null)
                {
                    Debug.LogError("[Billboard] BoxProject shader not found (Game.BoxProjectShader and Shader.Find both failed)");
                    return;
                }
            }

            rendererTransform = new GameObject("Renderer").transform;
            rendererTransform.SetParent(transform);
            rendererTransform.localPosition = Vector3.zero;
            rendererTransform.localRotation = Quaternion.identity;
            rendererTransform.localScale = Vector3.one;

            MeshFilter meshFilter = rendererTransform.gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = plane;
            meshRenderer = rendererTransform.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(sharedBoxProjectShader);
            meshRenderer.material.SetFloat("_TexOffsetX", 0.5f);

            if (sprite != null)
                SetTexture(sprite, false);
        }

        private Transform PlayerBody
        {
            get
            {
                return Player.current?.body != null ? Player.current.body.transform : null;
            }
        }

        private void LateUpdate()
        {
            var body = PlayerBody;
            if (body == null || rendererTransform == null)
                return;

            Vector3 toPlayer = body.position - rendererTransform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(toPlayer.normalized);
            rendererTransform.rotation = lookRotation;
        }


        public void SetTexture(Texture2D texture, bool mirrored = false)
        {
            if (!init)
                Init();

            rendererTransform.localScale = new Vector3(mirrored ? -1 : 1, 1, 1);

            meshRenderer.material.SetTexture("_FrontTex", texture);
        }

        private void OnDrawGizmos()
        {
            if (sprite == null)
                return;

            Vector3 pos = transform.position;
            pos.y += 0.5f;

            Gizmos.DrawIcon(pos, sprite.name, true);
        }
    }
}
