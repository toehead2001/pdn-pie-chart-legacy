// Author:          Evan Olds
// Creation Date:   October 15, 2010
// Description:     Provides drawing operations on 32-bit image pixel data.
// © 2012 Evan Olds
// References:
//  http://en.wikipedia.org/wiki/Bresenham's_line_algorithm
//  http://en.wikipedia.org/wiki/Alpha_compositing

using System;
using System.Collections.Generic;

// There is much in the System.Drawing namespace that isn't supported on many 
// platforms. Since this code should be platform independent, then only things 
// used in the System.Drawing namespace should be Rectangle and Point.
using System.Drawing;

namespace EOFC
{
    // Class-wide TODO:
    /*
     Change all methods to this proposed parameter order:
        1. SimpleImage always first (or equivalent data in the form: pxls, w, h)
        2. Color to draw with always second
        3. Information about the object to draw (circle object, line endpoint locations, etc.)
        4. Line thickness (if applicable)
    */

    // Notes:
    // 1. The design decision after having this class as a static class for quite a while was 
    //    to instead make it non-static. This is so that inheriting classes can utilize the 
    //    lookup tables (which are still static becuase only one instance of each is needed) 
    //    and utility methods. Inheriting classes must never modify the contents of the lookup 
    //    tables. (might go back to static in the future though, just dunno right now)
    // 2. The class should eventually be split up so that specialized drawing methods such 
    //    as for charts and arrows are in their own inheriting classes.

    /// <summary>
    /// Provides drawing operations on 32-bit image pixel data. All methods use alpha-composite 
    /// blending unless they take a blend-mode parameter that allows a different blend mode or 
    /// the summary lists the method as non-blending.
    /// </summary>
    internal class Drawing32
    {
        protected const double c_twoPi = Math.PI * 2.0;
        
        public enum BlendMode
        {
            // Direct pixel copy
            None,

            // Alpha composite source-over blend operation
            AlphaComposite
        }
        
        /// <summary>
        /// Simple structure to hold a width value, height value, and pointer to pixel data. No 
        /// allocation or deallocation occurs in this structure's code.
        /// </summary>
        public struct SimpleImage
        {
            public unsafe uint* Pixels;
            public int Width;
            public int Height;

            public unsafe SimpleImage(int width, int height, uint* pixels)
            {
                this.Width = width;
                this.Height = height;
                this.Pixels = pixels;
            }

            public unsafe uint* GetRowAtX(int yRow, int x)
            {
                return &Pixels[yRow * Width + x];
            }
        }

        public enum ArrowHeadStyle
        {
            UpperHookOnly,
            LowerHookOnly,
            BothHooks,
            NoHooks
        }

        /// <summary>
        /// Represents a section of a pie chart
        /// Subject to change - may or may not have a "Name" string value in the future
        /// </summary>
        public struct PieChartSection
        {
            private uint m_color;
            
            private double m_value;

            public PieChartSection(double value, uint color)
            {
                if (value <= 0.0)
                {
                    throw new ArgumentException(
                        "Pie chart sections must have values > 0.0");
                }
                
                m_color = color;
                m_value = value;
            }

            public uint Color
            {
                get
                {
                    return m_color;
                }
            }

            public double Value
            {
                get
                {
                    return m_value;
                }
            }
        }

        private static byte[] s_alphaLookup = null;

        /// <summary>
        /// s_premuls is a lookup table for two 8-bit values x and y. An index of
        /// s_premuls[x | (y&lt;&lt;8)] yields the precomputed value x * y / 255.
        /// 
        /// Inheriting classes must never modify the contents of this table. Direct 
        /// access is given for speed and convenience reasons.
        /// </summary>
        protected static byte[] s_premuls = null;

        /// <summary>
        /// Precomputed divisions table. Given two 8-bit values x and y an index of 
        /// s_predivs[x | (y&lt;&lt;8)] will yield the precomputed value x * 255 / y. In
        /// the cases where y is zero, the lookup value will be zero.
        /// 
        /// Inheriting classes must never modify the contents of this table. Direct 
        /// access is given for speed and convenience reasons.
        /// </summary>
        protected static byte[] s_predivs = null;

        /// <summary>
        /// Default and only constructor. Inheriting classes must make sure they call this 
        /// as the base constructor since it initializes static lookup tables if they 
        /// haven't already been initialized.
        /// </summary>
        public Drawing32()
        {
            Initialize();
        }

        public static unsafe void BlendComposite(uint src, uint* dest)
        {
            uint srcB = (src & 0x000000FF);
            uint srcG = (src & 0x0000FF00) >> 8;
            uint srcR = (src & 0x00FF0000) >> 16;
            uint srcA = src >> 24;
            uint iSrcA = ((255 - srcA) << 8);
            uint destB = (*dest) & 0x000000FF;
            uint destG = ((*dest) & 0x0000FF00) >> 8;
            uint destR = ((*dest) & 0x00FF0000) >> 16;
            uint destA = (*dest) >> 24;

            uint srcALookup = (srcA << 8);
            uint dASL8 = (destA << 8);

            // Use the two 8-bit alpha values to lookup the new alpha value
            uint newA = (uint)s_alphaLookup[srcALookup | destA];

            // Compute the first parts of the color components
            uint newB = (uint)s_premuls[srcALookup | srcB] +
                (uint)s_premuls[(uint)s_premuls[dASL8 | destB] | iSrcA];
            uint newG = (uint)s_premuls[srcALookup | srcG] +
                (uint)s_premuls[(uint)s_premuls[dASL8 | destG] | iSrcA];
            uint newR = (uint)s_premuls[srcALookup | srcR] +
                (uint)s_premuls[(uint)s_premuls[dASL8 | destR] | iSrcA];

            // Compute the last parts (* 255 / newA)
            uint newALookup = (newA << 8);
            newB = s_predivs[newB | newALookup];
            newG = s_predivs[newG | newALookup];
            newR = s_predivs[newR | newALookup];

            *dest = newB | (newG << 8) | (newR << 16) | (newA << 24);
        }

