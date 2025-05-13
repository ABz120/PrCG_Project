using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tao.FreeGlut;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace markin_ivan_pri121
{
    // Главный класс формы, реализующий 2D-анимацию с животными, ландшафтом и управлением
    public partial class Form1 : Form
    {
        // Переменные для управления позицией и состоянием животных
        private float horseX = 500f; // X-координата лошади
        private float horseY = 100f; // Y-координата лошади
        private float cowX = 300f; // X-координата коровы
        private float cowY = 100f; // Y-координата коровы
        private float goatX = 50f; // X-координата козы
        private float goatY = 100f; // Y-координата козы
        private int selectedAnimal = 0; // Выбранное животное: 0 - лошадь, 1 - корова, 2 - коза
        private Timer animationTimer; // Таймер для обновления анимации
        private float timeStep = 0; // Время для анимации ног
        private DateTime lastFrameTime = DateTime.Now; // Время последнего кадра для расчёта deltaTime
        private bool isMoving = false; // Флаг движения животного
        private float legOffset = 0; // Смещение ног для анимации
        private bool moveLeft = false; // Флаг движения влево
        private bool moveRight = false; // Флаг движения вправо
        private bool moveUp = false; // Флаг движения вверх (не используется)
        private bool moveDown = false; // Флаг движения вниз (не используется)
        private bool isJumping = false; // Флаг прыжка
        private float jumpTime = 0; // Время, прошедшее с начала прыжка
        private const float jumpDuration = 0.5f; // Длительность прыжка в секундах
        private const float jumpHeight = 50f; // Максимальная высота прыжка
        // Словарь, хранящий направление взгляда животных (true - вправо, false - влево)
        private Dictionary<string, bool> animalFacingRight = new Dictionary<string, bool>
        {
            { "horse", true },
            { "cow", true },
            { "goat", true }
        };
        private List<Cloud> clouds; // Список облаков для анимации

        // Внутренний класс для представления облака
        private class Cloud
        {
            public float X { get; set; } // X-координата центра облака
            public float Y { get; set; } // Y-координата центра облака
            public float Radius { get; set; } // Радиус облака
            public float Speed { get; set; } // Скорость движения по X (пиксели/секунда)

            public Cloud(float x, float y, float radius, float speed)
            {
                X = x;
                Y = y;
                Radius = radius;
                Speed = speed;
            }
        }

        // Конструктор формы
        public Form1()
        {
            InitializeComponent(); // Инициализация компонентов формы
            simpleOpenGlControl1.InitializeContexts(); // Инициализация контекста OpenGL
            simpleOpenGlControl1.Paint += simpleOpenGlControl1_Paint; // Привязка обработчика отрисовки
            this.Load += simpleOpenGlControl1_Load; // Привязка обработчика загрузки формы
            // Логирование фокуса OpenGL-контрола
            simpleOpenGlControl1.GotFocus += (s, e) => Console.WriteLine("OpenGL control got focus");
            simpleOpenGlControl1.LostFocus += (s, e) => Console.WriteLine("OpenGL control lost focus");
            this.Activated += (s, e) => simpleOpenGlControl1.Focus(); // Установка фокуса на OpenGL-контрол при активации формы
            // Настройка управления клавишами
            this.KeyPreview = true; // Форма обрабатывает клавиши до передачи их контролам
            this.KeyDown += Form1_KeyDown; // Привязка обработчика нажатия клавиш
            this.KeyUp += Form1_KeyUp; // Привязка обработчика отпускания клавиш

            // Инициализация облаков с разными позициями, радиусами и скоростями
            clouds = new List<Cloud>
            {
                new Cloud(300, 450, 40, 40f),
                new Cloud(350, 460, 30, 50f),
                new Cloud(400, 450, 35, 40f),
                new Cloud(600, 500, 50, 30f),
                new Cloud(70, 500, 50, 30f),
                new Cloud(160, 450, 40, 40f),
                new Cloud(650, 510, 40, 30f)
            };

            // Инициализация таймера анимации
            animationTimer = new Timer();
            animationTimer.Interval = 16; // Интервал ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick; // Привязка обработчика тика таймера
            animationTimer.Start(); // Запуск таймера
            simpleOpenGlControl1.Focus(); // Установка фокуса на OpenGL-контрол
            // Установка фокуса на OpenGL-контрол при клике мыши
            simpleOpenGlControl1.MouseClick += (s, e) => simpleOpenGlControl1.Focus();
        }

        // Обработчик тика таймера, обновляющий позиции и анимацию
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Расчёт времени, прошедшего с последнего кадра
            var now = DateTime.Now;
            var deltaTime = (float)(now - lastFrameTime).TotalSeconds;
            lastFrameTime = now;

            // Логирование времени кадра и состояния движения
            Console.WriteLine($"DeltaTime: {deltaTime}");
            Console.WriteLine($"Moving: {moveLeft || moveRight || isJumping}");
            Console.WriteLine($"moveLeft: {moveLeft}, moveRight: {moveRight}, isJumping: {isJumping}, jumpTime: {jumpTime}");

            // Обновление позиции
            float speed = 150f * deltaTime; // Скорость движения: 150 пикселей в секунду

            // Если есть движение или прыжок, анимировать ноги
            if (moveLeft || moveRight || isJumping)
            {
                timeStep += deltaTime * 10f; // Увеличение шага времени для анимации
                legOffset = 15f * (float)Math.Sin(timeStep); // Угол смещения ног (синусоидальная анимация)
            }
            else
            {
                legOffset = 0; // Сброс анимации ног, если нет движения
            }

            // Обработка движения влево/вправо
            if (moveLeft || moveRight)
            {
                // Перемещение выбранного животного по X
                if (selectedAnimal == 0)
                {
                    if (moveLeft) horseX -= speed;
                    if (moveRight) horseX += speed;
                }
                else if (selectedAnimal == 1)
                {
                    if (moveLeft) cowX -= speed;
                    if (moveRight) cowX += speed;
                }
                else if (selectedAnimal == 2)
                {
                    if (moveLeft) goatX -= speed;
                    if (moveRight) goatX += speed;
                }

                // Ограничение координат X в пределах сцены (0–800)
                horseX = Clamp(horseX, 0, 800);
                cowX = Clamp(cowX, 0, 800);
                goatX = Clamp(goatX, 0, 800);
            }

            // Проверка, находится ли животное на камне (x=580–680)
            bool isHorseOnRock = horseX >= 580f && horseX <= 680f;
            bool isCowOnRock = cowX >= 580f && cowX <= 680f;
            bool isGoatOnRock = goatX >= 580f && goatX <= 680f;

            // Обработка прыжка
            if (isJumping)
            {
                jumpTime += deltaTime; // Увеличение времени прыжка
                float t = jumpTime / jumpDuration; // Нормализованное время прыжка (0..1)
                float jumpOffset = jumpHeight * (float)Math.Sin(t * Math.PI); // Параболическая траектория прыжка

                // Установка Y-координаты с учётом прыжка и положения на камне
                if (selectedAnimal == 0)
                    horseY = (isHorseOnRock ? 130f : 100f) + jumpOffset;
                else if (selectedAnimal == 1)
                    cowY = (isCowOnRock ? 130f : 100f) + jumpOffset;
                else if (selectedAnimal == 2)
                    goatY = (isGoatOnRock ? 130f : 100f) + jumpOffset;

                // Завершение прыжка
                if (jumpTime >= jumpDuration)
                {
                    isJumping = false; // Сброс флага прыжка
                    jumpTime = 0; // Сброс времени прыжка
                    // Возвращение Y на уровень камня (130) или земли (100)
                    if (selectedAnimal == 0) horseY = isHorseOnRock ? 130f : 100f;
                    else if (selectedAnimal == 1) cowY = isCowOnRock ? 130f : 100f;
                    else if (selectedAnimal == 2) goatY = isGoatOnRock ? 130f : 100f;
                }
            }
            else
            {
                // Если не прыгаем, установка Y в зависимости от положения на камне или земле
                if (selectedAnimal == 0) horseY = isHorseOnRock ? 130f : 100f;
                else if (selectedAnimal == 1) cowY = isCowOnRock ? 130f : 100f;
                else if (selectedAnimal == 2) goatY = isGoatOnRock ? 130f : 100f;
            }

            // Логирование позиций и состояния на камне
            Console.WriteLine($"Horse: {horseX}, {horseY}, OnRock: {isHorseOnRock}");
            Console.WriteLine($"Cow: {cowX}, {cowY}, OnRock: {isCowOnRock}");
            Console.WriteLine($"Goat: {goatX}, {goatY}, OnRock: {isGoatOnRock}");

            // Обновление позиций облаков
            foreach (var cloud in clouds)
            {
                cloud.X += cloud.Speed * deltaTime; // Перемещение облака вправо
                // Если облако выходит за правую границу, перенос на левую
                if (cloud.X > 800 + cloud.Radius)
                {
                    cloud.X = -cloud.Radius;
                }
                // Если облако выходит за левую границу, перенос на правую
                else if (cloud.X < -cloud.Radius)
                {
                    cloud.X = 800 + cloud.Radius;
                }
            }

            simpleOpenGlControl1.Invalidate(); // Перерисовка сцены
        }

        // Метод для ограничения значения в заданном диапазоне
        private static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        // Переменные для эффекта тиснения
        private bool stippleEnabled = false; // Флаг включения эффекта тиснения
        private byte[] stipplePattern = new byte[128]; // Шаблон для тиснения

        // Обработчик нажатия клавиш
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"KeyDown: {e.KeyCode}");

            // Выбор животного
            if (e.KeyCode == Keys.D1) { selectedAnimal = 0; Console.WriteLine("Selected: Horse"); }
            if (e.KeyCode == Keys.D2) { selectedAnimal = 1; Console.WriteLine("Selected: Cow"); }
            if (e.KeyCode == Keys.D3) { selectedAnimal = 2; Console.WriteLine("Selected: Goat"); }

            // Обработка клавиш управления
            switch (e.KeyCode)
            {
                case Keys.A:
                    moveLeft = true; // Включение движения влево
                    // Установка направления взгляда влево
                    if (selectedAnimal == 0) animalFacingRight["horse"] = false;
                    else if (selectedAnimal == 1) animalFacingRight["cow"] = false;
                    else if (selectedAnimal == 2) animalFacingRight["goat"] = false;
                    Console.WriteLine($"moveLeft set to true, {GetAnimalName(selectedAnimal)} facingRight: {animalFacingRight[GetAnimalName(selectedAnimal)]}");
                    break;
                case Keys.D:
                    moveRight = true; // Включение движения вправо
                    // Установка направления взгляда вправо
                    if (selectedAnimal == 0) animalFacingRight["horse"] = true;
                    else if (selectedAnimal == 1) animalFacingRight["cow"] = true;
                    else if (selectedAnimal == 2) animalFacingRight["goat"] = true;
                    Console.WriteLine($"moveRight set to true, {GetAnimalName(selectedAnimal)} facingRight: {animalFacingRight[GetAnimalName(selectedAnimal)]}");
                    break;
                case Keys.W:
                    if (!isJumping) // Начало прыжка, если не прыгаем
                    {
                        isJumping = true;
                        jumpTime = 0;
                        Console.WriteLine("Jump started");
                    }
                    break;
                case Keys.S:
                    // Клавиша S игнорируется
                    break;
                case Keys.F:
                    stippleEnabled = !stippleEnabled; // Переключение эффекта тиснения
                    simpleOpenGlControl1.Invalidate(); // Перерисовка сцены
                    break;
            }

            e.Handled = true; // Событие обработано

            // Установка флага движения при нажатии A или D
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.D)
            {
                isMoving = true;
                Console.WriteLine("isMoving set to true");
            }
        }

        // Обработчик отпускания клавиш
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            Console.WriteLine($"KeyUp: {e.KeyCode}");
            switch (e.KeyCode)
            {
                case Keys.A: moveLeft = false; break; // Отключение движения влево
                case Keys.D: moveRight = false; break; // Отключение движения вправо
                case Keys.W: /* Прыжок обрабатывается в AnimationTimer_Tick */ break;
                case Keys.S: /* Не исользуется */ break;
            }

            // Сброс флага движения, если не нажаты A или D
            if (!moveLeft && !moveRight)
            {
                isMoving = false;
                Console.WriteLine("isMoving set to false");
            }
        }

        // Получение имени животного по его номеру
        private string GetAnimalName(int selected)
        {
            return selected == 0 ? "horse" : selected == 1 ? "cow" : "goat";
        }

        // Инициализация OpenGL при загрузке контрола
        private void simpleOpenGlControl1_Load(object sender, EventArgs e)
        {
            Gl.glClearColor(0.53f, 0.81f, 0.98f, 1.0f); // Установка цвета фона (голубой)
            Gl.glMatrixMode(Gl.GL_PROJECTION); // Выбор матрицы проекции
            Gl.glLoadIdentity(); // Сброс матрицы
            Glu.gluOrtho2D(0.0, 800.0, 0.0, 600.0); // Установка ортографической проекции (0–800 по X, 0–600 по Y)
            // Инициализация шаблона тиснения
            for (int i = 0; i < 128; i++)
            {
                stipplePattern[i] = (byte)((i % 16 < 8) ? 0xAA : 0x55);
            }
            Gl.glPolygonStipple(stipplePattern); // Установка шаблона тиснения
        }

        // Отрисовка сцены
        private void simpleOpenGlControl1_Paint(object sender, PaintEventArgs e)
        {
            // Логирование позиций животных
            Console.WriteLine($"Horse: {horseX}, {horseY}");
            Console.WriteLine($"Cow: {cowX}, {cowY}");
            Console.WriteLine($"Goat: {goatX}, {goatY}");

            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT); // Очистка буфера цвета

            // Включение/отключение эффекта тиснения
            if (stippleEnabled)
            {
                Gl.glEnable(Gl.GL_POLYGON_STIPPLE);
            }
            else
            {
                Gl.glDisable(Gl.GL_POLYGON_STIPPLE);
            }

            // Отрисовка элементов сцены
            DrawFarHill(350, 120, 200); // Дальний холм
            DrawHill(0, 0, 350); // Холм в левом углу
            DrawHill(775, 200, 200); // Холм в правом углу
            DrawHill(600, 175, 150); // Холм в центре

            // Отрисовка зелёного поля (земля)
            Gl.glColor3f(0.4f, 0.8f, 0.3f);
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glVertex2f(0.0f, 0.0f);
            Gl.glVertex2f(800.0f, 0.0f);
            Gl.glVertex2f(800.0f, 200.0f);
            Gl.glVertex2f(0.0f, 200.0f);
            Gl.glEnd();

            DrawSun(100, 500, 50); // Отрисовка солнца

            // Отрисовка всех облаков
            foreach (var cloud in clouds)
            {
                DrawCloud(cloud.X, cloud.Y, cloud.Radius);
            }

            DrawLake(360, 200, 90); // Отрисовка озера
            DrawField(0, 70, 200, 120); // Поле слева
            DrawField(600, 75, 200, 100); // Поле справа

            // Отрисовка деревьев
            DrawTree(50, 170, 14, 46);
            DrawTree(100, 165, 15, 48);
            DrawTree(150, 162, 15, 50);
            DrawTree(225, 150, 15, 50);
            DrawTree(270, 145, 15, 50);
            DrawTree(500, 145, 15, 50);
            DrawTree(600, 165, 15, 48);
            DrawTree(700, 163, 15, 50);

            // Отрисовка камня
            DrawRock(580f, 40f, 100f, 50f);

            // Отрисовка всех животных
            DrawAnimal("horse", horseX, horseY);
            DrawAnimal("cow", cowX, cowY);
            DrawAnimal("goat", goatX, goatY);

            Gl.glFlush(); // Завершение отрисовки
        }

        // Отрисовка камня (прямоугольник с полукругом сверху)
        private void DrawRock(float x, float y, float width, float height)
        {
            float rectHeight = height * 0.6f; // Высота прямоугольной части (60% от общей)
            float semiCircleHeight = height * 0.4f; // Высота полукруга (40% от общей)
            Gl.glColor3f(0.5f, 0.5f, 0.5f); // Установка серого цвета

            // Отрисовка прямоугольной нижней части
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glVertex2f(x, y);
            Gl.glVertex2f(x + width, y);
            Gl.glVertex2f(x + width, y + rectHeight);
            Gl.glVertex2f(x, y + rectHeight);
            Gl.glEnd();

            // Отрисовка верхнего полукруга
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            float centerX = x + width / 2;
            float centerY = y + rectHeight;
            Gl.glVertex2f(centerX, centerY); // Центр полукруга
            for (int i = 0; i <= 100; i++)
            {
                double angle = Math.PI * i / 100; // Угол от 0 до pi (полукруг сверху)
                float vx = centerX + (float)(width / 2 * Math.Cos(angle));
                float vy = centerY + (float)(semiCircleHeight * Math.Sin(angle));
                Gl.glVertex2f(vx, vy);
            }
            Gl.glEnd();
        }

        // Отрисовка холма (круг)
        private void DrawHill(float centerX, float centerY, float radius)
        {
            Gl.glColor3f(0.35f, 0.55f, 0.23f); // Установка тёмно-зелёного цвета
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            Gl.glVertex2f(centerX, centerY); // Центр холма
            for (int i = 0; i <= 100; i++)
            {
                double angle = 2 * Math.PI * i / 100; // Полный круг
                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle));
                Gl.glVertex2f(x, y);
            }
            Gl.glEnd();
        }

        // Отрисовка солнца (жёлтый круг)
        private void DrawSun(float centerX, float centerY, float radius)
        {
            Gl.glColor3f(1.0f, 1.0f, 0.0f); // Установка жёлтого цвета
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            Gl.glVertex2f(centerX, centerY); // Центр солнца
            for (int i = 0; i <= 100; i++)
            {
                double angle = 2 * Math.PI * i / 100; // Полный круг
                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle));
                Gl.glVertex2f(x, y);
            }
            Gl.glEnd();
        }

        // Вычисление точки на кривой Эрмита
        private (float x, float y) HermitePoint(float t, (float x, float y) p1, (float x, float y) p2, (float x, float y) t1, (float x, float y) t2)
        {
            // Коэффициенты для кривой Эрмита
            float h1 = 2 * t * t * t - 3 * t * t + 1;
            float h2 = -2 * t * t * t + 3 * t * t;
            float h3 = t * t * t - 2 * t * t + t;
            float h4 = t * t * t - t * t;
            // Вычисление координат точки
            float x = h1 * p1.x + h2 * p2.x + h3 * t1.x + h4 * t2.x;
            float y = h1 * p1.y + h2 * p2.y + h3 * t1.y + h4 * t2.y;
            return (x, y);
        }

        // Отрисовка облака с использованием кривой Эрмита
        private void DrawCloud(float centerX, float centerY, float radius)
        {
            Gl.glColor3f(1.0f, 1.0f, 1.0f); // Установка белого цвета
            // Контрольные точки для формы облака
            var controlPoints = new (float x, float y)[]
            {
                (centerX - radius * 1.2f, centerY),
                (centerX, centerY + radius * 0.8f),
                (centerX + radius * 1.2f, centerY),
                (centerX, centerY - radius * 0.8f)
            };
            // Касательные для сглаживания кривой
            var tangents = new (float x, float y)[]
            {
                (radius * 0.8f, radius * 0.4f),
                (-radius * 0.4f, radius * 0.8f),
                (-radius * 0.8f, -radius * 0.4f),
                (radius * 0.4f, -radius * 0.8f)
            };
            var cloudPoints = new List<(float x, float y)>(); // Список точек облака
            // Построение кривой Эрмита
            for (int i = 0; i < 4; i++)
            {
                var p1 = controlPoints[i];
                var p2 = controlPoints[(i + 1) % 4];
                var t1 = tangents[i];
                var t2 = tangents[(i + 1) % 4];
                for (int j = 0; j <= 25; j++)
                {
                    float t = j / 25.0f;
                    var point = HermitePoint(t, p1, p2, t1, t2);
                    cloudPoints.Add(point);
                }
            }
            // Отрисовка облака
            Gl.glBegin(Gl.GL_POLYGON);
            foreach (var point in cloudPoints)
            {
                Gl.glVertex2f(point.x, point.y);
            }
            Gl.glEnd();
        }

        // Отрисовка дерева (ствол и крона)
        private void DrawTree(float x, float y, float trunkWidth, float trunkHeight)
        {
            Gl.glColor3f(0.55f, 0.27f, 0.07f); // Установка коричневого цвета для ствола
            // Отрисовка ствола
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glVertex2f(x - trunkWidth / 2, y);
            Gl.glVertex2f(x + trunkWidth / 2, y);
            Gl.glVertex2f(x + trunkWidth / 2, y + trunkHeight);
            Gl.glVertex2f(x - trunkWidth / 2, y + trunkHeight);
            Gl.glEnd();
            // Отрисовка кроны с использованием дерева Пифагора
            DrawPythagorasTree(x - 10, y + trunkHeight - 5, trunkHeight / 2, 0, 8, true);
            DrawPythagorasTree(x, y + trunkHeight, trunkHeight / 2, 0, 8, false);
        }

        // Рекурсивная отрисовка дерева Пифагора для кроны
        private void DrawPythagorasTree(float x, float y, float size, float angle, int depth, bool upward)
        {
            if (depth == 0) return; // База рекурсии
            float rad = angle * (float)Math.PI / 180f; // Угол в радианах
            float dx = size * (float)Math.Cos(rad); // Смещение по X
            float dy = size * (float)Math.Sin(rad); // Смещение по Y
            float x1 = x;
            float y1 = y;
            float x2 = x1 + dx;
            float y2 = y1 + dy;
            float x3 = x2 - dy;
            float y3 = y2 + dx;
            float x4 = x1 - dy;
            float y4 = y1 + dx;
            // Установка цвета: коричневый для ствола, зелёный для листвы
            if (depth > 6)
                Gl.glColor3f(0.55f, 0.27f, 0.07f);
            else
                Gl.glColor3f(0.1f, 0.7f, 0.1f);
            // Отрисовка квадрата
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glVertex2f(x1, y1);
            Gl.glVertex2f(x2, y2);
            Gl.glVertex2f(x3, y3);
            Gl.glVertex2f(x4, y4);
            Gl.glEnd();
            float nextSize = size * 0.7f; // Уменьшение размера для следующего уровня
            float angleLeft = angle - 30; // Угол для левой ветви
            float angleRight = angle + 30; // Угол для правой ветви
            if (!upward)
            {
                angleLeft = angle + 30; // Инверсия углов для нижней кроны
                angleRight = angle - 30;
            }
            // Рекурсивный вызов для левой и правой ветвей
            DrawPythagorasTree(x4, y4, nextSize, angleLeft, depth - 1, upward);
            DrawPythagorasTree(x3, y3, nextSize, angleRight, depth - 1, upward);
        }

        // Отрисовка озера
        private void DrawLake(float centerX, float centerY, float radius)
        {
            Gl.glColor3f(1.0f, 0.87f, 0.68f); // Установка бежевого цвета для берега
            // Отрисовка берега
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            Gl.glVertex2f(centerX, centerY);
            for (int i = 0; i <= 50; i++)
            {
                double angle = Math.PI + Math.PI * i / 50; // Полукруг снизу
                float x = centerX + (float)((radius + 10) * Math.Cos(angle));
                float y = centerY + (float)((radius + 10) * Math.Sin(angle) * 0.6f);
                Gl.glVertex2f(x, y);
            }
            for (int i = 50; i >= 0; i--)
            {
                double angle = Math.PI + Math.PI * i / 50;
                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle) * 0.8f);
                Gl.glVertex2f(x, y);
            }
            Gl.glEnd();
            Gl.glColor3f(0.0f, 0.5f, 1.0f); // Установка синего цвета для воды
            // Отрисовка воды
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            Gl.glVertex2f(centerX, centerY);
            for (int i = 0; i <= 50; i++)
            {
                double angle = Math.PI + Math.PI * i / 50;
                float x = centerX + (float)(radius * Math.Cos(angle));
                float y = centerY + (float)(radius * Math.Sin(angle) * 0.6f);
                Gl.glVertex2f(x, y);
            }
            Gl.glEnd();
        }

        // Отрисовка поля с волнистой текстурой
        private void DrawField(float x, float y, float width, float height)
        {
            Gl.glColor3f(0.82f, 0.71f, 0.55f); // Установка бежевого цвета для поля
            // Отрисовка прямоугольника поля
            Gl.glBegin(Gl.GL_QUADS);
            Gl.glVertex2f(x, y);
            Gl.glVertex2f(x + width, y);
            Gl.glVertex2f(x + width, y + height);
            Gl.glVertex2f(x, y + height);
            Gl.glEnd();
            Gl.glColor3f(0.55f, 0.27f, 0.07f); // Установка коричневого цвета для линий
            // Отрисовка волнистых линий
            for (float i = y; i < y + height; i += 15)
            {
                Gl.glBegin(Gl.GL_LINE_STRIP);
                for (float j = x; j <= x + width; j += 10)
                {
                    float wave = (float)(5 * Math.Sin(j * 0.1)); // Волновой эффект
                    Gl.glVertex2f(j, i + wave);
                }
                Gl.glEnd();
            }
        }

        // Отрисовка дальнего холма (полукруг)
        private void DrawFarHill(float centerX, float centerY, float radius)
        {
            Gl.glColor3f(0.65f, 0.55f, 0.43f); // Установка коричнево-зелёного цвета
            Gl.glBegin(Gl.GL_TRIANGLE_FAN);
            Gl.glVertex2f(centerX, centerY); // Центр холма
            for (int i = 0; i <= 100; i++)
            {
                double angle = Math.PI * i / 100; // Полукруг сверху
                float x = centerX + (float)(radius * Math.Cos(angle) * 0.7f);
                float y = centerY + (float)(radius * Math.Sin(angle));
                Gl.glVertex2f(x, y);
            }
            Gl.glEnd();
        }

        // Класс для хранения параметров животного
        public class AnimalParameters
        {
            public float BodyWidth { get; set; } // Ширина тела
            public float BodyHeight { get; set; } // Высота тела
            public float NeckOffsetX { get; set; } // Смещение шеи по X
            public float NeckOffsetY { get; set; } // Смещение шеи по Y
            public float NeckWidth { get; set; } // Ширина шеи
            public float NeckHeight { get; set; } // Высота шеи
            public float HeadOffsetX { get; set; } // Смещение головы по X
            public float HeadOffsetY { get; set; } // Смещение головы по Y
            public float HeadRadiusX { get; set; } // Радиус головы по X
            public float HeadRadiusY { get; set; } // Радиус головы по Y
            public float EarOffsetX { get; set; } // Смещение уха по X
            public float EarOffsetY { get; set; } // Смещение уха по Y
            public float EarWidth { get; set; } // Ширина уха
            public float EarHeight { get; set; } // Высота уха
            public float HornAngle { get; set; } // Угол рогов
            public float TailOffsetX { get; set; } // Смещение хвоста по X
            public float TailOffsetY { get; set; } // Смещение хвоста по Y
            public float TailWidth { get; set; } // Ширина хвоста
            public float TailHeight { get; set; } // Высота хвоста
            public float LegInterval { get; set; } // Интервал между ногами
            public float LegWidth { get; set; } // Ширина ноги
            public float LegHeight { get; set; } // Высота ноги
            public float EyeOffsetX { get; set; } // Смещение глаза по X
            public float EyeOffsetY { get; set; } // Смещение глаза по Y
            public float HairOffsetX { get; set; } // Смещение гривы/шерсти по X
            public float HairOffsetY { get; set; } // Смещение гривы/шерсти по Y
            public float HairHeight { get; set; } // Высота гривы/шерсти
            public float HairWidth { get; set; } // Ширина гривы/шерсти
            public float NoseOffsetX { get; set; } // Смещение носа по X
            public float NoseOffsetY { get; set; } // Смещение носа по Y
            public (float R, float G, float B) BodyColor { get; set; } // Цвет тела
            public (float R, float G, float B) LegColor { get; set; } // Цвет ног
            public (float R, float G, float B) TailColor { get; set; } // Цвет хвоста
        }

        // Словарь параметров для каждого животного
        private static readonly Dictionary<string, AnimalParameters> AnimalParams = new Dictionary<string, AnimalParameters>
        {
            { "horse", new AnimalParameters
                {
                    BodyWidth = 80,
                    BodyHeight = 30,
                    NeckOffsetX = 65,
                    NeckOffsetY = -10,
                    NeckWidth = 15,
                    NeckHeight = 30,
                    HeadOffsetX = 105,
                    HeadOffsetY = 20,
                    HeadRadiusX = 20,
                    HeadRadiusY = 10,
                    EyeOffsetX = 90,
                    EyeOffsetY = 25,
                    NoseOffsetX = 112,
                    NoseOffsetY = 22,
                    EarOffsetX = 80,
                    EarOffsetY = 30,
                    EarWidth = 10,
                    EarHeight = 10,
                    HornAngle = 0,
                    LegHeight = 30,
                    LegWidth = 10,
                    LegInterval = 5,
                    TailOffsetX = 5,
                    TailOffsetY = 0,
                    TailWidth = 40,
                    TailHeight = 10,
                    HairOffsetX = 83,
                    HairOffsetY = 28,
                    HairHeight = 28,
                    HairWidth = 11,
                    BodyColor = (0.72f, 0.45f, 0.20f), // Коричневый для тела лошади
                    LegColor = (0.55f, 0.27f, 0.07f), // Тёмно-коричневый для ног
                    TailColor = (1.0f, 0.87f, 0.68f) // Светло-коричневый для хвоста
                }
            },
            { "cow", new AnimalParameters
                {
                    BodyWidth = 100,
                    BodyHeight = 40,
                    NeckOffsetX = 75,
                    NeckOffsetY = -10,
                    NeckWidth = 20,
                    NeckHeight = 20,
                    HeadOffsetX = 115,
                    HeadOffsetY = 12,
                    HeadRadiusX = 20,
                    HeadRadiusY = 15,
                    EyeOffsetX = 107,
                    EyeOffsetY = 22,
                    NoseOffsetX = 126,
                    NoseOffsetY = 17,
                    EarOffsetX = 100,
                    EarOffsetY = 27,
                    EarWidth = 10,
                    EarHeight = 10,
                    HornAngle = 8,
                    LegHeight = 25,
                    LegWidth = 13,
                    LegInterval = 5,
                    TailOffsetX = 6,
                    TailOffsetY = -3,
                    TailWidth = 30,
                    TailHeight = 3,
                    HairOffsetX = 103,
                    HairOffsetY = 26,
                    HairHeight = 13,
                    HairWidth = 8,
                    BodyColor = (0.9f, 0.9f, 0.9f), // Белый для тела коровы
                    LegColor = (0.1f, 0.1f, 0.1f), // Чёрный для ног
                    TailColor = (0.1f, 0.1f, 0.1f) // Чёрный для хвоста
                }
            },
            { "goat", new AnimalParameters
                {
                    BodyWidth = 70,
                    BodyHeight = 30,
                    NeckOffsetX = 60,
                    NeckOffsetY = -10,
                    NeckWidth = 10,
                    NeckHeight = 25,
                    HeadOffsetX = 95,
                    HeadOffsetY = 20,
                    HeadRadiusX = 15,
                    HeadRadiusY = 10,
                    EyeOffsetX = 90,
                    EyeOffsetY = 25,
                    NoseOffsetX = 104,
                    NoseOffsetY = 22,
                    EarOffsetX = 81,
                    EarOffsetY = 28,
                    EarWidth = 10,
                    EarHeight = 20,
                    HornAngle = -10,
                    LegHeight = 30,
                    LegWidth = 10,
                    LegInterval = 5,
                    TailOffsetX = 3,
                    TailOffsetY = 0,
                    TailWidth = 15,
                    TailHeight = 10,
                    HairOffsetX = 87,
                    HairOffsetY = 28,
                    HairHeight = 15,
                    HairWidth = 11,
                    BodyColor = (0.9f, 0.9f, 0.8f), // Светло-серый для тела козы
                    LegColor = (0.7f, 0.7f, 0.7f), // Серый для ног
                    TailColor = (0.5f, 0.5f, 0.5f) // Тёмно-серый для хвоста
                }
            }
        };

        // Отрисовка животного (тело, ноги, голова, хвост и т.д.)
        private void DrawAnimal(string animalType, float startX, float startY)
        {
            if (AnimalParams.ContainsKey(animalType))
            {
                var animal = AnimalParams[animalType]; // Получение параметров животного
                Gl.glPushMatrix(); // Сохранение текущей матрицы
                // Отражение животного, если смотрит влево
                if (!animalFacingRight[animalType])
                {
                    Gl.glTranslatef(startX, startY, 0.0f);
                    Gl.glScalef(-1.0f, 1.0f, 1.0f);
                    Gl.glTranslatef(-startX, -startY, 0.0f);
                }
                // Проверка, движется ли выбранное животное
                bool isSelectedAndMoving = (moveLeft || moveRight || isJumping) &&
                                          ((selectedAnimal == 0 && animalType == "horse") ||
                                           (selectedAnimal == 1 && animalType == "cow") ||
                                           (selectedAnimal == 2 && animalType == "goat"));
                float legAngle = isSelectedAndMoving ? legOffset : 0; // Угол анимации ног
                Gl.glColor3f(animal.BodyColor.R, animal.BodyColor.G, animal.BodyColor.B); // Установка цвета тела
                // Отрисовка тела
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glVertex2f(startX, startY);
                Gl.glVertex2f(startX + animal.BodyWidth, startY);
                Gl.glVertex2f(startX + animal.BodyWidth, startY - animal.BodyHeight);
                Gl.glVertex2f(startX, startY - animal.BodyHeight);
                Gl.glEnd();
                // Отрисовка шеи
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glVertex2f(startX + animal.NeckOffsetX, startY + animal.NeckOffsetY);
                Gl.glVertex2f(startX + animal.NeckOffsetX + animal.NeckWidth, startY + animal.NeckOffsetY);
                Gl.glVertex2f(startX + animal.NeckOffsetX + animal.NeckWidth + 20, startY + animal.NeckHeight);
                Gl.glVertex2f(startX + animal.NeckOffsetX + 20, startY + animal.NeckHeight);
                Gl.glEnd();
                Gl.glColor3f(animal.BodyColor.R, animal.BodyColor.G, animal.BodyColor.B); // Установка цвета головы
                // Отрисовка головы
                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                Gl.glVertex2f(startX + animal.HeadOffsetX, startY + animal.HeadOffsetY);
                for (int i = 0; i <= 100; i++)
                {
                    double angle = 2 * Math.PI * i / 100; // Полный круг
                    float x = startX + animal.HeadOffsetX - 5 + (float)(animal.HeadRadiusX * Math.Cos(angle));
                    float y = startY + animal.HeadOffsetY + (float)(animal.HeadRadiusY * Math.Sin(angle));
                    Gl.glVertex2f(x, y);
                }
                Gl.glEnd();
                Gl.glColor3f(1.0f, 1.0f, 1.0f); // Установка белого цвета для глаза
                // Отрисовка глаза
                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                Gl.glVertex2f(startX + animal.EyeOffsetX, startY + animal.EyeOffsetY);
                for (int i = 0; i <= 100; i++)
                {
                    double angle = 2 * Math.PI * i / 100;
                    float x = startX + animal.EyeOffsetX + (float)(2 * Math.Cos(angle));
                    float y = startY + animal.EyeOffsetY + (float)(2 * Math.Sin(angle));
                    Gl.glVertex2f(x, y);
                }
                Gl.glEnd();
                Gl.glColor3f(0.0f, 0.0f, 0.0f); // Установка чёрного цвета для зрачка
                // Отрисовка зрачка
                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                Gl.glVertex2f(startX + animal.EyeOffsetX + 3, startY + animal.EyeOffsetY);
                for (int i = 0; i <= 100; i++)
                {
                    double angle = 2 * Math.PI * i / 100;
                    float x = startX + animal.EyeOffsetX + 3 + (float)(2 * Math.Cos(angle));
                    float y = startY + animal.EyeOffsetY + (float)(2 * Math.Sin(angle));
                    Gl.glVertex2f(x, y);
                }
                Gl.glEnd();
                // Отрисовка носа коровы
                if (animalType == "cow")
                {
                    Gl.glColor3f(1.0f, 0.75f, 0.79f); // Установка розового цвета для носа
                    Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                    Gl.glVertex2f(startX + animal.NoseOffsetX, startY + animal.NoseOffsetY - 3);
                    for (int i = 0; i <= 100; i++)
                    {
                        double angle = 2 * Math.PI * i / 100;
                        float x = startX + animal.NoseOffsetX + (float)(5 * Math.Cos(angle));
                        float y = startY + animal.NoseOffsetY - 3 + (float)(9 * Math.Sin(angle));
                        Gl.glVertex2f(x, y);
                    }
                    Gl.glEnd();
                }
                Gl.glColor3f(0.0f, 0.0f, 0.0f); // Установка чёрного цвета для ноздри
                // Отрисовка ноздри
                Gl.glBegin(Gl.GL_TRIANGLE_FAN);
                Gl.glVertex2f(startX + animal.NoseOffsetX, startY + animal.NoseOffsetY);
                for (int i = 0; i <= 100; i++)
                {
                    double angle = 2 * Math.PI * i / 100;
                    float x = startX + animal.NoseOffsetX + (float)(2 * Math.Cos(angle));
                    float y = startY + animal.NoseOffsetY + (float)(2 * Math.Sin(angle));
                    Gl.glVertex2f(x, y);
                }
                Gl.glEnd();
                Gl.glColor3f(animal.TailColor.R, animal.TailColor.G, animal.TailColor.B); // Установка цвета уха
                // Отрисовка уха
                Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glVertex2f(startX + animal.EarOffsetX, startY + animal.EarOffsetY);
                Gl.glVertex2f(startX + (animal.EarOffsetX + animal.EarWidth / 2 + animal.HornAngle), startY + animal.EarOffsetY + animal.EarHeight);
                Gl.glVertex2f(startX + animal.EarOffsetX + animal.EarWidth, startY + animal.EarOffsetY);
                Gl.glEnd();
                Gl.glColor3f(animal.LegColor.R, animal.LegColor.G, animal.LegColor.B); // Установка цвета ног
                // Отрисовка первой ноги с анимацией
                Gl.glPushMatrix();
                if (isSelectedAndMoving)
                {
                    Gl.glTranslatef(startX + animal.LegInterval * 2 + animal.LegWidth / 2, startY - animal.BodyHeight, 0.0f);
                    Gl.glRotatef(legAngle, 0.0f, 0.0f, 1.0f);
                    Gl.glTranslatef(-(startX + animal.LegInterval * 2 + animal.LegWidth / 2), -(startY - animal.BodyHeight), 0.0f);
                }
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glVertex2f(startX + animal.LegInterval * 2 + animal.LegWidth, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + (animal.LegInterval + animal.LegWidth) * 2, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + (animal.LegInterval + animal.LegWidth) * 2, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glVertex2f(startX + animal.LegInterval * 2 + animal.LegWidth, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glEnd();
                Gl.glPopMatrix();
                // Отрисовка второй ноги с анимацией
                Gl.glPushMatrix();
                if (isSelectedAndMoving)
                {
                    Gl.glTranslatef(startX + animal.BodyWidth - animal.LegInterval - animal.LegWidth / 2, startY - animal.BodyHeight, 0.0f);
                    Gl.glRotatef(-legAngle, 0.0f, 0.0f, 1.0f);
                    Gl.glTranslatef(-(startX + animal.BodyWidth - animal.LegInterval - animal.LegWidth / 2), -(startY - animal.BodyHeight), 0.0f);
                }
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glVertex2f(startX + animal.BodyWidth - animal.LegInterval - animal.LegWidth, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + animal.BodyWidth - animal.LegInterval, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + animal.BodyWidth - animal.LegInterval, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glVertex2f(startX + animal.BodyWidth - animal.LegInterval - animal.LegWidth, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glEnd();
                Gl.glPopMatrix();
                // Отрисовка третьей ноги с анимацией
                Gl.glPushMatrix();
                if (isSelectedAndMoving)
                {
                    Gl.glTranslatef(startX + animal.LegInterval + animal.LegWidth / 2, startY - animal.BodyHeight, 0.0f);
                    Gl.glRotatef(-legAngle, 0.0f, 0.0f, 1.0f);
                    Gl.glTranslatef(-(startX + animal.LegInterval + animal.LegWidth / 2), -(startY - animal.BodyHeight), 0.0f);
                }
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glVertex2f(startX + animal.LegInterval, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + animal.LegInterval + animal.LegWidth, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + animal.LegInterval + animal.LegWidth, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glVertex2f(startX + animal.LegInterval, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glEnd();
                Gl.glPopMatrix();
                // Отрисовка четвёртой ноги с анимацией
                Gl.glPushMatrix();
                if (isSelectedAndMoving)
                {
                    Gl.glTranslatef(startX + animal.BodyWidth - animal.LegInterval * 2 - animal.LegWidth / 2, startY - animal.BodyHeight, 0.0f);
                    Gl.glRotatef(legAngle, 0.0f, 0.0f, 1.0f);
                    Gl.glTranslatef(-(startX + animal.BodyWidth - animal.LegInterval * 2 - animal.LegWidth / 2), -(startY - animal.BodyHeight), 0.0f);
                }
                Gl.glBegin(Gl.GL_QUADS);
                Gl.glVertex2f(startX + animal.BodyWidth - animal.LegInterval * 2 - animal.LegWidth, startY - animal.BodyHeight);
                Gl.glVertex2f(startX + animal.BodyWidth - 2 * (animal.LegInterval + animal.LegWidth), startY - animal.BodyHeight);
                Gl.glVertex2f(startX + animal.BodyWidth - 2 * (animal.LegInterval + animal.LegWidth), startY - animal.BodyHeight - animal.LegHeight);
                Gl.glVertex2f(startX + animal.BodyWidth - animal.LegInterval * 2 - animal.LegWidth, startY - animal.BodyHeight - animal.LegHeight);
                Gl.glEnd();
                Gl.glPopMatrix();
                Gl.glColor3f(animal.TailColor.R, animal.TailColor.G, animal.TailColor.B); // Установка цвета хвоста
                // Отрисовка хвоста
                Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glVertex2f(startX + animal.TailOffsetX, startY + animal.TailOffsetY);
                Gl.glVertex2f(startX + animal.TailOffsetX - animal.TailWidth, startY + animal.TailOffsetY - animal.TailHeight);
                Gl.glVertex2f(startX + animal.TailOffsetX - animal.TailWidth, startY + animal.TailOffsetY + animal.TailHeight);
                Gl.glEnd();
                Gl.glColor3f(animal.TailColor.R, animal.TailColor.G, animal.TailColor.B); // Установка цвета гривы/шерсти
                // Отрисовка гривы/шерсти
                Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glVertex2f(startX + animal.HairOffsetX, startY + animal.HairOffsetY);
                Gl.glVertex2f(startX + animal.HairOffsetX - animal.HairWidth, startY + animal.HairOffsetY - animal.HairHeight);
                Gl.glVertex2f(startX + animal.HairOffsetX - animal.HairWidth * 2, startY + animal.HairOffsetY - animal.HairHeight);
                Gl.glEnd();
                // Отрисовка красного треугольника для выбранного животного
                if ((selectedAnimal == 0 && animalType == "horse") ||
                    (selectedAnimal == 1 && animalType == "cow") ||
                    (selectedAnimal == 2 && animalType == "goat"))
                {
                    Gl.glColor3f(1f, 0f, 0f); // Установка красного цвета
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    Gl.glVertex2f(startX + animal.BodyWidth / 2, startY + 10);
                    Gl.glVertex2f(startX + animal.BodyWidth / 2 - 5, startY + 20);
                    Gl.glVertex2f(startX + animal.BodyWidth / 2 + 5, startY + 20);
                    Gl.glEnd();
                }
                Gl.glPopMatrix(); // Восстановление матрицы
            }
            else
            {
                Console.WriteLine("Животное не найдено."); // Логирование ошибки
            }
        }
    }
}
