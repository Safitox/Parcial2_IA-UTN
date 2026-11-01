using System.Collections.Generic;
using UnityEngine;

public class ObjectsOfInterestManager : MonoBehaviour
{
    private static List<ObjectOfInterest> _items = new List<ObjectOfInterest>();
    public static List<ObjectOfInterest> Items => _items;
    public static void AddItem(ObjectOfInterest ooi)
    {
        if (!_items.Contains(ooi))
        {
            _items.Add(ooi);
        }
    }
    public static void RemoveItem(ObjectOfInterest ooi)
    {
        _items.Remove(ooi);
    }
    private void OnEnable()
    {
        if (TryGetComponent(out ObjectOfInterest ooi))
        {
            _items.Add(ooi);
        }
    }
    private void OnDisable()
    {
        if (TryGetComponent(out ObjectOfInterest ooi))
        {
            _items.Remove(ooi);
        }
    }
}