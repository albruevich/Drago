//Код этой простой игры демонстрирует все основные принципы ООП (Объектно ориентируемого программирования)
//расписанно все комментариями для лучшего понимания программирования
//внизу приведен бонус: принципы ООП

using System;                      // базовые типы C#
using System.Threading;            // Thread.Sleep для FPS
using System.Collections.Generic;  // List<>
using System.IO;                   // работа с файлами

namespace Drago
{
    class Program 
    {
         static void Main()
         {
            new Game().Run();
         }
    }

    class Game //ООП: инкапсуляция (класс объединяет данные и поведение игры)
    {
        // путь к файлу рекорда
        readonly string HiScorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Drago", "hiscore.txt"); 

        public const int screenWidth = 62;     // ширина экрана консоли
        public const int screenHeight = 7;     // высота экрана консоли     

        const char floorChar = '\'';           // символ земли
        public const char cellChar = ' ';      // пустая ячейка экрана

        char[,] buffer = new char[screenHeight, screenWidth];     // текущий кадр
        char[,] oldBuffer = new char[screenHeight, screenWidth];  // хранит прошлый кадр для корректного рендеринга

        int obstacleTick = 0;            // таймер появления препятствий
        public static int dinoY;                // начальная Y позиция динозавра
        int nextObstacleAfter = 10;      // через сколько тиков появится следующее препятствие
        float score = 0;                 // текущий счёт
        float hiScore = 0;               // лучший счёт
        float deltaScore = 0.1f;         // скорость набора очков

        int obstacleSpeedTick = 0;       // таймер ускорения игры
        const float deltaSpeed = 0.015f;        // шаг ускорения
        const float speedIncreaseInterval = 800f; // как часто ускоряем игру
        const float maxSpeed = 1.2f;            // максимальная скорость
        const float startObstacleSpeed = 0.35f; // стартовая скорость
        public static float obstacleSpeed = startObstacleSpeed; // текущая скорость препятствий
        int dayNightCounter = 0;         // счётчик смены дня и ночи
        int birdChance = 26;            // шанс появления птицы, а не куста, 26 процентов

        const int minObstacleInterval = 15;  // минимальное количество тиков между препятствиями
        const int maxObstacleInterval = 60;  // максимальное количество тиков между препятствиями

        bool isGameOver = false;          // флаг окончания игры

        Random rnd = new Random();        // генератор случайных чисел

        List<Obstacle> obstacles = new List<Obstacle>(); //ООП: полиморфизм (список базового типа хранит разные наследники)

        enum DayTime { Day, Night }              // режим экрана: день или ночь 

        bool downArrowPressed = false;    // зажата ли стрелка вниз
        int downArrowCounter = 0;         // сколько времени она зажата

        public void Run()
        {
            LoadHiScore();                       // загрузка рекорда

            Console.Clear(); // очистка экрана от старых символов
            Console.SetWindowSize(screenWidth, screenHeight); // установка размера окна
            PaintTo(DayTime.Night);              // стартуем с ночи

            for (int y = 0; y < screenHeight; y++) // заполнение экрана
            {
                for (int x = 0; x < screenWidth; x++)
                {
                    if (y == screenHeight - 1)
                        buffer[y, x] = floorChar; // рисуем землю
                    else
                        buffer[y, x] = cellChar;  // пустое пространство                   
                }
            }

            bool running = true;                // основной флаг игры            
            int fps = 60;                       // желаемый FPS
            int frameTime = 1000 / fps;         // длительность кадра
            dinoY = screenHeight - 2;           // позиция динозавра над землёй

            Dino dino = new Dino(dinoY);        // создание динозавра

            // игровой цикл
            while (running)                     
            {
                DateTime start = DateTime.Now;  // время начала кадра             

                if (Console.KeyAvailable)       // если нажата клавиша
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                        running = false;        // выход из игры

                    if (key.Key == ConsoleKey.UpArrow)
                    {
                        downArrowPressed = false; // отменяем приседание
                        dino.Jump();               // прыжок                      
                    }

                    if (key.Key == ConsoleKey.DownArrow)
                    {
                        downArrowPressed = true;  // начать приседание
                        downArrowCounter = 0;                       
                    }

                    if (key.Key == ConsoleKey.Spacebar)
                        TryRestartGame(dino);    // рестарт по пробелу      (если Game Over)             
                }

                dino.BendDown(downArrowPressed); // обновление состояния приседания                

                Update(dino);                    // логика игры
                Render();                        // отрисовка

                int elapsed = (int)(DateTime.Now - start).TotalMilliseconds; // время кадра
                int delay = frameTime - elapsed;
                if (delay > 0)
                    Thread.Sleep(delay);         // стабилизация FPS
            }
        }

