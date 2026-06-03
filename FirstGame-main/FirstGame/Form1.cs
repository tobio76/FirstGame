using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FirstGame
{
    public partial class Form1 : Form
    {
        private const string ResultsFilePath = "results.txt";
        private Point pos;
        private bool dragging, lose = false;
        private int countCoins = 0;
        private Random rand = new Random();
        private int baseSpeed = 5;
        private int baseCoinValue = 1;
        private int elapsedTime = 0;

        // Для плавного движения
        private bool leftPressed = false;
        private bool rightPressed = false;
        private int playerSpeed = 8;

        // Для наклона машины
        private float currentRotation = 0f;
        private float targetRotation = 0f;
        private const float MAX_ROTATION = 45f;
        private const float ROTATION_SPEED = 8f;
        private Bitmap originalCarImage;

        // Защита при старте
        private int invincibilityFrames = 0;
        private const int INVINCIBILITY_DURATION = 60;

        // Для отображения рекорда
        private Label labelRecord;
        private int recordScore = 0;

        public Form1()
        {
            InitializeComponent();
            InitializeEvents();

            // Создаём Label для рекорда
            labelRecord = new Label();
            labelRecord.Text = "Рекорд: 0";
            labelRecord.ForeColor = Color.FromArgb(255, 255, 215, 0);
            labelRecord.BackColor = Color.Transparent;
            labelRecord.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            labelRecord.AutoSize = true;
            labelRecord.Location = new Point(20, 80);
            labelRecord.Parent = this;
            labelRecord.BringToFront();

            // Сохраняем оригинальное изображение машины
            if (player.Image != null)
            {
                originalCarImage = new Bitmap(player.Image);
            }

            LoadRecord();
            ResetGame();
        }

        private void InitializeEvents()
        {
            KeyPreview = true;
            KeyPress += Form1_KeyPress;
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
            btnRestart.Click += btnRestart_Click;
        }

        private void LoadRecord()
        {
            if (!File.Exists(ResultsFilePath))
            {
                recordScore = 0;
                labelRecord.Text = "Рекорд: 0";
                return;
            }

            var results = File.ReadAllLines(ResultsFilePath).ToList();
            string playerName = "Игрок";

            foreach (string line in results)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2 && parts[0] == playerName)
                {
                    if (int.TryParse(parts[1], out int score))
                    {
                        recordScore = score;
                        labelRecord.Text = $"Рекорд: {recordScore}";
                        break;
                    }
                }
            }
        }

        private void SaveResult(string playerName, int score)
        {
            if (!File.Exists(ResultsFilePath))
            {
                File.WriteAllText(ResultsFilePath, $"{playerName}:{score}\n");
                return;
            }

            var results = File.ReadAllLines(ResultsFilePath).ToList();
            bool nameFound = false;

            for (int i = 0; i < results.Count; i++)
            {
                string[] parts = results[i].Split(':');
                if (parts.Length != 2) continue;

                string name = parts[0];
                if (name == playerName)
                {
                    nameFound = true;
                    int existingScore = int.Parse(parts[1]);
                    if (score > existingScore)
                    {
                        results[i] = $"{playerName}:{score}";
                    }
                    break;
                }
            }

            if (!nameFound)
            {
                results.Add($"{playerName}:{score}");
            }

            File.WriteAllLines(ResultsFilePath, results);
        }

        private void ResetGame()
        {
            // Сбрасываем позиции объектов с безопасными координатами
            enemy1.Top = -200;
            enemy2.Top = -400;
            enemy1.Left = rand.Next(150, 300);
            enemy2.Left = rand.Next(300, 560);

            coin1.Top = -500;
            coin1.Left = rand.Next(150, 560);

            bg1.Top = 0;
            bg2.Top = -650;

            // Сбрасываем игровые параметры
            countCoins = 0;
            lose = false;
            elapsedTime = 0;
            baseSpeed = 5;
            baseCoinValue = 1;

            // Сбрасываем наклон
            currentRotation = 0f;
            targetRotation = 0f;
            ApplyPlayerRotation();

            // Включаем неуязвимость при старте
            invincibilityFrames = INVINCIBILITY_DURATION;

            // Сбрасываем флаги движения
            leftPressed = false;
            rightPressed = false;

            // Сбрасываем позицию игрока по центру
            player.Left = (this.ClientSize.Width - player.Width) / 2;

            // Обновляем UI
            labelLose.Visible = false;
            btnRestart.Visible = false;
            btn_StartMenu.Visible = false;
            labelCoins.Text = "Очки: 0";

            // Обновляем отображение рекорда
            LoadRecord();

            // Запускаем таймер
            timer.Enabled = true;
        }

        private void ApplyPlayerRotation()
        {
            if (originalCarImage == null) return;

            Bitmap rotatedImage = new Bitmap(player.Width, player.Height);

            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                PointF center = new PointF(player.Width / 2, player.Height / 2);

                g.TranslateTransform(center.X, center.Y);
                g.RotateTransform(currentRotation);
                g.TranslateTransform(-center.X, -center.Y);

                g.DrawImage(originalCarImage, 0, 0, player.Width, player.Height);
            }

            player.Image = rotatedImage;
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
                Application.Exit();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (lose) return;

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
            {
                leftPressed = true;
                targetRotation = -MAX_ROTATION;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
            {
                rightPressed = true;
                targetRotation = MAX_ROTATION;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
            {
                leftPressed = false;
                if (!rightPressed)
                    targetRotation = 0f;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
            {
                rightPressed = false;
                if (!leftPressed)
                    targetRotation = 0f;
            }
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            ResetGame();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            int speed = baseSpeed;
            bg1.Top += speed;
            bg2.Top += speed;

            int enemySpeed = baseSpeed + 1;
            enemy1.Top += enemySpeed;
            enemy2.Top += enemySpeed;

            coin1.Top += speed;

            // Плавный наклон машины
            if (Math.Abs(currentRotation - targetRotation) > 0.1f)
            {
                currentRotation += Math.Sign(targetRotation - currentRotation) * ROTATION_SPEED;
                currentRotation = Math.Max(-MAX_ROTATION, Math.Min(MAX_ROTATION, currentRotation));

                if (Math.Abs(currentRotation - targetRotation) < ROTATION_SPEED)
                    currentRotation = targetRotation;

                ApplyPlayerRotation();
            }

            // Плавное движение игрока
            if (leftPressed && player.Left > 150)
            {
                player.Left -= playerSpeed;
            }
            if (rightPressed && player.Right < 700)
            {
                player.Left += playerSpeed;
            }

            // Уменьшаем счетчик неуязвимости
            if (invincibilityFrames > 0)
            {
                invincibilityFrames--;
                if ((invincibilityFrames / 5) % 2 == 0)
                    player.BackColor = Color.Transparent;
                else
                    player.BackColor = Color.FromArgb(128, 255, 255, 255);
            }
            else
            {
                player.BackColor = Color.Transparent;
            }

            elapsedTime++;
            if (elapsedTime % 60 == 0)
            {
                countCoins += baseCoinValue;
                labelCoins.Text = "Очки: " + countCoins;
            }

            if (elapsedTime % 600 == 0 && elapsedTime > 0)
            {
                baseSpeed++;
                baseCoinValue += 1;
            }

            if (coin1.Top >= 650)
            {
                RespawnCoin();
            }

            if (bg1.Top >= 650)
            {
                bg1.Top = 0;
                bg2.Top = -650;
            }

            if (enemy1.Top >= 650)
            {
                RespawnEnemy(enemy1, 1);
            }

            if (enemy2.Top >= 650)
            {
                RespawnEnemy(enemy2, 2);
            }

            CheckCollisions();
        }

        private void RespawnCoin()
        {
            coin1.Top = -50;
            int newLeft;
            do
            {
                newLeft = rand.Next(150, 560);
            } while (Math.Abs(newLeft - player.Left) < 50);
            coin1.Left = newLeft;
        }

        private void RespawnEnemy(PictureBox enemy, int enemyNumber)
        {
            enemy.Top = -200;
            int newLeft;
            int minDistance = 100;

            do
            {
                if (enemyNumber == 1)
                    newLeft = rand.Next(150, 300);
                else
                    newLeft = rand.Next(300, 560);
            } while (Math.Abs(newLeft - player.Left) < minDistance);

            enemy.Left = newLeft;
        }

        private void CheckCollisions()
        {
            if (invincibilityFrames <= 0)
            {
                if (IsPartiallyColliding(player, enemy1, 25) || IsPartiallyColliding(player, enemy2, 25))
                {
                    GameOver();
                    return;
                }
            }

            if (player.Bounds.IntersectsWith(coin1.Bounds))
            {
                countCoins += 10;
                labelCoins.Text = "Очки: " + countCoins;
                RespawnCoin();
            }
        }

        private bool IsPartiallyColliding(Control obj1, Control obj2, int tolerance)
        {
            Rectangle obj1Bounds = new Rectangle(
                obj1.Left + tolerance, obj1.Top + tolerance,
                obj1.Width - 2 * tolerance, obj1.Height - 2 * tolerance);

            Rectangle obj2Bounds = obj2.Bounds;

            return obj1Bounds.IntersectsWith(obj2Bounds);
        }

        private void btn_StartMenu_Click(object sender, EventArgs e)
        {
            Question menuform = new Question();
            menuform.Show();
            this.Hide();
        }

        private void GameOver()
        {
            string playerName = "Игрок";

            // Проверяем, побил ли игрок рекорд
            bool isNewRecord = countCoins > recordScore;

            // Сохраняем результат (если он больше текущего рекорда)
            SaveResult(playerName, countCoins);

            // Обновляем отображение рекорда
            if (isNewRecord)
            {
                recordScore = countCoins;
                labelRecord.Text = $"Рекорд: {recordScore} ★ НОВЫЙ РЕКОРД! ★";
                labelRecord.ForeColor = Color.Gold;
            }
            else
            {
                labelRecord.Text = $"Рекорд: {recordScore}";
                labelRecord.ForeColor = Color.FromArgb(255, 255, 215, 0);
            }

            timer.Enabled = false;
            labelLose.Visible = true;
            btnRestart.Visible = true;
            lose = true;
            btn_StartMenu.Visible = true;

            // Сбрасываем наклон при смерти
            currentRotation = 0f;
            targetRotation = 0f;
            ApplyPlayerRotation();
        }
    }
}