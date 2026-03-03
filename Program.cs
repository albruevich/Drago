//Код этой простой игры демонстрирует все основные принципы ООП (Объектно ориентируемого программирования)
//расписанно все комментариями для лучшего понимания программирования
//внизу приведен бонус: принципы ООП

using System;                      // базовые типы C#
using System.Threading;            // Thread.Sleep для FPS
using System.Collections.Generic;  // List<>
using System.IO;                   // работа с файлами

namespace Drago
{
    class Program { static void Main() => new Game().Run(); }

    class Game //ООП: инкапсуляция (класс объединяет данные и поведение игры)
    {       
        public const int screenWidth = 62;     // ширина экрана консоли
        public const int screenHeight = 7;     // высота экрана консоли
        public const int startDinoY = screenHeight - 2; // начальная Y позиция динозавра    
        public const char cellChar = ' ';      // пустая ячейка экрана
        const char floorChar = '\'';           // символ земли
        const float deltaSpeed = 0.015f;       // шаг ускорения
        const float speedIncreaseInterval = 800f; // как часто ускоряем игру
        const float maxSpeed = 1.2f;           // максимальная скорость
        const float startObstacleSpeed = 0.35f;// стартовая скорость
        const int minObstacleInterval = 15;    // минимальное количество тиков между препятствиями
        const int maxObstacleInterval = 60;    // максимальное количество тиков между препятствиями

        public static float obstacleSpeed = startObstacleSpeed; // текущая скорость препятствий

        int obstacleTick = 0;                  // таймер появления препятствий   
        int nextObstacleAfter = 10;            // через сколько тиков появится следующее препятствие
        float score = 0;                       // текущий счёт
        float hiScore = 0;                     // лучший счёт
        float deltaScore = 0.1f;               // скорость набора очков
        int obstacleSpeedTick = 0;             // таймер ускорения игры        
        int dayNightCounter = 0;               // счётчик смены дня и ночи
        int birdChance = 26;                   // шанс появления птицы, а не куста (в процентах)       
        bool isGameOver = false;               // флаг окончания игры
        bool downArrowPressed = false;         // зажата ли стрелка вниз
        int downArrowCounter = 0;              // сколько времени она зажата
        bool running;

        Random rnd = new Random();             // генератор случайных чисел
        List<Obstacle> obstacles = new List<Obstacle>(); //ООП: полиморфизм (список базового типа хранит разные наследники)        
        Dino dino;

        // Выделенные классы
        Renderer renderer = new Renderer(screenWidth, screenHeight, floorChar, cellChar);
        FileManager fileManager = new FileManager("Drago", "hiscore.txt");

        public void Run()
        {
            Init();

            int fps = 60;                          // желаемый FPS
            int frameTime = 1000 / fps;            // длительность кадра          
         
            // игровой цикл
            while (running)
            {
                DateTime start = DateTime.Now;     // время начала кадра

                HandleInput();
                dino.BendDown(downArrowPressed);   // обновление состояния приседания

                Update(dino);                      // логика игры
                renderer.RenderFrame();            // отрисовка

                int elapsed = (int)(DateTime.Now - start).TotalMilliseconds; // время кадра
                int delay = frameTime - elapsed;
                if (delay > 0)
                    Thread.Sleep(delay);           // стабилизация FPS
            }
        }

        void Init()
        {
            hiScore = fileManager.LoadHiScore();   // загрузка рекорда

            renderer.SetupConsoleWindow();
            renderer.PaintTo(DayTime.Night);
            renderer.BuildInitialBoard();          // заполняем буфер (земля + пустота)

            running = true;                   // основной флаг игры
            dino = new Dino(startDinoY);           // создание динозавра
        }

