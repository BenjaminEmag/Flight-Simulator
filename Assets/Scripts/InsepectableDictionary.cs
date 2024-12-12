
// serializable dictionary
using static AircraftController;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public enum keys
{
    W,
    A,
    S,
    D,
    UP,
    Left,
    Right,
    Down
}

[System.Serializable]
public struct UIKey
{
    public keys Key;
    public Image Values;
}

public class InsepectableDictionary : MonoBehaviour
{
    // trick the unity to show your data as a dictionary.
    // unity dont have a nice way to show dictionaries on the inspector
    [SerializeField] public List<UIKey> keys;

    // use this to get the dictionary out of the nodes, in order to use it in your algorithms
    public Dictionary<keys, Image> ToDictionary()
    {
        Dictionary<keys, Image> ret = new Dictionary<keys, Image>();

        foreach (var entry in keys)
        {
            if (!ret.ContainsKey(entry.Key))
            {
                ret.Add(entry.Key, entry.Values);
            }

        }

        return ret;
    }
}