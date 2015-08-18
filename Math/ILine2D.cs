// Author:          Evan Olds
// Creation Date:   August 30, 2009

namespace EOFC
{
    /// <summary>
    /// Interface to represent a line in 2D space. The line could be in point-slope form, 
    /// slope-intercept form, standard form, general form, etc. Any class or struct that 
    /// implements ILine2D must be a line in 2D space, altough there need not be a guarantee 
    /// about whether or not its form can represent ANY line in 2D.
    /// </summary>
    internal interface ILine2D
    {
        bool IsDegenerate
        {
            get;
        }

        bool IsHorizontal
        {
            get;
        }
        
        bool IsPointOnLine(EOFC.Vector2D pt);

        bool IsVertical
        {
            get;
        }
    }
}