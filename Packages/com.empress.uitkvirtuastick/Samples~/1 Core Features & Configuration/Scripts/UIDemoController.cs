using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
using System.Reflection;
#endif

namespace Empress.UITK.Demo {

    [RequireComponent(typeof(UIDocument))]
    public class UIDemoController : MonoBehaviour {

        static readonly Dictionary<string, Color> StringToColor = new() {
            { "Onyx", new Color(0.100f, 0.100f, 0.100f, 0.500f) },
            { "Deep Purple", new Color(0.475f, 0.420f, 0.561f, 0.800f) },
            { "Light Purple", new Color(0.627f, 0.549f, 0.757f, 0.800f) },
            { "Slate Gray", new Color(0.250f, 0.250f, 0.250f, 0.500f) },
            { "Pearl", new Color(0.910f, 0.910f, 0.910f, 1.000f) },
            { "Clear", Color.clear },
            { "Black", Color.black },
            { "White", Color.white },
            { "Red", Color.red },
            { "Green", Color.green },
            { "Blue", Color.blue },
            { "Yellow", Color.yellow },
            { "Cyan", Color.cyan },
            { "Magenta", Color.magenta },
            { "Gray", Color.gray },
            { "Light Gray", new Color(0.8f, 0.8f, 0.8f) },
            { "Dark Gray", new Color(0.2f, 0.2f, 0.2f) },
            { "Orange", new Color(1f, 0.5f, 0f) },
            { "Pink", new Color(1f, 0.4f, 0.7f) },
            { "Violet", new Color(0.5f, 0f, 1f) }
        };

        static readonly Dictionary<Color32, string> ColorToString = StringToColor.ToDictionary(x => (Color32)x.Value, y => y.Key);

        VisualElement m_Root;
        VirtuaStickProcedural m_Vsp;
        readonly Dictionary<Action, Action> m_ResetValues = new();

