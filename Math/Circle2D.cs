// Author:          Evan Olds
// Creation Date:   August 28, 2009

using System;

namespace EOFC
{
    internal struct Circle2D
    {
        private Vector2D m_center;
        
        private float m_radius;

        public Circle2D(Vector2D center, float radius)
            : this(center.X, center.Y, radius)
        {

        }
        
        public Circle2D(float centerX, float centerY, float radius)
        {
            if (radius < 0.0f)
            {
                throw new ArgumentException(
                    "Circle radius value must be greater than 0 (or equal to 0 to construct a degenerate circle)");
            }
            
            m_center = new Vector2D(centerX, centerY);
            m_radius = radius;
        }

        public double Area
        {
            get
            {
                return Math.PI * m_radius * m_radius;
            }
        }

        /// <summary>
        /// Gets a copy of the circle's center point.
        /// </summary>
        public EOFC.Vector2D Center
        {
            get
            {
                return m_center;
            }
        }

        public double Circumference
        {
            get
            {
                return 2.0 * Math.PI * m_radius;
            }
        }

        public bool Contains(EOFC.Vector2D point)
        {
            return (point - m_center).Length <= m_radius;
        }

        /// <summary>
        /// Returns the distance between the specified point and the ceneter point of this circle.
        /// </summary>
        public float DistanceFromCenter(float pointX, float pointY)
        {
            return DistanceFromCenter(new Vector2D(pointX, pointY));
        }
        
        /// <summary>
        /// Returns the distance between the specified point and the ceneter point of this circle.
        /// </summary>
        public float DistanceFromCenter(Vector2D point)
        {
            return (point - m_center).Length;
        }

        public Vector2D GetPointOnPerimeter(double angle)
        {
            return m_center + new Vector2D(
                (float)(Math.Cos(angle) * m_radius), (float)(Math.Sin(angle) * m_radius));
        }

        /// <summary>
        /// Computes the x-values of intersection points with a horizontal line. If the line does 
        /// not intersect the circle, then both output x-values are set to float.NaN.
        /// </summary>
        public void HLineIntersection(float yValue, out float minx, out float maxx)
        {
            // (x - a)^2 + (y - b)^2 = r^2
            // (x - a)^2 = r^2 - (y - b)^2
            // (x - a) = +-sqrt(r^2 - (y-b)^2)
            // x = a +- sqrt(r^2 - (y-b)^2)
            // Take plus for max value and minus for min

            float yMinusB = yValue - m_center.Y;
            double square = m_radius * m_radius - (yMinusB * yMinusB);
            if (square < 0.0)
            {
                minx = float.NaN;
                maxx = float.NaN;
                return;
            }
            
            minx = m_center.X - (float)Math.Sqrt(square);
            maxx = m_center.X + (float)Math.Sqrt(square);
        }

        public bool HLineIntersects(float yValue)
        {
            // (x - a)^2 + (y - b)^2 = r^2
            // (x - a)^2 = r^2 - (y - b)^2
            // (x - a) = +-sqrt(r^2 - (y-b)^2)
            // x = a +- sqrt(r^2 - (y-b)^2)

            // We can look at the sign of the discriminant to know whether or not there 
            // is an intersection.

            float yMinusB = yValue - m_center.Y;
            double disc = m_radius * m_radius - (yMinusB * yMinusB);
            return (disc >= 0.0);
        }

        public bool IsDegenerate
        {
            get
            {
                return (0.0f == m_radius);
            }
        }

        /// <summary>
        /// Gets the maximum x value in the circle. This is always equal to the x coordinate of 
        /// the center point plus the radius.
        /// </summary>
        public float MaxX
        {
            get
            {
                return m_center.X + m_radius;
            }
        }

        /// <summary>
        /// Gets the maximum y value in the circle. This is always equal to the y coordinate of 
        /// the center point plus the radius.
        /// </summary>
        public float MaxY
        {
            get
            {
                return m_center.Y + m_radius;
            }
        }

        /// <summary>
        /// Gets the minimum x value in the circle. This is always equal to the x coordinate of 
        /// the center point minus the radius.
        /// </summary>
        public float MinX
        {
            get
            {
                return m_center.X - m_radius;
            }
        }

        /// <summary>
        /// Gets the minimum y value in the circle. This is always equal to the y coordinate of 
        /// the center point minus the radius.
        /// </summary>
        public float MinY
        {
            get
            {
                return m_center.Y - m_radius;
            }
        }

        public float Radius
        {
            get
            {
                return m_radius;
            }
        }

        public static void Swap(ref Circle2D a, ref Circle2D b)
        {
            Circle2D temp = a;
            a = b;
            b = temp;
        }

        /// <summary>
        /// Computes the y-values of intersection points with a vertical line. If the line does 
        /// not intersect the circle, then both output y-values are set to float.NaN.
        /// UNTESTED, BUT SHOULD BE FINE
        /// </summary>
        public void VLineIntersection(float xValue, out float miny, out float maxy)
        {
            // (x - a)^2 + (y - b)^2 = r^2
            // (y - b)^2 = r^2 - (x - a)^2
            // y-b = (+-)sqrt(r^2 - (x-a)^2)
            // y = b (+-) sqrt(r^2 - (x-a)^2)
            // Take plus for max value and minus for min

            float xMinusA = xValue - m_center.X;
            double square = m_radius * m_radius - (xMinusA * xMinusA);
            if (square < 0.0)
            {
                miny = float.NaN;
                maxy = float.NaN;
                return;
            }

            miny = m_center.Y - (float)Math.Sqrt(square);
            maxy = m_center.Y + (float)Math.Sqrt(square);
        }
    }
}