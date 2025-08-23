using System;

namespace Empress.UITK {

    [Serializable]
    public struct RectEdge {
        public float top;
        public float bottom;
        public float left;
        public float right;

        public RectEdge(float value) {
            top = value;
            bottom = value;
            left = value;
            right = value;
        }

        public void Set(float value) {
            top = value;
            bottom = value;
            left = value;
            right = value;
        }
    }
}
