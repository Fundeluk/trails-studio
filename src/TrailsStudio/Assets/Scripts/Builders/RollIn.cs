using Assets.Scripts;
using Assets.Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Assets.Scripts.Builders;
using Assets.Scripts.UI;



//[ExecuteInEditMode]
public class RollIn : MonoBehaviour, ILineElement
{
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
    private float slopeLength;
    private Vector3 endPoint;

    private GameObject[] legsInstances = new GameObject[4];
    private GameObject topInstance = null;
    private GameObject slopeInstance = null;

    private GameObject cameraTarget;

    private bool hasTooltipOn = false;

    private void Init()
    {
        CreateRollIn();

        cameraTarget = new GameObject("Camera Target");
        cameraTarget.transform.SetParent(GetTransform());
        RecalculateCameraTargetPosition();

        GetObstacleHeightmapCoordinates().MarkAs(CoordinateState.Occupied);
    }

    void Awake()
    {
#if !DEBUG
        height = MainMenuController.height;
        angle = MainMenuController.angle;
#endif

        Init();
        Debug.Log($"Exit speed: {GetExitSpeed()} m/s");
    }

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
            legsInstances[i].tag = Line.LINE_ELEMENT_TAG;

            legsInstances[i].transform.localScale = legScale;
        }
    }

    private void CreateTop()
    {
        var topPos = transform.position;
        topPos.y = height + flatThickness / 2;
        var topScale = new Vector3(topSize, flatThickness, topSize);

        topInstance = Instantiate(top, topPos, Quaternion.identity, transform);
        topInstance.tag = Line.LINE_ELEMENT_TAG;

        topInstance.transform.localScale = topScale;
    }

    private void CreateSlope()
    {
        float legToEndDist = Mathf.Tan((90 - angle) * Mathf.Deg2Rad) * height;

        // calculate Length of slope so that it reaches from top to floor
        slopeLength = Mathf.Sqrt(Mathf.Pow(height + flatThickness, 2) + Mathf.Pow(legToEndDist, 2));
       
        var slopePos = transform.position + transform.forward * ((topSize + legToEndDist)/2) + transform.up * height/2;
        var slopeRot = new Vector3(angle, 0, 0);
        var slopeScale = new Vector3(topSize, flatThickness, slopeLength);

        slopeInstance = Instantiate(slope, slopePos, Quaternion.identity, transform);
        slopeInstance.tag = Line.LINE_ELEMENT_TAG;

        Transform slopeTransform = slopeInstance.transform;

        slopeTransform.localScale = slopeScale;
        slopeTransform.eulerAngles = slopeRot;

        endPoint = transform.position + transform.forward * (topSize/2 + legToEndDist);

        length = legToEndDist + topSize;

        Line.Instance.AddLineElement(this);
    }

    public HeightmapCoordinates GetObstacleHeightmapCoordinates() => new(GetStartPoint(), GetEndPoint(), GetBottomWidth());

    private void RecalculateCameraTargetPosition()
    {
        cameraTarget.transform.position = GetEndPoint() - 0.5f * GetLength() * GetRideDirection() + 0.5f * GetHeight() * GetTransform().up;
    }

    public Vector3 GetEndPoint() => endPoint;

    public Vector3 GetStartPoint() => GetEndPoint() - GetLength() * GetRideDirection();

    public int GetIndex() => 0;
    public float GetHeight() => height;
    public float GetLength() => length;

    public float GetWidth() => topSize;

    public float GetPreviousElementBottomWidth() => GetBottomWidth();

    public float GetBottomWidth() => GetWidth() + 0.5f;

    public Vector3 GetRideDirection() => transform.forward;

    public Transform GetTransform() => transform;

    public GameObject GetCameraTarget() => cameraTarget;

    public void SetSlopeChange(SlopeChange slope)
    {
        throw new System.InvalidOperationException("Cannot set slope on rollin.");
    }

    public SlopeChange GetSlopeChange() => null;



    public HeightmapCoordinates GetUnderlyingSlopeHeightmapCoordinates() => null;

    public List<(string name, string value)> GetLineElementInfo()
    {
        return new List<(string name, string value)>
            {
                ("Type", "RollIn"),
                ("Height", $"{GetHeight(),10:0.00}m"),
                ("Angle", $"{angle,10:0}°"),
                ("Width", $"{GetWidth(),10:0.00}m"),
            };
    }

    public void Outline()
    {
        foreach (var child in GetComponentsInChildren<MeshRenderer>())
        {
            child.renderingLayerMask = Line.outlinedElementRenderLayerMask;
        }
    }

    public void RemoveOutline()
    {
        if (hasTooltipOn)
        {
            return;
        }

        if (this.gameObject == null || this == null)
        {
            return;
        }

        foreach (var child in GetComponentsInChildren<MeshRenderer>())
        {
            child.renderingLayerMask = RenderingLayerMask.defaultRenderingLayerMask;
        }
    }

    public void OnTooltipShow()
    {
        Outline();
        hasTooltipOn = true;
        CameraManager.Instance.DetailedView(GetCameraTarget());
    }

    public void OnTooltipClosed()
    {
        hasTooltipOn = false;
        RemoveOutline();
        CameraManager.Instance.SplineCamView();
    }

    public float GetExitSpeed()
    {
        return PhysicsManager.CalculateExitSpeed(0, slopeLength, angle * Mathf.Deg2Rad);
    }

    public void DestroyUnderlyingGameObject()
    {
        throw new System.InvalidOperationException("Cannot destroy rollin.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetStartPoint(), 0.5f);
        Gizmos.DrawSphere(GetEndPoint(), 0.5f);
    }

}