        void Update(Dino dino) //ООП: использование объекта как параметра (взаимодействие объектов, класс Program взаимодействует с объектом Dino)
        {
            if (isGameOver)
            {
                DrawText("GAME OVER", screenWidth / 2 - 5, screenHeight / 2 - 1); // сообщение
                return; //логика игры игнорируется есть Конец игры
            }            

            score += deltaScore;             // начисляем очки

            if (downArrowPressed)                 // таймер приседания
            {
                downArrowCounter++;
                if (downArrowCounter > 9 / startObstacleSpeed)
                {
                    downArrowPressed = false;
                    downArrowCounter = 0;
                }
            }

            obstacleTick++;                      // таймер препятствий

            if (obstacleTick > nextObstacleAfter / obstacleSpeed)
            {
                obstacleTick = 0;
                nextObstacleAfter = rnd.Next(minObstacleInterval, maxObstacleInterval);

                if (rnd.Next(0, 100) < birdChance)
                    obstacles.Add(new Bird(screenWidth - 1, rnd.Next(3, 6))); // птица, и на какой случайно высоте она полетит
                else
                    obstacles.Add(new Bush(screenWidth - 1));                 // куст            
            }

            obstacleSpeedTick++;                 // таймер ускорения игры

            if (obstacleSpeedTick > speedIncreaseInterval)
            {
                obstacleSpeedTick = 0;
                obstacleSpeed = Math.Min(obstacleSpeed + deltaSpeed, maxSpeed); // увеличение скорости

                dayNightCounter++;
                PaintTo(dayNightCounter % 2 == 0 ? DayTime.Night : DayTime.Day); // смена дня и ночи
            }

            for (int i = obstacles.Count - 1; i >= 0; i--) // обновление препятствий
            {
                Obstacle obstacle = obstacles[i];
                bool isObstacle = obstacle.Update(buffer, isGameOver);

                if (!isObstacle) //если препятсивие вышло за пределы экрана, то удаляем его
                    obstacles.Remove(obstacle);
                //проверяем, пересеклись ли прямоуголиники Динозавра и Препятствия
                else if (RectIntersectRect(dino.X, dino.Y - dino.Height, dino.Width, dino.Height,
                                          obstacle.X, obstacle.Y - obstacle.Height, obstacle.Width, obstacle.Height))
                {
                    isGameOver = true;           // столкновение

                    if (score > hiScore)
                    {
                        hiScore = score;
                        SaveHiScore();           // сохранение рекорда
                    }
                }
            }

            dino.Update(buffer, isGameOver);     // обновление динозавра
            DrawText($"HI: {(int)hiScore:D5}  {(int)score:D5}", screenWidth - 16, 1); // вывод очков
        }

        //Рендеринг (отрисовка игры)
        void Render()
        {
            for (int y = 0; y < screenHeight; y++)
                for (int x = 0; x < screenWidth; x++)
                    if (buffer[y, x] != oldBuffer[y, x]) // рисуем только изменения
                    {               
                        Console.SetCursorPosition(x, y); // ставим курсор в место которое изменилось
                        Console.Write(buffer[y, x]); // перерисовываем его
                        oldBuffer[y, x] = buffer[y, x];
                    }

            Console.SetCursorPosition(screenWidth - 1, screenHeight - 1); // убираем курсор в угол
        }

