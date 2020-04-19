using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace wanderer
{
    public class OctreeProjector : MonoBehaviour
    {

        [SerializeField]
        float _size = 10;
        [SerializeField]
        float _aspect = 1;
        [SerializeField]
        float _near = 0.1f;
        [SerializeField]
        float _far = 100f;
        public Matrix4x4 _mtx;


        // Start is called before the first frame update
        void Start()
        {
            _mtx = Matrix4x4.Ortho(-_size * _aspect, _size * _aspect, _size, -_size, -_near, -_far).inverse;
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDrawGizmos()
        {
            _mtx = Matrix4x4.Ortho(-_size * _aspect, _size * _aspect, _size, -_size, -_near, -_far).inverse;
            _mtx = transform.localToWorldMatrix * _mtx;

            Vector3 p1 = new Vector3(-1, -1, -1);
            Vector3 p2 = new Vector3(-1, 1, -1);
            Vector3 p3 = new Vector3(1, 1, -1);
            Vector3 p4 = new Vector3(1, -1, -1);
            Vector3 p5 = new Vector3(-1, -1, 1);
            Vector3 p6 = new Vector3(-1, 1, 1);
            Vector3 p7 = new Vector3(1, 1, 1);
            Vector3 p8 = new Vector3(1, -1, 1);
            p1 = _mtx.MultiplyPoint(p1);
            p2 = _mtx.MultiplyPoint(p2);
            p3 = _mtx.MultiplyPoint(p3);
            p4 = _mtx.MultiplyPoint(p4);
            p5 = _mtx.MultiplyPoint(p5);
            p6 = _mtx.MultiplyPoint(p6);
            p7 = _mtx.MultiplyPoint(p7);
            p8 = _mtx.MultiplyPoint(p8);

            Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            Gizmos.DrawLine(p5, p6);
            Gizmos.DrawLine(p6, p7);
            Gizmos.DrawLine(p7, p8);
            Gizmos.DrawLine(p8, p5);

            Gizmos.DrawLine(p1, p5);
            Gizmos.DrawLine(p2, p6);
            Gizmos.DrawLine(p3, p7);
            Gizmos.DrawLine(p4, p8);
        }

    }
}