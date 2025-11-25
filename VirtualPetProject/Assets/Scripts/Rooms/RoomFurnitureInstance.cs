using UnityEngine;

public class RoomFurnitureInstance : MonoBehaviour
{
    public PlacedFurniture Data { get; private set; }
    public FurnitureDefinition Definition { get; private set; }

    public void Initialize(PlacedFurniture data, FurnitureDefinition definition)
    {
        Data = data;
        Definition = definition;
    }
}

