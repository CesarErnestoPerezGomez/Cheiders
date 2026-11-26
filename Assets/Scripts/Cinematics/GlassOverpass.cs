using UnityEngine;

namespace ACG
{
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GlassOverpass : MonoBehaviour
    {
        public const int RiderIndex = 6;
        Mesh deck;

        // Arena-local coordinates. The blue rider and both edges sample the same curve.
        public static Vector3 Center(float raceTime)
        {
            float climb = Mathf.InverseLerp(.85f, 2.05f, raceTime);
            float turn = Mathf.InverseLerp(1.65f, 3.95f, raceTime);
            return new Vector3(Mathf.SmoothStep(17, -39, turn),
                Mathf.SmoothStep(0, 6.2f, climb), -61.5f + 15 * raceTime);
        }

        void OnEnable()
        {
            const int segments = 144;
            const float halfWidth = 2.15f;
            var vertices = new Vector3[(segments+1)*2];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var indices = new int[segments*12];
            for (int i=0; i<=segments; i++)
            {
                float fraction = (float)i/segments;
                float time = Mathf.Lerp(.85f, 8.6f, fraction);
                Vector3 center = Center(time);
                Vector3 tangent = (Center(time+.001f) - Center(time-.001f)).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                int left = i*2;
                // A slight lift at the join keeps the two glass surfaces from flickering.
                vertices[left] = center - right*halfWidth + Vector3.up*.012f;
                vertices[left+1] = center + right*halfWidth + Vector3.up*.012f;
                normals[left] = normals[left+1] = Vector3.Cross(tangent, right).normalized;
                uv[left] = new Vector2(0, fraction);
                uv[left+1] = new Vector2(1, fraction);
                if (i==segments) break;
                int j = i*12;
                indices[j] = left; indices[j+1] = left+2; indices[j+2] = left+1;
                indices[j+3] = left+1; indices[j+4] = left+2; indices[j+5] = left+3;
                for (int k=0; k<6; k++) indices[j+6+k] = indices[j+5-k];
            }
            deck = new Mesh { name = "Glass overpass", hideFlags = HideFlags.DontSave };
            deck.vertices = vertices;
            deck.normals = normals;
            deck.uv = uv;
            deck.triangles = indices;
            GetComponent<MeshFilter>().sharedMesh = deck;
        }

        void OnDisable()
        {
            if (!deck) return;
            GetComponent<MeshFilter>().sharedMesh = null;
            if (Application.isPlaying) Destroy(deck); else DestroyImmediate(deck);
        }
    }
}
