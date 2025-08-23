using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Empress.UITK.VirtuaStickExtensions;

namespace Empress.UITK {

    /// <summary>
    /// Custom UI Toolkit joystick control for touch input.
    /// Provides directional input through touch/drag interactions.
    /// </summary>
    /// <remarks>
    /// Features:
    /// - Configurable axis constraints (Free/Horizontal/Vertical)
    /// - Dynamic or fixed positioning
    /// - Visual feedback with directional arrow
    /// - USS and procedural styling support
    /// </remarks>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
    [UxmlElement(nameof(VirtuaStick))]
#endif
    public partial class VirtuaStick : VisualElement {

        #region CONSTANTS

        const string k_UssStickClass = "vs-stick";
        const string k_UssHandleClass = "vs-handle";
        const string k_UssDirectionalArrowClass = "vs-arrow";

        #endregion

        #region SERIALIZED

        /// <summary>
        /// Defines the source of visual styling for this control.
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public VisualStyleSource StyleSource { get; set; } = VisualStyleSource.Procedural;

        /// <summary>
        /// Constrains the joystick's movement to specific axes (None, Horizontal, or Vertical)
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public InputAxisConstraint AxisConstraint { get; set; } = InputAxisConstraint.Free;

        /// <summary>
        /// If true, the joystick dynamically repositions based on touch input. 
        /// If false, it remains fixed in place.
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public bool Dynamic { get; set; } = true;
        
        /// <summary>
        /// Initial position offset for the stick (in pixels relative to parent)
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public Vector2 Offset {
#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
            get => new(m_OffsetX, m_OffsetY);
#else
            get => m_Offset;
#endif
            set {
#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
                m_OffsetX = value.x;
                m_OffsetY = value.y;
#else
                m_Offset = value;
#endif
                m_Stick.transform.position = value;
            }
        }
        
#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
        protected float OffsetX {
            get => m_OffsetX;
            set => Offset = new Vector2(value, m_OffsetY);
        }
        protected float m_OffsetX;

        protected float OffsetY {
            get => m_OffsetY;
            set => Offset = new Vector2(m_OffsetX, value);
        }
        protected float m_OffsetY;
#else
        public Vector2 m_Offset;
#endif

        /// <summary>
        /// How far the handle can move from center (as % of stick radius, 0-100%)
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public float HandleMoveRadius {
            get => m_HandleMoveRadius;
            set => m_HandleMoveRadius = Mathf.Clamp(value, 0f, 100f);
        }
        private float m_HandleMoveRadius = 55f;

