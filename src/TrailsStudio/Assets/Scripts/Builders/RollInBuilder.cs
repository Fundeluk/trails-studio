using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//[ExecuteInEditMode]
public class RollInBuilder : MonoBehaviour
{
    public class RollIn : ILineElement
    {
        private readonly RollInBuilder builder;

        private readonly GameObject cameraTarget;

        public RollIn(RollInBuilder builder)
        {
            this.builder = builder;
            cameraTarget = new GameObject("Camera Target");
            cameraTarget.transform.SetParent(builder.transform);
            RecalculateCameraTargetPosition();
        }

        private void RecalculateCameraTargetPosition()
        {
            cameraTarget.transform.position = GetEndPoint() - 0.5f * GetLength() * GetRideDirection() + 0.5f * GetHeight() * GetTransform().up;
        }

        public Vector3 GetEndPoint() => builder.endPoint + Line.baseHeight * GetTransform().up;

        public int GetIndex() => 0;
        public float GetHeight() => builder.height;
        public float GetLength() => builder.length;

        public float GetWidth() => builder.topSize;

        public Vector3 GetRideDirection() => builder.rideDirection;

        public Transform GetTransform() => builder.transform;

        public GameObject GetCameraTarget() => cameraTarget;

        public void SetHeight(float height)
        {
            builder.height = height;
            builder.CreateRollIn();
            RecalculateCameraTargetPosition();
        }

        public void SetLength(float length)
        {
            throw new System.InvalidOperationException("Cannot set length of rollin.");
        }

        public void SetRideDirection(Vector3 rideDirection)
        {
            // TODO the current rollin rotation is fixed, the implementation of the builder needs to change in order to make this work
            throw new System.InvalidOperationException("Cannot change ride direction of rollin.");
        }

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
        
        // for legs (cylinder primitives), height in world units is double its scale's y coordinate
        // so we need to set the y coordinate to half the height
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

        // TODO if rollin rotation is variable, then this needs to accout for that
        endPoint = Vector3.ProjectOnPlane(new Vector3(slopeTransform.position.x, 0, slopeTransform.position.z + slopeToLegDist/2), Vector3.up);

        rideDirection = Vector3.ProjectOnPlane(slopeTransform.forward, Vector3.up);

        length = slopeToLegDist * 2 + topSize;

        RollIn rollIn = new(this); 

        // add the slope as the first element in the line
        Line.Instance.AddLineElement(rollIn);
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
