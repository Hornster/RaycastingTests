using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using Color = UnityEngine.Color;

public class CastRays
{
    private Color horizontalRaysColor = Color.blue;
    private Color verticalRaysColor = Color.cyan;
    private Color cornersColor = Color.magenta;
    private const int rectangleCornersCount = 4;
    private static CastRays instance = new CastRays();
    /// <summary>
    /// Stores origins for horizontal rays. From top to bottom.
    /// </summary>
    private List<Vector2> horizontalRayOrigins;
    /// <summary>
    /// Stores origins for vertical rays. From left to right.
    /// </summary>
    private List<Vector2> verticalRayOrigins;
    /// <summary>
    /// Stores origins for corner rays. Clockwise orientation, beginning from upper left corner.
    /// </summary>
    private List<Vector2> cornersRayOrigins;

    public static CastRays GetInstance()
    {
        return instance;
    }

    private CastRays()
    {
        cornersRayOrigins = new List<Vector2>(rectangleCornersCount);
        for (int i = 0; i < rectangleCornersCount; i++)
        {
            cornersRayOrigins.Add(new Vector2());
        }
    }
    /// <summary>
    /// Used to make sure the double calculations won't make the ray end upp ricocheting around inside the object.
    /// </summary>
    private const float safetyPadding = 0.000015f;
    /// <summary>
    /// Casts parallel rays from a provided rectangle (all edges + provided horizontal and vertical rays amount), defining a path along which the object may travel, bouncing
    /// off of surfaces met in the way.
    /// </summary>
    /// <param name="objectDimensions">Dimensions of the object. Starting point and size (2D) (make sure the position is in the UPPER LEFT CORNER of the object!)</param>
    /// <param name="distance">Max distance of each ray (travel distance).</param>
    /// <param name="additionalHorizontalRays">How many additional rays there will be coming out from vertical vertexes</param>
    /// <param name="additionalVerticalRays">How many additional rays there will be coming out from horizontal vertexes</param>
    /// <returns>List of movement segments and a vector of movement between starting and ending position.</returns>
    public (List<LaserPartData>, Vector2) ProjectRectangle(RectangleF objectDimensions, Vector2 direction, double distance,
        int additionalHorizontalRays = 3, int additionalVerticalRays = 2)
    {
        var movementList = new List<LaserPartData>();
        //Calculate the offsets between the rays. +1 since all of the rays will be on the edge, not on the corners and in regular offsets (n rays divide edge into n+1 parts).
        var edgeRayOffsets = new Vector2(objectDimensions.Width / (additionalVerticalRays + 1), objectDimensions.Height / (additionalHorizontalRays + 1));
        //Setup horizontal rays.
        PrepareRaysOrigins(objectDimensions, direction, edgeRayOffsets, additionalHorizontalRays, additionalVerticalRays);

        Vector2 objectPosition = new Vector2(objectDimensions.X, objectDimensions.Y);
        RaycastHit2D closestResultCast;
        Vector2 closestResultOrigin;

        do
        {
            var newLaserPart = CreateLaserPart(objectPosition, direction);
            movementList.Add(newLaserPart);
            (closestResultCast, closestResultOrigin) = CastMultipleRays(direction, distance);
            newLaserPart.Length = closestResultCast.distance;
            Debug.DrawRay(closestResultCast.point, closestResultCast.normal, Color.yellow, 3.0f);

            if (closestResultCast.collider == null)
            {
                newLaserPart.Length = distance;//The rest of the distance points into nothing.
                break;          //If the ray does not hit anything in particular - simply leave.
            }
            distance -= closestResultCast.distance;

            if (movementList.Count > 500)
            {
                break;  //Poor guy got in loop. Free him
            }

            Vector2 placeholder;
            //Stores the vector by which the whole shadow, that is the object and all raycasts, shall be moved.
            var movementVector = direction * (float) newLaserPart.Length + closestResultCast.normal * safetyPadding;
            objectPosition += movementVector;
            (placeholder, direction) = PrepareNewSegmentData(closestResultCast, closestResultOrigin);//Set the hit point as new beginning, rotate the normal of object that's been hit, making it the direction vector.

            //objectPosition += direction * safetyPadding;
            ReassignAdditionalRays(movementVector, objectDimensions, direction, objectPosition);
        } while (distance > 0.0f);
        
        var directMovementVector = movementList[movementList.Count-1].GetEndPoint() - movementList[0].StartPoint;

        return (movementList, directMovementVector);
    }

