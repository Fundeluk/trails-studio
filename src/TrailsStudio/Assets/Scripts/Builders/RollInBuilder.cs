using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollInBuilder : MonoBehaviour
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
    [SerializeField] 
    private float legDiameter; // meters

    [SerializeField]
    private int angle; // degrees of slope

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

            var legTransform = Instantiate(leg, legPos, Quaternion.identity, transform).transform;

            legTransform.localPosition = legPos;
            legTransform.localScale = legScale;
        }
    }

    private void CreateTop()
    {
        var topPos = new Vector3(0, height, 0);
        var topScale = new Vector3(topSize, flatThickness, topSize);

        var topTransform = Instantiate(top, topPos, Quaternion.identity, transform).transform;

        topTransform.localPosition = topPos;
        topTransform.localScale = topScale;
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

        var slopeTransform = Instantiate(slope, Vector3.one, Quaternion.identity, transform).transform;

        slopeTransform.localPosition = slopePos;
        slopeTransform.localScale = slopeScale;
        slopeTransform.eulerAngles = slopeRot;


        // TODO edit this so that endpoint is the end of the slope
        Vector3 endPoint = new Vector3(slopeTransform.position.x, 0, slopeTransform.position.z + slopeToLegDist/2);
        // add the slope as the first element in the line
        Line.Instance.AddToLine(slopeTransform.gameObject, endPoint);
    }


    // Start is called before the first frame update
    void Start()
    {
        height = MainMenuController.height;
        angle = MainMenuController.angle;

        CreateLegs();
        CreateTop();
        CreateSlope();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
