﻿namespace MineLW.API.Math
{
    public static class MathHelper
    {
        public static int RoundUp(int number, int interval)
        {
            if (interval == 0)
                return 0;

            if (number == 0)
                return interval;

            if (number < 0)
                interval *= -1;

            var i = number % interval;
            return i == 0 ? number : number + interval - i;
        }
    }
}