    public List<LaserPartData> GetLaserPath(Vector2 startingPoint, Vector2 direction, double distance)
    {
        var raySegments = new List<LaserPartData>();
        do
        {
            var newLaserPart = CreateLaserPart(startingPoint, direction); //add the new segment.
            raySegments.Add(newLaserPart);

            var resultCast = PerformCastRay(startingPoint, direction, distance);//Perform raycast
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
            
            (startingPoint, direction) = PrepareNewSegmentData(resultCast, startingPoint);//Set the hit point as new beginning, rotate the normal of object that's been hit, making it the direction vector.
        } while (distance > 0.0f);

        return raySegments;
    }
    private RaycastHit2D PerformCastRay(Vector2 startingPoint, Vector2 direction,double distance)
    {
        var raycastHit = Physics2D.Raycast(startingPoint, direction, (float)distance);
        return raycastHit;
    }

    private LaserPartData CreateLaserPart(Vector2 startingPoint, Vector2 direction)
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
    private (Vector2, Vector2) PrepareNewSegmentData(RaycastHit2D raycastHit, Vector2 startingPoint)
    {
        double angle = Vector2.SignedAngle(startingPoint - raycastHit.point, raycastHit.normal);
        var newDirectionVector = Quaternion.Euler(0.0f, 0.0f, (float)angle) * raycastHit.normal;   //Rotate the normal by raycast vector fall angle
        var newStartingPoint = raycastHit.point + raycastHit.normal * safetyPadding;        //Put the collision point slightly more away from the surface so the ray can bounce freely (double calculations artifacts).

        return (newStartingPoint, newDirectionVector);
    }
    
