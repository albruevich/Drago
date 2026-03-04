//This small console game demonstrates the core principles of OOP (Object-Oriented Programming).
//The entire project is carefully commented to help beginners understand how real programs are built.
//Bonus: at the end of the file there is a simple explanation of the main OOP principles used in this project.
//If this project helped you understand OOP, consider giving it a star ⭐

using System;                      // basic C# types
using System.Threading;            // Thread.Sleep for FPS
using System.Collections.Generic;  // List<>
using System.IO;                   // working with files

namespace Drago
{
    // Program entry point.   
    class Program { static void Main() => new Game().Run(); }

    class Game //OOP: encapsulation (the class combines game data and behavior)
    {
        // Constants are game parameters that do not change while the program runs.
        // OOP: the game's "state" is defined by fields; constants are part of the class "settings".
        public const int screenWidth = 62;     // console screen width
        public const int screenHeight = 7;     // console screen height
        public const int startDinoY = screenHeight - 2; // dino starting Y position    
        public const char cellChar = ' ';      // empty screen cell
        const char floorChar = '\'';           // ground symbol
        const float deltaSpeed = 0.015f;       // acceleration step
        const float speedIncreaseInterval = 800f; // how often we speed up the game
        const float maxSpeed = 1.2f;           // maximum speed
        const float startObstacleSpeed = 0.35f;// starting speed
        const int minObstacleInterval = 15;    // minimum ticks between obstacles
        const int maxObstacleInterval = 60;    // maximum ticks between obstacles

        public static float obstacleSpeed = startObstacleSpeed; // global current obstacle speed

        // Below is the internal state of Game (encapsulation: everything is stored inside the Game object)
        int obstacleTick = 0;                  // obstacle spawn timer   
        int nextObstacleAfter = 10;            // how many ticks until the next obstacle spawns
        float score = 0;                       // current score
        float hiScore = 0;                     // best score
        float deltaScore = 0.1f;               // score gain rate
        int obstacleSpeedTick = 0;             // game acceleration timer        
        int dayNightCounter = 0;               // day/night switch counter
        int birdChance = 26;                   // chance to spawn a bird instead of a bush (percent)       
        bool isGameOver = false;               // game over flag
        bool downArrowPressed = false;         // is Down Arrow pressed

        // Console applications do not provide a reliable "KeyUp" event.
        // Because of this, we cannot detect when the DownArrow key is released.
        // Instead we simulate "holding" the crouch by keeping it active for several frames
        // after the last DownArrow event is received.
        int crouchFramesLeft = 0;                 // how many frames the dino should remain crouched
        const int crouchHoldFramesFirst = 30;     // first press: longer hold to cover the OS key repeat delay
        const int crouchHoldFramesRepeat = 6;     // repeated presses: shorter extension while the key is being held

        bool running;

        Random rnd = new Random();             // random number generator

        // OOP: polymorphism — we store objects of different classes (Bush and Bird) in one list,
        // but work with them as Obstacle (a common base type).
        List<Obstacle> obstacles = new List<Obstacle>();

        Dino dino;  // OOP: dino is a separate object with its own logic (Jump/Update/Restart)

        // OOP: composition — Game contains other objects (Renderer, FileManager),
        // delegating part of the responsibility to them (rendering and saving).
        Renderer renderer = new Renderer(screenWidth, screenHeight, floorChar, cellChar);
        FileManager fileManager = new FileManager("Drago", "hiscore.txt");

        public void Run()
        {
            Init(); // Initialize game state and dependencies (renderer/fileManager/dino)

            int fps = 60;                          // target FPS
            int frameTime = 1000 / fps;            // frame duration          

            // game loop (main loop of the program)
            while (running)
            {

                DateTime start = DateTime.Now; // Measure frame start to keep FPS stable (frame start time)

                HandleInput();  // input                                              
                Update(dino);  // logic                      
                renderer.RenderFrame(); // render (it decides what to draw by comparing buffer and oldBuffer)

                // keep FPS
                int elapsed = (int)(DateTime.Now - start).TotalMilliseconds; // frame time
                int delay = frameTime - elapsed;
                if (delay > 0)
                    Thread.Sleep(delay);           // FPS stabilization
            }
        }

        void Init()
        {
            // set UTF-8 encoding so non-ASCII characters render correctly in different terminals
            Console.OutputEncoding = System.Text.Encoding.UTF8; 

            // OOP: delegate file work to the FileManager object
            hiScore = fileManager.LoadHiScore();   // load high score

            // OOP: delegate console work to the Renderer object
            renderer.SetupConsoleWindow();
            renderer.PaintTo(DayTime.Night);
            renderer.BuildInitialBoard();      // fill the buffer (ground + empty space)

            running = true;         // main game flag

            dino = new Dino(startDinoY);    // OOP: create a Dino object (constructor sets the starting state)       
        }

