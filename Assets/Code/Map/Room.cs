using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour {
    public Vector2 StartingPosition;
    public bool Enabled;

    [HideInInspector] public Vector2Int Position;
    [HideInInspector] public Vector2 FromLeftPosition;
    [HideInInspector] public Vector2 FromRightPosition;
    [HideInInspector] public Vector2 FromBelowPosition;
    [HideInInspector] public Vector2 FromAbovePosition;

    [Header("Allowed Doors")]
    public bool LeftDoorAllowed = true;
    public bool RightDoorAllowed = true;
    public bool UpDoorAllowed = true;
    public bool DownDoorAllowed = true;

    [Header("Doorways")]
    public Direction[] Directions;

    [Header("Special")]
    [SerializeField] private bool BossRoom;
    [SerializeField] private bool TreasureRoom;

    public void Awake() {
        this.FromLeftPosition = new(-15f, -1.5f);
        this.FromRightPosition = new(15f, -1.5f);
        this.FromBelowPosition = new(0, -7);
        this.FromAbovePosition = new(0, 6);
        this.transform.Find("Treasures").gameObject.SetActive(false);
    }

    public void GenerateBoss() {
        this.BossRoom = true;
        this.gameObject.name += " (Boss)";
    }

    public void GenerateTreasure() {
        this.transform.Find("Treasures").gameObject.SetActive(true);
        this.TreasureRoom = true;
        this.gameObject.name += " (Treasure)";
    }

    public void SpawnEnemies() {

    }
}
