// Dino represents the player character and encapsulates its state and behavior.
// It controls jumping, crouching, simple animation, and exposes a collision rectangle (Bounds).
// The Game class interacts with Dino through its public methods and properties.

namespace Drago
{
    class Dino //OOP: class (encapsulation of state and behavior)
    {
        // Dino "appearance"
        // OOP: data (characters) + behavior (Update/Jump/Restart) in one class
        char headChar = 'Q';              // dino head symbol
        char[] legs = { 'V', 'v', 'V', 'u' };  // symbols for leg animation (steps)

        const int x = 4;                        // fixed X position of the dino on screen     
        float y;                         // Y position changes while jumping (float for "smoothness")

        float legCounter = 0;                   // leg animation counter (loop)

        // Jump state
        bool isJumping = false;                 // is the dino jumping?
        int jumpDirection = 0;                  // -1 up, 1 down

        // Jump parameters
        const float jumpSpeed = .6f;            // base jump speed
        float speed;                            // current jump speed

        // Crouch state
        bool isBentDown = false;                // is the dino crouching?

        // Public properties are the outward "interface" of the Dino object
        // OOP: encapsulation — fields are hidden; we expose only what is needed
        public int X => x;
        public int Y => (int)y;
        public int Width => 2;
        public int Height => isBentDown ? 1 : 2;

        // Bounds is the collision rectangle
        // OOP: the object knows its own size/position and can "present" it outward
        public Rect Bounds => new Rect(X, Y - Height, Width, Height);

        public Dino(int y)
        {

            this.y = y;   // OOP: constructor sets initial state (starting height)
        }

        public void Update(char[,] buffer, bool isGameOver)
        {
            if (isGameOver) return;  // If the game is over, the dino "freezes"

            if (isJumping)
            {
                // Erase the previous dino position in the buffer
                buffer[(int)y - 1, x + 1] = Game.cellChar;
                buffer[(int)y, x] = Game.cellChar;
                buffer[(int)y, x + 1] = Game.cellChar;

                // Jump apex: switch direction to falling
                if (jumpDirection < 0 && y <= 2) jumpDirection = 1;

                // Slightly slow down on the way down so the jump looks more natural
                float currentSpeed = jumpDirection > 0 ? speed * 0.75f : speed;

                // Move the dino along Y
                y += jumpDirection * currentSpeed;

                // Landing
                if (y >= Game.startDinoY)
                {
                    y = Game.startDinoY;
                    jumpDirection = 0;
                    isJumping = false;

                    // Recalculate speed for the next jump
                    speed = CorrectedSpeed();
                }
            }

            // Draw the dino in the current state:
            // - if crouching: the head moves down
            buffer[(int)y - 1, x + 1] = isBentDown ? Game.cellChar : headChar;
            buffer[(int)y, x + 1] = isBentDown ? headChar : Game.cellChar;

            // Legs — walking (animation)
            buffer[(int)y, x] = legs[(int)legCounter];

            // Switch leg "frame" in a loop
            legCounter = (legCounter + .15f) % legs.Length;
        }

        public void Jump()
        {
            // Don't allow a "double jump" in the air
            if (isJumping) return;

            isJumping = true;
            jumpDirection = -1; // first go up
            speed = CorrectedSpeed();
        }

        public void Restart(char[,] buffer)
        {
            // Erase the old dino image
            buffer[(int)y - 1, x + 1] = Game.cellChar;
            buffer[(int)y, x] = Game.cellChar;
            buffer[(int)y, x + 1] = Game.cellChar;

            // Reset state
            y = Game.startDinoY;
            legCounter = 0;
            isJumping = false;
            jumpDirection = 0;
            speed = CorrectedSpeed();

            // Draw the starting pose
            buffer[(int)y - 1, x + 1] = headChar;
            buffer[(int)y, x] = legs[0];
        }

        // Jump speed depends on game speed:
        // the faster the obstacles, the faster the jump (so the game stays playable)
        float CorrectedSpeed() => jumpSpeed * Game.obstacleSpeed;

        // OOP: the method changes the internal state of the object (encapsulation)
        public void BendDown(bool isBentDown) => this.isBentDown = isBentDown;
    }
}
