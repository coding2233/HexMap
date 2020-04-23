using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Jobs;
using Unity.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace wanderer
{
    public class OctreeEditor : EditorWindow, IProjectorHandle
    {
        #region field
        //配置文件
        private OctreeEditorConfig _octreeConfig;
        //八叉树数据
        private Octree _octree;

        private OctreeDebug _octreeDebug;

        #endregion

        [MenuItem("Tools/Octree")]
        static void Main()
        {
            GetWindow<OctreeEditor>();
        }

        private void OnEnable()
        {

            //加载配置文件
            string configPath = Path.GetDirectoryName(Application.dataPath);
            configPath = Path.Combine(configPath, "ProjectSettings/octree.json");
            if (File.Exists(configPath))
            {
                _octreeConfig = JsonUtility.FromJson<OctreeEditorConfig>(File.ReadAllText(configPath));
            }
            else
            {
                _octreeConfig = new OctreeEditorConfig();
            }

            //默认打开某个物体
            if (Selection.activeObject != null)
            {
                _octree = Selection.activeObject as Octree;
                if (_octree != null)
                {
                    _octreeConfig.OctreeAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    _octreeConfig.MaxBounds = _octree.Root.Bounds;
                    _octreeConfig.MaxDepth = _octree.MaxDepth;
                }
            }

            //octreeDebug
            _octreeDebug = new GameObject("Octree Debug").AddComponent<OctreeDebug>();
            _octreeDebug.Octree = _octree;

            //update
            EditorApplication.update += OnUpdate;


        }

        private void OnDisable()
        {
            if (_octreeDebug != null)
            {
                DestroyImmediate(_octreeDebug.gameObject);
                _octreeDebug = null;
            }
            //保存配置文件
            string configPath = Path.GetDirectoryName(Application.dataPath);
            configPath = Path.Combine(configPath, "ProjectSettings/octree.json");
            if (_octreeConfig != null)
            {
                File.WriteAllText(configPath, JsonUtility.ToJson(_octreeConfig));
            }

            EditorApplication.update -= OnUpdate;

            _octree = null;
        }

        private void OnGUI()
        {
            if (_octreeConfig == null)
                return;

            SceneOctreeGUI();
            GUILayout.Space(30);
            MakeMeshGUI();
        }


        private void OnUpdate()
        {

        }


        #region 内部函数

        //场景的八叉树GUI
        private void SceneOctreeGUI()
        {
            GUILayout.Label("Scene Octree", EditorStyles.boldLabel);

            GUILayout.BeginVertical("HelpBox");
            Octree octree = (Octree)EditorGUILayout.ObjectField("Octree", _octree, typeof(Octree), false);
            if (octree != _octree)
            {
                _octree = octree;
                _octreeConfig.OctreeAssetPath = AssetDatabase.GetAssetPath(octree);
                _octreeConfig.MaxBounds = _octree.Root.Bounds;
                _octreeConfig.MaxDepth = _octree.MaxDepth;
                _octreeDebug.Octree = _octree;
            }
            bool hasOctree = (_octree != null);
            if (hasOctree)
            {
                EditorGUILayout.LabelField("OctreeAssetPath", _octreeConfig.OctreeAssetPath);
            }
            _octreeConfig.Mask = EditorGUILayout.MaskField("Mask", _octreeConfig.Mask, InternalEditorUtility.layers);

            GUILayout.BeginVertical("HelpBox");
            _octreeConfig.MaxBounds = EditorGUILayout.BoundsField("Bounds", _octreeConfig.MaxBounds);
            // _boundsCollider = (Collider)EditorGUILayout.ObjectField("BoundsCollider", _boundsCollider, typeof(Collider), true);
            // if (_boundsCollider != null && _octreeConfig.MaxBounds != _boundsCollider.bounds)
            // {
            //     _octreeConfig.MaxBounds = _boundsCollider.bounds;
            // }
            GUILayout.EndVertical();
            _octreeConfig.MaxDepth = EditorGUILayout.IntField("MaxDepth", _octreeConfig.MaxDepth);

            GUILayout.EndVertical();
            GUILayout.Space(5);
            if (GUILayout.Button(hasOctree ? "Rebuild Octree" : "Build Octree") && _octreeConfig.MaxBounds.size != Vector3.zero)
            {
                List<TriangleVertices> triangles = GetTriangles(FindObjectsWithLayer<MeshFilter>(_octreeConfig.Mask));
                //  triangles.AddRange(GetTriangles(FindObjectsWithLayer<Terrain>(_octreeConfig.Mask)));

                if (!hasOctree)
                    _octree = ScriptableObject.CreateInstance<Octree>();
                _octree.Setup(_octreeConfig.MaxBounds, _octreeConfig.MaxDepth, triangles);
                ListPool<TriangleVertices>.Release(triangles);

                if (!hasOctree)
                {
                    //创建物体
                    _octreeConfig.OctreeAssetPath = EditorUtility.SaveFilePanelInProject("octree", "octree", "asset", "xx");
                    if (!string.IsNullOrEmpty(_octreeConfig.OctreeAssetPath))
                    {
                        AssetDatabase.CreateAsset(_octree, _octreeConfig.OctreeAssetPath);
                        _octreeDebug.Octree = _octree;
                    }
                    else
                    {
                        _octree = null;
                    }
                }
            }
        }

        //制作模型GUI
        private void MakeMeshGUI()
        {
            GUILayout.Label("Mesh Make", EditorStyles.boldLabel);
            GUILayout.BeginVertical("HelpBox");
            _octreeConfig.Rows = EditorGUILayout.IntField("Row", _octreeConfig.Rows);
            _octreeConfig.Columns = EditorGUILayout.IntField("Column", _octreeConfig.Columns);
            _octreeConfig.ProjectorSize = EditorGUILayout.FloatField("Projector Size", _octreeConfig.ProjectorSize);//
            _octreeConfig.ProjectorInnerCircleScale = EditorGUILayout.FloatField("Projector Inner Circle Scale", _octreeConfig.ProjectorInnerCircleScale);
            _octreeConfig.ProjectorMaterial = (Material)EditorGUILayout.ObjectField("Projector Material", _octreeConfig.ProjectorMaterial, typeof(Material), false);
            GUILayout.EndVertical();
            GUILayout.Space(5);
            if (GUILayout.Button("Make Mesh") && _octree != null)
            {
                MakeMeshGameObject();
            }
        }

        /// <summary>
        /// 获取所有的三角形
        /// </summary>
        /// <param name="meshFilters"></param>
        /// <returns></returns>
        List<TriangleVertices> GetTriangles(List<MeshFilter> meshFilters)
        {
            List<TriangleVertices> triangles = ListPool<TriangleVertices>.Get();

            foreach (var meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] indices = mesh.GetIndices(i);
                    for (int m = 0; m < indices.Length; m += 3)
                    {
                        Vector3 vertex0 = meshFilter.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[indices[m]]);
                        Vector3 vertex1 = meshFilter.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[indices[m + 1]]);
                        Vector3 vertex2 = meshFilter.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[indices[m + 2]]);

                        triangles.Add(new TriangleVertices(vertex0, vertex1, vertex2));
                    }
                }
            }

            return triangles;
        }

        List<TriangleVertices> GetTriangles(List<Terrain> terrains)
        {
            List<TriangleVertices> triangles = new List<TriangleVertices>();
            Vector3[] vertices = new Vector3[4];
            foreach (var terrain in terrains)
            {
                int with = terrain.terrainData.heightmapResolution - 1;
                for (int i = 0; i < with; i++)
                {
                    for (int j = 0; j < with; j++)
                    {
                        //  vertices[0].x=terrain.terrainData.Get
                    }
                }
                // Vector3 vertex0 = meshFilter.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[indices[m]]);
                // Vector3 vertex1 = meshFilter.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[indices[m + 1]]);
                // Vector3 vertex2 = meshFilter.transform.localToWorldMatrix.MultiplyPoint(mesh.vertices[indices[m + 2]]);
                // triangles.Add(new TriangleVertices(vertex0, vertex1, vertex2));
            }
            return triangles;
        }

        /// <summary>
        /// 创建模型的GameObject
        /// </summary>
        private void MakeMeshGameObject()
        {
            //projetor的大小
            float size = _octreeConfig.ProjectorSize;
            float halfSize = size * 0.5f;
            float offsetSize = _octreeConfig.ProjectorSize * _octreeConfig.ProjectorInnerCircleScale;
            float offsetHalfSize = offsetSize * 0.5f;

            //初始化 projector的数据    
            Vector3 startPos = new Vector3(_octreeConfig.MaxBounds.min.x + halfSize, _octreeConfig.MaxBounds.center.y, _octreeConfig.MaxBounds.min.z + halfSize);
            Vector3 projectorSize = new Vector3(size, _octreeConfig.MaxBounds.size.y, size);
            //初始化 projectorBounds
            for (int i = 0; i < _octreeConfig.Rows; i++)
            {
                for (int j = 0; j < _octreeConfig.Columns; j++)
                {
                    float offset = i % 2 == 0 ? 0 : offsetHalfSize;
                    Vector3 center = startPos + new Vector3(j * offsetSize + offset, 0, i * halfSize * 1.5f);
                    Bounds projectorBounds = new Bounds(center, projectorSize);
                    _octreeDebug.ProjectorBounds = projectorBounds;
                    _octree.Root.Handle(projectorBounds, this, $"{i}_{j}");
                    // new Task(
                    //     () =>
                    //     {
                    //         _octree.Root.Handle(projectorBounds, this);
                    //     }
                    // ).Start();
                }
            }
        }

        /// <summary>
        /// 查找所有的物体
        /// </summary>
        /// <param name="mask"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private List<T> FindObjectsWithLayer<T>(int mask) where T : Component
        {
            List<T> objects = new List<T>();
            T[] allObjects = GameObject.FindObjectsOfType<T>();
            foreach (var item in allObjects)
            {
                int layer = 1 << item.gameObject.layer;
                if (mask == -1 || layer == (layer & mask))
                {
                    objects.Add(item);
                }
            }
            return objects;
        }

        /// <summary>
        /// 处理信息
        /// </summary>
        /// <param name="triangles"></param>
        public void ProjectorHandle(Mesh mesh)
        {
            GameObject clone = new GameObject($"Hex_{mesh.name}");
            clone.AddComponent<MeshFilter>().mesh = mesh;
            clone.AddComponent<MeshRenderer>().material = _octreeConfig.ProjectorMaterial;

            GameObject HexGameObject = GameObject.Find("HexGameObject");
            if (HexGameObject == null)
            {
                HexGameObject = new GameObject("HexGameObject");
            }
            clone.transform.SetParent(HexGameObject.transform);
        }
        #endregion



    }


    //八叉树 配置文件
    [System.Serializable]
    public class OctreeEditorConfig
    {
        /// <summary>
        /// 八叉树路径
        /// </summary>
        public string OctreeAssetPath = "";
        /// <summary>
        /// 层级遮罩
        /// </summary>
        public int Mask = -1;
        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds MaxBounds = new Bounds(Vector3.zero, Vector3.one * 100);
        /// <summary>
        /// 默认深度
        /// </summary>
        public int MaxDepth = 10;
        /// <summary>
        /// 行
        /// </summary>
        public int Rows = 10;
        /// <summary>
        /// 列
        /// </summary>
        public int Columns = 10;
        /// <summary>
        /// 投影包围盒子
        /// </summary>
        public float ProjectorSize = 10;
        /// <summary>
        /// 内圆比例
        /// </summary>
        public float ProjectorInnerCircleScale = 0.859375f;
        /// <summary>
        /// 材质
        /// </summary>
        public Material ProjectorMaterial;
    }

}