        void HandleInput()
        {
            // Read ALL key presses accumulated between frames (controls feel more responsive)            
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                // Control Game state (encapsulation: the running flag lives inside Game)
                if (key.Key == ConsoleKey.Escape)
                    running = false;           // exit the game

                if (key.Key == ConsoleKey.UpArrow)
                {
                    downArrowPressed = false;  // cancel crouch                    
                    dino.Jump();   // OOP: ask the Dino object to perform the "jump" action
                }

                if (key.Key == ConsoleKey.DownArrow)
                {
                    // If the dino is already crouching, extend the crouch by a small amount.
                    // If this is the first press, use a longer duration to bridge the keyboard auto-repeat delay.
                    int hold = (crouchFramesLeft > 0) ? crouchHoldFramesRepeat : crouchHoldFramesFirst;

                    // Keep the larger value so crouch time never shrinks due to rapid input events.
                    crouchFramesLeft = Math.Max(crouchFramesLeft, hold);
                }

                if (key.Key == ConsoleKey.Spacebar)
                    TryRestartGame(dino); // OOP: the game controls restart, but Dino resets its own state (via Restart)
            }
        }

        void Update(Dino dino)
        {
            // Game over condition: world logic stops, but drawing the text continues
            if (isGameOver)
            {
                // OOP: Renderer hides text drawing details (we just call DrawText)
                renderer.DrawText("GAME OVER", screenWidth / 2 - 5, screenHeight / 2 - 1);
                return;
            }

            score += deltaScore;  // Score increases every frame

            // Sub-methods: each piece of logic is separated (easier to read)
            UpdateCrouchTimer();
            dino.BendDown(downArrowPressed);   // update crouching state
            TrySpawnObstacle();
            TryIncreaseSpeedAndToggleDayNight();

            // OOP: polymorphism — obstacles are updated the same way via Obstacle.Update(),
            // but inside Draw() they have different implementations.
            UpdateObstaclesAndCheckCollisions(dino, renderer.Buffer);

            // OOP: Dino updates its own state (jump/landing/leg animation)
            dino.Update(renderer.Buffer, isGameOver);

            // UI score line
            renderer.DrawText($"HI: {(int)hiScore:D5}  {(int)score:D5}", screenWidth - 16, 1);
        }

        void UpdateCrouchTimer()
        {
            // Decrease crouch timer every frame
            if (crouchFramesLeft > 0)
                crouchFramesLeft--;

            // The dinosaur remains crouched while the timer is active
            downArrowPressed = crouchFramesLeft > 0;
        }

        void TrySpawnObstacle()
        {
            // Increase obstacle spawn timer
            obstacleTick++;

            // If it's not time yet, don't spawn
            if (obstacleTick <= nextObstacleAfter / obstacleSpeed)
                return;

            // It's time to create a new obstacle
            obstacleTick = 0;

            // Randomly choose after how many ticks the next one will spawn
            nextObstacleAfter = rnd.Next(minObstacleInterval, maxObstacleInterval);

            // Randomly choose obstacle type
            bool spawnBird = rnd.Next(0, 100) < birdChance;

            if (spawnBird)
                obstacles.Add(new Bird(screenWidth - 1, rnd.Next(3, 6))); // OOP: create a Bird object (an Obstacle descendant)
            else
                // OOP: create a Bush object (an Obstacle descendant)
                // We pass rnd so the bush can generate its shape but use the game's common RNG
                obstacles.Add(new Bush(screenWidth - 1, rnd));
        }

        void TryIncreaseSpeedAndToggleDayNight()
        {
            // Acceleration timer
            obstacleSpeedTick++;

            // If the interval hasn't passed yet, don't speed up
            if (obstacleSpeedTick <= speedIncreaseInterval)
                return;

            obstacleSpeedTick = 0;

            // Increase speed, but not above maxSpeed
            obstacleSpeed = Math.Min(obstacleSpeed + deltaSpeed, maxSpeed);

            // Switch "day/night" theme for visual variety
            dayNightCounter++;
            renderer.PaintTo(dayNightCounter % 2 == 0 ? DayTime.Night : DayTime.Day);
        }

