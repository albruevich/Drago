// Game is the main controller of the application.
// It runs the game loop, processes player input, updates game logic, and manages the game state.
// Rendering, entities, and file saving are delegated to specialized classes (Renderer, Dino, FileManager).

using System;                      // basic C# types
using System.Threading;            // Thread.Sleep for FPS
using System.Collections.Generic;  // List<>

namespace Drago
{
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

        bool running;          // main game loop flag (true while the game is running)

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
}
