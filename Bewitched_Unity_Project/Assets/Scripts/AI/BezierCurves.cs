using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class for creating bezier curves for smoother motion
/// </summary>
public class BezierCurves
{
    [Tooltip("The points in the array")]
    public Vector3[] points;

    /// <summary>
    /// Default constructor
    /// </summary>
    public BezierCurves()
    {
        points = new Vector3[4];
    }

    /// <summary>
    /// Constructor with array
    /// </summary>
    /// <param name="corners"> Corners of path </param>
    public BezierCurves(Vector3[] corners)
    {
        points = corners;
    }

    /// <summary>
    /// Gets the start position if it exists
    /// </summary>
    public Vector3 StartPosition
    {
        get{ return points[0]; }
    }

    /// <summary>
    /// Gets the end position if it exists
    /// </summary>
    public Vector3 EndPosition
    {
        get { return points[points.Length - 1]; }
    }

    /// <summary>
    /// Gets a segment based on a series of points
    /// </summary>
    /// <param name="time"> Time value of bezier curve </param>
    /// <returns> Segment vector position </returns>
    public Vector3 GetSegment(float time)
    {
        time = Mathf.Clamp01(time);
        float mTime = 1 - time;
        return (mTime * mTime * mTime * points[0])
            + (3 * mTime * mTime * time * points[1])
            + (3*mTime*time*time*points[2])
            + (time*time*time*points[3]);
    }

    public Vector3[] GetSegments(int numSegments)
    {
        Vector3[] segments = new Vector3[numSegments];

        float time;

        for (int i = 0; i < numSegments; i++)
        {
            time = (float)i / numSegments;
            segments[i] = GetSegment(time);
        }

        return segments;
    }
}
