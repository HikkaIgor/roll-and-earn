using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace RollAndEarn
{
    public static class CoroutineUtils
    {
        private static readonly SynchronizationContext MainContext = SynchronizationContext.Current;

        public static UniTask DelayAsync(float seconds)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds));
        }

        public static void RunOnMainThread(Action action)
        {
            MainContext.Post(_ => action(), null);
        }
    }
}
