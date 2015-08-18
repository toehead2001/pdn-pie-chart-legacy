// Author:          Evan Olds
// Creation Date:   January 6, 2012

using System.Collections.Generic;

namespace PaintDotNet.Effects
{
    class PieChartConfigToken : EffectConfigToken
    {
        private List<EOFC.Drawing32.PieChartSection> m_sections = new List<EOFC.Drawing32.PieChartSection>();

        /// <summary>
        /// Initializes the configuration token with empty dictionaries
        /// </summary>
        public PieChartConfigToken() : base()
        {

        }

        private PieChartConfigToken(List<EOFC.Drawing32.PieChartSection> sections)
        {
            m_sections = new List<EOFC.Drawing32.PieChartSection>(sections);
        }
        
        public override object Clone()
        {
            return new PieChartConfigToken(m_sections);
        }

        public List<EOFC.Drawing32.PieChartSection> Data
        {
            get
            {
                return m_sections;
            }
        }
    }
}
