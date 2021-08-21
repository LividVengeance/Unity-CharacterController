using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    // Ladder segment
    [SerializeField] private Vector3 ladderSegmentBottom;
    [SerializeField] private float ladderSegmentLength;

    // Points to move to when reaching one of the extremities and moving off of the ladder
    [SerializeField] private Transform bottomReleasePoint;
    [SerializeField] private Transform topReleasePoint;

    public Transform GetBottomReleasePoint => bottomReleasePoint;

    public Transform GetTopReleasePoint => topReleasePoint;
    
    /// Gets the position of the bottom point of the ladder segment
    public Vector3 BottomAnchorPoint => transform.position + transform.TransformVector(ladderSegmentBottom);

    /// Gets the position of the top point of the ladder segment
    public Vector3 TopAnchorPoint => transform.position + transform.TransformVector(ladderSegmentBottom) + (transform.up * ladderSegmentLength);
    
    /// Returns the closest point on the ladder from characters position
    public Vector3 ClosestPointOnLadderSegment(Vector3 fromPoint, out float onSegmentState)
    {
        Vector3 segment = TopAnchorPoint - BottomAnchorPoint;            
        Vector3 segmentPoint1ToPoint = fromPoint - BottomAnchorPoint;
        float pointProjectionLength = Vector3.Dot(segmentPoint1ToPoint, segment.normalized);

        // When higher than bottom point
        if (pointProjectionLength > 0)
        {
            // If we are not higher than top point
            if (pointProjectionLength <= segment.magnitude)
            {
                onSegmentState = 0;
                return BottomAnchorPoint + (segment.normalized * pointProjectionLength);
            }
            // If we are higher than top point
            else
            {
                onSegmentState = pointProjectionLength - segment.magnitude;
                return TopAnchorPoint;
            }
        }
        // When lower than bottom point
        else
        {
            onSegmentState = pointProjectionLength;
            return BottomAnchorPoint;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(BottomAnchorPoint, TopAnchorPoint);
    }
}
