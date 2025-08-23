using UnityEngine;
using UnityEngine.UIElements;

namespace Empress.UITK {

    public partial class VirtuaStick : VisualElement {

        /// <summary>
        /// Diameter of the stick area in pixels
        /// </summary>
        public float StickPixels {
            get => m_StickPixels;
            set {
                if (StyleSource != VisualStyleSource.Procedural)
                    return;

                m_StickPixels = value >= 0f ? value : 0f;
                m_Stick.style.width = m_StickPixels;
                m_Stick.style.height = m_StickPixels;
            }
        }
        private float m_StickPixels = 300f;

        /// <summary>
        /// Handle size as percentage of stick size
        /// </summary>
        public float HandleSizePercent {
            get => m_HandleSize;
            set {
                if (StyleSource != VisualStyleSource.Procedural)
                    return;

                m_HandleSize = Mathf.Clamp(value, 0, 100);
                m_Handle.style.width = Length.Percent(m_HandleSize);
                m_Handle.style.height = Length.Percent(m_HandleSize);
            }
        }
        public float m_HandleSize = 50f;

        #region D-PAD (TODO)

        // TODO

        #endregion

        public ProceduralStyleSettings stickStyleSettings;
        public ProceduralStyleSettings handleStyleSettings;
        public ProceduralStyleSettings arrowContentStyleSettings;
        public ProceduralStyleSettings arrowStyleSettings;
    }
}