    /// <summary>
    /// Resets all rays origins so that these cover the object that will be projected.
    /// </summary>
    /// <param name="objectDimensions">Dimensions of the object.</param>
    /// <param name="direction">Starting direction of the rays.</param>
    /// <param name="additionalHorizontalRays">Amount of additional rays (between the corners) on vertical edges.</param>
    /// <param name="additionalVerticalRays">Amount of additional rays between the corners on horizontal edges.</param>
    private void PrepareRaysOrigins(RectangleF objectDimensions, Vector2 direction, Vector2 edgeRayOffsets, int additionalHorizontalRays, int additionalVerticalRays)
    {
        horizontalRayOrigins = new List<Vector2>(additionalHorizontalRays);
        verticalRayOrigins = new List<Vector2>(additionalVerticalRays);
        var halfSize = new Vector2(objectDimensions.Width / 2, objectDimensions.Height / 2);

        cornersRayOrigins[0] = new Vector2(objectDimensions.Left, objectDimensions.Top) - halfSize;
        cornersRayOrigins[1] = new Vector2(objectDimensions.Right, objectDimensions.Top) - halfSize;
        cornersRayOrigins[2] = new Vector2(objectDimensions.Right, objectDimensions.Bottom) - halfSize;
        cornersRayOrigins[3] = new Vector2(objectDimensions.Left, objectDimensions.Bottom) - halfSize;
        
        //Setup horizontal rays.
        if (direction.x > 0.0f)
        {   //Rays are going to the right.
            for (int i = 0; i < additionalHorizontalRays; i++)
            {
                horizontalRayOrigins.Add(new Vector2(objectDimensions.Right, objectDimensions.Top + (i + 1) * edgeRayOffsets.y) - halfSize);
            }
        }
        else if (direction.x <= 0.0f)
        {//Rays are going to the left.
            for (int i = 0; i < additionalHorizontalRays; i++)
            {
                horizontalRayOrigins.Add(new Vector2(objectDimensions.Left, objectDimensions.Top + (i + 1) * edgeRayOffsets.y) - halfSize);
            }

        }
        //Setup vertical rays
        if (direction.y > 0.0f)
        {   //rays are going upwards.
            for (int i = 0; i < additionalVerticalRays; i++)
            {
                verticalRayOrigins.Add(new Vector2(objectDimensions.Left + (i + 1) * edgeRayOffsets.x, objectDimensions.Bottom) - halfSize);//Bottom for top since RectangleF used here states the starting point in upper left corner - bottom is bigger than top.
            }
        }
        else if (direction.y <= 0.0f)
        {   //Rays are going downwards
            for (int i = 0; i < additionalVerticalRays; i++)
            {
                verticalRayOrigins.Add(new Vector2(objectDimensions.Left + (i + 1) * edgeRayOffsets.x, objectDimensions.Top) - halfSize);//Top for bottom - same result as above, from different point of view.
            }
        }
    }
    /// <summary>
    /// Reassigns ray origins around the object accordingly to provided direction vector. Note that the lists that store the origins must
    /// be already filled.
    /// </summary>
    /// <param name="movementVector">Current movementVector of the object image cast by rays.</param>
    /// <param name="objectDimensions">Dimensions of the object.</param>
    /// <param name="direction">Direction vector.</param>
    /// <param name="currentObjectProjectionPosition">Most recently calculated position of object.</param>
    private void ReassignAdditionalRays(Vector2 movementVector, RectangleF objectDimensions, Vector2 direction, Vector2 currentObjectProjectionPosition)
    {
        //Setup corner rays
        for (int i = 0; i < cornersRayOrigins.Count; i++)
        {
            cornersRayOrigins[i] += movementVector;
        }
        
        //Setup horizontal rays.
        if (direction.x > 0.0f)
        {
            //Rays are going to the right.
            for (int i = 0; i < horizontalRayOrigins.Count; i++)
            {
                var currentOrigin = horizontalRayOrigins[i];
                currentOrigin += movementVector;
                //If the direction vector points to the right and the ray origins are on the left side of the object - move them to the right (add object width to their x coord).
                if (currentOrigin.x < currentObjectProjectionPosition.x)
                {
                    currentOrigin = new Vector2(currentOrigin.x + objectDimensions.Width, currentOrigin.y);
                }
                horizontalRayOrigins[i] = currentOrigin;
            }
        }
        else if (direction.x <= 0.0f)
        {
            //Rays are going to the left.
            for (int i = 0; i < horizontalRayOrigins.Count; i++)
            {
                var currentOrigin = horizontalRayOrigins[i];
                currentOrigin += movementVector;
                //If the direction vector points to the left and the ray origins are on the right side of the object - move them to the left (subtract object width to their x coord).
                if (currentOrigin.x > currentObjectProjectionPosition.x)
                {
                    currentOrigin = new Vector2(currentOrigin.x - objectDimensions.Width, currentOrigin.y);
                }

                horizontalRayOrigins[i] = currentOrigin;
            }
        }
        
        //Setup vertical rays
        if (direction.y > 0.0f)
        {
            //rays are going upwards.
            for (int i = 0; i < verticalRayOrigins.Count; i++)
            {
                var currentOrigin = verticalRayOrigins[i];
                currentOrigin += movementVector;
                //If the direction vector points to the top and the ray origins are on the bottom side of the object - move them to the bottom (add object height to their y coord).
                if (currentOrigin.y < currentObjectProjectionPosition.y)
                {
                    currentOrigin = new Vector2(currentOrigin.x, currentOrigin.y + objectDimensions.Height);
                }

                verticalRayOrigins[i] = currentOrigin;
            }
        }
        else if (direction.y <= 0.0f)
        {
            
            //Rays are going downwards
            for (int i = 0; i < verticalRayOrigins.Count; i++)
            {
                var currentOrigin = verticalRayOrigins[i];
                currentOrigin += movementVector;
                //If the direction vector points to the bottom and the ray origins are on the top side of the object - move them to the top (subtract object height to their y coord).
                if (currentOrigin.y > currentObjectProjectionPosition.y)
                {
                    currentOrigin = new Vector2(currentOrigin.x, currentOrigin.y - objectDimensions.Height);
                }

                verticalRayOrigins[i] = currentOrigin;
            }
        }
    }
    /// <summary>
    /// Casts all rays - horizontal, vertical and corner. Returns shortest distance of them all and origin of the ray that the distance concerns.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private (RaycastHit2D, Vector2) CastMultipleRays(Vector2 direction, double distance)
    {
        //Used to draw debug lines when nothing has been hit.
        var noHitDrawingVector = direction * (float) distance;
        RaycastHit2D closestHit = new RaycastHit2D(){distance = float.MaxValue};
        Vector2 closestHitRayOrigin = new Vector2();
        //Debug
        RaycastHit2D lastHit;
        //Debug

        int colorCount = 0;
        //Cast corners
        foreach (var rayOrigin in cornersRayOrigins)
        {
            (closestHit, lastHit, closestHitRayOrigin) = CastAndTestSingleRay(ref closestHit, rayOrigin, ref closestHitRayOrigin, ref direction, (float)distance);
            if (lastHit.collider != null)
            {
                
                //Draw the lines only if you actually managed to hit something. Otherwise these will be incorrect.
                //Debug.DrawLine(rayOrigin, lastHit.point, new Color(colorCount * 0.2f, colorCount * 0.2f, 0.0f, 1.0f), 3.0f);
            }
            else
            {//If no collider has been hit by any of the rays, draw the lines up to the end.
                //Debug.DrawLine(rayOrigin, noHitDrawingVector + rayOrigin, new Color(colorCount * 0.2f, colorCount * 0.2f, 0.0f, 1.0f), 3.0f);
            }
            
            colorCount++;
        }

        ////Then horizontal rays
        foreach (var rayOrigin in horizontalRayOrigins)
        {
            (closestHit, lastHit, closestHitRayOrigin) = CastAndTestSingleRay(ref closestHit, rayOrigin, ref closestHitRayOrigin, ref direction, (float)distance);
            //if (lastHit.collider != null || closestHit.collider != null)
            //{
            //    Debug.DrawLine(rayOrigin, lastHit.point, horizontalRaysColor, 3.0f);
            //}
            //else
            //{
            //    Debug.DrawLine(rayOrigin, noHitDrawingVector + rayOrigin, horizontalRaysColor, 3.0f);
            //}
            
        }
        //Then vertical rays
        foreach (var rayOrigin in verticalRayOrigins)
        {
            (closestHit, lastHit, closestHitRayOrigin) = CastAndTestSingleRay(ref closestHit, rayOrigin, ref closestHitRayOrigin, ref direction, (float)distance);

            //if (lastHit.collider != null)
            //{
            //    Debug.DrawLine(rayOrigin, lastHit.point, verticalRaysColor, 3.0f);
            //}
            //else
            //{
            //    Debug.DrawLine(rayOrigin, noHitDrawingVector + rayOrigin, verticalRaysColor, 3.0f);
            //}
        }

        return (closestHit, closestHitRayOrigin);
    }
    /// <summary>
    /// Casts a single ray, then tests if its collision point is closer than already existing one.
    /// </summary>
    /// <param name="currentClosestHit">Existing collision result - distances of new and this results will be compared and closer one will be returned.</param>
    /// <param name="rayOrigin">Origin of cast ray.</param>
    /// <param name="direction">Direction of cast ray.</param>
    /// <param name="distance">Length of the cast ray.</param>
    /// <returns>First arg - currently closest hit. Second arg - a cast made during this function call.</returns>
    private (RaycastHit2D, RaycastHit2D, Vector2) CastAndTestSingleRay(ref RaycastHit2D currentClosestHit, Vector2 rayOrigin, ref Vector2 currentClosestHitRayOrigin,
        ref Vector2 direction, float distance)
    {
        var raycastHit = Physics2D.Raycast(rayOrigin, direction, (float)distance);
        //Check if result has shorter distance than current result (and if it hit any wall at all)
        if (raycastHit.distance < currentClosestHit.distance && raycastHit.collider != null)
        {
            return (raycastHit, raycastHit, rayOrigin);
        }

        return (currentClosestHit, raycastHit, currentClosestHitRayOrigin);
    }

    private Vector2 CalculateNewRectanglePosition(Vector2 currentPosition, Vector2 direction, float distance)
    {
        return currentPosition + direction * distance;
    }
}

//todo
//You don't have to return all lasers. Keep them in here, contained and return only  object movement path (translations) with/or the movement vector, from start to end.

    //Rays after first iteration seem to return to point 0,0 (or start from this point)