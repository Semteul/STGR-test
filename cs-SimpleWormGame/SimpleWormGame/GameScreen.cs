﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleWormGame {
    public partial class GameScreen:Form {

        public static GameScreen instance;

        public GameScreen() {
            InitializeComponent();
            instance = this;
            GameProgress.init();
            Thread gt = new Thread(new ThreadStart(GameProgress.tick));
            gt.Start();
        }



        private void Label3_Paint(object sender, System.Windows.Forms.PaintEventArgs e) {
            string text = "TEST\nTEST";
            Graphics g = e.Graphics;
            Font font = new Font("Consolas", 16F);
            Brush brushWhite = new SolidBrush(Color.White);
            Brush brushYellow = new SolidBrush(Color.Yellow);
            float lineSpacing = 0.5f;

            SizeF size = g.MeasureString("■", font);

            float pos = 0.0f;
            for(int i = 0; i < text.Length; ++i) {
                string charToDraw = text.Substring(i, 1);
                g.DrawString(charToDraw, font, brushWhite, pos, 0.0f);
                SizeF sizeChar = g.MeasureString(charToDraw, font);
                pos += sizeChar.Width + size.Width * lineSpacing;
            }
        }

        public struct Vector2 {

            public int x;
            public int y;

            public Vector2(int x, int y) {
                this.x = x;
                this.y = y;
            }

            public bool isEqual(Vector2 v2) {
                return this.x == v2.x && this.y == v2.y;
            }
        }



        public delegate void SetTextCallback(Label label, string text);

        public void setText(Label label, string text) {
            if(label.InvokeRequired) {
                SetTextCallback d = new SetTextCallback(setText);
                try {
                    this.Invoke(d, new object[] { label, text });
                } catch {}
            } else {
                label.Text = text;
            }
        }



        public delegate void CloseDelegate();

        public void SafeClose() {
            CloseDelegate d = new CloseDelegate(Close);
            try {
                this.Invoke(d, new object[] {});
            } catch { }
        }



        public static class GameProgress {

            const string BORDER = "▒";
            const string BLANK = " ";
            const string PLAYER_HEAD = "@";
            const string PLAYER_BODY = "O";
            const string FOOD = "$";
            const string CRASH = "X";

            const int LEFT = 0;
            const int TOP = 1;
            const int RIGHT = 2;
            const int BOTTOM = 3;

            static int maxFood = 5;
            static int width;
            static int height;
            static int score;
            static List<Vector2> player;
            static List<Vector2> foods;
            static int direction;
            static int lastDirection;
            static bool running;
            static Random rx = new Random();
            static Stopwatch sw = new Stopwatch();



            public static void init() {
                width = 0x21;
                height = 0x11;
                score = 0;
                running = true;
                player = new List<Vector2>() {new Vector2(width / 2, height / 2)}; //Player first position
                direction = -1; //Not move
                lastDirection = -1;
                foods = new List<Vector2>();
                for(int i = 0; i < maxFood; i++) {
                    randomFoodGenerator(0);
                }
            }



            public static void end() {
                running = false;
                sw.Stop();
                GameScreen.instance.setText(GameScreen.instance.game_direction, "Game Over");
                GameScreen.instance.setText(GameScreen.instance.game_detail, "Length: " + player.Count +" TIme: " + getTime());
            }



            public static int getTime() {
                return (int) sw.Elapsed.TotalSeconds;
            }



            public static void setRunning(bool running) {
                GameProgress.running = running;
            }



            public static bool isRunning() {
                return GameProgress.running;
            }



            public static int getScore() {
                return GameProgress.score;
            }



            public static bool setDirection(int direction) {
                if(!(direction >= 0 && direction < 4)) {
                    throw new Exception("undefined direction");
                }
                //이미 진행중인 방향이나 바로 반대방향으로는 진행불가
                if(!GameProgress.isRunning()
                    || (GameProgress.getDirection() != -1
                    && Math.Abs(GameProgress.lastDirection - direction) % 2 == 0)) {
                    return false;
                }
                GameProgress.direction = direction;
                return true;
            }



            public static int getDirection() {
                return direction;
            }



            public static int collisionCheck(Vector2 v2) { // -3: PlayerHead -2: Player, -1: Wall, 0: Nothing, 1: Food

                if(v2.x < 0 || v2.y < 0 || v2.x >= width || v2.y >= height) {
                    return -1;
                }

                foreach(Vector2 v2_2 in player) {
                    if(v2.isEqual(v2_2))
                        if(v2_2.isEqual(player[0]))
                            return -3;
                        else
                            return -2;
                }

                foreach(Vector2 v2_3 in foods) {
                    if(v2.isEqual(v2_3))
                        return 1;
                }

                return 0;
            }



            public static void randomFoodGenerator(int failCount) {
                
                Vector2 v2 = new Vector2(rx.Next(0, width), rx.Next(0, height));
                if(GameProgress.collisionCheck(v2) != 0) {
                    if(++failCount > 128) {
                        throw new Exception("Food generate fail");
                    }
                    randomFoodGenerator(failCount);
                } else {
                    foods.Add(v2);
                }
            }



            public static bool move() {
                int binaryDat = GameProgress.getDirection(); 
                if(binaryDat < 0) {
                    draw(new Vector2(-2, -2));
                    return true; //Not ready
                }else if(!sw.IsRunning)
                    sw.Start(); //타임워치 시작
                //한줄코딩 변태짓
                Vector2 loc = new Vector2(player[0].x + (binaryDat % 2 == 0 ? binaryDat > 1 ? 1 : -1 : 0), player[0].y + (binaryDat % 2 == 1 ? binaryDat > 1 ? 1 : -1 : 0));
                int collisionType = GameProgress.collisionCheck(loc); //충돌체크 TODO: 비효율적임
                bool canMove = collisionType >= 0; //움직임이 가능한가
                if(canMove) {
                    if(collisionType == 1) { // 음식
                        foods.RemoveAt(foods.IndexOf(loc));
                        player.Insert(0, loc); // 플레이어 머리부분으로 크기 증가
                        score += 10;
                        randomFoodGenerator(0); // 음식 재 생성
                    } else {
                        player.Insert(0, loc); // 한칸씩 앞으로 이동
                        player.RemoveAt(player.Count-1);
                    }
                    draw(new Vector2(-2, -2)); // 충돌한 부위가 없을때
                } else {
                    draw(loc); // 충돌한 부위가 있을떄
                }
                lastDirection = direction; //마지막 진행방향
                return canMove;
            }



            public static void draw(Vector2 whereCrash) {
                string result = "";
                bool crash = whereCrash.x != -2;
                for(int y = -1; y <= height; y++) {
                    for(int x = -1; x <= width; x++) {
                        if(crash) {
                            if(whereCrash.isEqual(new Vector2(x, y))) {
                                result += CRASH;
                                continue;
                            }   
                        }
                        switch(GameProgress.collisionCheck(new Vector2(x, y))) {
                        case -3:
                            result += PLAYER_HEAD;
                            break;
                        case -2:
                            result += PLAYER_BODY;
                            break;
                        case -1:
                            result += BORDER;
                            break;
                        case 0:
                            result += BLANK;
                            break;
                        case 1:
                            result += FOOD;
                            break;
                        default:
                            result += "?";
                            break;
                        }
                    }
                    result += "\n";
                }

                GameScreen.instance.setText(GameScreen.instance.game_filed, result);
                

                GameScreen.instance.setText(GameScreen.instance.game_detail, "Length: " + player.Count + " TIme: " + getTime());
                if(score > 0 && (sw.ElapsedMilliseconds % 1000) < 300) {
                    score--;
                }
                GameScreen.instance.setText(GameScreen.instance.game_score, score +" :Score");
            }



            public static void tick() {
                while(running) {
                    if(!move()) {
                        Console.WriteLine("GameEnd");
                        end();
                    }
                    Thread.Sleep(300);
                }
                StartScreen.setScore(GameProgress.getScore());
                Thread.Sleep(3000);
                GameScreen.instance.SafeClose();
            }
        }



        public static string directionToString(int direction) {
            switch(direction) {
            case 0:
                return "←";
            case 1:
                return "↑";
            case 2:
                return "→";
            case 3:
                return "↓";
            default:
                return "Press arrow to Start";
            }
        }

        private void GameScreen_FormClosing(object sender, FormClosingEventArgs e) {
            GameProgress.setRunning(false); //When window close
        }

        private void GameScreen_KeyDown(object sender, KeyEventArgs e) {
            bool valid;
            switch(e.KeyData) {
            case Keys.Left:
                valid = GameProgress.setDirection(0);
                break;
            case Keys.Up:
                valid = GameProgress.setDirection(1);
                break;
            case Keys.Right:
                valid = GameProgress.setDirection(2);
                break;
            case Keys.Down:
                valid = GameProgress.setDirection(3);
                break;
            default:
                valid = false;
                break;
            }
            if(valid)
                GameScreen.instance.setText(GameScreen.instance.game_direction, GameScreen.directionToString(GameProgress.getDirection()));
        }
    }
}
