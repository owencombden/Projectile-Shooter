# UITK VirtuaStick 🎮  
[![Unity UITK](https://img.shields.io/badge/Unity-UI_Toolkit_Ready-2C8EFF?logo=unity)](https://docs.unity3d.com/6000.1/Documentation/Manual/UIElements.html)
[![Input System](https://img.shields.io/badge/Input_System-1.4+-FFB900)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/index.html)
[![Mobile](https://img.shields.io/badge/Mobile-Optimized-38B6FF)](https://unity.com/resources/mobile-xr-web-game-performance-optimization-unity-6)

**Professional Virtual Joystick for Unity's UI Toolkit**  
*Smooth • Native • Input System Ready*  

A lightweight virtual joystick designed **natively for Unity's UI Toolkit**, offering plug-and-play touch controls with full **Input System compatibility**. Ideal for mobile games, editor tools, or any UITK-based project requiring precise input.

> ✅ Fully USS customizable  
> ✅ Handles touch input with <1ms latency  
> ✅ Just bind a `Vector2` action — and you're done

---

![demo-screenshot](https://github.com/user-attachments/assets/7fa561b7-3f00-4c46-a945-7bf3d588c342)

---

## 🔧 Crafted for Efficiency and Control

Built for developers who need native, scalable, and mobile-ready joystick solutions — without workarounds or overhead.

✅ **True UITK Integration** – No adapters or UGUI bridges  
✅ **Input System First** – One-click binding to `Vector2` actions  
✅ **Optimized for Mobile** – Touch input with <1ms latency  
✅ **Flexible Modes** – Dynamic (follows touch) or Fixed (anchored) joystick  
✅ **100% USS Styling** – Full visual control via USS or runtime API  
✅ **Visual Feedback** – Built-in directional highlight  
✅ **Direction Constraints** – Omni / Horizontal / Vertical

---

## 🔍 Solves These UITK Pain Points

| ❌ Problem                                   | ✅ VirtuaStick Solution                            |
|---------------------------------------------|---------------------------------------------------|
| "My joystick breaks on different screen sizes" | Fully responsive USS layout                       |
| "Input System setup is too complex"         | Native InputAction support with one-click binding |
| "My controls feel laggy on mobile"          | Input processed on touch phase with <1ms latency  |

---

## ⚙️ Installation

1. **Import the package**
2. **Add `VirtuaStick`** via UXML or use `VirtuaStickProcedural` directly in code
3. **Attach the `OnScreenVirtuaStick` component** to connect to the Input System
4. **Assign your `Vector2` InputAction binding**
5. ✅ That’s it — ready for testing

---

## 🧩 Core Components

| File                   | Description                                                   |
|------------------------|---------------------------------------------------------------|
| `VirtuaStick.cs`       | Defines the **VisualElement** for the joystick                |
| `OnScreenVirtuaStick.cs` | Handles **Input System** integration and binding setup        |

---

## 📦 Requirements

- **Unity**: 2021.3 or newer  
- **Input System Package**: v1.4.0+

---

## 🧪 Example Usage

```csharp
// Subscribe to drag updates (delta represents normalized direction)
virtuaStick.OnDrag += (delta) => {
    // Apply movement or force
    character.Move(new Vector3(delta.x, 0, delta.y) * speed);
};

// Reset on release
virtuaStick.OnEndDrag += () => {
    character.Move(Vector3.zero);
};
```

## 📫 Contact

For support, feature requests, or feedback, feel free to contact:  
📧 [empress.games.20@gmail.com](mailto:empress.games.20@gmail.com)

---

*UITK VirtuaStick is part of a growing set of developer-focused tools for Unity UI Toolkit.*
