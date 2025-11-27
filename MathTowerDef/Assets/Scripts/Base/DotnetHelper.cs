using System.Collections.Generic;

public static class DotnetHelper {
    public static bool tryRemove<TKey, TValue>(
        this Dictionary<TKey, TValue> dict,
        TKey key,
        out TValue value) {
        if (dict.TryGetValue(key, out value)) {
            // .Remove()는 내부적으로 같은 해시 인덱스를 다시 탐색
            // 하지만 여기서 TryGetValue 성공 시, Remove는 O(1)에 가까움 (해시 충돌 적으면)
            return dict.Remove(key);
        }
        return false;
    }

    public static void swapRemoveAt<T>(this List<T> list, int idx) {
        int lastIdx = list.Count - 1;
        list[idx] = list[lastIdx];
        list.RemoveAt(lastIdx);
    }

    private static readonly System.Random _random = new System.Random();

    public static void shuffle<T>(this IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = _random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]); // swap
        }
    }
}