        void HandleInput()
        {
            while (Console.KeyAvailable)        // если нажата клавиша
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                    running = false;           // выход из игры

                if (key.Key == ConsoleKey.UpArrow)
                {
                    downArrowPressed = false;  // отменяем приседание
                    dino.Jump();               // прыжок
                }

                if (key.Key == ConsoleKey.DownArrow)
                {
                    downArrowPressed = true;   // начать приседание
                    downArrowCounter = 0;
                }

                if (key.Key == ConsoleKey.Spacebar)
                    TryRestartGame(dino);      // рестарт по пробелу (если Game Over)
            }
        }

        void Update(Dino dino)
        {
            if (isGameOver)
            {
                renderer.DrawText("GAME OVER", screenWidth / 2 - 5, screenHeight / 2 - 1);
                return;
            }

            score += deltaScore;

            UpdateCrouchTimer();
            TrySpawnObstacle();
            TryIncreaseSpeedAndToggleDayNight();

            var buffer = renderer.Buffer;

            UpdateObstaclesAndCheckCollisions(dino, buffer);

            dino.Update(buffer, isGameOver);

            renderer.DrawText($"HI: {(int)hiScore:D5}  {(int)score:D5}", screenWidth - 16, 1);
        }

        void UpdateCrouchTimer()
        {
            if (!downArrowPressed) return;

            downArrowCounter++;

            // How long the crouch lasts (in ticks). Using a named value is clearer than "9 / startObstacleSpeed".
            int crouchTicks = (int)(9 / startObstacleSpeed);

            if (downArrowCounter > crouchTicks)
            {
                downArrowPressed = false;
                downArrowCounter = 0;
            }
        }

        void TrySpawnObstacle()
        {
            obstacleTick++;

            if (obstacleTick <= nextObstacleAfter / obstacleSpeed)
                return;

            obstacleTick = 0;
            nextObstacleAfter = rnd.Next(minObstacleInterval, maxObstacleInterval);

            bool spawnBird = rnd.Next(0, 100) < birdChance;

            if (spawnBird)
                obstacles.Add(new Bird(screenWidth - 1, rnd.Next(3, 6)));
            else
                obstacles.Add(new Bush(screenWidth - 1, rnd));
        }

        void TryIncreaseSpeedAndToggleDayNight()
        {
            obstacleSpeedTick++;

            if (obstacleSpeedTick <= speedIncreaseInterval)
                return;

            obstacleSpeedTick = 0;
            obstacleSpeed = Math.Min(obstacleSpeed + deltaSpeed, maxSpeed);

            dayNightCounter++;
            renderer.PaintTo(dayNightCounter % 2 == 0 ? DayTime.Night : DayTime.Day);
        }

        void UpdateObstaclesAndCheckCollisions(Dino dino, char[,] buffer)  
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                Obstacle obstacle = obstacles[i];

                bool stillVisible = obstacle.Update(buffer, isGameOver: false);

                if (!stillVisible)
                {
                    obstacles.RemoveAt(i); 
                    continue;
                }