        /// <summary>
        /// Copies a rectangular section from one image to another. The two images must have the same 
        /// dimensions.
        /// The rectangle width and height must be positive. If either is less than or equal to zero 
        /// then an exception will be thrown.
        /// If the rectangle contains regions that are not on the image, then it will be clipped 
        /// appropriately.
        /// </summary>
        public static unsafe void CopyRectangle(int x, int y, int rectW, int rectH, uint* source, 
            uint* destination, int w, int h)
        {
            if (rectW <= 0 || rectH <= 0)
            {
                throw new ArgumentException();
            }
            
            // First see if the rectangle is compeletely off the image
            if (x >= w || y >= h)
            {
                return;
            }
            
            if (x < 0)
            {
                w += x;
                x = 0;
            }
            if (y < 0)
            {
                h += y;
                y = 0;
            }

            // If the width or height is <= 0 at this point then return
            if (w <= 0 || h <= 0)
            {
                return;
            }

            // Loop through rows of pixels
            while (rectH-- > 0)
            {
                uint* src = &source[y * w + x];
                uint* dst = &destination[y * w + x];
                for (int i = 0; i < rectW; i++)
                {
                    *dst++ = *src++;
                }
                
                // Go to the next row of pixels
                y++;
            }
        }

        /// <summary>
        /// Draws an arc using alpha-compositing as the blend mode. Currently uses a hard-coded softening 
        /// distance value of 1.5 pixels.
        /// </summary>
        /// <param name="startAngle">Start angle, in radians</param>
        /// <param name="sweepAngle">Sweep angle, in radians</param>
        public unsafe void DrawArc(Circle2D circle, float lineThickness, double startAngle, double sweepAngle,
            uint color, uint* pixels, int imageWidth, int imageHeight)
        {
            // Check parameters
            if (lineThickness <= 0.0f)
            {
                throw new ArgumentException(
                    "Line thickness value must be a positive number");
            }

            // If the circle is degenerate then we have nothing to render
            if (circle.IsDegenerate)
            {
                return;
            }
            
            // Special case: if the sweep angle is >= 2*pi then we just frame the whole circle
            if (sweepAngle >= c_twoPi)
            {
                FrameCircle(circle, lineThickness, color, pixels, imageWidth, imageHeight);
                return;
            }

            // Softening radius (hard-coded for now) (change method summary if this changes)
            float sr = 1.5f;

            // Compute lineThickness / 2 because it's used a lot
            float lto2 = lineThickness / 2.0f;

            double colorAlphaDouble = ((double)(color >> 24)) / 255.0;

            // Another special case if the line is thick enough to extend in and past the center of the circle
            if (circle.Radius - lto2 <= 0.0f)
            {
                FillCircle(new SimpleImage(imageWidth, imageHeight, pixels), color, circle);
                return;
            }

            // Build the "outer" circle
            Circle2D outer = new Circle2D(circle.Center, circle.Radius + lto2);

            // Build the "inner" circle
            Circle2D inner = new Circle2D(circle.Center, circle.Radius - lto2);

            // Build the arc object
            CircleArc ca = new CircleArc(circle, startAngle, sweepAngle);

            // Find out the starting y-value
            int ystart = (int)Math.Floor(ca.MinY - lto2);

            // If it's below the bottom of the image then we have nothing to render
            if (ystart >= imageHeight)
            {
                return;
            }

            // Make sure it's not < 0
            if (ystart < 0)
            {
                ystart = 0;
            }

            // Now determine the inclusive ending y-value
            int yend = (int)Math.Ceiling(ca.MaxY + lto2);
            if (yend < 0) {return;}
            if (yend >= imageHeight)
            {
                yend = imageHeight - 1;
            }

            // Loop through rows of pixels and render
            while (ystart++ <= yend)
            {
                // Compute outer circle intersections
                float ocx1, ocx2;
                outer.HLineIntersection((float)ystart, out ocx1, out ocx2);

                // If there's no intersection or the two x-values are equal, then move onto 
                // the next y-value
                if (float.IsNaN(ocx1) || float.IsNaN(ocx2) || ocx1 == ocx2)
                {
                    continue;
                }

                // Compute the inner circle intersections
                float icx1, icx2;
                inner.HLineIntersection((float)ystart, out icx1, out icx2);

                float[] xvals;
                if (float.IsNaN(icx1) || float.IsNaN(icx2) || icx1 == icx2)
                {
                    // If there's no inner circle intersections then our array just has the two 
                    // x-values for the outer intersection.
                    xvals = new float[] { ocx1, ocx2 };
                }
                else
                {
                    xvals = new float[] { ocx1, icx1, icx2, ocx2 };
                }

                // Render between pairs of intersection points
                for (int i = 0; i < xvals.Length; i+=2)
                {
                    int x1 = (int)Math.Floor(xvals[i]);
                    if (x1 >= imageWidth)
                    {
                        continue;
                    }
                    if (x1 < 0) { x1 = 0; }
                    int x2 = (int)Math.Ceiling(xvals[i + 1]);
                    if (x2 >= imageWidth) { x2 = imageWidth - 1; }

                    uint* start = &pixels[ystart * imageWidth + x1];
                    uint* end = &pixels[ystart * imageWidth + x2];
                    while (start++ <= end)
                    {
                        float dist = ca.DistanceU(new Vector2D((float)x1, (float)ystart));
                        if (dist >= lto2)
                        {
                            x1++;
                            continue;
                        }
                        else if (dist > (lto2 - sr))
                        {
                            double alpha = (lto2 - dist) / sr;
                            alpha *= colorAlphaDouble;
                            uint clr = color & 0x00FFffFF;
                            clr |= (((uint)(alpha * 255.0)) << 24);
                            BlendComposite(clr, start);
                        }
                        else
                        {
                            BlendComposite(color, start);
                        }

                        x1++;
                    }
                }
            }
        }

        public void DrawArrowRoundStyle(SimpleImage img, uint clr, Vector2D start, Vector2D end,
            float hookLength, float lineThickness)
        {
            DrawArrowRoundStyle(img, clr, start, end, ArrowHeadStyle.NoHooks,
                ArrowHeadStyle.BothHooks, hookLength, lineThickness);
        }