        void Awake() {
            m_Root = GetComponent<UIDocument>().rootVisualElement;
            m_Vsp = GetComponent<VirtuaStickProcedural>();

            m_Vsp.OnValidate();
            m_Vsp.VirtuaStick.PlaceBehind(m_Root.Q("UI_Controls"));

            var axisConstraint = m_Root.Q<DropdownField>("Axis_Constraint");
            var dynamic = m_Root.Q<Toggle>("Dynamic");
            var debugMode = m_Root.Q<Toggle>("Debug_Mode");
            var stickAreaX = m_Root.Q<Slider>("Stick_Rect_X");
            var stickAreaY = m_Root.Q<Slider>("Stick_Rect_Y");
            var stickOffsetX = m_Root.Q<Slider>("Stick_Offset_X");
            var stickOffsetY = m_Root.Q<Slider>("Stick_Offset_Y");
            var stickSize = m_Root.Q<Slider>("Stick_Size");
            var handleSize = m_Root.Q<Slider>("Handle_Size");
            var handleMoveRadius = m_Root.Q<Slider>("Handle_Move_Radius");
            var directionalArrow = m_Root.Q<Toggle>("Directional_Arrow");
            var directionalArrowSmooth = m_Root.Q<Slider>("Directional_Arrow_Smooth");
            var stickElement = m_Vsp.VirtuaStick.Q("stick");

            m_Vsp.VirtuaStick.RegisterCallback<GeometryChangedEvent>(_ => {
                var w = (m_Vsp.VirtuaStick.layout.width / 2) - (stickElement.layout.width / 2);
                var h = (m_Vsp.VirtuaStick.layout.height / 2) - (stickElement.layout.height / 2);
                stickOffsetX.lowValue = -w;
                stickOffsetY.lowValue = -h;
                stickOffsetX.highValue = w;
                stickOffsetY.highValue = h;
                stickSize.highValue = Mathf.Min(m_Vsp.VirtuaStick.layout.width, m_Vsp.VirtuaStick.layout.height);
            });

            InitValue(v => axisConstraint.index = v, () => (int)m_Vsp.axisConstraint);
            InitValue(v => dynamic.value = v, () => m_Vsp.dynamic);
            InitValue(v => debugMode.value = v, () => m_Vsp.debugInfo);
            InitValue(v => stickAreaX.value = v, () => m_Vsp.stickArea.x);
            InitValue(v => stickAreaY.value = v, () => m_Vsp.stickArea.y);
            InitValue(v => stickOffsetX.value = v, () => m_Vsp.stickOffset.x);
            InitValue(v => stickOffsetY.value = v, () => m_Vsp.stickOffset.y);
            InitValue(v => stickSize.value = v, () => m_Vsp.stickPixels);
            InitValue(v => handleSize.value = v, () => m_Vsp.handleSizePercent);
            InitValue(v => handleMoveRadius.value = v, () => m_Vsp.handleMoveRadius);
            InitValue(v => directionalArrow.value = v, () => m_Vsp.directionalArrow);
            InitValue(v => directionalArrowSmooth.value = v, () => m_Vsp.directionalArrowSmooth);

            OnValueChanged(axisConstraint, () => {
                m_Vsp.axisConstraint = (InputAxisConstraint)axisConstraint.index;
            });

            axisConstraint.RegisterValueChangedCallback(_ => {
                m_Vsp.axisConstraint = (InputAxisConstraint)axisConstraint.index;
                m_Vsp.OnValidate();
            });
            dynamic.RegisterValueChangedCallback(_ => {
                m_Vsp.dynamic = dynamic.value;
                m_Vsp.OnValidate();
            });
            debugMode.RegisterValueChangedCallback(_ => {
                m_Vsp.debugInfo = debugMode.value;
                m_Vsp.OnValidate();
            });
            stickAreaX.RegisterValueChangedCallback(_ => {
                var w = (m_Vsp.VirtuaStick.layout.width / 2) - (stickElement.layout.width / 2);
                stickOffsetX.lowValue = -w;
                stickOffsetX.highValue = w;
                stickSize.highValue = Mathf.Min(m_Vsp.VirtuaStick.layout.width, m_Vsp.VirtuaStick.layout.height);
                m_Vsp.stickArea.x = stickAreaX.value;
                m_Vsp.OnValidate(); 
            });
            stickAreaY.RegisterValueChangedCallback(_ => {
                var h = (m_Vsp.VirtuaStick.layout.height / 2) - (stickElement.layout.height / 2);
                stickOffsetY.lowValue = -h;
                stickOffsetY.highValue = h;
                stickSize.highValue = Mathf.Min(m_Vsp.VirtuaStick.layout.width, m_Vsp.VirtuaStick.layout.height);
                m_Vsp.stickArea.y = stickAreaY.value;
                m_Vsp.OnValidate();
            });
            stickOffsetX.RegisterValueChangedCallback(_ => {
                m_Vsp.stickOffset.x = stickOffsetX.value;
                m_Vsp.OnValidate(); 
            });
            stickOffsetY.RegisterValueChangedCallback(_ => {
                m_Vsp.stickOffset.y = stickOffsetY.value;
                m_Vsp.OnValidate();
            });
            stickSize.RegisterValueChangedCallback(_ => {
                m_Vsp.stickPixels = stickSize.value;
                m_Vsp.OnValidate(); 
            });
            handleSize.RegisterValueChangedCallback(_ => {
                m_Vsp.handleSizePercent = handleSize.value; 
                m_Vsp.OnValidate(); 
            });
            handleMoveRadius.RegisterValueChangedCallback(_ => { 
                m_Vsp.handleMoveRadius = handleMoveRadius.value; 
                m_Vsp.OnValidate(); 
            });
            directionalArrow.RegisterValueChangedCallback(_ => {
                directionalArrowSmooth.SetEnabled(directionalArrow.value);
                m_Vsp.directionalArrow = directionalArrow.value; 
                m_Vsp.OnValidate();
            });
            directionalArrowSmooth.RegisterValueChangedCallback(_ => { 
                m_Vsp.directionalArrowSmooth = directionalArrowSmooth.value; 
                m_Vsp.OnValidate(); 
            });

            // Stick Style
            var stick = m_Root.Q<Foldout>("Stick_Style");
            var stickBorder = stick.Q<Slider>("Border");
            var stickRadius = stick.Q<Slider>("Radius");
            var stickColor = Q<DropdownField>(stick, "Background_Color");
            var stickColorTopBottom = Q<DropdownField>(stick, "Color_Top_Bottom");
            var stickColorLeftRight = Q<DropdownField>(stick, "Color_Left_Right");

            stickSize.RegisterValueChangedCallback(_ => {
                stickBorder.highValue = stickSize.value / 2;
            });

            stickColor.choices = ColorToString.Values.ToList();
            stickColorTopBottom.choices = ColorToString.Values.ToList();
            stickColorLeftRight.choices = ColorToString.Values.ToList();

            m_Vsp.stick.radius.Set(600f);
            InitValue(v => stickBorder.value = v, () => m_Vsp.stick.border.top);
            InitValue(v => stickBorder.highValue = v, () => stickSize.value / 2);
            InitValue(v => stickRadius.value = v, () => 600f);
            InitValue(v => stickColor.value = v, () => ColorToString[m_Vsp.stick.color]);
            InitValue(v => stickColorTopBottom.value = v, () => ColorToString[m_Vsp.stick.borderColor.top]);
            InitValue(v => stickColorLeftRight.value = v, () => ColorToString[m_Vsp.stick.borderColor.left]);

            stickBorder.RegisterValueChangedCallback(_ => {
                m_Vsp.stick.border.Set(stickBorder.value);
                m_Vsp.OnValidate();
            });

            stickRadius.RegisterValueChangedCallback(_ => {
                m_Vsp.stick.radius.Set(stickRadius.value);
                m_Vsp.OnValidate();
            });

            stickColor.RegisterValueChangedCallback(_ => {
                m_Vsp.stick.color = StringToColor[stickColor.value];
                m_Vsp.OnValidate();
            });

            stickColorTopBottom.RegisterValueChangedCallback(_ => {
                m_Vsp.stick.borderColor.top = StringToColor[stickColorTopBottom.value];
                m_Vsp.stick.borderColor.bottom = StringToColor[stickColorTopBottom.value];
                m_Vsp.OnValidate();
            });

            stickColorLeftRight.RegisterValueChangedCallback(_ => {
                m_Vsp.stick.borderColor.left = StringToColor[stickColorLeftRight.value];
                m_Vsp.stick.borderColor.right = StringToColor[stickColorLeftRight.value];
                m_Vsp.OnValidate();
            });

            // Handle Style
            var handle = m_Root.Q<Foldout>("Handle_Style");
            var handleBorder = handle.Q<Slider>("Border");
            var handleRadius = handle.Q<Slider>("Radius");
            var handleColor = Q<DropdownField>(handle, "Background_Color");
            var handleColorTopBottom = Q<DropdownField>(handle, "Color_Top_Bottom");
            var handleColorLeftRight = Q<DropdownField>(handle, "Color_Left_Right");
            var handleElement = m_Vsp.VirtuaStick.Q("handle");

            m_Vsp.VirtuaStick.RegisterCallback<GeometryChangedEvent>(_ => {
                handleBorder.highValue = handleElement.layout.width / 2f;
            });

            handleSize.RegisterValueChangedCallback(_ => {
                handleBorder.highValue = handleElement.layout.width / 2f;
            });

            handleColor.choices = ColorToString.Values.ToList();
            handleColorTopBottom.choices = ColorToString.Values.ToList();
            handleColorLeftRight.choices = ColorToString.Values.ToList();

            m_Vsp.handle.radius.Set(600f);
            InitValue(v => handleBorder.value = v, () => m_Vsp.handle.border.top);
            InitValue(v => handleRadius.value = v, () => 600f);
            InitValue(v => handleColor.value = v, () => ColorToString[m_Vsp.handle.color]);
            InitValue(v => handleColorTopBottom.value = v, () => ColorToString[m_Vsp.handle.borderColor.top]);
            InitValue(v => handleColorLeftRight.value = v, () => ColorToString[m_Vsp.handle.borderColor.left]);

            handleBorder.RegisterValueChangedCallback(_ => {
                m_Vsp.handle.border.Set(handleBorder.value);
                m_Vsp.OnValidate();
            });

            handleRadius.RegisterValueChangedCallback(_ => {
                m_Vsp.handle.radius.Set(handleRadius.value);
                m_Vsp.OnValidate();
            });

            handleColor.RegisterValueChangedCallback(_ => {
                m_Vsp.handle.color = StringToColor[handleColor.value];
                m_Vsp.OnValidate();
            });

            handleColorTopBottom.RegisterValueChangedCallback(_ => {
                m_Vsp.handle.borderColor.top = StringToColor[handleColorTopBottom.value];
                m_Vsp.handle.borderColor.bottom = StringToColor[handleColorTopBottom.value];
                m_Vsp.OnValidate();
            });

            handleColorLeftRight.RegisterValueChangedCallback(_ => {
                m_Vsp.handle.borderColor.left = StringToColor[handleColorLeftRight.value];
                m_Vsp.handle.borderColor.right = StringToColor[handleColorLeftRight.value];
                m_Vsp.OnValidate();
            });

            // Arrow Style
            var arrow = m_Root.Q<Foldout>("Arrow_Style");
            var arrowBorder = arrow.Q<Slider>("Border");
            var arrowRadius = arrow.Q<Slider>("Radius");
            var arrowOffset = arrow.Q<Slider>("Offset");
            var arrowColor = Q<DropdownField>(arrow, "Background_Color");

            m_Vsp.VirtuaStick.RegisterCallback<GeometryChangedEvent>(_ => {
                var w = stickElement.layout.width + 50f;
                arrowOffset.lowValue = -w;
                arrowOffset.highValue = w;
                arrowOffset.SetValueWithoutNotify(m_Vsp.VirtuaStick.arrowStyleSettings.margin.right);
            });

            stickSize.RegisterValueChangedCallback(_ => {
                arrowBorder.highValue = stickSize.value / 2;
            });

            directionalArrow.RegisterValueChangedCallback(_ => {
                arrowBorder.SetEnabled(directionalArrow.value);
                arrowRadius.SetEnabled(directionalArrow.value);
                arrowOffset.SetEnabled(directionalArrow.value);
                arrowColor.SetEnabled(directionalArrow.value);
            });

            arrowColor.choices = ColorToString.Values.ToList();

            m_Vsp.arrowContent.radius.Set(600f);
            InitValue(v => arrowBorder.value = v, () => m_Vsp.arrowContent.border.right);
            InitValue(v => arrowBorder.highValue = v, () => stickSize.value / 2);
            InitValue(v => arrowRadius.value = v, () => 600f);
            InitValue(v => arrowColor.value = v, () => ColorToString[m_Vsp.arrowContent.color]);

            arrowBorder.RegisterValueChangedCallback(_ => {
                m_Vsp.arrowContent.border.right = arrowBorder.value;
                m_Vsp.OnValidate();
                showArrowDuringEdit();
            });

            arrowRadius.RegisterValueChangedCallback(_ => {
                m_Vsp.arrowContent.radius.Set(arrowRadius.value);
                m_Vsp.OnValidate();
                showArrowDuringEdit();
            });

            arrowOffset.RegisterValueChangedCallback(_ => {
                m_Vsp.arrow.margin.right = arrowOffset.value;
                m_Vsp.OnValidate();
                showArrowDuringEdit();
            });

            arrowColor.RegisterValueChangedCallback(_ => {
                m_Vsp.arrowContent.borderColor.Set(StringToColor[arrowColor.value]);
                m_Vsp.arrow.backgroundTint = StringToColor[arrowColor.value];
                m_Vsp.OnValidate();
                showArrowDuringEdit();
            });

            void showArrowDuringEdit() {
                var stickContent = m_Vsp.VirtuaStick.Q("stick-content");
                var arrowContentElement = m_Vsp.VirtuaStick.arrowContentStyleSettings.element;
                var arrowElement = m_Vsp.VirtuaStick.arrowStyleSettings.element;
                stickContent.style.display = DisplayStyle.Flex;
                arrowContentElement.style.display = DisplayStyle.Flex;
                arrowElement.style.display = DisplayStyle.Flex;
            }

            // Reset Values Button
            m_Root.Q<Button>("Reset_Values").clicked += () => {
                foreach (var resetAction in m_ResetValues.Values)
                    resetAction();
            };
        }

