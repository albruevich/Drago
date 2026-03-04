// Obstacle is the abstract base class for all game obstacles.
// It defines common data (position, movement, collision bounds) and shared update logic.
// Specific obstacle types (Bush, Bird) inherit from it and implement their own drawing behavior.

namespace Drago
{
    abstract class Obstacle //OOP: abstraction (base class)
    {
        // Common data for all obstacles:
        // - position (x,y)
        // - oldX for clearing the previous frame
        protected float x;
        protected int y;
        protected int oldX;

        // Public properties — outward "interface"
        public int X => (int)x;
        public int Y => y;

        // Abstract properties are the contract for derived classes:
        // each obstacle type must report its dimensions
        public abstract int Width { get; }
        public abstract int Height { get; }

        // Collision rectangle
        // OOP: common collision code does not depend on the concrete obstacle type (polymorphism)
        public Rect Bounds => new Rect(X, Y - Height, Width, Height);

        protected Obstacle(float x, int y)
        {
            // OOP: base constructor initializes common fields
            this.x = x;
            this.y = y;
            oldX = (int)x;
        }

        public bool Update(char[,] buffer, bool isGameOver)
        {
            // If the game is over, we don't move/draw obstacles
            if (isGameOver) return false;

            // Move the obstacle to the left
            x -= Game.obstacleSpeed;
            int currentX = (int)x;

            // Drawing is delegated to the derived class via the abstract Draw() method
            // OOP: polymorphism — we call Draw() without knowing whether it's a bush or a bird
            Draw(buffer, currentX, oldX, Game.screenWidth - 1);

            // Update oldX if we actually moved
            if (currentX != oldX) oldX = currentX;

            // Return true while the obstacle is still visible on screen
            return currentX >= 0;
        }

        // Abstract drawing method. Each derived class implements drawing in its own way.       
        protected abstract void Draw(char[,] buffer, int currentX, int oldX, int edge);
    }
}
