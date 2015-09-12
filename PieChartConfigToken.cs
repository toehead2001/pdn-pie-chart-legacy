// Author:          Evan Olds
// Creation Date:   January 6, 2012

using System.Collections.Generic;

namespace PaintDotNet.Effects
{
    class PieChartConfigToken : EffectConfigToken
    {
        private List<EOFC.Drawing32.PieChartSection> m_sections = new List<EOFC.Drawing32.PieChartSection>();
        double t_angle;
        uint t_outline;

        /// <summary>
        /// Initializes the configuration token with empty dictionaries
        /// </summary>
        public PieChartConfigToken() : base()
        {
            t_angle = 0;
            t_outline = 0xFF000000;
        }

        private PieChartConfigToken(List<EOFC.Drawing32.PieChartSection> sections, double angle, uint outline)
        {
            m_sections = new List<EOFC.Drawing32.PieChartSection>(sections);
            t_angle = angle;
            t_outline = outline;
        }
        
        public override object Clone()
        {
            return new PieChartConfigToken(m_sections, t_angle, t_outline);
        }

        public List<EOFC.Drawing32.PieChartSection> Data
        {
            get
            {
                return m_sections;
            }
        }
        public double Angle
        {
            get
            {
                return t_angle;
            }
            set
            {
                t_angle = value;
            }
        }
        public uint Outline
        {
            get
            {
                return t_outline;
            }
            set
            {
                t_outline = value;
            }
        }
    }
}
