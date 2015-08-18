// Author:          Evan Olds
// Creation Date:   August 31, 2009

using System;

namespace EOFC
{
    internal struct GeneralLine2D : ILine2D
    {
        public float A;

        public float B;

        public float C;

        public GeneralLine2D(float a, float b, float c)
        {
            A = a;
            B = b;
            C = c;
        }

        public GeneralLine2D(GeneralLine2D copyme)
        {
            this.A = copyme.A;
            this.B = copyme.B;
            this.C = copyme.C;
        }

        /// <summary>
	    /// Constructs a line instance from 2 different points that lie on the line.
	    /// </summary>
        public GeneralLine2D(Vector2D point1, Vector2D point2)
	    {
		    EOFC.Vector2D LV = (point2 - point1);
		    LV.Normalize();
		    A = -LV.Y;
		    B = LV.X;
		    C = -(A * point1.X + B * point1.Y);
	    }

        /// <summary>
        /// Returns the x-coordinate of the point on the line with the specified y-coordinate.
        /// Warning: Divides by zero for horizontal lines!
        /// </summary>
        public float GetXCoord(float y)
        {
            return ((-B * y) - C) / A;
        }

        // Returns the Y-coordinate of the point on the line with the specified X-coordinate.
        // WARNING: Divides by zero for vertical lines!
        public float GetYCoord(float x)
        {
            return ((-A * x) - C) / B;
        }

        /// <summary>
        /// Returns true if this line intersects the circle with the specified center point and 
        /// radius. If the line is tangent to the circle, this will be considered an intersection 
        /// and true will be returned.
        /// </summary>
        public bool IntersectsCircle(Vector2D center, float radius)
        {
            return (PointDistanceUnsigned(center) <= radius);
        }

        /// <summary>
        /// Determines whether or not the line intersects the specified circle and computes points of 
        /// intersection if it does. The point objects referenced will not be modified and false will 
        /// be returned if there is no intersection.
        /// </summary>
        public bool IntersectsCircle(Vector2D center, float radius, ref Vector2D pt1, ref Vector2D pt2)
        {
            // Handle special case first (horizontal line)
            if (0.0f == A)
            {
                float s = radius * radius - ((-C / B) - center.Y) * ((-C / B) - center.Y);
                if (s < 0.0f)
                {
                    return false;
                }
                float x1 = (float)Math.Sqrt(s) + center.X;
                float x2 = (float)-Math.Sqrt(s) + center.X;
                pt1.Set(x1, GetYCoord(x1));
                pt2.Set(x2, GetYCoord(x2));
                return true;
            }
            
            float t = (B * B) / (A * A) + 1;
            float u = (2 * B * C) / (A * A) + (2 * center.X * B / A) - (2 * center.Y);
            float v = (C * C) / (A * A) + (2 * center.X * C / A) + (center.X * center.X) + (center.Y * center.Y) -
                (radius * radius);

            float det = u * u - (4 * t * v);
            if (det < 0.0f)
            {
                return false;
            }

            float y1 = (-u + (float)Math.Sqrt(det)) / (2 * t);
            float y2 = (-u - (float)Math.Sqrt(det)) / (2 * t);

            // Set the intersection points
            pt1.Set(GetXCoord(y1), y1);
            pt2.Set(GetXCoord(y2), y2);

            return true;
        }

        public bool Intersects(GeneralLine2D line2, ref EOFC.Vector2D intersectionPt)
        {
            // If the two lines are parallel then they won't intersect
            if (IsParallel(line2))
            {
                return false;
            }

            float x, y;

            // Handle special case
            if (0.0f == A)
            {
                y = -C / B;
                x = (-line2.B * y - line2.C) / line2.A;
                intersectionPt = new EOFC.Vector2D(x, y);
                return true;
            }

            float val1 = (-line2.A * B) / A + line2.B;
            y = ((line2.A * C / A) - line2.C) / val1;
            x = (-B * y - C) / A;
            intersectionPt = new EOFC.Vector2D(x, y);

            return true;
        }
        
        public bool IsDegenerate
        {
            get
            {
                return (0f == A && 0f == B);
            }
        }

        public bool IsHorizontal
        {
            get
            {
                return (0f == A);
            }
        }

        public bool IsParallel(GeneralLine2D line2)
        {
            // The two lines will be parallel if their normalized normal vectors are 
            // the same or one is the negative of the other.
            float len1 = NormalLength;
            float na1 = A / len1;
            float nb1 = B / len1;
            float len2 = line2.NormalLength;
            float na2 = line2.A / len2;
            float nb2 = line2.B / len2;
            return ((na1 == na2 && nb1 == nb2) || (na1 == -na2 && nb1 == -nb2));
        }

        public bool IsPointOnLine(Vector2D pt)
        {
            return (0.0f == (A * pt.X + B * pt.Y + C));
        }

        public bool IsVertical
        {
            get
            {
                return (0.0f == B);
            }
        }

        public Vector2D Normal
        {
            get
            {
                return new Vector2D(A, B);
            }
        }

        /// <summary>
        /// Normalizes the line, making the normal vector (A, B) a unit vector. Will throw an 
        /// exception if the line is degenerate.
        /// </summary>
        public void Normalize()
        {
            float length = (float)Math.Sqrt(A * A + B * B);
            A /= length;
            B /= length;
            C /= length;
        }

        /// <summary>
        /// Gets the length of the normal vector for this line.
        /// </summary>
        public float NormalLength
        {
            get
            {
                return (float)Math.Sqrt(A * A + B * B);
            }
        }

        public float PointDistanceUnsigned(EOFC.Vector2D pt)
        {
            return Math.Abs(A * pt.X + B * pt.Y + C) / NormalLength;
        }
    }
}