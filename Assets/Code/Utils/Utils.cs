using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Utils {
    public static T RandomRange<T>(List<(float, T)> o) {
        float total = 0;
        foreach ((float value, T _) in o) {
            total += value;
        }
        float choice = Random.Range(0, total);
        float index = 0;
        foreach ((float value, T item) in o) {
            index += value;
            if (choice <= index)
                return item;
        }

        return o[^1].Item2;
    }

    public static T Sample<T>(List<T> list) {
        if (list.Count == 0) {
            Debug.LogError("Trying to sample an empty list");
            return default;
        }
        return list[Random.Range(0, list.Count)];
    }

    public static List<T> Shuffle<T>(List<T> list) {
        return Sample(list, list.Count);
    }

    public static List<T> Sample<T>(List<T> list, int n) {
        List<T> clone = new();
        clone.AddRange(list);

        List<T> result = new();
        while (result.Count < n && clone.Count > 0) {
            T element = Sample(clone);
            clone.Remove(element);
            result.Add(element);
        }
        return result;
    }

    public static bool Rate(float rate) {
        return Random.Range(0, 1f) < rate;
    }
}
