using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace wanderer
{
    public class SceneMeshData : MonoBehaviour
    {

        wanderer.Octree _octree;

        public Collider Projector;

        private List<TriangleVertices> _triangle = new List<TriangleVertices>();

        private bool _updateed = false;

        public Material HexMat;

        public bool DrawDebug = true;

        public int row = 10;
        public int column = 10;

        public float size = 6.88f;

        private void Start()
        {
            FindAllMeshData();
        }
        int iiii = 0;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("update projector Space:" + Projector.bounds.center + "##" + Projector.bounds.size);

                if (_octree != null && Projector != null)
                {
                    StartCoroutine(MakeManyMesh());
                    //MakeMesh();
                    //  Debug.Log("update projector");
                }
            }

            if (Input.GetKeyDown(KeyCode.P))
                DrawDebug = !DrawDebug;

        }


        IEnumerator MakeManyMesh()
        {
            int yyyy = 0;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                {
                    Debug.Log($"make mesh :{yyyy++}");
                    _triangle.Clear();
                    //     _octree.Root.Handle(Projector.bounds, _triangle);
                    Debug.Log($"make mesh find triangle");
                    yield return new WaitForEndOfFrame();
                    float offet = j % 2 == 0 ? 0 : size;
                    Projector.transform.position = new Vector3(i * (size + 0.3f) * 2 + offet, 0, j * size * 2);
                    Debug.Log($"make mesh projector position {Projector.transform.position}");
                    yield return new WaitForEndOfFrame();
                    MakeMesh();
                    Debug.Log($"make mesh sccuess!!!!!");
                    //    clone.SetColor((i + j) % 2 == 0 ? Color.black : Color.white);
                }
            }
        }

        void MakeMesh()
        {
            GameObject go = new GameObject();
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            mr.material = new Material(HexMat);

            mf.mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> indexs = new List<int>();

            int index = 0;

            Vector3 min = Projector.bounds.min;
            Vector3 max = Projector.bounds.max;

            float xWidth = max.x - min.x;
            float zWith = max.z - min.z;

            if (_triangle != null)
            {
                index = 0;
                foreach (var item in _triangle)
                {
                    // if (!Projector.bounds.Contains(item._Vertex0) || !Projector.bounds.Contains(item._Vertex1) || !Projector.bounds.Contains(item._Vertex2))
                    // {
                    //     continue;
                    // }

                    vertices.Add(item.Vertex0);
                    vertices.Add(item.Vertex1);
                    vertices.Add(item.Vertex2);

                    Vector2 uv = new Vector2();
                    uv.x = (item.Vertex0.x - min.x) / xWidth;
                    uv.y = (item.Vertex0.z - min.z) / xWidth;
                    Debug.Log($"{uv}--");
                    //    Vector3 pj0 = world2Projector.MultiplyPoint(item._Vertex0);
                    //  uv = new Vector2(pj0.x * 0.5f + 0.5f, pj0.y * 0.5f + 0.5f);
                    uvs.Add(uv);

                    uv.x = (item.Vertex1.x - min.x) / xWidth;
                    uv.y = (item.Vertex1.z - min.z) / xWidth;
                    Debug.Log($"{uv}--");
                    //  pj0 = world2Projector.MultiplyPoint(item._Vertex1);
                    //  uv = new Vector2(pj0.x * 0.5f + 0.5f, pj0.y * 0.5f + 0.5f);
                    uvs.Add(uv);

                    uv.x = (item.Vertex2.x - min.x) / xWidth;
                    uv.y = (item.Vertex2.z - min.z) / xWidth;
                    Debug.Log($"{uv}--");
                    //  pj0 = world2Projector.MultiplyPoint(item._Vertex2);
                    //  uv = new Vector2(pj0.x * 0.5f + 0.5f, pj0.y * 0.5f + 0.5f);
                    uvs.Add(uv);


                    indexs.Add(index);
                    indexs.Add(index + 1);
                    indexs.Add(index + 2);

                    index += 3;
                }
            }
            mf.mesh.SetVertices(vertices);
            mf.mesh.SetUVs(0, uvs);
            mf.mesh.SetTriangles(indexs, 0);
            // mf.mesh.SetIndices(indexs.ToArray(), MeshTopology.Triangles, 0);
        }

        void FindAllMeshData()
        {
            List<TriangleVertices> triangles = new List<TriangleVertices>();
            MeshFilter[] mfs = GameObject.FindObjectsOfType<MeshFilter>();
            for (int i = 0; i < mfs.Length; i++)
            {
                if (mfs[i] != null)
                {
                    // int[] indices = mfs[i].mesh.GetIndices(0);
                    // for (int m = 0; m < indices.Length; m += 3)
                    // {
                    //     Vector3 vertex0 = mfs[i].transform.localToWorldMatrix.MultiplyPoint(mfs[i].mesh.vertices[indices[m]]);
                    //     Vector3 vertex1 = mfs[i].transform.localToWorldMatrix.MultiplyPoint(mfs[i].mesh.vertices[indices[m + 1]]);
                    //     Vector3 vertex2 = mfs[i].transform.localToWorldMatrix.MultiplyPoint(mfs[i].mesh.vertices[indices[m + 2]]);

                    //     triangles.Add(new TriangleVertices(vertex0, vertex1, vertex2));
                    // }
                    for (int j = 0; j < mfs[i].mesh.subMeshCount; j++)
                    {
                        int[] indices = mfs[i].mesh.GetIndices(j);
                        for (int m = 0; m < indices.Length; m += 3)
                        {
                            Vector3 vertex0 = mfs[i].transform.localToWorldMatrix.MultiplyPoint(mfs[i].mesh.vertices[indices[m]]);
                            Vector3 vertex1 = mfs[i].transform.localToWorldMatrix.MultiplyPoint(mfs[i].mesh.vertices[indices[m + 1]]);
                            Vector3 vertex2 = mfs[i].transform.localToWorldMatrix.MultiplyPoint(mfs[i].mesh.vertices[indices[m + 2]]);

                            triangles.Add(new TriangleVertices(vertex0, vertex1, vertex2));
                        }
                    }
                }
            }

            //_octree = new wanderer.Octree(new Bounds(Vector3.zero, Vector3.one * 500), 5, triangles);
        }



        private void OnDrawGizmos()
        {
            if (!DrawDebug)
                return;
            if (Projector != null)
            {
                Gizmos.color = Color.yellow;

                Gizmos.DrawWireCube(Projector.bounds.center, Projector.bounds.size);
            }

            if (_octree != null)
            {
                DrawCube(_octree.Root.ChildNodes);
            }

            if (_triangle != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < _triangle.Count; i++)
                {
                    Gizmos.DrawWireCube(_triangle[i].Bounds.center, _triangle[i].Bounds.size);
                }

            }
        }

        private void DrawCube(wanderer.OctreeNode[] octreeNode)
        {
            if (octreeNode == null)
                return;
            for (int i = 0; i < octreeNode.Length; i++)
            {
                if (octreeNode[i].ChildNodes != null)
                {
                    DrawCube(octreeNode[i].ChildNodes);
                }
                octreeNode[i].DrawBounds(new Color(0, 0.7f, 0));

                // octreeNode[i].m_Bounds.DrawBounds(new Color(0, 0.7f, 0));
            }
        }

        public void ProjectorHandle(List<TriangleVertices> triangles)
        {
            // _triangle = triangles;
        }
    }
}