        public void DrawArrowRoundStyle(SimpleImage img, uint clr, Vector2D start, Vector2D end,
            ArrowHeadStyle startHead, ArrowHeadStyle endHead, float hookLength, float lineThickness)
        {
            if (hookLength <= 0f)
            {
                DrawRoundLine(img, start, end, lineThickness, clr);
                return;
            }
            
            // If the start and end points are the same then it is a degenerate arrow. We'll just fill a 
            // circle in this case.
            if (start.Equals(end))
            {
                FillCircle(img, clr, new Circle2D(start, lineThickness / 2f));
                return;
            }

            // For the round style arrows, we essentially just have to render a series of round lines. 
            // What we DON'T want to do is draw one for the arrow "body" and then additional ones for 
            // the hooks at the end. This is because there will be duplicate blending near the ends and 
            // it won't look as good.

            Vector2D dirNorm = Vector2D.Normalize(end - start);
            Vector2D perpNorm = Vector2D.Normalize((start - end).GetPerpendicular());

            List<Stadium> slist = new List<Stadium>();
            // We always need the main line
            slist.Add(new Stadium(start, end, lineThickness / 2f));
            if (ArrowHeadStyle.BothHooks == startHead ||
                ArrowHeadStyle.UpperHookOnly == startHead)
            {
                slist.Add(new Stadium(start, start + (dirNorm * hookLength) + (perpNorm * hookLength),
                    lineThickness / 2f));
            }
            if (ArrowHeadStyle.BothHooks == startHead ||
                ArrowHeadStyle.LowerHookOnly == startHead)
            {
                slist.Add(new Stadium(start, start + (dirNorm * hookLength) - (perpNorm * hookLength),
                    lineThickness / 2f));
            }
            if (ArrowHeadStyle.BothHooks == endHead ||
                ArrowHeadStyle.UpperHookOnly == endHead)
            {
                slist.Add(new Stadium(end, end - (dirNorm * hookLength) + (perpNorm * hookLength),
                    lineThickness / 2f));
            }
            if (ArrowHeadStyle.BothHooks == endHead ||
                ArrowHeadStyle.LowerHookOnly == endHead)
            {
                slist.Add(new Stadium(end, end - (dirNorm * hookLength) - (perpNorm * hookLength),
                    lineThickness / 2f));
            }

            Stadium[] stads = slist.ToArray();

            // Soften radius, hard-coded for now
            float sr = 2f;

            FillStadiums(img, clr, stads, sr);
        }
        
        ///// <summary>
        ///// Draws a circle with anti-aliasing. The alpha value of the drawing color is ignored. The alpha 
        ///// value of destination pixels are also ignored. That is, when blending edges to achieve anti-
        ///// aliasing, normal alpha blending will be used, not alpha compositing.
        ///// </summary>
        ///// <remarks>Need to be more clear about blend modes. This function, and ideally all public drawing 
        ///// functions in this class should have a BlendMode parameter.</remarks>
        //[Obsolete("Use functions with blend modes (like FillCircle)")]
        //public static unsafe void DrawCircleAA(float centerX, float centerY, float radius, uint clr, 
        //    uint* pixels, int imageWidth, int imageHeight)
        //{
        //    clr |= 0xFF000000;
            
        //    EOFC.Circle2D circle = new Circle2D(centerX, centerY, radius);

        //    int bottom = (int)Math.Ceiling(circle.MaxY);
        //    int y = Math.Max((int)Math.Floor(circle.MinY), 0);

        //    // If we're completely below the image, then return
        //    if (y >= imageHeight)
        //    {
        //        return;
        //    }

        //    while (y <= bottom && y < imageHeight)
        //    {
        //        float minx, maxx;
        //        circle.HLineIntersection((float)y, out minx, out maxx);

        //        // Continue the loop if there was no intersection
        //        if (float.IsNaN(minx) || float.IsNaN(maxx))
        //        {
        //            continue;
        //        }
        //        // Also continue if we're completely to the right of the image
        //        if (minx > (float)imageWidth)
        //        {
        //            continue;
        //        }

        //        int left = (int)Math.Floor(minx);
        //        uint* p = &pixels[y * imageWidth + left];
                
        //        // Clip if we're off the image
        //        if (left < 0)
        //        {
        //            left = 0;
        //            p = &pixels[y * imageWidth];
        //        }
        //        else if ((float)left != minx)
        //        {
        //            // Do anti-aliasing for left side
        //            double alpha = Math.Ceiling(minx) - minx;
        //            Blend(clr, alpha, p);
        //            left++;
        //            p++;
        //        }

        //        // Fill solid color pixels across
        //        int x;
        //        for (x = left; x < (int)Math.Floor(maxx); x++)
        //        {
        //            *p++ = clr;
        //        }

        //        if ((float)x != maxx)
        //        {
        //            // Do anti-aliasing for right side
        //            double alpha = 1.0 - (Math.Ceiling(maxx) - maxx);
        //            Blend(clr, alpha, p);
        //        }

        //        y++;
        //    }
        //}

        /// <summary>
        /// Draws a 1-pixel thick horizontal line that is inclusive of both endpoints. The endpoints are 
        /// permitted to lie outside of the image boundaries and proper clipping will occur.
        /// </summary>
        /// <param name="x1">Starting x-value of line, which is included in rasterization.</param>
        /// <param name="x2">Ending x-value of the line, which is included in rasterization.</param>
        /// <param name="y">Y-value of the line.</param>
        /// <param name="clr">Pixel color for the line.</param>
        /// <param name="pxls">Pointer to 32-bit pixel data.</param>
        /// <param name="imageWidth">Width of image.</param>
        /// <param name="imageHeight">Height of image.</param>
        public unsafe void DrawHorizontalLine(int x1, int x2, int y, uint clr, uint* pxls,
            int imageWidth, int imageHeight, BlendMode blendMode)
        {
            // If the y-value is outside of the image, then we don't draw anything
            if (y < 0 || y >= imageHeight)
            {
                return;
            }

            // Make sure x1 is the lesser and x2 the greater
            if (x1 > x2)
            {
                int temp = x1;
                x1 = x2;
                x2 = temp;
            }

            // If the greater of the two x-values is less than 0, then there's nothing to draw. Also, 
            // if the lesser of the two x-values is greater than or equal to the image width then there 
            // is nothing to draw.
            if (x2 < 0 || x1 >= imageWidth)
            {
                return;
            }

            // Make sure the x-values are within the image
            if (x1 < 0)
            {
                x1 = 0;
            }
            if (x2 >= imageWidth)
            {
                x2 = imageWidth - 1;
            }

            uint* p = &pxls[y * imageWidth + x1];
            int pCount = x2 - x1;
            if (BlendMode.None == blendMode || 
                (0xFF000000 & clr) == 0xFF000000)
            {
                // If we aren't blending or the pixel has 100% opacity, then we just 
                // copy pixels
                while (pCount-- >= 0)
                {
                    // Draw the pixel
                    *p++ = clr;
                }
            }
            else
            {
                while (pCount >= 0)
                {
                    // Blend the pixel
                    BlendComposite(clr, p);
                    p++;
                    pCount--;
                }
            }
        }

