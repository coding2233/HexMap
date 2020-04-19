using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace wanderer
{
    [System.Serializable]
    public struct OctreeNode
    {
        public Bounds Bounds;
        public int CurrentDepth;
        public List<TriangleVertices> Triangles;

        // 节点的子节点
        public OctreeNode[] ChildNodes;

        public void DrawBounds(Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);
        }

        public void Handle(Bounds bounds, IProjectorHandle handle, string meshName = null)
        {
            List<TriangleVertices> triangles = ListPool<TriangleVertices>.Get();
            //索取所有的三角形
            GetTriangles(bounds, this, triangles);
            //转换成模式
            Mesh mesh = new Mesh();
            if (!string.IsNullOrEmpty(meshName))
                mesh.name = $"mesh_{meshName}";
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> indexs = new List<int>();
            Vector2 uv = new Vector2();
            int index = 0;
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleVertices triangle = triangles[i];
                //vertices
                vertices.Add(triangle.Vertex0);
                vertices.Add(triangle.Vertex1);
                vertices.Add(triangle.Vertex2);
                //uvs
                uv.x = (triangle.Vertex0.x - bounds.min.x) / bounds.size.x;
                uv.y = (triangle.Vertex0.z - bounds.min.z) / bounds.size.z;
                uvs.Add(uv);
                uv.x = (triangle.Vertex1.x - bounds.min.x) / bounds.size.x;
                uv.y = (triangle.Vertex1.z - bounds.min.z) / bounds.size.z;
                uvs.Add(uv);
                uv.x = (triangle.Vertex2.x - bounds.min.x) / bounds.size.x;
                uv.y = (triangle.Vertex2.z - bounds.min.z) / bounds.size.z;
                uvs.Add(uv);
                //index
                indexs.Add(index);
                indexs.Add(index + 1);
                indexs.Add(index + 2);
                index += 3;
            }
            //设置模型信息
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indexs, 0);
            ListPool<TriangleVertices>.Release(triangles);
            //回调
            handle.ProjectorHandle(mesh);
        }

        void GetTriangles(Bounds bounds, OctreeNode node, List<TriangleVertices> triangles)
        {
            int count = node.Triangles == null ? 0 : node.Triangles.Count;
            if (bounds.Intersects(node.Bounds))
            {
                if (node.Triangles != null && node.Triangles.Count > 0)
                {
                    triangles.AddRange(node.Triangles);
                }

                if (node.ChildNodes != null)
                {
                    for (int i = 0; i < node.ChildNodes.Length; i++)
                    {
                        GetTriangles(bounds, node.ChildNodes[i], triangles);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class Octree : ScriptableObject
    {
        public OctreeNode Root;
        public int MaxDepth;
        public int TriangleCount;
        public void Setup(Bounds maxBounds, int maxDepth, List<TriangleVertices> triangles)
        {
            MaxDepth = maxDepth;
            Root = new OctreeNode();
            Root.Bounds = maxBounds;

            SplitBounds(ref Root, triangles, 0);
        }

        private void SplitBounds(ref OctreeNode node, List<TriangleVertices> triangles, int deep)
        {
            node.CurrentDepth = deep;
            if (deep < MaxDepth)
            {
                node.Triangles = new List<TriangleVertices>();
                node.ChildNodes = new OctreeNode[8];

                Bounds bounds = node.Bounds;
                Vector3 half2Vector = bounds.size / 4;
                // 八块空间的位置
                node.ChildNodes[0].Bounds.center = bounds.center - half2Vector;
                node.ChildNodes[0].Bounds.size = bounds.size / 2;
                node.ChildNodes[1].Bounds.center = node.ChildNodes[0].Bounds.center + new Vector3(bounds.size.x / 2, 0, 0);
                node.ChildNodes[1].Bounds.size = bounds.size / 2;
                node.ChildNodes[2].Bounds.center = node.ChildNodes[1].Bounds.center + new Vector3(0, 0, bounds.size.z / 2);
                node.ChildNodes[2].Bounds.size = bounds.size / 2;
                node.ChildNodes[3].Bounds.center = node.ChildNodes[0].Bounds.center + new Vector3(0, 0, bounds.size.z / 2);
                node.ChildNodes[3].Bounds.size = bounds.size / 2;
                node.ChildNodes[4].Bounds.center = node.ChildNodes[0].Bounds.center + new Vector3(0, bounds.size.y / 2, 0);
                node.ChildNodes[4].Bounds.size = bounds.size / 2;
                node.ChildNodes[5].Bounds.center = node.ChildNodes[1].Bounds.center + new Vector3(0, bounds.size.y / 2, 0);
                node.ChildNodes[5].Bounds.size = bounds.size / 2;
                node.ChildNodes[6].Bounds.center = node.ChildNodes[2].Bounds.center + new Vector3(0, bounds.size.y / 2, 0);
                node.ChildNodes[6].Bounds.size = bounds.size / 2;
                node.ChildNodes[7].Bounds.center = node.ChildNodes[3].Bounds.center + new Vector3(0, bounds.size.y / 2, 0);
                node.ChildNodes[7].Bounds.size = bounds.size / 2;

                //整理在自己的碰撞盒子里面
                for (int i = 0; i < triangles.Count; i++)
                {
                    TriangleVertices triangle = triangles[i];
                    if (!node.Bounds.Intersects(triangle.Bounds))
                    {
                        triangles.RemoveAt(i);
                        i--;
                    }
                }
                // 3、遍历8个区域 递归调用
                List<TriangleVertices> childtriangles = new List<TriangleVertices>();
                for (int i = 0; i < node.ChildNodes.Length; i++)
                {
                    childtriangles.Clear();
                    node.ChildNodes[i].Triangles = new List<TriangleVertices>();
                    for (int j = 0; j < triangles.Count; j++)
                    {
                        TriangleVertices triangle = triangles[j];
                        if (node.ChildNodes[i].Bounds.Intersects(triangle.Bounds))
                        {
                            childtriangles.Add(triangle);
                        }
                    }

                    if (childtriangles.Count > 0)
                    {
                        SplitBounds(ref node.ChildNodes[i], childtriangles, node.CurrentDepth + 1);
                    }
                }


            }
            else
            {
                node.Triangles.AddRange(triangles);
                TriangleCount += triangles.Count;
            }
        }
    }

}