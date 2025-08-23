using System;

namespace Empress.UITK {

    /// <summary>
    /// Defines the allowed axis of movement for input controls.
    /// </summary>
    [Serializable]
    public enum InputAxisConstraint {
        /// <summary>
        /// No axis restriction (movement allowed in all directions)
        /// </summary>
        Free,

        /// <summary>
        /// Movement constrained to the X-axis (left/right only)
        /// </summary>
        Horizontal,

        /// <summary>
        /// Movement constrained to the Y-axis (up/down only)
        /// </summary>
        Vertical
    }
}
