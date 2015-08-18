// Author:          Evan Olds
// Creation Date:   February 25, 2009

using System;

namespace EOFC
{
    /// <summary>
    /// Represents an infinite line in 2D space, using the general form of the line 
    /// equation: Ax+By+C=0. The line is stored normalized internally.
    /// </summary>
    internal struct NormalizedGeneralLine2D : IEquatable<EOFC.NormalizedGeneralLine2D>, ILine2D
    {
        private float m_a, m_b, m_c;

	    /// <summary>
	    /// Constructs a line instance from 2 different points that lie on the line.
	    /// </summary>
        public NormalizedGeneralLine2D(EOFC.Vector2D point1, EOFC.Vector2D point2)
	    {
		    EOFC.Vector2D LV = (point2 - point1);
		    LV.Normalize();
		    m_a = -LV.Y;
		    m_b = LV.X;
		    m_c = -(m_a * point1.X + m_b * point1.Y);
	    }

	    public NormalizedGeneralLine2D(float x1, float y1, float x2, float y2)
	    {
		    EOFC.Vector2D LV = new EOFC.Vector2D(x2 - x1, y2 - y1);
		    LV.Normalize();
		    m_a = -LV.Y;
		    m_b = LV.X;
		    m_c = -(m_a * x1 + m_b * y1);
	    }

        /// <summary>
        /// Constructs the line from general-form equation coefficients. The line will be normalized 
        /// internally.
        /// </summary>
        /// <param name="a">"A" value in the equation Ax+By+C=0</param>
        /// <param name="b">"B" value in the equation Ax+By+C=0</param>
        /// <param name="c">"C" value in the equation Ax+By+C=0</param>
        public NormalizedGeneralLine2D(float a, float b, float c)
        {
            Vector2D norm = new Vector2D(a, b);
            c /= norm.Length;
            norm.Normalize();

            m_a = norm.X;
            m_b = norm.Y;
            m_c = c;
        }

        public NormalizedGeneralLine2D(NormalizedGeneralLine2D copyme)
        {
            this.m_a = copyme.m_a;
            this.m_b = copyme.m_b;
            this.m_c = copyme.m_c;
        }

        public float A
        {
            get
            {
                return m_a;
            }
        }

        public float B
        {
            get
            {
                return m_b;
            }
        }

        public float C
        {
            get
            {
                return m_c;
            }
        }

        /// <summary>
        /// Generates and returns an arbitrary point that lies on this line. Throws an 
        /// InvalidOperationException if the line is degenerate.
        /// </summary>
        public Vector2D GetArbitraryPoint()
        {
            if (0.0f != m_b)
            {
                return new Vector2D(1.0f, (-(m_a + m_c)) / m_b);
            }
            else if (0.0f != m_a)
            {
                return new Vector2D((-(m_b + m_c)) / m_a, 1.0f);
            }
            throw new InvalidOperationException(
                "Cannot get an arbitrary point because the line is degenerate");
        }

        public bool Equals(NormalizedGeneralLine2D other)
        {
            return (this.m_a == other.m_a && this.m_b == other.m_b && this.m_c == other.m_c);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NormalizedGeneralLine2D))
            {
                return false;
            }

