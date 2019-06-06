using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Assets.Scripts;
using UnityEngine;
using Color = UnityEngine.Color;

public class LaserPointerController : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed;
    [SerializeField]
    private float rayLength;
    [SerializeField]
    private float colliderWidth;
    [SerializeField]
    private float colliderHeight;

    [SerializeField] private Transform markerTransform;

    private RectangleF collider;

    private CastRays castRays = CastRays.GetInstance();
    
    // Start is called before the first frame update
    void Start()
    {
        collider = new RectangleF(gameObject.transform.position.x, gameObject.transform.position.y, colliderWidth, colliderHeight);
    }

    // Update is called once per frame
    void Update()
    {
        float timeElapsed = Time.deltaTime;
        float rotation = GetRotationInput();

        gameObject.transform.Rotate(new Vector3(0.0f, 0.0f, 1.0f), rotation * timeElapsed);

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            Vector2 newMarkerPosition;
            List<LaserPartData> laserParts;
            var segments = PerformRaycasts();
            DrawDebugLines(segments, Color.red);
            (laserParts, newMarkerPosition) = PerformRectangleCasts();
            DrawDebugLines(laserParts, Color.white);

            markerTransform.position = new Vector3(newMarkerPosition.x, newMarkerPosition.y);
        }

        DrawAimingLaser();

    }

    private void DrawAimingLaser()
    {
        var startDirection = gameObject.transform.rotation.eulerAngles;
        startDirection.x = (float)Math.Cos(Mathf.Deg2Rad * startDirection.z) * rayLength;
        startDirection.y = (float)Math.Sin(Mathf.Deg2Rad * startDirection.z) * rayLength;
        //for now, only from this object.
        Debug.DrawRay(gameObject.transform.position, startDirection, Color.green, 0.0f, false);
    }
    private void DrawDebugLines(List<LaserPartData> laserSegments, Color color)
    {
        laserSegments.ForEach(seg => Debug.DrawRay(seg.StartPoint, seg.Direction*(float)seg.Length, color, 3.0f, false));
        /*
        var startDirection = gameObject.transform.rotation.eulerAngles;
        startDirection.x = (float)Math.Cos(Mathf.Deg2Rad * startDirection.z)* rayLength;
        startDirection.y = (float)Math.Sin(Mathf.Deg2Rad * startDirection.z)* rayLength;
        //for now, only from this object.
        Debug.DrawRay(gameObject.transform.position, startDirection, Color.red, 0.0f, false);*/
    }
    private float GetRotationInput()
    {
        var rotation = 0.0f;
        if (Input.GetButton("a"))
        {
            rotation = rotationSpeed;
        }
        else if (Input.GetButton("d"))
        {
            rotation = -rotationSpeed;
        }
        

        return rotation;
    }

    private Vector2 GetPointingVector()
    {
        var direction = new Vector2();
        var angle = Mathf.Deg2Rad * gameObject.transform.localRotation.eulerAngles.z;
        direction.x = (float)Math.Cos(angle);
        direction.y = (float) Math.Sin(angle);

        return direction;
    }
    private List<LaserPartData> PerformRaycasts()
    {
        var direction = GetPointingVector();
        return castRays.GetLaserPath(gameObject.transform.position, direction, rayLength);
    }

    private (List<LaserPartData>, Vector2) PerformRectangleCasts()
    {
        return castRays.ProjectRectangle(collider, GetPointingVector(), rayLength);
    }
}

//sin, cos w radianach