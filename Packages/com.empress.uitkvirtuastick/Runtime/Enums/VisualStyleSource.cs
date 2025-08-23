using System;

namespace Empress.UITK {

    /// <summary>
    /// Defines the source of visual styling for UI elements
    /// </summary>
    [Serializable]
    public enum VisualStyleSource {
        /// <summary>
        /// Style is controlled programmatically by the component's properties
        /// (Default visual appearance defined in code)
        /// </summary>
        Procedural,

        /// <summary>
        /// Style is fully controlled by Unity Style Sheets (USS)
        /// (Visual appearance defined in UI Toolkit stylesheets)
        /// </summary>
        USS,

        /// <summary>
        /// Hybrid approach combining procedural properties with USS overrides
        /// </summary>
        Hybrid
    }
}