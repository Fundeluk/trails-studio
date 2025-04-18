using Assets.Scripts;
using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Assets.Scripts.Builders;



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

            GetHeightmapCoordinates().MarkAsOccupied();
        }        

        public HeightmapCoordinates GetHeightmapCoordinates() => new(GetStartPoint(), GetEndPoint(), GetBottomWidth());

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

        public float GetPreviousElementBottomWidth() => GetBottomWidth();

        public float GetBottomWidth() => GetWidth() + 0.5f;

        public Vector3 GetRideDirection() => builder.transform.forward;

        public Transform GetTransform() => builder.transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public void SetSlope(SlopeChange slope) 
        { 
            throw new System.InvalidOperationException("Cannot set slope on rollin.");
        }

        public HeightmapCoordinates? GetSlopeHeightmapCoordinates() => null;

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

    private float legDiameter; // meters

    /// <summary>
    /// Angle of the slope in degrees. Straight down is 90 degrees, flat is 0.
    /// </summary>
    [SerializeField]
    private int angle;

    private float length;
    private Vector3 endPoint;

    private GameObject[] legsInstances = new GameObject[4];
    private GameObject topInstance = null;
    private GameObject slopeInstance = null;    

    public void CreateRollIn()
    {
        if (!(topInstance == null) || !(slopeInstance == null))
        {
            DestroyCurrentRollIn();
        }

        legsInstances = new GameObject[4];

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
        legDiameter = topSize / 8;
        float legSpacing = topSize / 2;
        
        // for legs (cylinder primitives), Height in world units is double its scale's y coordinate
        // so we need to set the y coordinate to half the Height
        float yCoord = height / 2;
        float baseX = transform.position.x;
        float baseZ = transform.position.z;

        for (int i = 0; i < 4; i++)
        {
            float xCoord = i % 2 == 0 ? baseX - legSpacing + legDiameter / 2 : baseX + legSpacing - legDiameter / 2;
            float zCoord = i < 2 ? baseZ - legSpacing + legDiameter / 2 : baseZ + legSpacing - legDiameter / 2;

            var legPos = new Vector3(xCoord, yCoord, zCoord);
            var legScale = new Vector3(legDiameter, height / 2, legDiameter);

            legsInstances[i] = Instantiate(leg, legPos, Quaternion.identity, transform);

            legsInstances[i].transform.localScale = legScale;
        }
    }

    private void CreateTop()
    {
        var topPos = transform.position;
        topPos.y = height + flatThickness / 2;
        var topScale = new Vector3(topSize, flatThickness, topSize);

        topInstance = Instantiate(top, topPos, Quaternion.identity, transform);

        topInstance.transform.localScale = topScale;
    }

    private void CreateSlope()
    {
        float legToEndDist = Mathf.Tan((90 - angle) * Mathf.Deg2Rad) * height;

        // calculate length of slope so that it reaches from top to floor
        float slopeLength = Mathf.Sqrt(Mathf.Pow(height + flatThickness, 2) + Mathf.Pow(legToEndDist, 2));
       
        var slopePos = transform.position + transform.forward * ((topSize + legToEndDist)/2) + transform.up * height/2;
        var slopeRot = new Vector3(angle, 0, 0);
        var slopeScale = new Vector3(topSize, flatThickness, slopeLength);

        slopeInstance = Instantiate(slope, slopePos, Quaternion.identity, transform);

        Transform slopeTransform = slopeInstance.transform;

        slopeTransform.localScale = slopeScale;
        slopeTransform.eulerAngles = slopeRot;

        // TODO if rollin rotation is variable, then this needs to account for that
        endPoint = transform.position + transform.forward * (topSize/2 + legToEndDist);

        length = legToEndDist + topSize;

        RollIn rollIn = new(this, TerrainManager.GetTerrainForPosition(slopeInstance.transform.position)); 

        Line.Instance.AddLineElement(rollIn);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPoint, 0.5f);
        Gizmos.DrawCube(endPoint - transform.forward * length, Vector3.one);
    }


    // Start is called before the first frame update
    void Awake()
    {
#if !DEBUG
        height = MainMenuController.height;
        angle = MainMenuController.angle;
#endif

        CreateRollIn();
    }
}