        T Q<T>(VisualElement parent, string elementName) where T : VisualElement {
            var element = parent.Q<T>(elementName);

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
            if (element is DropdownField dropdown)
                ApplyDropdownWorkaround(dropdown);
#endif
            return element;
        }

        void InitValue<T>(Action<T> setter, Func<T> getter) {
            // Capture value ONLY ONCE during initialization
            T initialValue = getter();

            // Apply the value immediately
            setter(initialValue);

            // Store both the reset action and its initial value
            m_ResetValues[() => setter(initialValue)] = () => setter(initialValue);
        }

        void OnValueChanged<T>(T element, Action onValueChanged) where T : VisualElement {
            void callback() {
                onValueChanged();
                m_Vsp.OnValidate();
            }
            if (element is DropdownField dropdown)
                dropdown.RegisterValueChangedCallback(_ => callback());
            if (element is Toggle toggle)
                toggle.RegisterValueChangedCallback(_ => callback());
            if (element is Slider slider)
                slider.RegisterValueChangedCallback(_ => callback());
        }

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
        void ApplyDropdownWorkaround(DropdownField dropdown) {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            GenericDropdownMenu dropdownMenu = null;

            dropdown.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button != 0)
                    return;

                // Create a new dropdown menu instance we can control
                dropdownMenu = new GenericDropdownMenu();