                if (dino.Bounds.Intersects(obstacle.Bounds)) 
                {
                    isGameOver = true;

                    if (score > hiScore)
                    {
                        hiScore = score;
                        fileManager.SaveHiScore((int)hiScore);
                    }

                    return;
                }
            }
        }      

        // Рестарт игры, если Конец игры
        void TryRestartGame(Dino dino) //ООП: взаимодействие объектов
        {
            if (!isGameOver) return;

            isGameOver = false;                  // сброс состояния
            nextObstacleAfter = 50;
            score = 0;
            obstacleSpeed = startObstacleSpeed;
            obstacleTick = 0;
            obstacleSpeedTick = 0;

            renderer.ClearPlayfield();           // очистка экрана
            obstacles.Clear();
            dino.Restart(renderer.Buffer);       // сброс динозавра
            renderer.ForceFullRedraw();          // сброс oldBuffer (полная перерисовка)
            renderer.PaintTo(DayTime.Night);
        }
    }

    class Renderer
    {
        int width;
        int height;
        char floorChar;
        char emptyChar;

        char[,] buffer;
        char[,] oldBuffer;

        public char[,] Buffer => buffer;

        public Renderer(int width, int height, char floorChar, char emptyChar)
        {
            this.width = width;
            this.height = height;
            this.floorChar = floorChar;
            this.emptyChar = emptyChar;

            buffer = new char[height, width];
            oldBuffer = new char[height, width];
        }

        public void SetupConsoleWindow()
        {
            Console.Clear(); // очистка экрана от старых символов

            try { Console.SetWindowSize(width, height); }
            catch { }           
        }

        public void BuildInitialBoard()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y == height - 1)
                        buffer[y, x] = floorChar; // рисуем землю
                    else
                        buffer[y, x] = emptyChar; // пустое пространство
                }
            }
        }

        public void RenderFrame()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (buffer[y, x] != oldBuffer[y, x]) // рисуем только изменения
                    {
                        Console.SetCursorPosition(x, y);
                        Console.Write(buffer[y, x]);
                        oldBuffer[y, x] = buffer[y, x];
                    }

            Console.SetCursorPosition(width - 1, height - 1); // убираем курсор в угол
        }

        public void DrawText(string text, int x, int y)
        {
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
            for (int y = 0; y < height - 1; y++)
                for (int x = 0; x < width; x++)
                    buffer[y, x] = emptyChar;
        }

        public void ForceFullRedraw()
        {
            Array.Clear(oldBuffer, 0, oldBuffer.Length);
        }

        public void PaintTo(DayTime dayTime)
        {
            Console.BackgroundColor = dayTime == DayTime.Day ? ConsoleColor.White : ConsoleColor.Black;
            Console.ForegroundColor = dayTime == DayTime.Day ? ConsoleColor.Black : ConsoleColor.White;

            ForceFullRedraw();
        }
    }

    class FileManager
    {
        string hiScorePath;

        public FileManager(string appFolderName, string fileName)
        {
            hiScorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appFolderName, fileName);
        }

        public float LoadHiScore()
        {
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

    // --- КЛАССЫ ---

    class Dino //ООП: класс(инкапсуляция состояния и поведения)
    {
        const char headChar = 'D';              // символ головы динозавра
        char[] legs = { 'R', 'F', 'H', 'F' };   // символы для анимации ног (шаги)

        const int x = 4;                        // фиксированная X позиция динозавра на экране
        float y;                                // Y позиция (меняется при прыжке)
        float legCounter = 0;                   // счётчик анимации ног

        bool isJumping = false;                 // динозавр прыгает?
        int jumpDirection = 0;                  // -1 вверх, 1 вниз
        const float jumpSpeed = .6f;            // базовая скорость прыжка
        float speed;                            // текущая скорость прыжка

        bool isBentDown = false;                // динозавр присел?

        public int X => x;
        public int Y => (int)y;
        public int Width => 2;
        public int Height => isBentDown ? 1 : 2;

        public Rect Bounds => new Rect(X, Y - Height, Width, Height);

        public Dino(int y)
        {
            this.y = y;
        }

        public void Update(char[,] buffer, bool isGameOver)
        {
            if (isGameOver) return;

            if (isJumping)
            {
                buffer[(int)y - 1, x + 1] = Game.cellChar;
                buffer[(int)y, x] = Game.cellChar;
                buffer[(int)y, x + 1] = Game.cellChar;

                if (jumpDirection < 0 && y <= 2) jumpDirection = 1;

                float currentSpeed = jumpDirection > 0 ? speed * 0.75f : speed;

                y += jumpDirection * currentSpeed;

                if (y >= Game.startDinoY)
                {
                    y = Game.startDinoY;
                    jumpDirection = 0;
                    isJumping = false;
                    speed = CorrectedSpeed();
                }
            }

            buffer[(int)y - 1, x + 1] = isBentDown ? Game.cellChar : headChar;
            buffer[(int)y, x + 1] = isBentDown ? headChar : Game.cellChar;
            buffer[(int)y, x] = legs[(int)legCounter];

            legCounter = (legCounter + .15f) % legs.Length;
        }

        public void Jump()
        {
            if (isJumping) return;

            isJumping = true;
            jumpDirection = -1;
            speed = CorrectedSpeed();
        }

        public void Restart(char[,] buffer)
        {
            buffer[(int)y - 1, x + 1] = Game.cellChar;
            buffer[(int)y, x] = Game.cellChar;
            buffer[(int)y, x + 1] = Game.cellChar;

            y = Game.startDinoY;
            legCounter = 0;
            isJumping = false;
            jumpDirection = 0;
            speed = CorrectedSpeed();

            buffer[(int)y - 1, x + 1] = headChar;
            buffer[(int)y, x] = legs[0];
        }

        float CorrectedSpeed() => jumpSpeed * Game.obstacleSpeed;

        public void BendDown(bool isBentDown) => this.isBentDown = isBentDown;
    }

    abstract class Obstacle //ООП: абстракция (базовый класс)
    {
        protected float x;
        protected int y;
        protected int oldX;

        public int X => (int)x;
        public int Y => y;
        public abstract int Width { get; }
        public abstract int Height { get; }

        public Rect Bounds => new Rect(X, Y - Height, Width, Height);

        protected Obstacle(float x, int y)
        {
            this.x = x;
            this.y = y;
            oldX = (int)x;
        }

        public bool Update(char[,] buffer, bool isGameOver)
        {
            if (isGameOver) return false;

            x -= Game.obstacleSpeed;
            int currentX = (int)x;

            Draw(buffer, currentX, oldX, Game.screenWidth - 1);
            if (currentX != oldX) oldX = currentX;

            return currentX >= 0;
        }

        protected abstract void Draw(char[,] buffer, int currentX, int oldX, int edge);
    }

    class Bush : Obstacle //ООП: наследование
    {
        int width;
        int height;

        char[] bushCharsWithEmpty = { 'W', 'K', 'R', 'M', 'V', 'J', 'I', 'L', 'F', 'X', ' ', ' ', ' ' };
        char[] bushChars = { 'W', 'K', 'R', 'M', 'V', 'J', 'I', 'L', 'F', 'X' };
        char[,] symbols;

        public override int Width => width;
        public override int Height => height;

        public Bush(float x, Random rnd) : base(x, Game.screenHeight - 2)
        {
            width = rnd.Next(1, 4);
            height = rnd.Next(1, 3);
            symbols = new char[width, height];

            bool wasEmpty = false;

            for (int i = 0; i < width; i++)
                for (int n = 0; n < height; n++)
                {
                    char ch = bushCharsWithEmpty[rnd.Next(0, bushCharsWithEmpty.Length)];

                    if (ch == ' ' && (width == 1 || height == 1 || wasEmpty))
                        ch = bushChars[rnd.Next(0, bushChars.Length)];

                    wasEmpty |= ch == ' ';
                    symbols[i, n] = ch;
                }
        }

        protected override void Draw(char[,] buffer, int currentX, int oldX, int edge)
        {
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                {
                    int y1 = y - h;

                    if (currentX >= 1)
                        buffer[y1, Math.Min(currentX + w, edge)] = symbols[w, h];

                    if (currentX != oldX)
                        buffer[y1, Math.Min(oldX + w, edge)] = Game.cellChar;
                }
        }
    }

    class Bird : Obstacle //ООП: наследование
    {
        const int width = 2;
        string symbols = "<-";

        public override int Width => width;
        public override int Height => 1;

        public Bird(float x, int y) : base(x, y) { }

        protected override void Draw(char[,] buffer, int currentX, int oldX, int edge)
        {
            for (int w = 0; w < width; w++)
            {
                if (currentX >= 1)
                    buffer[y, Math.Min(currentX + w, edge)] = symbols[w];

                if (currentX != oldX)
                    buffer[y, Math.Min(oldX + w, edge)] = Game.cellChar;
            }
        }
    }

    enum DayTime { Day, Night }            // режим экрана: день или ночь

    struct Rect
    {
        public int x, y, w, h;

        public Rect(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
        }

        public bool Intersects(Rect other)
        {
            return x < other.x + other.w && x + w > other.x &&
                   y < other.y + other.h && y + h > other.y;
        }
    }
}

