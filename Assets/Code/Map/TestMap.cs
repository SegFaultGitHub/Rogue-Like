using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMap : MonoBehaviour {
    [SerializeField] private List<Room> Rooms;

    public void Start() {
        Room roomPrefab = Utils.Sample(this.Rooms);
        Room room = Instantiate(roomPrefab);
        Vector2Int position = new();
        room.Position = position;
        room.name = "Room: " + position.x + "/" + position.y + " [" + roomPrefab.name + "]";
    }
}
