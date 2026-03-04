// Rect is a lightweight structure used for collision detection.
// It represents a rectangle (position and size) and provides an AABB intersection check.
// Game objects expose their bounds as Rect so collisions can be tested in a simple, unified way.

namespace Drago
{
    struct Rect
    {
        public int x, y, w, h;

        public Rect(int x, int y, int w, int h)
        {
            // Struct constructor assigns values
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }

        public bool Intersects(Rect other)
        {
            // AABB collision: rectangle intersection on X/Y axes
            return x < other.x + other.w && x + w > other.x &&
                   y < other.y + other.h && y + h > other.y;
        }
    }
}