        void UpdateObstaclesAndCheckCollisions(Dino dino, char[,] buffer)
        {
            // Iterate from the end to safely remove obstacles during the loop
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                // Take the obstacle as Obstacle (polymorphism)
                Obstacle obstacle = obstacles[i];

                // Update the obstacle (movement + drawing into buffer)
                // isGameOver is passed as false because UpdateObstacles runs only when the game is not over.
                bool stillVisible = obstacle.Update(buffer, isGameOver: false);

                // If the obstacle left the screen, remove it
                if (!stillVisible)
                {
                    obstacles.RemoveAt(i);
                    continue;
                }              

                // Collision: compare rectangles (AABB)
                // OOP: Bounds is the "interface" of entities outward (they compute their rectangle from their own data)
                if (dino.Bounds.Intersects(obstacle.Bounds))
                {
                    isGameOver = true;

                    if (score > hiScore) // If we beat the record, save it
                    {
                        hiScore = score;
                        fileManager.SaveHiScore((int)hiScore);  // OOP: saving is delegated to FileManager
                    }

                    return;  // Exit because after collision the rest of obstacle logic this frame is not needed
                }
            }
        }

        // Restart the game if it's Game Over
        void TryRestartGame(Dino dino)
        {
            if (!isGameOver) return;

            // Reset Game state
            isGameOver = false;
            nextObstacleAfter = 50;
            score = 0;
            obstacleSpeed = startObstacleSpeed;
            obstacleTick = 0;
            obstacleSpeedTick = 0;

            renderer.ClearPlayfield();           // clear the screen

            obstacles.Clear();  // Remove obstacles

            dino.Restart(renderer.Buffer);  // OOP: Dino can reset its own state

            // OOP: Renderer controls how to force a full redraw
            renderer.ForceFullRedraw();          // reset oldBuffer (full redraw)

            // Restore theme
            renderer.PaintTo(DayTime.Night);
        }
    }

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

    class FileManager
    {
        string hiScorePath;  // FileManager state is the path to the high score file (encapsulation)

        public FileManager(string appFolderName, string fileName)
        {
            // OOP: FileManager hides "where exactly" the file is saved. It only exposes Load/Save methods.           
            hiScorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appFolderName, fileName);
        }

        public float LoadHiScore()
        {
            // Try to read the record safely (the game should not crash because of a file)
            try
            {
                if (File.Exists(hiScorePath) && int.TryParse(File.ReadAllText(hiScorePath), out int value))
                    return value;
            }
            catch { }
            return 0;
        }

        public void SaveHiScore(int hiScore)
        {
            // Save the record safely: create the folder and write the file
            try
            {
                string dir = Path.GetDirectoryName(hiScorePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(hiScorePath, hiScore.ToString());
            }
            catch { }
        }
    }

    // -- ENTITIES --

    class Dino //OOP: class (encapsulation of state and behavior)
    {
        // Dino "appearance"
        // OOP: data (characters) + behavior (Update/Jump/Restart) in one class
        char headChar = 'Q';              // dino head symbol
        char[] legs = { 'п', 'П', 'д', 'П'  };  // symbols for leg animation (steps)

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

    // screen mode: day or night
    // OOP: enum is a "type" for more readable code (no magic numbers)
    enum DayTime { Day, Night }

    // Simple data struct for AABB collision
    // struct is chosen as a "lightweight value container" (no reference semantics)
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

/*

BONUS

OOP principles (Object-Oriented Programming)

1. Encapsulation – when data and the methods that work with it are combined in one class, and access is controlled.
    For example, in the Dino class there is the field 'y' and the methods Jump() and Update(). You don't let external code change 'y'
    directly (only through methods), so the jump logic can't be broken accidentally.
    Why: protects data and simplifies maintenance.

2. Inheritance – when one class "inherits" properties and methods of another class.
    For example, there is Obstacle as a base class, and Bush and Bird are derived classes. They automatically get the field 'x' and the methods
    Draw and Update and can add their own behavior.
    Why: reuse code and build object hierarchies.

3. Polymorphism – the ability of objects to use a common interface but behave differently.
    The Draw() method exists in both Bush and Bird. We call Draw without caring who it is: a bush or a bird.
    Why: the same code can work with different objects.

4. Abstraction – selecting only the necessary details and hiding the unnecessary ones.
    The Obstacle class is abstract, and it says: "Every obstacle has Width, Height, and Draw()", but how to draw
    is defined in derived classes.
    Why: simplifies understanding and working with objects without being distracted by implementation details.

5. Information hiding – closely related to encapsulation, but emphasizes that internal details are not accessible from outside.
    For example, the fields symbols in Bush or y in Dino are private; external code cannot change them directly.
    Why: prevents incorrect use of objects.

6. Composition (or aggregation) – when one object contains other objects, using them as parts of itself.
    Example: Program contains List<Obstacle> obstacles. Each Obstacle is a separate object, but together they create the game scene.
    Why: build complex systems from simple objects.

7. Messages/Methods as an interface – objects communicate with each other through method calls, not by directly changing internal fields.
    Example: dino.Jump() starts the jump, and Update() works with that internal state.
    Why: the object controls its own behavior and data.



| OOP principle                         | Where in the code                       | Concrete example                                                                    |
| --------------------------------------|-----------------------------------------|-------------------------------------------------------------------------------------|
| Encapsulation                         |Dino                                     | The field `y` is hidden; access via `Y`; `Jump()` controls the jump                 |
| Information hiding                    |Bush and Obstacle                        | Private fields `symbols`, `x`, `oldX` are not accessible from outside               |
| Inheritance                           |Bush : Obstacle, Bird : Obstacle         | Derived classes get `x`, `y`, `Update()` from the base class                        |
| Polymorphism                          |List<Obstacle> obstacles                 | The list can contain both `Bush` and `Bird`; we call `Draw()` the same way for all  |
| Abstraction                           |abstract class Obstacle                  | Defines the interface `Width`, `Height`, `Draw()`; implementation details are hidden|
| Composition / Aggregation             |Program                                  | Contains a list of `obstacles`, each manages its own behavior                       |
| Message passing / Methods as interface| Calls to dino.Jump(), obstacle.Draw(...)| Program controls objects through their methods, not by changing internal fields     |

*/