            return this.Equals((NormalizedGeneralLine2D)obj);
        }

        public override int GetHashCode()
        {
            return (m_a.GetHashCode() ^ m_b.GetHashCode() ^ m_c.GetHashCode());
        }

	    // Returns an EOFC.Vector2D that points in the direction of the line.
	    // If you negate the vector, it will still point in the direction of
	    // the line, since lines are infinite in both directions.
        public EOFC.Vector2D GetLineVector()
	    {
		    return new EOFC.Vector2D(-m_b, m_a);
	    }

        // Creates a 3x3 matrix (in an array of floats) such that
        //  out_Matrix3x3 * Point = Reflected point
        // (where the points are treated as column vectors)
        public void GetReflectionMatrix(float[] out_Matrix3x3)
	    {
		    out_Matrix3x3[0] = 1-2*m_a*m_a;
		    out_Matrix3x3[1] = -2*m_a*m_b;
		    out_Matrix3x3[2] = -2*m_a*m_c;
		    out_Matrix3x3[3] = -2*m_a*m_b;
		    out_Matrix3x3[4] = 1-2*m_b*m_b;
		    out_Matrix3x3[5] = -2*m_b*m_c;
		    out_Matrix3x3[6] = 0.0f;
		    out_Matrix3x3[7] = 0.0f;
		    out_Matrix3x3[8] = 1.0f;
	    }

        /// <summary>
        /// Returns the x-coordinate of the point on the line with the specified y-coordinate.
        /// Warning: Divides by zero for horizontal lines!
        /// </summary>
        public float GetXCoord(float yCoord)
        {
            return ((-m_b * yCoord) - m_c) / m_a;
        }

	    // Returns the Y-coordinate of the point on the line with the 
	    // specified X-coordinate.
	    // WARNING: Divides by zero for vertical lines!
        public float GetYCoord(float XCoord)
	    {
		    return ((-m_a*XCoord)-m_c)/m_b;
	    }
    	
	    // Checks to see if this line intersects with "line2".  True is returned
	    // if they do intersect. The "ISectPt" is optional and can be NULL, but, if
	    // provided, it is filled with the point of intersection.
	    // IMPORTANT: If the two lines are equal then false is returned, not true.
	    // Do not call for degenerate lines (check with IsDegenerate).
        public bool Intersects(NormalizedGeneralLine2D line2, ref EOFC.Vector2D ISectPt)
	    {
		    // If the two lines are parallel then they won't intersect
		    if (IsParallel(line2))
		    {
			    return false;
		    }

            float x, y;
    		
		    // Handle special case
		    if (m_a == 0.0f)
		    {
			    y = -m_c/m_b;
			    x = (-line2.m_b*y-line2.m_c)/line2.m_a;
			    ISectPt = new EOFC.Vector2D(x, y);
			    return true;
		    }
		    // else
		    float val1 = (-line2.m_a*m_b)/m_a+line2.m_b;
		    y = ((line2.m_a*m_c/m_a)-line2.m_c)/val1;
		    x = (-m_b*y-m_c)/m_a;
	        ISectPt = new EOFC.Vector2D(x, y);
    		
		    return true;
	    }

        /// <summary>
        /// Returns true if this line intersects the circle with the specified center point and radius. If the 
        /// line is tangent to the circle, this will be considered an intersection and true will be returned.
        /// </summary>
        public bool InteresectsCircle(Vector2D center, float radius)
        {
            return (Math.Abs(PointDistance(center)) <= radius);
        }

        public bool IsDegenerate
        {
            get
            {
                return (m_a == 0.0f && m_b == 0.0f);
            }
        }

        public bool IsHorizontal
        {
            get
            {
                return (0.0f == m_a);
            }
        }

        public bool IsParallel(NormalizedGeneralLine2D line2)
	    {
		    // The two lines will be parallel if their normal vectors are the same
		    // or one is the negative of the other.
            return ((m_a == line2.m_a && m_b == line2.m_b) ||
                    (m_a == -line2.m_a && m_b == -line2.m_b));
	    }

        public bool IsPointOnLine(EOFC.Vector2D pt)
        {
            return (0.0f == (m_a * pt.X + m_b * pt.Y + m_c));
        }

        public bool IsVertical
	    {
            get
            {
                return (0.0f == m_b);
            }
	    }

        /// <summary>
        /// Returns the signed point distance from the specified point to this line.
        /// </summary>
        public float PointDistance(EOFC.Vector2D pt)
        {
            return (m_a * pt.X + m_b * pt.Y + m_c);
        }

	    // ProjectPoint
	    // Returns the point on the line that has the shortest distance to the
	    // specified point. This equivalent to visually bringing the point "straight
	    // down" onto the line, somewhat like "projecting" it onto the line.
        public EOFC.Vector2D ProjectPoint(EOFC.Vector2D point)
	    {
		    float distance = m_a*point.X + m_b*point.Y + m_c;
		    EOFC.Vector2D Norm = new EOFC.Vector2D(m_a, m_b);
		    return (point + Norm*(-distance));
	    }

        public EOFC.Vector2D ReflectPoint(EOFC.Vector2D pt)
	    {
		    float[] M = new float[9];
		    GetReflectionMatrix(M);
		    EOFC.Vector2D output = new EOFC.Vector2D(
			    M[0]*pt.X + M[1] * pt.Y + M[2],
			    M[3]*pt.X + M[4] * pt.Y + M[5]);
		    return output;
	    }
    }
}