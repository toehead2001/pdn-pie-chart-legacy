// Author:			Evan Olds
// Creation Date:	February 20, 2009

using System;

namespace EOFC
{
    internal struct Vector2D : IEquatable<Vector2D>
    {
        public float X;
        public float Y;

        public enum Quadrant
        {
            Quad1 = 1,
            Quad2 = 2,
            Quad3 = 3,
            Quad4 = 4
        }

        public Vector2D(float xval, float yval)
        {
            X = xval;
            Y = yval;
        }

        public Vector2D(Vector2D copyMe)
        {
            this.X = copyMe.X;
            this.Y = copyMe.Y;
        }

        public static Vector2D operator +(Vector2D lhs, Vector2D rhs)
        {
            return new Vector2D(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Vector2D operator -(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector2D operator *(Vector2D lhs, float f)
        {
            return new Vector2D(lhs.X * f, lhs.Y * f);
        }

        public static Vector2D operator /(Vector2D lhs, float rhs)
        {
            return new Vector2D(lhs.X / rhs, lhs.Y / rhs);
        }

        public double AngleBetween(Vector2D vec)
        {
            return Math.Acos(DotProduct(vec) / (this.Length * vec.Length));
        }

        /// <summary>
        /// Compares this vector with another using the specified tolerance
        /// </summary>
        /// <param name="v">Vector to compare with</param>
        /// <param name="tolerance">Tolerance of comparison. If the absolute value of 
        /// the difference between the vectors' components is less than or equal to this 
        /// value, then the components are considered to be equal.</param>
        /// <returns>True if vectors are equivalent using the given tolerance, false otherwise</returns>
        public bool CompareTo(Vector2D v, float tolerance)
        {
            return (Math.Abs(v.X - this.X) <= tolerance && Math.Abs(v.Y - this.Y) <= tolerance);
        }

        /// <summary>
        /// Returns the dot product of this vector with "vec".
        /// </summary>
        /// <param name="vec">Vector to dot product with this instance</param>
        /// <returns>The dot product of this vector and "vec"</returns>
        public float DotProduct(Vector2D vec)
        {
            return (vec.X * X + vec.Y * Y);
        }

        public bool Equals(float x, float y)
        {
            return (this.X == x && this.Y == y);
        }

        public bool Equals(Vector2D v2)
        {
            return (this.X == v2.X && this.Y == v2.Y);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2D))
            {
                return false;
            }

            Vector2D v = (Vector2D)obj;
            return (this.X == v.X && this.Y == v.Y);
        }

        
        /// <summary>
        /// Gets the counter-clockwise angle for this vector. This value will be in the range 
        /// [0.0, 2 * Pi). If the vector is a zero vector then zero is returned.
        /// </summary>
        public double GetCCWAngle()
        {
            // Handle special cases
            if (0.0f == X)
            {
                if (0.0f == Y)
                {
                    return 0.0;
                }

                return ((Y >= 0.0f) ? Math.PI / 2.0 : 3.0 * Math.PI / 2.0);
            }

            switch (GetQuadrant())
            {
                case Quadrant.Quad1:
                    return Math.Atan(this.Y / this.X);
                case Quadrant.Quad2:
                    return Math.PI - Math.Atan(-this.Y / this.X);
                case Quadrant.Quad3:
                    return Math.PI + Math.Atan(this.Y / this.X);
                case Quadrant.Quad4:
                    return 2.0 * Math.PI - Math.Atan(-this.Y / this.X);
            }
            return 0.0;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode());
        }

        /// <summary>
        /// Builds and returns a new vector that is perpendicular to this vector. The returned vector 
        /// will have an x-value of -Y and a y-value of X.
        /// </summary>
        public Vector2D GetPerpendicular()
        {
            return new Vector2D(-Y, X);
        }

        /// <summary>
        /// Returns the quadrant that the vector lies within. For zero vectors, vectors 
        /// along the positive x-axis, or vectors along the positive y-axis, quadrant 1 is 
        /// returned. For vectors along the negative x-axis, quadrant 2 is returned. For 
        /// vectors along the negative y-axis, quadrant 4 is returned.
        /// </summary>
        public Quadrant GetQuadrant()
        {
            if (X >= 0.0f)
            {
                return (Y >= 0.0f) ? Quadrant.Quad1 : Quadrant.Quad4;
            }
            return (Y >= 0.0f) ? Quadrant.Quad2 : Quadrant.Quad3;
        }

        /// <summary>
        /// Returns true if both components of the vector are equal to zero.
        /// </summary>
        public bool IsZero
        {
            get
            {
                return (0.0f == X && 0.0f == Y);
            }
        }

        /// <summary>
        /// Gets the length of the vector. This will always be a non-negative value.
        /// </summary>
        public float Length
        {
            get
            {
                double dx = (double)X;
                double dy = (double)Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// Returns the squared length of this vector. This is computed more quickly than the length.
        /// </summary>
        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        /// <summary>
        /// Generates a normalized version of the specified vector.
        /// </summary>
        public static Vector2D Normalize(Vector2D vector)
        {
            float len = vector.Length;
            if (len > 0.0f)
            {
                vector.X = vector.X / len;
                vector.Y = vector.Y / len;
            }
            return vector;
        }

        public void Normalize()
        {
            float sr = (float)Math.Sqrt(X * X + Y * Y);
            if (sr > 0.0f)
            {
                X = X / sr;
                Y = Y / sr;
            }
        }

        public static Vector2D Project(Vector2D me, Vector2D ontome)
        {
            ontome.Normalize();
            ontome *= (me.DotProduct(ontome));
            return ontome;
        }

        public void Rotate(double angle)
        {
            float oldx = X;
            this.X = (float)((double)X * Math.Cos(angle) + (double)Y * Math.Sin(angle));
            this.Y = (float)((double)-oldx * Math.Sin(angle) + (double)Y * Math.Cos(angle));
        }

        public static Vector2D Rotate(Vector2D vec, double angle)
        {
            return new Vector2D(
                (float)((double)vec.X * Math.Cos(angle) + (double)vec.Y * Math.Sin(angle)),
                (float)((double)-vec.X * Math.Sin(angle) + (double)vec.Y * Math.Cos(angle)));
        }

        // Returns the scalar projection of this instance onto the specified vector
        // "ontome", which must not be a zero length vector or a division by zero will occur.
        public float ScalarProjectOnto(Vector2D ontome)
        {
            return (X * ontome.X + Y * ontome.Y) / ontome.Length;
        }

        public void Set(float newx, float newy)
        {
            X = newx;
            Y = newy;
        }

        public void Set(Vector2D v)
        {
            this.X = v.X;
            this.Y = v.Y;
        }

        /// <summary>
        /// Sets the length of this vector while preserving its direction. This is done by normalizing the 
        /// vector then multiplying by the new length. If the length is zero, no modification occurs.
        /// </summary>
        public void SetLength(float newLength)
        {
            double dx = (double)X;
            double dy = (double)Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (0.0f != len)
            {
                this.X = this.X / len * newLength;
                this.Y = this.Y / len * newLength;
            }
        }

        public static void Swap(ref Vector2D a, ref Vector2D b)
        {
            Vector2D temp = a;
            a = b;
            b = temp;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}", X, Y);
        }

        public static readonly Vector2D UnitNegativeX = new Vector2D(-1.0f, 0.0f);
        
        public static readonly Vector2D UnitNegativeY = new Vector2D(0.0f, -1.0f);
        
        public static readonly Vector2D UnitPositiveX = new Vector2D(1.0f, 0.0f);

        public static readonly Vector2D UnitPositiveY = new Vector2D(0.0f, 1.0f);
    }
}
