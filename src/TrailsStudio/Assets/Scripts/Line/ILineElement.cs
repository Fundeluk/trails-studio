using Assets.Scripts.Builders;
using Assets.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;

public interface ILineElement
{
    public int GetIndex();
    public Transform GetTransform();

    public Vector3 GetStartPoint();

    public GameObject GetCameraTarget();

    public HeightmapCoordinates GetObstacleHeightmapCoordinates();

    public float GetHeight();

    public float GetLength();

    public Vector3 GetEndPoint();

    public Vector3 GetRideDirection();

    public float GetWidth();

    public HeightmapCoordinates GetUnderlyingSlopeHeightmapCoordinates();

    public void SetSlopeChange(SlopeChange slope);

    public SlopeChange GetSlopeChange();

    public float GetPreviousElementBottomWidth();

    /// <summary>
    /// Returns the Width of the line element at its bottom level.
    /// </summary>
    public float GetBottomWidth();
    public List<(string name, string value)> GetLineElementInfo();

    public void AddOutline();

    public void RemoveOutline();

    public void OnTooltipShow();

    public void OnTooltipClosed();

    public void DestroyUnderlyingGameObject();

    /// <returns>The speed at which a rider exits the line element in meters per second.</returns>
    public float GetExitSpeed();
}