        // Adapted pseudo-code from Wikipedia. This is the only method in this class that isn't 100%
        // original, from-scratch code written by Evan Olds. Well, I guess the alpha compositing equations 
        // were also obtained from Wikipedia, but that's something that's more along the lines of being 
        // mathematical fact that an actual algorithm.
        public unsafe void DrawLine(int x1, int y1, int x2, int y2, uint clr, uint* pixels, int w, int h,
            BlendMode blendMode)
        {
            // Special case if the line is horizontal...
            if (y1 == y2)
            {
                DrawHorizontalLine(x1, x2, y1, clr, pixels, w, h, BlendMode.None);
                return;
            }
            // ... or vertical
            if (x1 == x2)
            {
                DrawVerticalLine(x1, y1, y2, clr, pixels, w, h, BlendMode.None);
                return;
            }

            if (x1 > x2)
            {
                Swap(ref x1, ref x2);
                Swap(ref y1, ref y2);
            }

            int Dx = x2 - x1;
            int Dy = y2 - y1;
            bool steep = (Math.Abs(Dy) >= Math.Abs(Dx));
            if (steep)
            {
                Swap(ref x1, ref y1);
                Swap(ref x2, ref y2);
                // recompute Dx, Dy after swap
                Dx = x2 - x1;
                Dy = y2 - y1;
            }
            int xstep = 1;
            if (Dx < 0)
            {
                xstep = -1;
                Dx = -Dx;
            }
            int ystep = 1;
            if (Dy < 0)
            {
                ystep = -1;
                Dy = -Dy;
            }
            int TwoDy = 2 * Dy;
            int TwoDyTwoDx = TwoDy - 2 * Dx; // 2*Dy - 2*Dx
            int E = TwoDy - Dx; //2*Dy - Dx
            int y = y1;
            int xDraw, yDraw;
            for (int x = x1; x != x2; x += xstep)
            {
                if (steep)
                {
                    xDraw = y;
                    yDraw = x;
                }
                else
                {
                    xDraw = x;
                    yDraw = y;
                }
                // Draw the pixel
                if (xDraw >= 0 && xDraw < w && yDraw >= 0 && yDraw < h)
                {
                    if (BlendMode.AlphaComposite == blendMode)
                    {
                        BlendComposite(clr, &pixels[yDraw * w + xDraw]);
                    }
                    else
                    {
                        pixels[yDraw * w + xDraw] = clr;
                    }
                }
                // next
                if (E > 0)
                {
                    E += TwoDyTwoDx; //E += 2*Dy - 2*Dx;
                    y = y + ystep;
                }
                else
                {
                    E += TwoDy; //E += 2*Dy;
                }
            }
        }

        // TODO: Make this obey blending rules properly
        public unsafe void DrawPieChart(uint* pxls, int width, int height,
            PieChartSection[] sections)
        {
            if (0 == sections.Length)
            {
                throw new ArgumentException("Pie chart sections array cannot be empty");
            }

            // We need at least a 5x5 image
            if (width < 5 || height < 5)
            {
                return;
            }

            // If the image is not square, we want non-zero starting points
            int ystart = 0;
            int yend = height - 1; // Inclusive upper bound
            if (height > width)
            {
                ystart = (height - width) / 2;
                yend = height - ystart;
            }

            float xcenter = (float)width / 2.0f;
            float ycenter = (float)height / 2.0f;
            double radius = Math.Min(xcenter, ycenter) - 2.0;

            // Make a circle object for the pie chart area
            Circle2D circle = new Circle2D(xcenter, ycenter, (float)radius);

            // Compute the sum of all values in the pie sections
            double sum = 0.0;
            foreach (PieChartSection pcs in sections)
            {
                sum += pcs.Value;
            }

            // Make an angle table. This table stores the ending angle of each category. Also 
            // make a color table.
            double[] angles = new double[sections.Length];
            uint[] clrLookup = new uint[sections.Length];
            int i = 0;
            double angle = 0.0;
            foreach (PieChartSection pcs in sections)
            {
                clrLookup[i] = pcs.Color;
                angles[i] = pcs.Value / sum * (Math.PI * 2.0) + angle;
                angle = pcs.Value / sum * (Math.PI * 2.0) + angle;
                i++;
            }

            while (ystart <= yend)
            {
                // Compute the intersections of the scan line with the pie chart
                float minx, maxx;
                circle.HLineIntersection((float)ystart, out minx, out maxx);

                // Clear the row if there isn't an intersection
                if (float.IsNaN(minx) || float.IsNaN(maxx))
                {
                    DrawHorizontalLine(0, width - 1, ystart, 0, pxls, width, height, BlendMode.None);
                    ystart++;
                    continue;
                }
                
                int xstart = (int)Math.Floor(minx);
                int xend = (int)Math.Min(Math.Ceiling(maxx), (float)(width - 1));
                uint* row = &pxls[ystart * width + xstart];
                for (int x = xstart; x <= xend; x++)
                {
                    // For each pixel, we want to start by computing the distance from the center
                    Vector2D pos = new Vector2D((float)x - xcenter, ycenter - (float)ystart);
                    double dist = pos.Length;

                    // Set the pixel to 0 if we're outside of the pie chart area
                    if (dist >= radius)
                    {
                        *row++ = 0;
                        continue;
                    }

                    // Compute the angle so we know what category of the pie we're in
                    angle = pos.GetCCWAngle();
                    i = 0;
                    while (angle > angles[i])
                    {
                        i++;
                    }

                    // Advance to the next pixel
                    *row++ = clrLookup[i];
                }
                
                ystart++;
            }

            // Draw lines between sections
            if (sections.Length > 1)
            {
                List<Stadium> stadiums = new List<Stadium>();
                foreach (double tempAngle in angles)
                {
                    stadiums.Add(new Stadium(circle.Center,
                            new Vector2D(xcenter + (float)(radius * Math.Cos(tempAngle)),
                            ycenter - (float)(radius * Math.Sin(tempAngle))), 1.75f));
                }
                FillStadiums(new SimpleImage(width, height, pxls), 0xFF000000, stadiums.ToArray(), 2f);
            }

            // Draw a circular border around the pie chart
            FrameCircle_Composite(circle, 3.5f, 0xFF000000, pxls, width, height);
        }

