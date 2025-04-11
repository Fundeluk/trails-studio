using Assets.Scripts;
using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;



//[ExecuteInEditMode]
public class RollInBuilder : MonoBehaviour
{
    public class RollIn : ILineElement
    {
        private readonly RollInBuilder builder;

        private readonly GameObject cameraTarget;

        private readonly Terrain terrain;

        public RollIn(RollInBuilder builder, Terrain terrain)
        {
            this.builder = builder;
            cameraTarget = new GameObject("Camera Target");
            cameraTarget.transform.SetParent(builder.transform);
            RecalculateCameraTargetPosition();
            this.terrain = terrain;

            RecalculateHeightmapBounds();

            TerrainManager.Instance.MarkTerrainAsOccupied(this);
        }

        private void RecalculateHeightmapBounds()
        {
            Vector3 topGlobalPos = GetTransform().TransformPoint(builder.top.transform.position);
            Bounds bounds = new(topGlobalPos, Vector3.zero);
            bounds.Encapsulate(GetEndPoint());
            bounds.Encapsulate(GetEndPoint() - GetRideDirection() * GetLength());
            bounds.Encapsulate(topGlobalPos + Vector3.Cross(GetRideDirection(), Vector3.down) * (builder.topSize/2 + builder.legDiameter));
            bounds.Encapsulate(topGlobalPos - Vector3.Cross(GetRideDirection(), Vector3.down) * (builder.topSize/2 + builder.legDiameter));
            
            //TerrainManager.DrawBoundsGizmos(bounds, 20);
        }

        public HeightmapCoordinates GetHeightmapCoordinates() => new HeightmapCoordinates(GetStartPoint(), GetEndPoint(), GetBottomWidth());

        public Terrain GetTerrain() => terrain;

        private void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = GetEndPoint() - 0.5f * GetLength() * GetRideDirection() + 0.5f * GetHeight() * GetTransform().up;
        }

        public Vector3 GetEndPoint() => builder.endPoint;
        
        public Vector3 GetStartPoint() => GetEndPoint() - GetLength() * GetRideDirection();

        public int GetIndex() => 0;
        public float GetHeight() => builder.height;
        public float GetLength() => builder.length;

        public float GetWidth() => builder.topSize;

        public float GetBottomWidth() => GetWidth();

        public Vector3 GetRideDirection() => builder.rideDirection.normalized;

        public Transform GetTransform() => builder.transform;

        public GameObject GetCameraTarget() => cameraTarget;
        
        public void DestroyUnderlyingGameObject()
        {
            throw new System.InvalidOperationException("Cannot destroy rollin.");
        }
    }

    [Header("Prefabs")]
    [SerializeField]
    private GameObject leg;

    [SerializeField]
    private GameObject top;

    [SerializeField]
    private GameObject slope;

    [Header("Parameters")]
    [SerializeField]
    private float height; // meters

    [SerializeField]
    private float topSize; // meters

    [SerializeField]
    private float flatThickness; // meters
    [SerializeField] 
    private float legDiameter; // meters

    [SerializeField]
    private int angle; // degrees of slope

    private float length;
    private Vector3 endPoint;
    private Vector3 rideDirection;

    private GameObject[] legsInstances;
    private GameObject topInstance = null;
    private GameObject slopeInstance = null;    

    private void CreateRollIn()
    {
        if (!(topInstance == null) || !(slopeInstance == null))
        {
            DestroyCurrentRollIn();
        }

        CreateLegs();
        CreateTop();
        CreateSlope();
    }


    private void DestroyCurrentRollIn()
    {
        for (int i = 0; i < 4; i++)
        {
            Destroy(legsInstances[i]);
            legsInstances[i] = null;
        }

        Destroy(topInstance);
        topInstance = null;

        Destroy(slopeInstance);
        slopeInstance = null;
    }

        private void CreateLegs()
    {
        float legSpacing = topSize / 2;
        
        // for legs (cylinder primitives), Height in world units is double its scale's y coordinate
        // so we need to set the y coordinate to half the Height
        float yCoord = height / 2;

        for (int i = 0; i < 4; i++)
        {
            float xCoord = i % 2 == 0 ? 0 - legSpacing + legDiameter / 2 : 0 + legSpacing - legDiameter / 2;
            float zCoord = i < 2 ? 0 - legSpacing + legDiameter / 2 : 0 + legSpacing - legDiameter / 2;

            var legPos = new Vector3(xCoord, yCoord, zCoord);
            var legScale = new Vector3(legDiameter, height / 2, legDiameter);

            legsInstances[i] = Instantiate(leg, legPos, Quaternion.identity, transform);

            legsInstances[i].transform.localPosition = legPos;
            legsInstances[i].transform.localScale = legScale;
        }
    }

    private void CreateTop()
    {
        var topPos = new Vector3(0, height, 0);
        var topScale = new Vector3(topSize, flatThickness, topSize);

        topInstance = Instantiate(top, topPos, Quaternion.identity, transform);

        topInstance.transform.localPosition = topPos;
        topInstance.transform.localScale = topScale;
    }

    private void CreateSlope()
    {
        float slopeToLegDist = height / Mathf.Tan(angle * Mathf.Deg2Rad);

        // calculate length of slope so that it reaches from top to floor
        float slopeLength = Mathf.Sqrt(Mathf.Pow(height + flatThickness, 2) + Mathf.Pow(slopeToLegDist, 2));

        // z-axis distance from slope center to top center
        float slopeCenterToTopCenterDist = (topSize + slopeToLegDist) / 2; 

        var slopePos = new Vector3(0, (height + flatThickness) / 2, slopeCenterToTopCenterDist);
        var slopeRot = new Vector3(angle, 0, 0);
        var slopeScale = new Vector3(topSize, flatThickness, slopeLength);

        slopeInstance = Instantiate(slope, slopePos, Quaternion.identity, transform);

        Transform slopeTransform = slopeInstance.transform;

        slopeTransform.localPosition = slopePos;
        slopeTransform.localScale = slopeScale;
        slopeTransform.eulerAngles = slopeRot;

        // TODO if rollin rotation is variable, then this needs to account for that
        endPoint = Vector3.ProjectOnPlane(new Vector3(slopeTransform.position.x, 0, slopeTransform.position.z + slopeToLegDist/2), Vector3.up);

        rideDirection = Vector3.ProjectOnPlane(slopeTransform.forward, Vector3.up);

        length = slopeToLegDist * 2 + topSize;

        RollIn rollIn = new(this, TerrainManager.GetTerrainForPosition(slopeInstance.transform.position)); 

        Line.Instance.AddLineElement(rollIn);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPoint, 0.5f);
        Gizmos.DrawLine(endPoint, endPoint - rideDirection * length);
    }


    // Start is called before the first frame update
    void Start()
    {
        height = MainMenuController.height;
        angle = MainMenuController.angle;

        legsInstances = new GameObject[4];

        CreateRollIn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
