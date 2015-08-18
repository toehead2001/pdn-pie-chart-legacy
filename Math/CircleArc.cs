// Author:          Evan Olds
// Creation Date:   September 3, 2012

using System;

namespace EOFC
{
    /// <summary>
    /// Represents an arc along the perimeter of a circle in two dimensions.
    /// </summary>
    internal struct CircleArc
    {
        private const double c_twoPi = Math.PI * 2.0;
        
        private Circle2D m_circle;

        private double m_startAngle;

        private double m_sweepAngle;

        public CircleArc(Circle2D circle, double startAngle, double sweepAngle)
        {
            // For negative sweep angles we need to adjust the start angle so that we can make the sweep 
            // angle positive and be representing the same arc.
            if (sweepAngle < 0.0)
            {
                startAngle += sweepAngle;
                sweepAngle = -sweepAngle;
            }
            
            // Get the start angle in the range [0, 2*pi)
            if (startAngle < 0.0)
            {
                startAngle = (startAngle % c_twoPi) + c_twoPi;
            }
            else
            {
                startAngle = startAngle % c_twoPi;
            }
            // Get the sweep angle in the range [0, 2*pi). Note that it's non-negative at this point.
            sweepAngle %= c_twoPi;

            m_circle = circle;
            m_startAngle = startAngle;
            m_sweepAngle = sweepAngle;
        }

        public Circle2D Circle
        {
            get
            {
                return m_circle;
            }
        }

        /// <summary>
        /// Returns the shortest unsigned distance from the specified point to this arc.
        /// </summary>
        public float DistanceU(EOFC.Vector2D point)
        {
            // Start by getting the angle of the point with respect to the circle center
            double angle = (point - m_circle.Center).GetCCWAngle();

            // If the angle is in the angle range for this arc then the shortest distance will 
            // be from the point to the edge of the circle.
            if (IsAngleInRange(angle, m_startAngle, m_sweepAngle))
            {
                return Math.Abs(m_circle.Radius - (point - m_circle.Center).Length);
            }

            // Otherwise the distance is the smaller of the two distances between the point and the 
            // arc endpoints.
            return Math.Min(
                (point - m_circle.GetPointOnPerimeter(m_startAngle)).Length,
                (point - m_circle.GetPointOnPerimeter(m_startAngle + m_sweepAngle)).Length);
        }

        /// <summary>
        /// Computes the intersection x-values of a horizontal line with this arc. Either of the output values 
        /// can potentially be set to float.NaN if an intersection does not occur. Note that minX can be NaN 
        /// while maxX is a valid real number. This happens in the case where the horizontal line intersects 
        /// the circle that the arc is a part of, but the minimum intersection point is not on the arc while 
        /// the maximum intersection point is. Likewise, minX can be a valid real number with maxX being 
        /// NaN when the minimum intersection point is on the arc and the maximum is not.
        /// </summary>
        public void HLineIntersection(float yValue, out float minX, out float maxX)
        {
            m_circle.HLineIntersection(yValue, out minX, out maxX);

            // Check the intersection points to see if they're within the angle range of this arc. If 
            // they are, leave them be, otherwise set them to NaN.
            if (!IsAngleInRange(((new Vector2D(minX, yValue)) - m_circle.Center).GetCCWAngle(),
                    m_startAngle, m_sweepAngle))
            {
                minX = float.NaN;
            }
            if (!IsAngleInRange(((new Vector2D(maxX, yValue)) - m_circle.Center).GetCCWAngle(),
                    m_startAngle, m_sweepAngle))
            {
                maxX = float.NaN;
            }
        }

        // All angles passed to this function must be in the range [0, 2*pi)
        private static bool IsAngleInRange(double angleToCheck, double startAngle, double sweepAngle)
        {
            // If we're sweeping 2*pi or more, then we're covering the entire circle, implying that 
            // the angle is in range
            if (sweepAngle >= Math.PI * 2.0)
            {
                return true;
            }

            double end = startAngle + sweepAngle;

            // Since this method assumes that the starting angle is in the range [0, 2*pi), then there 
            // are two cases with respect to the ending angle (start + sweep). It can be greater than 2*pi 
            // or less than or equal to.
            if (end > 2.0 * Math.PI)
            {
                return (angleToCheck >= startAngle || angleToCheck <= (end - (Math.PI * 2d)));
            }
            return (angleToCheck >= startAngle && angleToCheck <= end);
        }

        public bool IsDegenerate
        {
            get
            {
                return m_circle.IsDegenerate || (0.0 == m_sweepAngle);
            }
        }

        public float MaxX
        {
            get
            {
                // The point on the circle with the minimum possible x-value would be at angle 
                // 0. If this angle is in the range of our arc then we return the maximum x-
                // value of the circle. Otherwise we take the maximum x-value of the two 
                // endpoints.
                if (IsAngleInRange(0.0, m_startAngle, m_sweepAngle))
                {
                    return m_circle.MaxX;
                }

                return Math.Max(
                    m_circle.GetPointOnPerimeter(m_startAngle).X,
                    m_circle.GetPointOnPerimeter(m_startAngle + m_sweepAngle).X);
            }
        }

        public float MaxY
        {
            get
            {
                // The highest possible point on the circle would be at angle pi/2. If this angle is 
                // in the range of our arc then we return the maximum y-value of the circle. Otherwise 
                // we take the maximum y-value of the two endpoints.
                if (IsAngleInRange(Math.PI / 2.0, m_startAngle, m_sweepAngle))
                {
                    return m_circle.MaxY;
                }

                return Math.Max(
                    m_circle.GetPointOnPerimeter(m_startAngle).Y,
                    m_circle.GetPointOnPerimeter(m_startAngle + m_sweepAngle).Y);
            }
        }

        public float MinX
        {
            get
            {
                // The point on the circle with the minimum possible x-value would be at angle 
                // Pi. If this angle is in the range of our arc then we return the minimum x-
                // value of the circle. Otherwise we take the minimum x-value of the two 
                // endpoints.
                if (IsAngleInRange(Math.PI, m_startAngle, m_sweepAngle))
                {
                    return m_circle.MinX;
                }

                return Math.Min(
                    m_circle.GetPointOnPerimeter(m_startAngle).X,
                    m_circle.GetPointOnPerimeter(m_startAngle + m_sweepAngle).X);
            }
        }

        public float MinY
        {
            get
            {
                // The lowest possible point on the circle would be at angle 3pi/2. If this angle is 
                // in the range of our arc then we return the minimum y-value of the circle. Otherwise 
                // we take the minimum y-value of the two endpoints.
                if (IsAngleInRange(3.0 * Math.PI / 2.0, m_startAngle, m_sweepAngle))
                {
                    return m_circle.MinY;
                }

                return Math.Min(
                    m_circle.GetPointOnPerimeter(m_startAngle).Y,
                    m_circle.GetPointOnPerimeter(m_startAngle + m_sweepAngle).Y);
            }
        }
    }
}