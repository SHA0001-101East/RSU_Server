using System.Collections;
using System;
using System.Threading;

namespace RSU_Server
{
    public class Clock
    {
        public const int updateRate = 200;
        public int ticksSinceStartUp = 0;

        public Server_Game game;
        public bool run = false;

        private DateTime time1;
        private DateTime time2;
        private TimeSpan dt;
        private int delay;

        public Clock(Server_Game game1)
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
            if (run)
            {
                time1 = DateTime.Now;

                ticksSinceStartUp++;
                //call on the game to do whatever it has to do

                time2 = DateTime.Now;

                dt = time2 - time1;
                delay = (int)Math.Floor(Functions.Max(0, 200 - (float)dt.TotalMilliseconds)); //change math.floor to own function

                Thread.Sleep(delay);
                InternalClock();
                return;
            }
            run = false;
        }
    }
}