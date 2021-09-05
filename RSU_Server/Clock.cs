using System.Collections;
using System;
using System.Threading;

namespace RSU_Server
{
    public class Clock
    {
        public const int nodeUpdateTick = 200;
        public const int gameUpdateTick = 16;

        public Game game;
        public bool run = false;

        private DateTime time1;
        private DateTime time2;
        private TimeSpan dt;
        private int delay;

        public Clock(Game game1)
        {
            game = game1;
        }

        public void StartInternalClock()
        {
            run = true;
            InternalClock();
        }

        private void InternalClock()
        {
            time1 = DateTime.Now;

            //call on the game to do whatever it has to do


            time2 = DateTime.Now;

            dt = time2 - time1;
            delay = (int)Math.Floor(Functions.Max(0, 200 - (float)dt.TotalMilliseconds)); //change math.floor to own function

            if (run)
            {
                Thread.Sleep(delay);
                InternalClock();
            }
        }
    }
}