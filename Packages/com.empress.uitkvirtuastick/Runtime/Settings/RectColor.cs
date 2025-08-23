using System;
using UnityEngine;

namespace Empress.UITK {

    [Serializable]
    public struct RectColor {
        public Color top;
        public Color bottom;
        public Color left;
        public Color right;

        public RectColor(Color value) {
            top = value;
            bottom = value;
            left = value;
            right = value;
        }

        public void Set(Color value) {
            top = value;
            bottom = value;
            left = value;
            right = value;
        }
    }
}
