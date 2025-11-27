using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base {
    public static class Utils {
        public static void shuffle<T>(this List<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static async Awaitable awaitAll(IEnumerable<Awaitable> awaitables) {
            foreach (var awaitable in awaitables) {
                await awaitable;
            }
        }

        public static async Awaitable<bool> until(Func<bool> condition, float timeout = -1) {
            float startTime = Time.time;
            while (true) {
                if (condition() == true) 
                    return true;

                await Awaitable.NextFrameAsync();

                float elapsedTime = Time.time - startTime;
                if(elapsedTime > timeout) {
                    return false;
                }
            }
        }
    }
}
