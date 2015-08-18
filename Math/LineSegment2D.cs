// Author:          Evan Olds
// Creation Date:   February 25, 2009

using System;

namespace EOFC
{
    internal struct LineSegment2D
    {
        private Vector2D m_p1;
	    private Vector2D m_p2;
        private float m_l;

	    public LineSegment2D(EOFC.Vector2D point1, EOFC.Vector2D point2)
	    {
		    m_p1 = point1;
		    m_p2 = point2;
            m_l = (m_p2 - m_p1).Length;
	    }

        public LineSegment2D(float x1, float y1, float x2, float y2)
        {
            m_p1 = new Vector2D(x1, y1);
            m_p2 = new Vector2D(x2, y2);
            m_l = (m_p2 - m_p1).Length;
        }

        public LineSegment2D(LineSegment2D copyme)
	    {
		    m_p1 = copyme.m_p1;
		    m_p2 = copyme.m_p2;
            m_l = (m_p2 - m_p1).Length;
	    }

        /// <summary>
        /// Returns the signed distance from the specified point to this line segment. 
        /// This will be the distance from the point to the closet point on the segment. 
        /// The sign is with respect to an arbitrary, but consistent, normal vector which 
        /// can be retrieved from the "GetUnitNormal" function.
        /// </summary>
        public float Distance(EOFC.Vector2D point)
        {
            if (DoesPointProjectOnto(point))
            {
                EOFC.Vector2D unorm = GetUnitNormal();
                return (point - m_p1).DotProduct(unorm);
            }

            // If the point doesn't project onto the segment, then the smallest distance 
            // will be from one of the endpoints.
            return Math.Min((point - m_p1).Length, (point - m_p2).Length);
        }

        /// <summary>
        /// Returns the unsigned distance from the specified point to this line segment
        /// </summary>
        public float DistanceU(Vector2D point)
        {
            EOFC.Vector2D pointMinusP1 = (point - m_p1);
            float p = pointMinusP1.ScalarProjectOnto(m_p2 - m_p1);
            
            if (p >= 0.0f && p <= m_l)
            {
                float s = pointMinusP1.LengthSquared - (p * p);
                if (s <= 0.0f)
                {
                    // This should only ever occur because of rounding errors
                    return 0.0f;
                }
                return (float)Math.Sqrt(s);
            }

            float d1 = pointMinusP1.Length;
            float d2 = (point - m_p2).Length;
            return (d1 < d2) ? d1 : d2;
        }

        public bool DoesPointProjectOnto(EOFC.Vector2D pt)
	    {
		    float p = (pt - m_p1).ScalarProjectOnto(m_p2 - m_p1);
		    return (p >= 0.0f && p <= m_l);
	    }

        public override bool Equals(object obj)
        {
            // If the object is not of the same type then it cannot be equal
            if (!(obj is LineSegment2D))
            {
                return false;
            }
            
            LineSegment2D s = (LineSegment2D) obj;
            return (this.m_p1.Equals(s.m_p1) && this.m_p2.Equals(s.m_p2));
        }

        public override int GetHashCode()
        {
            return (m_p1.GetHashCode() ^ m_p2.GetHashCode());
        }
    	
        /// <summary>
        /// Builds and returns an infinite line object that represents the infinite line 
        /// for this segment.
        /// </summary>
        public NormalizedGeneralLine2D GetInfiniteLine()
	    {
		    return new NormalizedGeneralLine2D(m_p1, m_p2);
	    }

        /// <summary>
        /// Retrieves a copy of whichever of the two endpoints has the smaller x-value
        /// </summary>
        public EOFC.Vector2D GetPointWithSmallestX()
        {
            if (m_p1.X < m_p2.X)
            {
                return new EOFC.Vector2D(m_p1);
            }
            return new EOFC.Vector2D(m_p2);
        }

