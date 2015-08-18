// Author:          Evan Olds
// Creation Date:   September 21, 2009 (adapted from EOStadium.cs, created February 23, 2008)
// Modification:    April 16, 2001 - changed from class to struct

using System;
using System.Collections.Generic;

namespace EOFC
{
    internal struct Stadium
    {
        private Vector2D m_p1;
        private Vector2D m_p2;
        private float m_r;

        public Stadium(Vector2D pt1, Vector2D pt2, float radius)
        {
            if (radius <= 0.0f)
            {
                throw new ArgumentException(
                    "Radius of a stadium must be greater than zero");
            }
            
            m_p1 = new Vector2D(pt1);
            m_p2 = new Vector2D(pt2);
            m_r = radius;
        }

        /// <summary>
        /// Returns a line segment axis for the stadium. Modifying the line segment will not change 
        /// the stadium.
        /// </summary>
        public LineSegment2D Axis
        {
            get
            {
                return new LineSegment2D(m_p1, m_p2);
            }
        }

        public bool Contains(Vector2D point)
        {
            return ((new LineSegment2D(m_p1, m_p2)).DistanceU(point) <= m_r);
        }

        /// <summary>
        /// Computes the x-values of intersection points with a horizontal line. If the line does 
        /// not intersect the stadium, then both output x-values are set to float.NaN.
        /// </summary>
        public void HLineIntersection(float yValue, out float minx, out float maxx)
        {
            List<float> xList = new List<float>(6);
            
            float x1, x2;
            (new Circle2D(m_p1, m_r)).HLineIntersection(yValue, out x1, out x2);
            if (!float.IsNaN(x1) && !float.IsNaN(x2))
            {
                // Add intersections to the list
                xList.Add(x1);
                xList.Add(x2);
            }
                
            // Now check the second circle
            (new Circle2D(m_p2, m_r)).HLineIntersection(yValue, out x1, out x2);
            if (!float.IsNaN(x1) && !float.IsNaN(x2))
            {
                // Add intersections to the list
                xList.Add(x1);
                xList.Add(x2);
            }
            
            // Now check the first line segment edge
            Vector2D perp = Vector2D.Normalize(m_p2 - m_p1).GetPerpendicular();
            Vector2D pt = new Vector2D();
            if ((new LineSegment2D(m_p1 + (perp * m_r), m_p2 + (perp * m_r))).GetPointWithYValue(yValue, ref pt))
            {
                xList.Add(pt.X);
            }

            // Build the second edge and see if we intersect that too
            if ((new LineSegment2D(m_p1 - (perp * m_r), m_p2 - (perp * m_r))).GetPointWithYValue(yValue, ref pt))
            {
                xList.Add(pt.X);
            }

            // If we don't have at least 2 values in the list then we have no intersection
            if (xList.Count < 2)
            {
                minx = float.NaN;
                maxx = float.NaN;
                return;
            }

            // Sort the list of intersections so that the first item in the list will be the minimum and the last 
            // item will be the maximum.
            xList.Sort();

            // Set intersection x-values
            minx = xList[0];
            maxx = xList[xList.Count-1];
        }

        public float MaxX
        {
            get
            {
                return Math.Max(m_p1.X, m_p2.X) + m_r;
            }
        }

        public float MaxY
        {
            get
            {
                return Math.Max(m_p1.Y, m_p2.Y) + m_r;
            }
        }

        public float MinX
        {
            get
            {
                return Math.Min(m_p1.X, m_p2.X) - m_r;
            }
        }

        public float MinY
        {
            get
            {
                return Math.Min(m_p1.Y, m_p2.Y) - m_r;
            }
        }

        /// <summary>
        /// Gets the radius of the stadium.
        /// </summary>
        public float Radius
        {
            get
            {
                return m_r;
            }
        }

        /// <summary>
        /// Computes the y-values of intersection points with a vertical line. If the line does 
        /// not intersect the stadium, then both output x-values are set to float.NaN.
        /// </summary>
        public void VLineIntersection(float xValue, out float miny, out float maxy)
        {
            List<float> yList = new List<float>(6);

            float y1, y2;
            // Check the first circle for intersections
            (new Circle2D(m_p1, m_r)).VLineIntersection(xValue, out y1, out y2);
            if (!float.IsNaN(y1) && !float.IsNaN(y2))
            {
                // Add intersections to the list
                yList.Add(y1);
                yList.Add(y2);
            }

            // Now check the second circle
            (new Circle2D(m_p2, m_r)).VLineIntersection(xValue, out y1, out y2);
            if (!float.IsNaN(y1) && !float.IsNaN(y2))
            {
                // Add intersections to the list
                yList.Add(y1);
                yList.Add(y2);
            }

            // Now check the first line segment edge
            Vector2D perp = Vector2D.Normalize(m_p2 - m_p1).GetPerpendicular();
            Vector2D pt = new Vector2D();
            if ((new LineSegment2D(m_p1 + (perp * m_r), m_p2 + (perp * m_r))).GetPointWithXValue(xValue, ref pt))
            {
                yList.Add(pt.Y);
            }

            // Build the second edge and see if we intersect that too
            if ((new LineSegment2D(m_p1 - (perp * m_r), m_p2 - (perp * m_r))).GetPointWithXValue(xValue, ref pt))
            {
                yList.Add(pt.Y);
            }

            // If we don't have at least 2 values in the list then we have no intersection
            if (yList.Count < 2)
            {
                miny = float.NaN;
                maxy = float.NaN;
                return;
            }

            // Sort the list of intersections so that the first item in the list will be the minimum and the last 
            // item will be the maximum.
            yList.Sort();

            // Set intersection x-values
            miny = yList[0];
            maxy = yList[yList.Count - 1];
        }
    }
}