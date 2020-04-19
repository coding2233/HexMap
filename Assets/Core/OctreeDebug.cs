using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace wanderer
{
    public class OctreeDebug : MonoBehaviour
    {
        public Octree Octree;

        public List<TriangleVertices> Triangles;

        public Bounds ProjectorBounds;

        private void OnDrawGizmos()
        {
            if (Octree != null)
            {
                DrawBounds(Octree.Root.ChildNodes);
                // start = true;
            }

            if (ProjectorBounds != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(ProjectorBounds.center, ProjectorBounds.size);
            }

            if (Triangles != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < Triangles.Count; i++)
                {
                    Gizmos.DrawWireCube(Triangles[i].Bounds.center, Triangles[i].Bounds.size);
                }

            }
        }

        public bool start = false;

        private void DrawBounds(wanderer.OctreeNode[] octreeNode)
        {
            for (int i = 0; i < octreeNode.Length; i++)
            {
                if (octreeNode[i].ChildNodes != null)
                {
                    DrawBounds(octreeNode[i].ChildNodes);
                }
                octreeNode[i].DrawBounds(new Color(0, 0.7f, 0));
            }
        }
    }
}