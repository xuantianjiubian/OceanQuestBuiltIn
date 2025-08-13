using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OceanQuest
{
    public class Buoyancy : MonoBehaviour
    {

        private bool isMeshCol { get; set; }
        private Rigidbody rb { get; set; }
        private Collider col { get; set; }
        public WaveCalculator waveCal;

        public LayerMask mask;

        [SerializeField] private int voxelSlicePerAxis = 2;
        [SerializeField] private int maxVoxelCount = 16;

        private List<Vector3> voxels = new List<Vector3>();

        [SerializeField] private float density = 500f;
        const float WATER_DENSITY = 1000f;

        private float perVoxelsBouyancy = 0f;
        private float voxelHalfHeight = 0f;

        [SerializeField] float resistance_factor = 1f;


        List<Vector3[]> forces = new List<Vector3[]>();
        // Start is called before the first frame update
        void Start()
        {
            InitRbAndCol();
            SetUpRigidbody();
            InitVoxels();
            CaculatePerVoxelBouyancy();
            CaculateVoxelHalfHeight();
        }

        private void FixedUpdate()
        {
            forces.Clear();
            foreach (var voxel in voxels)
            {
                ApplyBouyancy(voxel);
            }
        }
        private void InitRbAndCol()
        {
            rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            col = GetComponent<Collider>() ?? gameObject.AddComponent<MeshCollider>();
            isMeshCol = col is MeshCollider;
        }
        private void SetUpRigidbody()
        {
            var bounds = col.bounds;
            rb.centerOfMass = transform.InverseTransformPoint(bounds.center);
        }

        Vector3 GetVoxelPos(Bounds bounds, int x, int y, int z)
        {
            Vector3 pos = new Vector3();
            pos.x = bounds.min.x + ((bounds.size.x / voxelSlicePerAxis)) * (0.5f + x);
            pos.y = bounds.min.y + ((bounds.size.y / voxelSlicePerAxis)) * (0.5f + y);
            pos.z = bounds.min.z + ((bounds.size.z / voxelSlicePerAxis)) * (0.5f + z);
            pos = transform.InverseTransformPoint(pos);
            return pos;
        }

        List<Vector3> SliceVoxels()
        {
            List<Vector3> voxelList = new List<Vector3>();
            var bounds = col.bounds;
            for (int i = 0; i < voxelSlicePerAxis; i++)
            {
                for (int j = 0; j < voxelSlicePerAxis; j++)
                {
                    for (int k = 0; k < voxelSlicePerAxis; k++)
                    {
                        voxelList.Add(GetVoxelPos(bounds, i, j, k));
                    }
                }
            }
            return voxelList;
        }
        static void LimitVoxelCount(IList<Vector3> voxelList, int maxCount)
        {
            if (voxelList.Count <= 2 || maxCount <= 2)
            {
                return;
            }
            while (voxelList.Count > maxCount)
            {
                int firstID = 0;
                int secondID = 0;
                FindClosestVoxels(voxelList, out firstID, out secondID);
                Vector3 mix = (voxelList[firstID] + voxelList[secondID]) * 0.5f;
                voxelList.RemoveAt(firstID);
                voxelList.RemoveAt(secondID);
                voxelList.Add(mix);
            }
        }
        static void FindClosestVoxels(IList<Vector3> voxelList, out int firstID, out int secondID)
        {
            float minDis = float.MaxValue;
            int tempIDx = 0;
            int tempIDy = 0;
            for (int i = 0; i < voxelList.Count - 1; i++)
            {
                for (int j = i + 1; j < voxelList.Count; j++)
                {
                    Vector3 disVec = voxelList[i] - voxelList[j];
                    float dis = disVec.magnitude;
                    if (dis < minDis)
                    {
                        tempIDx = i;
                        tempIDy = j;
                        minDis = dis;
                    }
                }
            }
            firstID = tempIDx;
            secondID = tempIDy;
        }
        void InitVoxels()
        {
            voxels = SliceVoxels();
            LimitVoxelCount(voxels, maxVoxelCount);
        }

        void CaculatePerVoxelBouyancy()
        {
            float volume = rb.mass / density;
            float totalBouyacy = WATER_DENSITY * Mathf.Abs(Physics.gravity.y) * volume;
            perVoxelsBouyancy = totalBouyacy / voxels.Count;
        }
        void ApplyBouyancy(Vector3 point)
        {

            Vector3 worldPoint = transform.TransformPoint(point);
            float waterlevel = GetWaterLevel(worldPoint.x, worldPoint.z,1);
            if (worldPoint.y - voxelHalfHeight < waterlevel)
            {
                float k = Mathf.Clamp01((waterlevel - worldPoint.y) / (2 * voxelHalfHeight) + 0.5f);
                Vector3 bouyancy = new Vector3(0, perVoxelsBouyancy, 0) * k;
                Vector3 resistance = -1 * rb.GetPointVelocity(worldPoint) * rb.GetPointVelocity(worldPoint).magnitude*resistance_factor * rb.mass;
                

                Vector3 totalForce = bouyancy + resistance;
                forces.Add(new[] { worldPoint,totalForce});
                rb.AddForceAtPosition(totalForce, worldPoint, ForceMode.Force);
            }
        }
        /*float GetWaterLevel(float x, float z)
        {
            return 0;
        }*/
        float GetWaterLevel(float x, float z)
        {
            float baseHeight = 0f; // 基础水面高度
            float waveHeight = Mathf.Sin(Time.time * 2f + x*10) * 0.5f
                             + Mathf.Cos(Time.time * 1.5f + z*10) * 0.3f;
            return baseHeight + waveHeight;
        }
        float GetWaterLevel(float x,float z,int isRay=1)
        {
            
            Vector3 origin = new Vector3(x, 10, z);

            RaycastHit ray;
            if(Physics.Raycast(origin, Vector3.down, out ray, Mathf.Infinity,mask))
            {
                Vector3 rayPos = ray.point;
                float waterlevel = waveCal.GetExactWaveHeight(rayPos);
                Debug.Log(waterlevel);
                return waterlevel+0.5f;
            }
            
            return 0;
        }
        void CaculateVoxelHalfHeight()
        {
            var bounds = col.bounds;
            voxelHalfHeight = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) / (voxelSlicePerAxis * 2);
        }
        // Update is called once per frame
        void Update()
        {

        }
        private void OnDrawGizmos()
        {
            if (voxels == null || forces == null)
                return;
            Gizmos.color = Color.yellow;
            const float gizmosSize = 0.05f;
            foreach(var p in voxels)
            {
                Gizmos.DrawCube(transform.TransformPoint(p), new Vector3(gizmosSize, gizmosSize, gizmosSize));
            }

            Gizmos.color = Color.cyan;
            foreach(var f in forces)
            {
                Gizmos.DrawLine(f[0], f[0]+f[1]/rb.mass);
            }
        }
    }
}

