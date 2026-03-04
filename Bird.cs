// Bird is a flying obstacle that moves across the screen.
// It inherits movement and collision behavior from Obstacle.
// The class only defines the bird's appearance and how it is drawn in the buffer.

using System;  // basic C# types

namespace Drago
{
    class Bird : Obstacle //OOP: inheritance
    {
        const int width = 2;  // The bird has fixed dimensions

        string symbols = "<-";  // Bird symbols (a string is convenient as a char array by index)

        // Override the Obstacle contract
        public override int Width => width;
        public override int Height => 1;

        // OOP: the base Obstacle constructor sets the common part (positions),
        // and Bird only adds the "appearance".
        public Bird(float x, int y) : base(x, y) { }

        // OOP: polymorphism — Draw() implementation for the bird.
        protected override void Draw(char[,] buffer, int currentX, int oldX, int edge)
        {
            // Draw 2 bird characters
            for (int w = 0; w < width; w++)
            {
                if (currentX >= 1)
                    buffer[y, Math.Min(currentX + w, edge)] = symbols[w];

                if (currentX != oldX)
                    buffer[y, Math.Min(oldX + w, edge)] = Game.cellChar;
            }
        }
    }

}
