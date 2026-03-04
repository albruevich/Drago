// Renderer is responsible for all console drawing in the game.
// It builds each frame in an internal buffer and prints only the changed characters to the console.
// This keeps rendering fast and separates drawing logic from the game logic (Game class).

using System;
namespace Drago
{
    class Renderer
    {
        // Renderer data is its state (encapsulation)
        int width;
        int height;
        char floorChar;
        char emptyChar;

        // Double buffering: buffer is the current frame, oldBuffer is the previous frame,
        // so we draw only changed characters (faster output).
        char[,] buffer;
        char[,] oldBuffer;

        // Public property to access the buffer.
        // OOP: we expose access through a property, not directly to fields (we control the class "contract")
        public char[,] Buffer => buffer;

        public Renderer(int width, int height, char floorChar, char emptyChar)
        {
            // OOP: constructor sets the initial object state
            this.width = width;
            this.height = height;
            this.floorChar = floorChar;
            this.emptyChar = emptyChar;

            buffer = new char[height, width];
            oldBuffer = new char[height, width];
        }

        public void SetupConsoleWindow()
        {
            Console.Clear(); // clear the screen from old characters

            // try/catch: in some terminals SetWindowSize can throw an exception
            try { Console.SetWindowSize(width, height); }
            catch { }
        }

        public void BuildInitialBoard()
        {
            // Fill the buffer: "ground" at the bottom, empty space above
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y == height - 1)
                        buffer[y, x] = floorChar; // draw the ground
                    else
                        buffer[y, x] = emptyChar; // empty space
                }
            }
        }

        public void RenderFrame()
        {
            // Draw only changed cells (diff rendering)
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (buffer[y, x] != oldBuffer[y, x]) // draw only changes
                    {
                        Console.SetCursorPosition(x, y);  // move cursor to the change position                       
                        Console.Write(buffer[y, x]);  // write the character                       
                        oldBuffer[y, x] = buffer[y, x];  // remember what we drew
                    }

            Console.SetCursorPosition(width - 1, height - 1);  // move cursor to the corner so it doesn't blink in the center
        }

        public void DrawText(string text, int x, int y)
        {
            // Write the string directly into buffer (not immediately to console)
            // This is convenient: we build the whole frame in memory, then RenderFrame outputs the changes.
            for (int i = 0; i < text.Length; i++)
            {
                int px = x + i;
                if (px < 0 || px >= width || y < 0 || y >= height)
                    continue;

                buffer[y, px] = text[i];
            }
        }

        public void ClearPlayfield()
        {
            // Clear only the "air", leaving the ground untouched
            for (int y = 0; y < height - 1; y++)
                for (int x = 0; x < width; x++)
                    buffer[y, x] = emptyChar;
        }

        public void ForceFullRedraw()
        {
            // Clear the previous frame so the next frame redraws everything
            Array.Clear(oldBuffer, 0, oldBuffer.Length);
        }

        public void PaintTo(DayTime dayTime)
        {
            // Change console colors
            Console.BackgroundColor = dayTime == DayTime.Day ? ConsoleColor.White : ConsoleColor.Black;
            Console.ForegroundColor = dayTime == DayTime.Day ? ConsoleColor.Black : ConsoleColor.White;

            ForceFullRedraw();  // After changing colors we need a full redraw
        }
    }
}
