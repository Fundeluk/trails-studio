using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Assets.Scripts;

/// <summary>
/// This class rotates the object around a target at a constant speed.
/// </summary>
public class ConstantRotation : MonoBehaviour
{
    [SerializeField]
    float rotationSpeed;

    [SerializeField]
    GameObject target;

    private void OnEnable()
    {
        if (target == null)
        {
            target = Line.Instance.GetLastLineElement().GetCameraTarget();
        }
    }

    private void OnDisable()
    {
        target = null;
    }

    public void SetTarget(GameObject newTarget)
    {
        target = newTarget;
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(target.transform.position, Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
