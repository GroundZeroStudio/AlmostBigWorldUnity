using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static bool RemoveInList<T>(List<T> list, T element) where T : class
    {
        for (int i = 0; i < list.Count; ++i)
        {
            if (list[i] == element)
            {
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}
