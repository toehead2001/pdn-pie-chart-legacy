﻿// Author:          Evan Olds
// Creation Date:   January 6, 2012

using System;
using System.Drawing;
using System.Reflection;

namespace PaintDotNet.Effects
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return ((AssemblyCopyrightAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
            }
        }
        public string Copyright
        {
            get
            {
                return ((AssemblyDescriptionAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description;
            }
        }

        public string DisplayName
        {
            get
            {
                return ((AssemblyProductAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("http://www.getpaint.net/redirect/plugins.html");
            }
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Pie Chart")]


	internal class RenderPieChart : Effect<PieChartConfigToken>
    {
        private EOFC.Drawing32 m_d32 = new EOFC.Drawing32();
        
        private Surface m_surface = null;
        
        public RenderPieChart()
            : base("Pie Chart", new Bitmap(typeof(RenderPieChart), "PieChart.png"),
            SubmenuNames.Render, EffectFlags.Configurable)
        {

        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            return new PieChartConfigDialog();
        }
        
        /// <summary>
        /// Primary rendering function. Since the pie chart is pre-rendered to a surface, this function 
        /// just copies over that rendered image.
        /// </summary>
        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            while (length > 0)
            {
                Rectangle r = renderRects[startIndex];

                for (int y = r.Top; y < r.Bottom; y++)
                {
                    for (int x = r.Left; x < r.Right; x++)
                    {
                        unsafe
                        {
                            *DstArgs.Surface.GetPointAddressUnchecked(x, y) =
                                m_surface.GetPointUnchecked(x, y);
                        }
                    }
                }

                startIndex++;
                length--;
            }
        }

        protected override void OnSetRenderInfo(PieChartConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            if (null != m_surface)
            {
                m_surface.Dispose();
            }

            // Allocate the surface to render to
            m_surface = new Surface(srcArgs.Size);
            
            // Render the pie chart to it
            unsafe
            {
                if (0 == newToken.Data.Count)
                {
                    m_surface.ClearWithCheckerboardPattern();
                }
                else
                {
                    m_d32.DrawPieChart((uint*)m_surface.GetRowAddressUnchecked(0),
                        srcArgs.Width, srcArgs.Height, newToken.Data.ToArray());
                }
            }
        }
    }
}
