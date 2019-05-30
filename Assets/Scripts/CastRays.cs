using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

public static class CastRays
{
    /// <summary>
    /// Used to make sure the double calculations won't make the ray end upp ricocheting around inside the object.
    /// </summary>
    private const float safetyPadding = 0.00015f;
    public static List<LaserPartData> GetLaserPath(Vector2 startingPoint, Vector2 direction, double distance)
    {
        var raySegments = new List<LaserPartData>();
        do
        {
            var newLaserPart = CreateLaserPart(startingPoint, direction); //add the new segment.
            raySegments.Add(newLaserPart);

            var resultCast = PerformCastRays(startingPoint, direction, distance);//Perform raycast
            newLaserPart.Length = resultCast.distance;                          //ASsign length of newly created segment.
            distance -= resultCast.distance;                                    //Decrease the total distance by length of last segment.
            Debug.DrawRay(resultCast.point, resultCast.normal, Color.yellow, 3.0f);
            if (resultCast.collider == null)
            {
                newLaserPart.Length = distance;//The rest of the distance points into nothing.
                break;          //If the ray does not hit anything in particular - simply leave.
            }

            if (raySegments.Count > 500)
            {
                break;  //Poor guy got in loop. Free him
            }
            
            (startingPoint, direction) = PrepareNewSegmentData(resultCast, startingPoint, direction);//Set the hit point as new beginning, rotate the normal of object that's been hit, making it the direction vector.
        } while (distance > 0.0f);

        return raySegments;
    }
    private static RaycastHit2D PerformCastRays(Vector2 startingPoint, Vector2 direction,double distance)
    {
        var raycastHit = Physics2D.Raycast(startingPoint, direction, (float)distance);
        double angle = Vector2.SignedAngle(startingPoint - raycastHit.point, raycastHit.normal);
        Debug.Log("Distance:" + raycastHit.distance);
        Debug.Log("Normal:" + raycastHit.normal);
        Debug.Log("Angle:" + angle);
        return raycastHit;
    }

    private static LaserPartData CreateLaserPart(Vector2 startingPoint, Vector2 direction)
    {
        var laserPartData = new LaserPartData
        {
            StartPoint = startingPoint,
            Direction = direction
        };

        return laserPartData;
    }
    /// <summary>
    /// Returns new segment data - its beginning, direction and length.
    /// </summary>
    /// <param name="raycastHit"></param>
    /// <param name="startingPoint"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static (Vector2, Vector2) PrepareNewSegmentData(RaycastHit2D raycastHit, Vector2 startingPoint, Vector2 direction)
    {
        double angle = Vector2.SignedAngle(startingPoint - raycastHit.point, raycastHit.normal);
        var newDirectionVector = Quaternion.Euler(0.0f, 0.0f, (float)angle) * raycastHit.normal;   //Rotate the normal by raycast vector fall angle
        var newStartingPoint = raycastHit.point + raycastHit.normal * safetyPadding;        //Put the collision point slightly more away from the surface so the ray can bounce freely (double calculations artifacts).

        return (newStartingPoint, newDirectionVector);
    }
}