        /// <summary>
        /// Controls whether the directional arrow (visual direction indicator) is visible.
        /// When enabled, shows an arrow that rotates to match the stick's input direction.
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public bool DirectionalArrow {
            get => m_DirectionalArrow;
            set {
                m_DirectionalArrow = value;
                m_StickContent.style.display = value && (IsDragging || !Application.isPlaying)  ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        protected bool m_DirectionalArrow = true;

        /// <summary>
        /// Controls the smoothing applied to the directional arrow's rotation movement.
        /// Lower values result in smoother but slower rotation (typical range: 0.01-1.0).
        /// Note: Value is clamped between 0.01 and 1.0 automatically.
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public float DirectionalArrowSmooth { get; set; } = 0.15f;

        /// <summary>
        /// 
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public bool ShowStickArea {
            get => m_ShowStickArea;
            set {
                m_ShowStickArea = value;
                m_DebugStickArea.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        protected bool m_ShowStickArea;

        /// <summary>
        /// 
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public bool DebugInfo {
            get => m_DebugInfo;
            set {
                m_DebugInfo = value;
                m_DebugInfoLabel.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;

                if (value && !m_DebugScheduleStarted) {
                    m_DebugScheduleStarted = true;
                    schedule.Execute(UpdateDebugInfo).Every(50L).Until(() => !m_DebugInfo);
                }
                else
                    m_DebugScheduleStarted = false;
            }
        }
        protected bool m_DebugInfo;
        protected bool m_DebugScheduleStarted;

        /// <summary>
        /// 
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public CornerAlign DebugAlign {
            get => m_DebugAlign;
            set {
                m_DebugAlign = value;

                switch (value) {
                    case CornerAlign.TopLeft:
                        m_DebugContainer.style.justifyContent = Justify.FlexStart;
                        m_DebugInfoLabel.style.alignSelf = Align.FlexStart;
                        break;
                    case CornerAlign.TopRight:
                        m_DebugContainer.style.justifyContent = Justify.FlexStart;
                        m_DebugInfoLabel.style.alignSelf = Align.FlexEnd;
                        break;
                    case CornerAlign.BottomLeft:
                        m_DebugContainer.style.justifyContent = Justify.FlexEnd;
                        m_DebugInfoLabel.style.alignSelf = Align.FlexStart;
                        break;
                    case CornerAlign.BottomRight:
                        m_DebugContainer.style.justifyContent = Justify.FlexEnd;
                        m_DebugInfoLabel.style.alignSelf = Align.FlexEnd;
                        break;
                }
            }
        }
        protected CornerAlign m_DebugAlign = CornerAlign.TopLeft;

        /// <summary>
        /// 
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public float DebugInfoSize {
            get => m_DebugInfoSize;
            set {
                m_DebugInfoSize = value;
                m_DebugInfoLabel.style.fontSize = value;
            }
        }
        protected float m_DebugInfoSize = 14f;

        #endregion // SERIALIZED

        #region PROPERTIES

        /// <summary>
        /// Normalized input vector (values between -1 and 1)
        /// </summary>
        public Vector2 Delta { get; protected set; }

        /// <summary>
        /// Triggered continuously while the stick is being moved, providing the normalized input direction.
        /// </summary>
        /// <remarks>
        /// Returns a Vector2 where:
        /// - x ranges from -1 (left) to 1 (right)
        /// - y ranges from -1 (down) to 1 (up)
        /// </remarks>
        public event Action<Vector2> OnDrag = delegate { };

        /// <summary>
        /// Triggered when the user releases the stick (end of interaction).
        /// </summary>
        public event Action OnEndDrag = delegate { };

        /// <summary>
        /// True when the user is actively dragging the stick handle.
        /// </summary>
        public bool IsDragging { get; protected set; }

        /// <summary>
        /// Container for custom child elements (e.g., additional UI overlays).
        /// Defaults to the arrow visual element.
        /// </summary>
        public override VisualElement contentContainer => m_ArrowContent;

        #endregion

        #region PRIVATE

        protected readonly VisualElement m_Stick;
        protected readonly VisualElement m_Handle;
        protected readonly VisualElement m_StickContent;
        protected readonly VisualElement m_ArrowContent;
        protected readonly VisualElement m_Arrow;

        // Debug elements
        protected readonly VisualElement m_DebugContainer;
        protected readonly VisualElement m_DebugStickArea;
        protected readonly Label m_DebugInfoLabel;
        protected readonly StringBuilder m_SB = new();

        protected Vector2 m_Direction;
        protected float m_CurrentArrowAngle;

        protected static Texture2D m_ArrowTexture;

        #endregion

        #region CONSTRUCTOR & SETUP

        /// <summary>
        /// Creates a new VirtuaStick control with default settings.
        /// </summary>
        /// <remarks>
        /// The stick consists of three main parts:
        /// - Base background (the static outer area)
        /// - Handle (the draggable inner circle)
        /// - Optional direction arrow (visual feedback)
        /// 
        /// By default configured with:
        /// - Centered position
        /// - 50% handle size relative to stick
        /// - Arrow hidden during gameplay
        /// </remarks>
        public VirtuaStick() {
            if (!m_ArrowTexture)
                m_ArrowTexture = Resources.Load<Texture2D>("UITK_VirtuaStick_Arrow");

            // Main container setup
            style.alignItems = Align.Center;
            style.justifyContent = Justify.SpaceAround;

            // Create stick base (the movable area)
            m_Stick = new VisualElement {
                name = "stick",
                transform = { position = Offset }
            };

            // Handle that users actually drag
            m_Handle = new VisualElement { name = "handle" };

            // Stick content
            m_StickContent = new VisualElement {
                name = "stick-content",
                style = {
                    display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex,
                    position = Position.Absolute, // Covers entire stick
                    // Full stretch:
                    top = Length.Percent(0),
                    bottom = Length.Percent(0),
                    left = Length.Percent(0),
                    right = Length.Percent(0),
                    alignItems = Align.Center,
                    justifyContent = Justify.Center
                },
            };

            // The actual arrow graphic
            m_ArrowContent = new VisualElement { name = "arrow-content" };

            // Procedural arrow element
            m_Arrow = new VisualElement { 
                name = "arrow",
                style = {
                    position = Position.Absolute,
                    width = Length.Percent(20),
                    height = Length.Percent(10),
                    //translate = new Translate(Length.Percent(530), 0), // Adjust for arrow offset
                    rotate = new Rotate(90f), // Rotate to point right
                }
            };

            // Debugging elements
            m_DebugContainer = new VisualElement { 
                name = "debug-container",
                style = {
                    position = Position.Absolute,
                    top = Length.Percent(0),
                    bottom = Length.Percent(0),
                    left = Length.Percent(0),
                    right = Length.Percent(0),
                }
            };

            m_DebugStickArea = new VisualElement {
                name = "debug-stick-area",
                style = {
                    position = Position.Absolute,
                    top = Length.Percent(0),
                    bottom = Length.Percent(0),
                    left = Length.Percent(0),
                    right = Length.Percent(0),
                }
            };
            m_DebugStickArea.SetBorder(2f);
            m_DebugStickArea.SetBorderColor(Color.green);
            m_DebugStickArea.SetBorderRadius(5f);

            m_DebugInfoLabel = new() {
                name = "debug-info-label",
                style = {
                    fontSize = DebugInfoSize,
                    color = Color.white,
                    backgroundColor = new Color(.1f, .1f, .1f, .5f)
                }
            };
            m_DebugInfoLabel.SetPadding(15f);
            m_DebugInfoLabel.style.paddingBottom = 5f;
            m_DebugInfoLabel.SetBorderRadius(10f);

            // Styles
            m_Stick.AddToClassList(k_UssStickClass);
            m_Handle.AddToClassList(k_UssHandleClass);
            m_ArrowContent.AddToClassList(k_UssDirectionalArrowClass);

            // Build hierarchy
            m_Stick.Add(m_StickContent);
            m_StickContent.Add(m_ArrowContent);
            m_ArrowContent.Add(m_Arrow);
            m_Stick.Add(m_Handle);

            m_DebugContainer.Add(m_DebugStickArea);
            m_DebugContainer.Add(m_DebugInfoLabel);
            hierarchy.Add(m_Stick);
            hierarchy.Add(m_DebugContainer);

            // Set up interaction events
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);

            // On Geometry Changed
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // Init style settings
            stickStyleSettings = new(this, m_Stick) {
                color = new Color(.1f, .1f, .1f, .5f),
                borderColor = new(new Color(0.85f, 0.82f, 0.9f, 0.8f)),
                border = new(20f),
                radius = new(300f)
            };
            handleStyleSettings = new(this, m_Handle) {
                color = new Color(.25f, .25f, .25f, .5f),
                borderColor = new(new Color(.75f, .75f, .75f, 1f)),
                border = new(10f),
                radius = new(300f)
            };
            arrowContentStyleSettings = new(this, m_ArrowContent) {
                borderColor = new(new Color(0.95f, 0.8f, 0.95f, 1f)),
                border = new() { right = 20f },
                radius = new(300f),
                margin = new(-18.5f) { right = -35.5f }
            };
            arrowStyleSettings = new(this, m_Arrow) {
                background = m_ArrowTexture,
                backgroundTint = new Color(0.95f, 0.8f, 0.95f, 1f),
                margin = new() { right = 280 }
            };

            // Init Debug
            ShowStickArea = m_ShowStickArea;
            DebugInfo = m_DebugInfo;
            DebugAlign = m_DebugAlign;
            DebugInfoSize = m_DebugInfoSize;
        }

        protected virtual void OnGeometryChanged(GeometryChangedEvent e) {
            m_Arrow.style.display = StyleSource == VisualStyleSource.Procedural
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (StyleSource == VisualStyleSource.Procedural) {
                // Stick
                m_Stick.style.width = StickPixels;
                m_Stick.style.height = StickPixels;
                m_Stick.style.alignItems = Align.Center;
                m_Stick.style.justifyContent = Justify.Center;
                m_Stick.ApplyBackgroundStyle();

                // Handle
                m_Handle.style.width = Length.Percent(HandleSizePercent);
                m_Handle.style.height = Length.Percent(HandleSizePercent);
                m_Handle.ApplyBackgroundStyle();

                // Arrow container
                m_ArrowContent.style.position = Position.Absolute;
                m_ArrowContent.style.top = Length.Percent(0);
                m_ArrowContent.style.bottom = Length.Percent(0);
                m_ArrowContent.style.left = Length.Percent(0);
                m_ArrowContent.style.right = Length.Percent(0);
                m_ArrowContent.style.alignItems = Align.Center;
                m_ArrowContent.style.justifyContent = Justify.Center;
                m_ArrowContent.ApplyBackgroundStyle();

                // Arrow
                m_Arrow.style.translate = new Translate(Length.Percent(arrowStyleSettings.margin.right), 0);

                // Visual settings
                stickStyleSettings.Update();
                handleStyleSettings.Update();
                arrowContentStyleSettings.Update();
                arrowStyleSettings.Update();
            }
            else {
                m_Stick.ResetStyle();
                m_Handle.ResetStyle();
                m_ArrowContent.ResetStyle();
            }
        }

        #endregion

        #region LOGIC

        protected virtual void OnPointerDown(PointerDownEvent e) {
            if (!Dynamic && !m_Stick.worldBound.Contains(e.position))
                return; // Ignore clicks outside the stick area when not dynamic

            IsDragging = true;
            this.CapturePointer(e.pointerId);

            // Calculate movement boundaries (centered in parent)
            var pointerMaxDelta = (worldBound.size - m_Stick.worldBound.size) / 2;
            var relativePosition = (Vector2)e.position - worldBound.center;

            // Clamp to allowed movement area
            var clampedPosition = new Vector2(
                Mathf.Clamp(relativePosition.x, -pointerMaxDelta.x, pointerMaxDelta.x),
                Mathf.Clamp(relativePosition.y, -pointerMaxDelta.y, pointerMaxDelta.y)
            );

            // Apply position to stick
            if (Dynamic)
                m_Stick.transform.position = clampedPosition;

            m_StickContent.style.display = DirectionalArrow ? DisplayStyle.Flex : DisplayStyle.None;

            // Arrow rotation
            m_Direction = GetDirection((Vector2)e.position - m_Stick.worldBound.center);
            schedule.Execute(OnUpdate).Until(() => !IsDragging);
        }

        protected virtual void OnPointerUp(PointerUpEvent e) {
            IsDragging = false;
            this.ReleasePointer(e.pointerId);

            // Reset all elements to default positions
            m_Stick.transform.position = Offset;
            m_Handle.transform.position = Vector2.zero;
            m_StickContent.style.display = DisplayStyle.None;

            m_Direction = Vector2.zero;
            Delta = Vector2.zero;
            OnEndDrag?.Invoke();
        }

        protected virtual void OnPointerMove(PointerMoveEvent e) {
            if (!IsDragging || !this.HasPointerCapture(e.pointerId))
                return;

            // Current input position in parent coordinates
            Vector2 currentPos = e.position;
            Vector2 center = m_Stick.worldBound.center;

            // Direction vector from stick center
            m_Direction = GetDirection(currentPos - center);

            // Calculate maximum movement radius as percentage of parent's radius
            float maxRadius = m_Stick.worldBound.width * 0.5f * (HandleMoveRadius / 100f);

            // Clamp handle position to this radius
            Vector2 clampedPos = m_Direction.magnitude > maxRadius
                ? m_Direction.normalized * maxRadius
                : m_Direction;

            // Update handle position
            m_Handle.transform.position = clampedPos;

            // Normalize to [-1, 1] range (input space) and invert Y axis
            // to match standard game coordinates (Y+ = up, Y- = down)
            Delta = clampedPos / maxRadius;
            Delta = new Vector2(Delta.x, -Delta.y);
        }

        protected virtual void OnUpdate() {
            if (!IsDragging) return;

            // Update arrow rotation smoothly
            float targetAngle;

            switch (AxisConstraint) {
                case InputAxisConstraint.Free:
                    // Calculate normal angle when both axes are allowed
                    targetAngle = Mathf.Atan2(-m_Direction.x, m_Direction.y) * Mathf.Rad2Deg + 90f;
                    break;

                case InputAxisConstraint.Horizontal:
                    // Lock to 0° (right) or 180° (left) based on X direction
                    targetAngle = m_Direction.x > 0 ? 0f : 180f;
                    break;

                case InputAxisConstraint.Vertical:
                    // Lock to 90° (up) or 270° (down) based on Y direction
                    targetAngle = m_Direction.y > 0 ? 90f : 270f;
                    break;

                default:
                    Debug.LogWarning($"Unhandled direction: {AxisConstraint}");
                    targetAngle = 0f;
                    break;
            }

            // Apply smoothing only if needed (not for locked directions)
            if (AxisConstraint == InputAxisConstraint.Free)
                m_CurrentArrowAngle += Mathf.DeltaAngle(m_CurrentArrowAngle, targetAngle) * DirectionalArrowSmooth;
            else
                m_CurrentArrowAngle = targetAngle; // Immediate snap for restricted directions

            m_StickContent.style.rotate = new Rotate(m_CurrentArrowAngle);

            // Notify input change
            OnDrag?.Invoke(Delta); // [-1,1] normalized input
        }

        protected void UpdateDebugInfo() {
            //m_DebugStickArea.SetBorder(2f);
            //m_DebugStickArea.SetBorderColor(Color.green);
            //m_DebugStickArea.SetBorderRadius(5f);

            m_SB.Clear();

            // GENERAL INFO
            const int displayNameLength = 20;
            var displayName = name.Length > displayNameLength ? name[..displayNameLength] + "..." : name;
            m_SB.AppendLine("[<color=#4DA6FF>INFO</color>]");
            m_SB.AppendLine($"Name: <b><color=#7EFFB3>{displayName}</color></b>");

            // INPUT DATA
            m_SB.AppendLine("\n[<color=#4DA6FF>INPUT DATA</color>]");
            m_SB.AppendLine($"Delta: X= <b><color=#FF7E7E>{Delta.x:F2}</color></b> | Y= <b><color=#FF7E7E>{Delta.y:F2}</color></b>");
            m_SB.AppendLine($"Magnitude: <b><color=#FF7E7E>{Delta.magnitude:F2}</color></b> | Angle: <b><color=#FF7E7E>{Mathf.Atan2(Delta.y, Delta.x) * Mathf.Rad2Deg:F0}°</color></b>");

            // POSITIONS
            m_SB.AppendLine("\n[<color=#4DA6FF>POSITIONS</color>]");
            m_SB.AppendLine($"Stick: X= <b><color=#7EFFB3>{m_Stick.transform.position.x:F2}</color></b> | Y= <b><color=#7EFFB3>{m_Stick.transform.position.y:F2}</color></b>");
            m_SB.AppendLine($"Handle: X= <b><color=#7EFFB3>{m_Handle.transform.position.x:F2}</color></b> | Y= <b><color=#7EFFB3>{m_Handle.transform.position.y:F2}</color></b>");

            // STATE
            m_SB.AppendLine("\n[<color=#4DA6FF>STATE</color>]");
            m_SB.AppendLine($"Dragging: <b><color=#FFB84D>{(IsDragging ? "YES" : "NO")}</color></b>");
            m_SB.AppendLine($"Dynamic: <b><color=#FFB84D>{(Dynamic ? "ON" : "OFF")}</color></b> | Constraint: <b><color=#FFB84D>{AxisConstraint}</color></b>");

            // CONFIG
            m_SB.AppendLine("\n[<color=#4DA6FF>CONFIG</color>]");
            m_SB.AppendLine($"Radius: <b><color=#C97EFF>{HandleMoveRadius}%</color></b> | Offset: <b><color=#C97EFF>({Offset.x:F0}, {Offset.y:F0})</color></b>");
            m_SB.AppendLine($"Arrow: <b><color=#C97EFF>{(DirectionalArrow ? "ON" : "OFF")}</color></b> | Smooth: <b><color=#C97EFF>{DirectionalArrowSmooth:F2}</color></b>");

            m_DebugInfoLabel.text = m_SB.ToString();
        }

        /// <summary>
        /// Filters the input vector to enforce current axis constraints.
        /// </summary>
        /// <param name="directionBase">Raw input direction.</param>
        /// <returns>Constrained direction vector.</returns>
        /// <remarks>
        /// Applies axis locking based on InputAxisConstraint:
        /// - Free: Returns original vector
        /// - Horizontal: Zeroes Y component
        /// - Vertical: Zeroes X component
        /// </remarks>
        protected virtual Vector2 GetDirection(Vector2 directionBase) {
            switch (AxisConstraint) {
                case InputAxisConstraint.Horizontal:
                    directionBase.y = 0f;
                    break;
                case InputAxisConstraint.Vertical:
                    directionBase.x = 0f;
                    break;
            }

            return directionBase;
        }

        #endregion
    }
}