        public unsafe void DrawRoundLine(int x1, int y1, int x2, int y2, float lineWidth, uint clr,
            uint* pxls, int w, int h, BlendMode blendMode)
        {            
            // Route the call to the function with the proper blending
            switch (blendMode)
            {
                case BlendMode.None:
                    DrawRoundLine_None(x1, y1, x2, y2, lineWidth, clr, pxls, w, h);
                    break;

                case BlendMode.AlphaComposite:
                    DrawRoundLine_Composite(x1, y1, x2, y2, lineWidth, clr, pxls, w, h);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public unsafe void DrawRoundLine(SimpleImage image, Vector2D pt1, Vector2D pt2, float lineWidth,
            uint color)
        {
            DrawRoundLine_Composite(new Stadium(pt1, pt2, lineWidth / 2f), color, image.Pixels,
                image.Width, image.Height);
        }

        private unsafe void DrawRoundLine_Composite(int x1, int y1, int x2, int y2, float lineWidth, 
            uint clr, uint* pxls, int w, int h)
        {
            DrawRoundLine_Composite(new Stadium(new Vector2D(x1, y1), new Vector2D(x2, y2), lineWidth / 2.0f),
                clr, pxls, w, h);
        }

        private unsafe void DrawRoundLine_Composite(Stadium stad, uint clr, uint* pxls, int w, int h)
        {
            FillStadiums(new SimpleImage(w, h, pxls), clr, new Stadium[] { stad }, 2f);
        }

        private unsafe void DrawRoundLine_None(int x1, int y1, int x2, int y2, float lineWidth, uint clr, 
            uint* pxls, int w, int h)
        {
            DrawRoundLine_None(new Stadium(new Vector2D(x1, y1), new Vector2D(x2, y2), lineWidth / 2.0f),
                clr, pxls, w, h);
        }

        private unsafe void DrawRoundLine_None(Stadium stad, uint clr, uint* pxls, int w, int h)
        {
            // Compute the inclusive maximum bound for Y
            int maxy = (int)Math.Ceiling(stad.MaxY);
            if (maxy > h - 1)
            {
                maxy = h - 1;
            }
            else if (maxy < 0)
            {
                // We're not even on the image and don't need to draw anything
                return;
            }
            
            // Fill pixels within the stadium
            for (int y = (int)Math.Max(Math.Floor(stad.MinY), 0.0); y <= maxy; y++)
            {
                float minx, maxx;
                stad.HLineIntersection((float)y, out minx, out maxx);

                // If there's no intersection, then we can move to the next row
                if (float.IsNaN(minx) || float.IsNaN(maxx))
                {
                    continue;
                }

                // Compute the inclusive upper and lower bounds for X for this row
                int x = (int)Math.Floor(minx);
                if (x < 0)
                {
                    x = 0;
                }
                int rowMaxX = (int)Math.Ceiling(maxx);
                if (rowMaxX > w - 1)
                {
                    rowMaxX = w - 1;
                }

                // Get a pointer to the row of pixels
                uint* rowPtr = &pxls[y * w + x];

                while (x <= rowMaxX)
                {
                    float dist = stad.Axis.DistanceU(new Vector2D(x, y));
                    if (dist >= stad.Radius)
                    {
                        x++;
                        rowPtr++;
                        continue;
                    }

                    float distFromEdge = stad.Radius - dist;
                    if (distFromEdge < 2.0f)
                    {
                        // Soften alpha value
                        uint newa = (uint)((distFromEdge / 2.0f) * (float)((*rowPtr) >> 24));
                        *rowPtr = (clr & 0x00FFffFF) | (newa << 24);
                    }
                    else
                    {
                        *rowPtr = clr;
                    }

                    x++;
                    rowPtr++;
                }
            }
        }

        /// <summary>
        /// Draws a 1-pixel-thick, vertical line that is inclusive of both endpoints. The endpoints 
        /// are permitted to lie outside of the image boundaries.
        /// </summary>
        public unsafe void DrawVerticalLine(int x, int y1, int y2, uint clr, uint* pxls,
            int w, int h, BlendMode blendMode)
        {
            // If the x-value is outside of the image, then we don't draw anything
            if (x < 0 || x >= w)
            {
                return;
            }

            // Make sure y1 is the lesser and y2 the greater
            if (y1 > y2)
            {
                int temp = y1;
                y1 = y2;
                y2 = temp;
            }

            // If the greater of the two y-values is less than 0, then there's nothing to draw. Also, if the lesser 
            // of the two y-values is greater than or equal to the image height then there is nothing to draw.
            if (y2 < 0 || y1 >= h)
            {
                return;
            }

            // Make sure the y-values are within the image
            if (y1 < 0)
            {
                y1 = 0;
            }
            if (y2 >= h)
            {
                y2 = h - 1;
            }

            uint* p = &pxls[y1 * w + x];
            int pCount = y2 - y1;

            if (BlendMode.None == blendMode)
            {
                while (pCount >= 0)
                {
                    // Draw the pixel
                    *p = clr;
                    p += w;
                    pCount--;
                }
            }
            else
            {
                while (pCount >= 0)
                {
                    // Draw the pixel with blending
                    BlendComposite(clr, p);
                    p += w;
                    pCount--;
                }
            }
        }

        public unsafe void FillCircle(SimpleImage img, uint clr, float circleCenterX, 
            float circleCenterY, float circleRadius)
        {
            FillCircle(img, clr, new Circle2D(circleCenterX, circleCenterY, circleRadius));
        }

        public unsafe void FillCircle(SimpleImage img, uint clr, Circle2D circle)
        {
            // Optimization: For composite-blending, we don't have do draw anything if the
            // drawing color has an alpha value of zero
            if (0 == (clr & 0xFF000000))
            {
                return;
            }

            // Soften radius (hard-coded for now)
            float sr = 2.0f;

            // Compute the inclusive y-starting point
            int ystart = (int)Math.Floor(circle.MinY);
            // Compute the inclusive y-ending point
            int yend = (int)Math.Ceiling(circle.MaxY);

            // Do a quick check to see if we're completely off the image
            if (yend < 0 || ystart >= img.Height)
            {
                return;
            }

            // Constrain y-values, if needed
            if (ystart < 0)
            {
                ystart = 0;
            }
            if (yend > img.Height - 1)
            {
                yend = img.Height - 1;
            }

            // Compute a float alpha value in the range [0.0, 1.0]
            float alpha = (float)(clr >> 24) / 255.0f;

            // Loop through rows of pixels
            for (; ystart <= yend; ystart++)
            {
                // Compute intersections for this row
                float minx, maxx;
                circle.HLineIntersection((float)ystart, out minx, out maxx);

                // Skip to the next row if there was no intersection
                if (float.IsNaN(minx) || float.IsNaN(maxx))
                {
                    continue;
                }

                // Compute the starting point on the left edge of the circle
                int xLeft = (int)Math.Floor(minx);
                // Skip this row if it's off the image
                if (xLeft >= img.Width)
                {
                    continue;
                }
                // Constrain if necessary
                if (xLeft < 0)
                {
                    xLeft = 0;
                }

                // Now compute the starting point on the right edge of the circle
                int xRight = (int)Math.Ceiling(maxx);
                // Skip if we're completely off the image
                if (xRight < 0)
                {
                    continue;
                }
                // Constrain, if necessary
                if (xRight >= img.Width)
                {
                    xRight = img.Width - 1;
                }

                // Make a pointer to this row of pixels
                uint* row = &img.Pixels[ystart * img.Width + xLeft];

                // Draw from the left edge until we hit a point where we're no longer within 
                // the softening radius distance from the edge
                while (xLeft <= xRight)
                {
                    float dist = circle.DistanceFromCenter((float)xLeft, (float)ystart);
                    if (dist >= circle.Radius)
                    {
                        // Advance to next pixel
                        row++;
                        xLeft++;
                        continue;
                    }
                    if (dist <= circle.Radius - sr)
                    {
                        break;
                    }

                    // Compute the alpha value
                    float newAFloat = (circle.Radius - dist) / sr * alpha * 255.0f;
                    // Blend
                    BlendComposite((clr & 0x00FFffFF) | (((uint)newAFloat) << 24), row);

                    // Advance to next pixel
                    row++;
                    xLeft++;

                    // See if we end up heading off the right edge of the image
                    if (xLeft >= img.Width)
                    {
                        break;
                    }
                }

                // If we drew pixels until we went off the image, then are done with this row
                if (xLeft >= img.Width)
                {
                    continue;
                }

                // It's possible, depending on the softening radius and which part of the circle we're
                // rendering, that the entire row has already been processed. Remember that xLeft at 
                // this point is one to the right of the last pixel that was processed.
                if (xRight < xLeft)
                {
                    continue;
                }

                // Set the pointer to where we're starting
                row = &img.Pixels[ystart * img.Width + xRight];

                // Draw from the right edge until we hit a point where we're:
                //  a) no longer within the softening radius distance from the edge
                //  b) we're off the image (xRight < 0)
                //  or
                //  c) we're less than xLeft
                while (xRight >= 0)
                {
                    float dist = circle.DistanceFromCenter((float)xRight, (float)ystart);
                    if (dist >= circle.Radius)
                    {
                        // Advance to next pixel
                        row--;
                        xRight--;
                        continue;
                    }
                    if (dist <= circle.Radius - sr)
                    {
                        break;
                    }

                    // Compute the alpha value
                    float newAFloat = (circle.Radius - dist) / sr * alpha * 255.0f;
                    // Blend
                    BlendComposite((clr & 0x00FFffFF) | (((uint)newAFloat) << 24), row);

                    // Advance to next pixel
                    row--;
                    xRight--;

                    if (xRight < xLeft)
                    {
                        break;
                    }
                }

                // At this point we've processed from the left and right edges of the circle 
                // on this row and have taken care of all pixels that need edge softening. All 
                // that remains is filling pixels in between where we left off on the left and 
                // right.
                if (xRight >= xLeft)
                {
                    DrawHorizontalLine(xLeft, xRight, ystart, clr, img.Pixels, img.Width, img.Height,
                        BlendMode.AlphaComposite);
                }
            }
        }

        /// <summary>
        /// Fills the entire image with the specified pixel color. This function directly copies the 
        /// color to every pixel in the image. No blending is performed.
        /// </summary>
        /// <param name="pxls">Pointer to image pixel data</param>
        /// <param name="clr">Color to fill with</param>
        /// <param name="imageWidth">Width of the image data</param>
        /// <param name="imageHeight">Height of the image data</param>
        public static unsafe void FillImage(uint* pxls, uint clr, int imageWidth, int imageHeight)
        {
            int pxlCount = imageWidth * imageHeight;
            while (pxlCount-- > 0)
            {
                *pxls++ = clr;
            }
        }

        public unsafe void FillRectangle(int x, int y, int rectangleWidth, int rectangleHeight,
            uint* pxls, uint clr, int imageWidth, int imageHeight, BlendMode blendMode)
        {
            if (BlendMode.None == blendMode || 0xFF000000 == (clr & 0xFF000000))
            {
                FillRectangle_NoBlend(x, y, rectangleWidth, rectangleHeight, pxls, clr, imageWidth, imageHeight);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected static unsafe void FillRectangle_NoBlend(int x, int y, int rectangleWidth, 
            int rectangleHeight, uint* pxls, uint clr, int imageWidth, int imageHeight)
        {
            // See if we're completely outside of the image
            if (x >= imageWidth || y >= imageHeight || 
                x + rectangleWidth <= 0 || y + rectangleHeight <= 0)
            {
                return;
            }

            // Clip
            if (x < 0)
            {
                rectangleWidth += x;
                x = 0;
            }
            if (x + rectangleWidth > imageWidth)
            {
                rectangleWidth = imageWidth - x;
            }
            if (y < 0)
            {
                rectangleHeight += y;
                y = 0;
            }
            if (y + rectangleHeight > imageHeight)
            {
                rectangleHeight = imageHeight - y;
            }

            uint* p;
            while (rectangleHeight > 0)
            {
                // Set the pointer to where we want to start drawing pixels
                p = &pxls[y * imageWidth + x];

                for (int row=0; row < rectangleWidth; row++)
                {
                    *p++ = clr;
                }

                rectangleHeight--;
                y++;
            }
        }

        private unsafe void FillStadiums(SimpleImage img, uint clr, Stadium[] stads,
            float sr)
        {
            float clrAlphaFloat = (float)(clr >> 24) / 255f;

            // Find the start and end (inclusive) Y-values
            float minyFloat = float.MaxValue, maxyFloat = float.MinValue;
            foreach (Stadium sTemp in stads)
            {
                if (sTemp.MaxY > maxyFloat)
                {
                    maxyFloat = sTemp.MaxY;
                }
                if (sTemp.MinY < minyFloat)
                {
                    minyFloat = sTemp.MinY;
                }
            }
            int miny = (int)Math.Floor(minyFloat);
            if (miny >= img.Height)
            {
                // Content is completely off the image
                return;
            }
            miny = Math.Max(miny, 0);
            int maxy = Math.Min((int)Math.Ceiling(maxyFloat), img.Height - 1);

            // We need lists to keep track of minimum and maximum intersections per row
            List<float> mins = new List<float>();
            List<float> maxes = new List<float>();

            List<float> finals = new List<float>();

            // Loop through rows of pixels
            while (miny <= maxy)
            {
                // Clear intersection lists
                mins.Clear();
                maxes.Clear();
                finals.Clear();

                // Get all intersections for this row
                foreach (Stadium sTemp in stads)
                {
                    float tempMin, tempMax;
                    sTemp.HLineIntersection((float)miny, out tempMin, out tempMax);
                    if (!float.IsNaN(tempMin) && !float.IsNaN(tempMax))
                    {
                        mins.Add(tempMin);
                        maxes.Add(tempMax);
                    }
                }

                if (0 == mins.Count || 0 == maxes.Count)
                {
                    miny++;
                    continue;
                }

                // Sort the intersection lists
                mins.Sort();
                maxes.Sort();

                // Debug: plot a pixel at each intersection point
                //foreach (float f in mins)
                //{
                //    *img.GetRowAtX(miny, (int)f) = 0xFFFF0000;
                //}
                //foreach (float f in maxes)
                //{
                //    *img.GetRowAtX(miny, (int)f) = 0xFF00FF00;
                //}

                bool rowIsComplex = false;
                // When > 0, we're inside, otherwise we're outside the object. This gets incremented 
                // when we hit a value in the "mins" list and decremented when we hit a value in 
                // the "maxes" list.
                int in_out = 0;
                while (mins.Count > 0 || maxes.Count > 0)
                {
                    if (0 == mins.Count)
                    {
                        finals.Add(maxes[maxes.Count - 1]);
                        break;
                    }
                    else if (0 == maxes.Count)
                    {
                        // Should never occur
                        break;
                    }

                    if (mins[0] < maxes[0])
                    {
                        if (0 == in_out)
                        {
                            // 0 == in_out implies that we were outside and now we're entering
                            finals.Add(mins[0]);
                        }
                        else
                        {
                            // This means that in_out is > 0 and we've entered 2 stadiums (or 
                            // more) at once. This implies a "complex" row, which gets rendered 
                            // without certain optimizations.
                            rowIsComplex = true;
                        }
                        mins.RemoveAt(0);
                        in_out++;
                    }
                    else
                    {
                        in_out--;
                        if (0 == in_out)
                        {
                            // This is an exit point
                            finals.Add(maxes[0]);
                        }
                        maxes.RemoveAt(0);
                    }
                }

                int xstart, xend;
                
                // The "finals" list now contains pairs of enter-exit x-values
                for (int i = 0; i < finals.Count - 1; i += 2)
                {
                    xstart = Math.Max((int)Math.Floor(finals[i]), 0);
                    if (xstart >= img.Width)
                    {
                        continue;
                    }
                    xend = Math.Min((int)Math.Ceiling(finals[i + 1]), img.Width - 1);
                    if (xend < 0)
                    {
                        continue;
                    }
                    uint* row = &img.Pixels[miny * img.Width + xstart];

                    // Start by coming in from the left edge until we are no longer in the edge 
                    // softening section
                    while (xstart <= xend)
                    {
                        float minDist = float.MaxValue;
                        Stadium minDistStadium = stads[0];
                        foreach (Stadium stadium in stads)
                        {
                            float dist = stadium.Axis.DistanceU(new Vector2D((float)xstart, (float)miny));
                            if (dist < minDist)
                            {
                                minDist = dist;
                                minDistStadium = stadium;
                            }
                        }

                        if (minDist < minDistStadium.Radius)
                        {
                            if (minDist > minDistStadium.Radius - sr)
                            {
                                float alpha = (minDistStadium.Radius - minDist) / sr;
                                alpha *= clrAlphaFloat;
                                uint newAlpha = (uint)(alpha * 255f);
                                newAlpha <<= 24;
                                BlendComposite((clr & 0x00FFffFF) | newAlpha, row);
                            }
                            else
                            {
                                if (rowIsComplex)
                                {
                                    BlendComposite(clr, row);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        xstart++;
                        row++;
                    }

                    // It's possible that we completed this section in the above loop
                    if (xstart > xend)
                    {
                        continue;
                    }

                    // Get a pointer to pixels on the right edge
                    row = img.GetRowAtX(miny, xend);

                    // Now come in from the right edge until we are no longer in the edge 
                    // softening section
                    while (xend >= xstart)
                    {
                        float minDist = float.MaxValue;
                        Stadium minDistStadium = stads[0];
                        foreach (Stadium stadium in stads)
                        {
                            float dist = stadium.Axis.DistanceU(new Vector2D((float)xend, (float)miny));
                            if (dist < minDist)
                            {
                                minDist = dist;
                                minDistStadium = stadium;
                            }
                        }

                        if (minDist < minDistStadium.Radius)
                        {
                            if (minDist > minDistStadium.Radius - sr)
                            {
                                float alpha = (minDistStadium.Radius - minDist) / sr;
                                alpha *= clrAlphaFloat;
                                uint newAlpha = (uint)(alpha * 255f);
                                newAlpha <<= 24;
                                BlendComposite((clr & 0x00FFffFF) | newAlpha, row);
                            }
                            else
                            {
                                break;
                            }
                        }

                        xend--;
                        row--;
                    }

                    // If there are remaining pixels between xstart and xend then render them
                    if (xstart <= xend)
                    {
                        DrawHorizontalLine(xstart, xend, miny, clr, img.Pixels, img.Width, img.Height,
                            BlendMode.AlphaComposite);
                    }
                }

                miny++;
            }
        }

        public unsafe void FrameCircle(Circle2D circle, float frameThickness, uint clr,
            uint* pxls, int imgWidth, int imgHeight)
        {
            FrameCircle_Composite(circle, frameThickness, clr, pxls, imgWidth, imgHeight);
        }

        protected unsafe void FrameCircle_Composite(Circle2D circle, float frameThickness,
            uint clr, uint* pxls, int w, int h)
        {
            // Softening radius (hard-coded for now)
            float sr = 2.0f;
            
            // Handle case where the frame is so thick that we just fill the whole circle
            if (frameThickness / 2.0f >= circle.Radius + sr)
            {
                Circle2D newCircle = new Circle2D(circle.Center, circle.Radius + frameThickness / 2f);
                FillCircle(new SimpleImage(w, h, pxls), clr, circle);
                return;
            }

            // First make the outer and inner circle objects
            Circle2D outer = new Circle2D(circle.Center, circle.Radius + frameThickness / 2.0f);
            Circle2D inner = new Circle2D(circle.Center, circle.Radius - frameThickness / 2.0f);

            int y = (int)Math.Max(0.0f, Math.Floor(outer.MinY));
            for (; y < h; y++)
            {
                // Compute the x-intersection points for this y-value
                float outMin, outMax, inMin, inMax;
                outer.HLineIntersection((float)y, out outMin, out outMax);
                inner.HLineIntersection((float)y, out inMin, out inMax);

                // If there's not a valid intersection, then skip this row. Note that there 
                // could be an intersection with the outer circle and not the inner, but not 
                // the other way around.
                if (float.IsNaN(outMin) || float.IsNaN(outMax))
                {
                    continue;
                }

                float[] ranges;
                if (float.IsNaN(inMin) || float.IsNaN(inMax))
                {
                    // This means there's an intersection with the outer circle and not the 
                    // inner. In this case we only have one continuous range of pixels to 
                    // process for this row.

                    // Check to see if we're off the image
                    if (outMax < 0.0f || outMin > (float)(w - 1))
                    {
                        continue;
                    }

                    ranges = new float[] { outMin, outMax };
                }
                else
                {
                    ranges = new float[] { outMin, inMin, inMax, outMax };
                }

                // Process the pixel ranges
                for (int i = 0; i < ranges.Length; i += 2)
                {
                    int x = (int)Math.Max(0.0f, Math.Floor(ranges[i]));
                    int maxx = (int)Math.Min((float)(w - 1), Math.Ceiling(ranges[i + 1]));

                    // Make a pointer to the pixel where we start rendering
                    uint* row = &pxls[y * w + x];

                    while (x <= maxx)
                    {
                        // Compute the distance from this pixel to the edge of the circle and
                        // soften if needed
                        float dist = circle.DistanceFromCenter(new Vector2D(x, y));
                        if (dist >= outer.Radius || dist <= inner.Radius)
                        {
                            x++;
                            row++;
                            continue;
                        }
                        if ((outer.Radius - dist) <= sr)
                        {
                            float newA = ((outer.Radius - dist) / sr) * 255.0f;
                            BlendComposite((clr & 0x00FFffFF) | (((uint)newA) << 24), row);
                        }
                        else if ((dist - inner.Radius) <= sr)
                        {
                            float newA = ((dist - inner.Radius) / sr) * 255.0f;
                            BlendComposite((clr & 0x00FFffFF) | (((uint)newA) << 24), row);
                        }
                        else
                        {
                            BlendComposite(clr, row);
                        }

                        x++;
                        row++;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the lookup functions that are used by many of the drawing functions. 
        /// This must be called before any methods in this class are called.
        /// </summary>
        private static void Initialize()
        {
            // If the static tables have already been allocated and initialized, just return
            if (null != s_predivs)
            {
                return;
            }

            // Allocate the lookup tables
            s_alphaLookup = new byte[65536];
            s_premuls = new byte[65536];
            s_predivs = new byte[65536];
            // Make sure they allocated OK
            if (null == s_alphaLookup || s_premuls == null || s_predivs == null)
            {
                throw new OutOfMemoryException(
                    "Blending lookup tables could not be allocated. An out of memory exception " +
                    "occured when trying to allocate 192 kB of memory.");
            }

            // Initialize the lookup tables
            for (uint clr = 0; clr < 256; clr++)
            {
                for (uint a = 0; a < 256; a++)
                {
                    // Compute the index (used for all tables)
                    uint index = (clr | (a << 8));

                    uint val = clr * a / 255;
                    s_premuls[index] = (byte)val;

                    float fSrcA = (float)a / 255.0f;
                    float fDestA = (float)clr / 255.0f;
                    float fNewAlpha = (fSrcA + fDestA * (1.0f - fSrcA));
                    s_alphaLookup[index] = (byte)(fNewAlpha * 255.0f);

                    // Predivs table: clr * 255 / a
                    if (a == 0)
                    {
                        s_predivs[index] = 0;
                    }
                    else
                    {
                        s_predivs[index] = (byte)(clr * 255 / a);
                    }
                }
            }
        }

        private static void Swap(ref int i1, ref int i2)
        {
            int temp = i1;
            i1 = i2;
            i2 = temp;
        }
    }
}