                // Get the internal menu container element
                var menuContainer = dropdownMenu.GetType().GetProperty("menuContainer", flags).GetValue(dropdownMenu) as VisualElement;

                // Get internal methods we need to override
                var menuClose = dropdownMenu.GetType().GetMethod("Hide", flags);
                var addMenuItems = dropdown.GetType().GetMethod("AddMenuItems", flags);
                var visualInput = dropdown.GetType().GetProperty("visualInput", flags).GetValue(dropdown) as VisualElement;

                // Track if dropdown is open to prevent infinite loops
                var dropdownOpened = false;

                // When the menu attaches to UI:
                menuContainer.RegisterCallback<AttachToPanelEvent>(_ => {
                    if (dropdownOpened)
                        return;

                    dropdownOpened = true;

                    // First close the incorrectly positioned menu
                    menuClose.Invoke(dropdownMenu, null);

                    // Then schedule a corrected version to open
                    dropdown.schedule.Execute(() => {
                        // Rebuild the menu items
                        addMenuItems.Invoke(dropdown, new object[] { dropdownMenu });

                        // Calculate new position - moves menu UP when near bottom of screen
                        var worldBound = visualInput.worldBound;
                        worldBound.y -= dropdown.resolvedStyle.height * (dropdown.choices.Count - 6) + (dropdown.resolvedStyle.height / 2 + 4);

                        // Open corrected dropdown
                        dropdownMenu.DropDown(worldBound, dropdown, anchored: true);
                    });
                });
            });

            // Replace Unity's default menu creation with our fixed version
            Func<GenericDropdownMenu> createMenuCallback = () => dropdownMenu;
            dropdown.GetType().GetField("createMenuCallback", flags).SetValue(dropdown, createMenuCallback);
        }
#endif
    }
}