        bool RectIntersectRect(int ax, int ay, int aw, int ah,
                                      int bx, int by, int bw, int bh)
        {
            return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by; // AABB коллизия (проверка на столкновения прямоугольников)
        }

        void DrawText(string text, int x, int y)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int px = x + i;
                if (px < 0 || px >= screenWidth || y < 0 || y >= screenHeight)
                    continue;

                buffer[y, px] = text[i];         // запись текста в буфер
            }
        }

        //Рестарт игры, если Конец игры
        void TryRestartGame(Dino dino) //ООП: использование объекта как параметра (взаимодействие объектов)
        {
            if (!isGameOver) return;

            isGameOver = false;                  // сброс состояния
            nextObstacleAfter = 50;
            score = 0;
            obstacleSpeed = startObstacleSpeed;
            obstacleTick = 0;
            obstacleSpeedTick = 0;

            ClearPlayfield();                    // очистка экрана
            obstacles.Clear();
            dino.Restart(buffer);                // сброс динозавра
            Array.Clear(oldBuffer, 0, oldBuffer.Length);

            PaintTo(DayTime.Night);
        }

        void ClearPlayfield()
        {
            for (int y = 0; y < screenHeight - 1; y++)
                for (int x = 0; x < screenWidth; x++)
                    buffer[y, x] = cellChar;     // очистка поля
        }

        // Перекрашивание экрана в ночь или день
        void PaintTo(DayTime dayTime)
        {
            Console.BackgroundColor = dayTime == DayTime.Day ? ConsoleColor.White : ConsoleColor.Black;
            Console.ForegroundColor = dayTime == DayTime.Day ? ConsoleColor.Black : ConsoleColor.White;
            Array.Clear(oldBuffer, 0, oldBuffer.Length); // полная перерисовка
        }

        // загрузка рекорда
        void LoadHiScore()
        {
            try
            {
                if (File.Exists(HiScorePath) && float.TryParse(File.ReadAllText(HiScorePath), out float value))
                    hiScore = value;           
            }
            catch { hiScore = 0; }
        }

        // сохранение рекорда
        void SaveHiScore()
        {
            try
            {
                string dir = Path.GetDirectoryName(HiScorePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(HiScorePath, hiScore.ToString()); 
            }
            catch { }
        }
    }   

    // --- КЛАССЫ ---

    class Dino  //ООП: класс(инкапсуляция состояния и поведения)
    {
        const char headChar = 'D';                // символ головы динозавра
        char[] legs = { 'R', 'F', 'H', 'F' };    // символы для анимации ног (шаги)

        const int x = 4;                          // фиксированная X позиция динозавра на экране
        float y;                                  // Y позиция (меняется при прыжке)
        float legCounter = 0;                     // счётчик для анимации ног (какой символ ног показывать)

        bool isJumping = false;                   // флаг: динозавр прыгает?
        int jumpDirection = 0;                    // направление прыжка: -1 вверх, 1 вниз
        const float jumpSpeed = .6f;              // базовая скорость прыжка
        float speed;                               // текущая скорость прыжка, зависит от скорости препятствий

        bool isBentDown = false;                  // флаг: динозавр присел?

        // Получение текущих размеров и позиции 
        public int X => x;                  //ООП: инкапсуляция (геттер к скрытому параметру x)
        public int Y => (int)y;             //ООП: инкапсуляция (геттер к скрытому параметру y)
        public int Width => 2;                    // ширина динозавра всегда 2 символа. ООП: инкапсуляция (свойство)
        public int Height => isBentDown ? 1 : 2;  // высота зависит от приседания. ООП: инкапсуляция + скрытие логики

        // конструктор динозавра
        public Dino(int y) //ООП: конструктор
        {
            this.y = y;  // начальная позиция по Y
        }

        // Метод обновления состояния динозавра каждый кадр
        public void Update(char[,] buffer, bool isGameOver) //ООП: метод поведения объекта
        {
            if (isGameOver) return; // если игра окончена, динозавр не двигается

            if (isJumping) // если динозавр в прыжке
            {
                // очищаем старые позиции на буфере, чтобы "стереть" старое изображение динозавра
                buffer[(int)y - 1, x + 1] = Game.cellChar;
                buffer[(int)y, x] = Game.cellChar;
                buffer[(int)y, x + 1] = Game.cellChar;

                // если достиг верхней границы прыжка, начинаем падение
                if (jumpDirection < 0 && y <= 2) jumpDirection = 1;

                // замедление при падении
                float currentSpeed = jumpDirection > 0 ? speed * 0.75f : speed;

                // двигаем динозавра по Y
                y += jumpDirection * currentSpeed;

                // приземление на землю
                if (y >= Game.dinoY)
                {
                    y = Game.dinoY;
                    jumpDirection = 0;
                    isJumping = false;
                    speed = CorrectedSpeed(); // сброс скорости прыжка
                }
            }

            // отрисовка головы и туловища с учётом приседания
            buffer[(int)y - 1, x + 1] = isBentDown ? Game.cellChar : headChar; // если присел, голова исчезает
            buffer[(int)y, x + 1] = isBentDown ? headChar : Game.cellChar;     // при приседании голова опускается на уровень тела
            buffer[(int)y, x] = legs[(int)legCounter];                             // отрисовка ног

            // обновление счётчика анимации ног (чтобы ноги "ходили")
            legCounter = (legCounter + .15f) % legs.Length;
        }

        // Метод прыжка
        public void Jump() //ООП: поведение объекта
        {
            if (isJumping) return;   // если уже прыгаем, не начинаем новый прыжок

            isJumping = true;
            jumpDirection = -1;      // направление вверх
            speed = CorrectedSpeed(); // скорость прыжка зависит от скорости препятствий
        }

        // Сброс состояния динозавра при перезапуске игры
        public void Restart(char[,] buffer) //ООП: управление внутренним состоянием
        {
            // очищаем старое изображение
            buffer[(int)y - 1, x + 1] = Game.cellChar;
            buffer[(int)y, x] = Game.cellChar;
            buffer[(int)y, x + 1] = Game.cellChar;

            // возвращаем на землю и сбрасываем анимацию
            y = Game.dinoY;
            legCounter = 0;
            isJumping = false;
            jumpDirection = 0;
            speed = CorrectedSpeed();

            // отрисовываем заново динозавра в исходном положении
            buffer[(int)y - 1, x + 1] = headChar;
            buffer[(int)y, x] = legs[0];
        }

        // Вычисление скорости прыжка с учётом текущей скорости препятствий
        float CorrectedSpeed() => jumpSpeed * Game.obstacleSpeed; //ООП: инкапсуляция внутренней логики

        // Метод приседания
        public void BendDown(bool isBentDown) => this.isBentDown = isBentDown;  //ООП: инкапсуляция состояния
    }

    //абстрактный родительский класс, от него будут наследоваться Кусты и Птицы, у которых есть много общего для объединения
    abstract class Obstacle  //ООП: абстракция (базовый класс)
    {
        protected float x;    // текущая X позиция препятствия (с плавающей точкой (то есть float) для плавного движения)
        protected int y;      // Y позиция препятствия на экране
        protected int oldX;   // предыдущая X позиция для очистки старого рисунка

        public int X => (int)x;  // текущая X позиция как целое число
        public int Y => y;       // Y позиция
        public abstract int Width { get; }  // ширина препятствия, определяется в наследниках,  ООП: абстракция (контракт для наследников)
        public abstract int Height { get; } // высота препятствия, определяется в наследниках,  ООП: абстракция

        protected Obstacle(float x, int y)  //ООП: конструктор базового класса (препятствия)
        {
            this.x = x;
            this.y = y;
            oldX = (int)x;                // запоминаем начальную позицию
        }

        public bool Update(char[,] buffer, bool isGameOver)
        {
            if (isGameOver) return false; // если игра окончена, препятствие не двигается

            x -= Game.obstacleSpeed;   // сдвигаем препятствие влево
            int currentX = (int)x;        // округляем позицию для буфера

            Draw(buffer, currentX, oldX, Game.screenWidth - 1); // рисуем препятствие
            if (currentX != oldX) oldX = currentX;                  // обновляем oldX для следующего кадра

            return currentX >= 0;         // возвращаем true, если препятствие ещё видно на экране
        }

        protected abstract void Draw(char[,] buffer, int currentX, int oldX, int edge); // метод рисования, реализуется в наследниках,  ООП: полиморфизм
    }
    
    class Bush : Obstacle  //ООП: наследование от Препятствия (Obstacle)
    {
        int width;   // ширина куста
        int height;  // высота куста

        // массив символов для кустов (с пробелами, чтобы иногда куст был "пустым")
        char[] bushCharsWithEmpty = { 'W', 'K', 'R', 'M', 'V', 'J', 'I', 'L', 'F', 'X', ' ', ' ', ' ' };
        char[] bushChars = { 'W', 'K', 'R', 'M', 'V', 'J', 'I', 'L', 'F', 'X' }; // только символы без пробелов
        char[,] symbols;  // двумерный массив символов куста

        // геттеры
        public override int Width => width;   // ширина куста.  ООП: полиморфизм (переопределение)
        public override int Height => height; // высота куста.  ООП: полиморфизм

        //ООП: наследование + вызов базового конструктора
        public Bush(float x) : base(x, Game.screenHeight - 2) // создаём куст над землёй   
        {
            Random rnd = new Random();

            width = rnd.Next(1, 4);  // случайная ширина 1-3
            height = rnd.Next(1, 3); // случайная высота 1-2
            symbols = new char[width, height]; // создаём массив символов

            bool wasEmpty = false;    // флаг, чтобы не было слишком много пустых символов подряд

            for (int i = 0; i < width; i++)
                for (int n = 0; n < height; n++)
                {
                    char ch = bushCharsWithEmpty[rnd.Next(0, bushCharsWithEmpty.Length)]; // случайный символ

                    // если символ пробел, проверяем условия
                    if (ch == ' ' && (width == 1 || height == 1 || wasEmpty))
                        ch = bushChars[rnd.Next(0, bushChars.Length)]; // заменяем на непустой символ

                    wasEmpty |= ch == ' ';      // запоминаем, был ли пробел
                    symbols[i, n] = ch;         // записываем символ в массив
                }
        }

        // рисуем куст
        protected override void Draw(char[,] buffer, int currentX, int oldX, int edge)  //ООП: полиморфизм
        {
            for (int h = 0; h < height; h++)        // по высоте
                for (int w = 0; w < width; w++)     // по ширине
                {
                    int y1 = y - h;                 // высота в буфере

                    if (currentX >= 1)
                        buffer[y1, Math.Min(currentX + w, edge)] = symbols[w, h]; // рисуем символ

                    if (currentX != oldX)
                        buffer[y1, Math.Min(oldX + w, edge)] = Game.cellChar;  // очищаем предыдущую позицию
                }
        }
    }
    
    class Bird : Obstacle  //ООП: наследование от Препятствия (Obstacle)
    {
        const int width = 2;           // ширина птицы всегда 2 символа
        string symbols = "<-";  // символы для птицы

        public override int Width => width;   //ООП: полиморфизм
        public override int Height => 1; // высота птицы всегда 1 символ   ООП: полиморфизм

        public Bird(float x, int y) : base(x, y) { } //ООП: наследование + конструктор

        // рисуем птицу
        protected override void Draw(char[,] buffer, int currentX, int oldX, int edge) //ООП: полиморфизм
        {
            for (int w = 0; w < width; w++) // рисуем символы птицы
            {
                if (currentX >= 1)
                    buffer[y, Math.Min(currentX + w, edge)] = symbols[w]; // текущая позиция

                if (currentX != oldX)
                    buffer[y, Math.Min(oldX + w, edge)] = Game.cellChar; // очищаем старую позицию
            }
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