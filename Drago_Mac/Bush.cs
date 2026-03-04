// Bush is a specific obstacle that appears on the ground.
// It inherits common movement and collision behavior from Obstacle.
// Each bush generates a small random shape and draws it into the frame buffer.

using System;  // basic C# types

namespace Drago
{
    class Bush : Obstacle //OOP: inheritance
    {
        // Bush dimensions vary (random)
        int width;
        int height;

        // Character sets for generating the bush "shape"
        char[] bushCharsWithEmpty = { 'W', 'K', 'R', 'M', 'V', 'J', 'I', 'L', 'F', 'X', ' ', ' ', ' ' };
        char[] bushChars = { 'W', 'K', 'R', 'M', 'V', 'J', 'I', 'L', 'F', 'X' };

        // Generated shape of this specific bush (its "sprite" as a matrix)
        char[,] symbols;

        // Override the base class contract (abstract properties)
        public override int Width => width;
        public override int Height => height;

        // OOP: inheritance — the base Obstacle constructor runs first,
        // then we initialize Bush-specific fields.
        public Bush(float x, Random rnd) : base(x, Game.screenHeight - 2)
        {
            // Generate random bush dimensions
            width = rnd.Next(1, 4);
            height = rnd.Next(1, 3);

            symbols = new char[width, height];

            bool wasEmpty = false;

            // Generate the character matrix (bush shape)
            for (int i = 0; i < width; i++)
                for (int n = 0; n < height; n++)
                {
                    char ch = bushCharsWithEmpty[rnd.Next(0, bushCharsWithEmpty.Length)];

                    // So the bush isn't "too empty":
                    // if the char is a space, in some cases we replace it with a non-empty one
                    if (ch == ' ' && (width == 1 || height == 1 || wasEmpty))
                        ch = bushChars[rnd.Next(0, bushChars.Length)];

                    wasEmpty |= ch == ' ';
                    symbols[i, n] = ch;
                }
        }

        // OOP: polymorphism — this Draw() implementation is specific to a bush.
        protected override void Draw(char[,] buffer, int currentX, int oldX, int edge)
        {
            // Draw the symbols matrix into the buffer
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                {
                    int y1 = y - h;

                    // Draw current position
                    if (currentX >= 1)
                        buffer[y1, Math.Min(currentX + w, edge)] = symbols[w, h];

                    // Clear old position (if we moved)
                    if (currentX != oldX)
                        buffer[y1, Math.Min(oldX + w, edge)] = Game.cellChar;
                }
        }
    }
}