        /// <summary>
        /// Gets the point on this line segment with the specified x-value. If no such point 
        /// exists on the segment then false is returned. Note that if the segment is vertical, 
        /// even if it has a x-value of "x", then false is returned.
        /// </summary>
        public bool GetPointWithXValue(float x, ref EOFC.Vector2D outPt)
        {
            // If this segment is vertical, return false.
            if (m_p1.X == m_p2.X)
            {
                return false;
            }

            NormalizedGeneralLine2D infLine = GetInfiniteLine();
            float y = infLine.GetYCoord(x);
            EOFC.Vector2D pt = new EOFC.Vector2D(x, y);
            if (IsPointOnSegment(pt))
            {
                outPt.Set(pt);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the point on this line segment with the specified y-value. If no such point 
        /// exists on the segment then false is returned. Note that if the segment is horizontal, 
        /// even if it has a y-value of "y", then false is returned.
        /// </summary>
        public bool GetPointWithYValue(float y, ref EOFC.Vector2D outPt)
        {
            // If this segment is horizontal, return false.
            if (m_p1.Y == m_p2.Y)
            {
                return false;
            }

            NormalizedGeneralLine2D infLine = GetInfiniteLine();
            float x = infLine.GetXCoord(y);
            EOFC.Vector2D pt = new EOFC.Vector2D(x, y);
            if (IsPointOnSegment(pt))
            {
                outPt.Set(pt);
                return true;
            }
            return false;
        }

        public Vector2D GetUnitNormal()
        {
            return Vector2D.Normalize(new EOFC.Vector2D(m_p1.Y - m_p2.Y, m_p2.X - m_p1.X));
        }
        
        /// <summary>
        /// Gets the vector of the line segment.
        /// </summary>
        /// <returns>A vector pointing from point 1 to point 2 with a length equal to the length of 
        /// the segment.</returns>
        public EOFC.Vector2D GetVector()
	    {
		    return (m_p2 - m_p1);
	    }

        public bool Intersects(GeneralLine2D line, ref EOFC.Vector2D outISectPt)
        {
            GeneralLine2D thisInfLine = new GeneralLine2D(m_p1, m_p2);
            EOFC.Vector2D temp = new Vector2D();
            if (!thisInfLine.Intersects(line, ref temp))
            {
                return false;
            }

            // The two infinite lines intersect, now see if the intersection point
            // is on this segment
            if (this.IsPointOnSegment(temp))
            {
                outISectPt = temp;
                return true;
            }
            return false;
        }

        public bool Intersects(LineSegment2D segment2, ref EOFC.Vector2D outISectPt)
        {
            float t = 0.0f;

            if (!Intersects(segment2, ref t))
            {
                return false;
            }
            
            // Fill the intersection point
            float dx1 = m_p2.X - m_p1.X;
            float dy1 = m_p2.Y - m_p1.Y;
            outISectPt.Set(m_p1.X + t * dx1, m_p1.Y + t * dy1);

            return true;
        }

        /// <summary>
        /// Determines whether or not this segment intersects another. If the two segments 
        /// do intersect, then t will be set to a value in the range [0.0f, 1.0f] indicating 
        /// where the intersection occurs along this line, and true will be returned. If no 
        /// intersection occurs, false is returned and t is not modified.
        /// </summary>
        /// <returns>True if the segments intersect, false otherwise.</returns>
        public bool Intersects(LineSegment2D segment2, ref float t)
        {
            // Build directional vectors for both lines. Negate the ones for the
            // second segment by flipping the subtraction order.
            float dx1 = m_p2.X - m_p1.X;
            float dy1 = m_p2.Y - m_p1.Y;
            float dx2 = segment2.m_p1.X - segment2.m_p2.X;
            float dy2 = segment2.m_p1.Y - segment2.m_p2.Y;

            // Calculate determinate
            float det = (dx1 * dy2) - (dx2 * dy1);

            // If the determinate is zero then there is no intersection
            if (0.0f == det)
            {
                return false;
            }

            // Calculate origin differences
            float ox = segment2.m_p1.X - m_p1.X;
            float oy = segment2.m_p1.Y - m_p1.Y;

            // Calculate t1 and test bounds
            float t1 = (ox * dy2 - dx2 * oy) / det;
            if (t1 < 0.0f || t1 > 1.0f)
            {
                return false;
            }

            // Calculate t2 and test bounds
            float t2 = (dx1 * oy - ox * dy1) / det;
            if (t2 < 0.0f || t2 > 1.0f)
            {
                return false;
            }

            // Both t-values are in range, fill output t value
            t = t1;

            return true;
        }

        public bool Intersects(NormalizedGeneralLine2D line, ref EOFC.Vector2D outISectPt)
        {
            NormalizedGeneralLine2D thisInfLine = GetInfiniteLine();
            EOFC.Vector2D temp = new Vector2D();
            if (!thisInfLine.Intersects(line, ref temp))
            {
                return false;
            }

            // The two infinite lines intersect, now see if the intersection point
            // is on this segment
            if (this.IsPointOnSegment(temp))
            {
                outISectPt = temp;
                return true;
            }
            return false;
        }

        public bool IsHorizontal
        {
            get
            {
                return (m_p1.Y == m_p2.Y);
            }
        }

        public bool IsParallel(LineSegment2D seg2)
        {
            // The two segments will be parallel if their line vectors are the same
            // or one is the negative of the other.
            EOFC.Vector2D v1 = GetVector();
            EOFC.Vector2D v2 = seg2.GetVector();
            v1.Normalize();
            v2.Normalize();
            if (v1.DotProduct(v2) == 1.0f)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tests if a point, WHICH IS ALREADY KNOWN TO BE ON THE INFINITE LINE, is on this segment.
        /// </summary>
        private bool IsPointOnSegment(EOFC.Vector2D pt)
        {
            // If distance (squared) from p1 to pt and distance (squared) from p2 to pt are 
            // both less than the distance (squared) from p1 to p2, then it's on the segment.
            float lsqrd = m_l * m_l;
            float l1 = (pt.X - m_p1.X) * (pt.X - m_p1.X) + (pt.Y - m_p1.Y) * (pt.Y - m_p1.Y);
            float l2 = (pt.X - m_p2.X) * (pt.X - m_p2.X) + (pt.Y - m_p2.Y) * (pt.Y - m_p2.Y);
            return (l1 <= lsqrd && l2 <= lsqrd);
        }

        public bool IsVertical
        {
            get
            {
                return (m_p1.X == m_p2.X);
            }
        }

        /// <summary>
        /// Gets the length of this line segment.
        /// </summary>
        public float Length
        {
            get
            {
                return m_l;
            }
        }

        /// <summary>
        /// Gets the maximum x-value of the line. This value will be the greater of the two 
        /// endpoints' x-values.
        /// </summary>
        public float MaxX
        {
            get
            {
                return ((m_p1.X > m_p2.X) ? m_p1.X : m_p2.X);
            }
        }

        public float MaxY
        {
            get
            {
                return ((m_p1.Y > m_p2.Y) ? m_p1.Y : m_p2.Y);
            }
        }

        /// <summary>
        /// Gets the minimum x-value of the line. This value will be the lesser of the two 
        /// endpoints' x-values.
        /// </summary>
        public float MinX
        {
            get
            {
                return ((m_p1.X < m_p2.X) ? m_p1.X : m_p2.X);
            }
        }

        public float MinY
        {
            get
            {
                return ((m_p1.Y < m_p2.Y) ? m_p1.Y : m_p2.Y);
            }
        }

        /// <summary>
        /// Gets a copy of the first point of the segment
        /// </summary>
        public Vector2D Point1
        {
            get
            {
                return m_p1;
            }
        }

        /// <summary>
        /// Gets a cpoy of the second point of the segment
        /// </summary>
        public Vector2D Point2
        {
            get
            {
                return m_p2;
            }
        }
    }
}