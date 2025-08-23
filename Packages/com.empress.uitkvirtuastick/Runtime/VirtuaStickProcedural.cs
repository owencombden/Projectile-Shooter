using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Empress.UITK {

    /// <summary>
    /// 
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIDocument))]
    public class VirtuaStickProcedural : MonoBehaviour {

        [Header("Input Area")]
        [Tooltip("")]
        public CornerAlign align = CornerAlign.BottomLeft;

        [Tooltip("Stick area dimensions as percentage of screen")]
        public Vector2 stickArea = new(50f, 25f);

        [Tooltip("Additional position offset from calculated position")]
        public Vector2 stickOffset;

        [Min(0f), Tooltip("Base size of the stick in pixels")]
        public float stickPixels = 300f;

        [Range(0f, 100f), Tooltip("Handle size as percentage of stick size")]
        public float handleSizePercent = 50f;

        [Header("Stick Behavior")]
        [Tooltip("Allowed movement axis constraints")]
        public InputAxisConstraint axisConstraint;

        [Tooltip("If true, stick follows initial touch position")]
        public bool dynamic = true;

        [Tooltip("")]
        public bool showStickArea;

        [Tooltip("")]
        public bool debugInfo;

        [Tooltip("")]
        public CornerAlign debugAlign = CornerAlign.TopLeft;

        [Tooltip("")]
        public float debugInfoSize = 22f;

        [Tooltip("Maximum movement radius for the handle")]
        public float handleMoveRadius = 55f;

        [Header("Visual Feedback")]
        [Tooltip("Show directional arrow when stick is moved")]
        public bool directionalArrow = true;

        [Tooltip("Smoothing factor for arrow movement animation")]
        public float directionalArrowSmooth = 0.15f;

        [Header("Visual Style Settings")]
        [Tooltip("Style settings for stick background")]
        public ProceduralStyleSettings stick = new() {
            color = new Color(.1f, .1f, .1f, .5f),
            borderColor = new(new Color(0.85f, 0.82f, 0.9f, 0.8f)),
            border = new(20f),
            radius = new(300f)
        };

        [Tooltip("Style settings for stick handle")]
        public ProceduralStyleSettings handle = new() {
            color = new Color(.25f, .25f, .25f, .5f),
            borderColor = new(new Color(.75f, .75f, .75f, 1f)),
            border = new(10f),
            radius = new(300f)
        };

        [Tooltip("Style settings for arrow container")]
        public ProceduralStyleSettings arrowContent = new() {
            borderColor = new(new Color(0.95f, 0.8f, 0.95f, 1f)),
            border = new() { right = 20f },
            radius = new(300f),
            margin = new(-18.5f) { right = -35.5f }
        };

        [Tooltip("Style settings for directional arrow")]
        public ProceduralStyleSettings arrow = new() {
            backgroundTint = new Color(0.95f, 0.8f, 0.95f, 1f),
            margin = new() { right = 280 }
        };

        // =====================================================================
        // UI References
        // =====================================================================

        /// <summary>
        /// 
        /// </summary>
        public UIDocument UIDocument { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public VisualElement Root => UIDocument ? UIDocument.rootVisualElement : null;

        /// <summary>
        /// 
        /// </summary>
        public VirtuaStick VirtuaStick { get; protected set; }

        // =====================================================================

        protected virtual void OnEnable() => OnValidate();

        protected virtual void OnDisable() {
            if (VirtuaStick != null)
                VirtuaStick.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Validates and updates all stick properties and visual styles
        /// </summary>
        public virtual void OnValidate() {
            if (!this) return;
            if (!UIDocument) UIDocument = GetComponent<UIDocument>();

            if (!UIDocument || Root == null) {
#if UNITY_EDITOR
                if (Application.isEditor)
                    EditorApplication.delayCall += OnValidate;
#endif
                return;
            }

            if (VirtuaStick != null && VirtuaStick.parent != Root) {
                VirtuaStick?.RemoveFromHierarchy();
                VirtuaStick = null;
            }

            if (VirtuaStick == null)
                Root.Add(VirtuaStick = new VirtuaStick {
                    name = name,
                    StyleSource = VisualStyleSource.Procedural
                });

            VirtuaStick.style.display = DisplayStyle.Flex;
            VirtuaStick.ShowStickArea = showStickArea;
            VirtuaStick.DebugInfo = debugInfo;
            VirtuaStick.DebugInfoSize = debugInfoSize;
            VirtuaStick.DebugAlign = debugAlign;

            switch (align) {
                case CornerAlign.Center:
                    Root.style.justifyContent = Justify.Center;
                    VirtuaStick.style.alignSelf = Align.Center;
                    break;
                case CornerAlign.TopLeft:
                    Root.style.justifyContent = Justify.FlexStart;
                    VirtuaStick.style.alignSelf = Align.FlexStart;
                    break;
                case CornerAlign.TopRight:
                    Root.style.justifyContent = Justify.FlexStart;
                    VirtuaStick.style.alignSelf = Align.FlexEnd;
                    break;
                case CornerAlign.BottomLeft:
                    Root.style.justifyContent = Justify.FlexEnd;
                    VirtuaStick.style.alignSelf = Align.FlexStart;
                    break;
                case CornerAlign.BottomRight:
                    Root.style.justifyContent = Justify.FlexEnd;
                    VirtuaStick.style.alignSelf = Align.FlexEnd;
                    break;
            }
            
            stickArea.x = Mathf.Clamp(stickArea.x, 0f, 100f);
            stickArea.y = Mathf.Clamp(stickArea.y, 0f, 100f);

            VirtuaStick.style.width = Length.Percent(stickArea.x);
            VirtuaStick.style.height = Length.Percent(stickArea.y);

            VirtuaStick.Offset = stickOffset;

            // Update behavior properties
            VirtuaStick.AxisConstraint = axisConstraint;
            VirtuaStick.Dynamic = dynamic;
            VirtuaStick.StickPixels = stickPixels;
            VirtuaStick.HandleSizePercent = handleSizePercent;
            VirtuaStick.HandleMoveRadius = handleMoveRadius;

            // Update visual feedback
            VirtuaStick.DirectionalArrow = directionalArrow;
            VirtuaStick.DirectionalArrowSmooth = directionalArrowSmooth;

            // Apply all style settings
            var stickElement = VirtuaStick.stickStyleSettings.element;
            var handleElement = VirtuaStick.handleStyleSettings.element;
            var arrowContentElement = VirtuaStick.arrowContentStyleSettings.element;
            var arrowElement = VirtuaStick.arrowStyleSettings.element;

            VirtuaStick.stickStyleSettings = stick;
            VirtuaStick.handleStyleSettings = handle;
            VirtuaStick.arrowContentStyleSettings = arrowContent;
            arrow.background = VirtuaStick.arrowStyleSettings.background;
            VirtuaStick.arrowStyleSettings = arrow;

            VirtuaStick.stickStyleSettings.Update(VirtuaStick, stickElement);
            VirtuaStick.handleStyleSettings.Update(VirtuaStick, handleElement);
            VirtuaStick.arrowContentStyleSettings.Update(VirtuaStick, arrowContentElement);
            VirtuaStick.arrowStyleSettings.Update(VirtuaStick, arrowElement);
            arrowElement.style.translate = new Translate(Length.Percent(arrow.margin.right), 0);
        }
    }
}