/*

БОНУС

Принципы ООП (Объектно ориентируемого программирования)

1. Инкапсуляция – это когда данные и методы, которые с ними работают, объединяются в одном классе, а доступ к ним контролируется.
    Например, в классе Dino есть поле 'y' и методы Jump() и Update(). Ты не даёшь внешнему коду менять 'y' напрямую (только через методы),
    чтобы нельзя было случайно сломать логику прыжка.
    Зачем: защищает данные и упрощает поддержку кода.

2. Наследование – это когда один класс «наследует» свойства и методы другого класса.
    Например, есть Obstacle как базовый класс, а Bush и Bird — это наследники. Они автоматически получают поле 'x', методы Draw и Update
    и могут добавлять своё поведение.
    Зачем: переиспользуем код и создаём иерархии объектов.

3. Полиморфизм – это способность объектов использовать общий интерфейс, но вести себя по-разному.
    Метод Draw() есть и у Bush, и у Bird. Мы вызываем Draw, не заботясь, кто именно: куст или птица.
    Зачем: один и тот же код может работать с разными объектами.

4. Абстракция – это выделение только нужных деталей и скрытие ненужных.
    Класс Obstacle абстрактный, и он говорит: «У каждого препятствия есть Width, Height и метод Draw», но как именно рисовать
    определяется в наследниках.
    Зачем: упрощает понимание и работу с объектами, не отвлекаясь на детали реализации.

5. Сокрытие информации – тесно связано с инкапсуляцией, но отдельно акцентируется на том, что внутренние детали класса недоступны извне.
    Например, поля symbols в Bush или y в Dino — приватные, внешний код не может изменить их напрямую.
    Зачем: предотвращает неправильное использование объектов.

6. Композиция (или агрегирование) – это когда один объект содержит другие объекты, используя их как части себя.
    Пример: Program содержит List<Obstacle> obstacles. Каждый Obstacle — отдельный объект, но вместе они создают сцену игры.
    Зачем: строим сложные системы из простых объектов.

7. Сообщения/Методы как интерфейс – объекты общаются друг с другом через вызовы методов, а не напрямую меняют внутренние поля.
    Пример: dino.Jump() запускает прыжок, а Update() работает с этим внутренним состоянием.
    Зачем: объект сам контролирует своё поведение и данные.



| Принцип ООП                                  | Где в коде                                        | Конкретный пример                                                                  |
| -------------------------------------------- | --------------------------------------------------| ---------------------------------------------------------------------------------- |
| **Инкапсуляция**                             | `Dino`                                            | Поле `y` скрыто, доступ через `Y`, метод `Jump()` управляет прыжком                |
| **Сокрытие информации**                      | `Bush` и `Obstacle`                               | Приватные поля `symbols`, `x`, `oldX` недоступны внешнему коду                     |
| **Наследование**                             | `Bush : Obstacle`, `Bird : Obstacle`              | Наследники получают `x`, `y`, `Update()` из базового класса                        |
| **Полиморфизм**                              | `List<Obstacle> obstacles`                        | В списке могут быть и `Bush`, и `Bird`; вызываем `Draw()` одинаково для всех       |
| **Абстракция**                               | `abstract class Obstacle`                         | Определяет интерфейс `Width`, `Height`, `Draw()`, детали реализации скрыты         |
| **Композиция / Агрегирование**               | `Program`                                         | Содержит список объектов `obstacles`, каждый из которых управляет своим поведением |
| **Обмен сообщениями / Методы как интерфейс** | Вызовы методов `dino.Jump()`, `obstacle.Draw(...)`| Program управляет объектами через их методы, не меняя внутренние поля напрямую     |

*/