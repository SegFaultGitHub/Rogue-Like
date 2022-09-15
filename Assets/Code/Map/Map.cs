using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public enum Direction {
    None = 0,
    Up, Down, Left, Right
}

public class Map : MonoBehaviour {
    private class RoomEntry {
        public Vector2Int Position;
        public Dictionary<Direction, RoomEntry> Doors;
        public bool Start, End;
        public Room Room;

        public RoomEntry(Vector2Int position) {
            this.Position = position;
            this.Doors = new();
        }
    }

    private struct CreatedRoom {
        public Vector2Int Position;
        public bool Created;
    }
    private struct AdjacentRoom {
        public Vector2Int Position;
        public Direction Door;
    }
    [Serializable]
    private struct Mask {
        public Direction Direction;
        public RoomMask RoomMask;
    }

    [Header("Map attributes")]
    [Tooltip("The minimum number of doors in the first room")]
    [Range(1, 4)]
    [SerializeField] private int MinDirections = 1;
    [Tooltip("The maximum number of doors in the first room")]
    [Range(1, 4)]
    [SerializeField] private int MaxDirections = 4;
    [Tooltip("The minimum length of each path from the start")]
    [Range(1, 15)]
    [SerializeField] private int MinLength = 1;
    [Tooltip("The minimum length of each path from the start")]
    [Range(1, 15)]
    [SerializeField] private int MaxLength = 4;
    [SerializeField][Range(1, 3)] private float AlternatePathDecreaseRatio = 1.2f;
    [Tooltip("Check if you want the paths to be longer if there is fewer paths")]
    [SerializeField] private bool IncreasePathLength = true;
    [SerializeField] private int Seed;

    [SerializeField] private List<Room> Rooms;
    [SerializeField] private List<Mask> Masks;

    private Dictionary<Vector2Int, RoomEntry> RoomEntries;
    private Vector2Int ActiveRoom = new();
    private Room CurrentRoom { get => this.RoomEntries[this.ActiveRoom].Room; }

    public void Start() {
        this.GenerateMap();
        this.GenerateSpecialRooms();

        Vector2 startingPosition = this.CurrentRoom.StartingPosition;
        GameObject.FindGameObjectWithTag("Player").transform.position = new(startingPosition.x, startingPosition.y);
    }

    public void ChangeRoom(Direction direction) {
        TransitionMask transitionInPrefab = Resources.Load<TransitionMask>("Transitions/TransitionIn");
        TransitionMask transitionOutPrefab = Resources.Load<TransitionMask>("Transitions/TransitionOut");

        TransitionMask transitionOut = Instantiate(transitionOutPrefab);
        transitionOut.Activate()
            .setOnComplete(() => {

                this.CurrentRoom.gameObject.SetActive(false);
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector2 position;
                switch (direction) {
                    case Direction.Left:
                        this.ActiveRoom = new(this.ActiveRoom.x - 1, this.ActiveRoom.y);
                        position = this.CurrentRoom.FromRightPosition;
                        player.transform.position = new(position.x, position.y);
                        break;
                    case Direction.Right:
                        this.ActiveRoom = new(this.ActiveRoom.x + 1, this.ActiveRoom.y);
                        position = this.CurrentRoom.FromLeftPosition;
                        player.transform.position = new(position.x, position.y);
                        break;
                    case Direction.Up:
                        this.ActiveRoom = new(this.ActiveRoom.x, this.ActiveRoom.y - 1);
                        position = this.CurrentRoom.FromBelowPosition;
                        player.transform.position = new(position.x, position.y);
                        break;
                    case Direction.Down:
                        this.ActiveRoom = new(this.ActiveRoom.x, this.ActiveRoom.y + 1);
                        position = this.CurrentRoom.FromAbovePosition;
                        player.transform.position = new(position.x, position.y);
                        break;
                };
                this.CurrentRoom.gameObject.SetActive(true);

                TransitionMask transitionIn = Instantiate(transitionInPrefab);
                Destroy(transitionOut.gameObject);
                transitionIn.Activate()
                    .setOnComplete(() => {
                        Destroy(transitionIn.gameObject);
                    });
            });
    }

    private void GenerateMap() {
        if (this.Seed != 0) {
            Random.InitState(this.Seed);
        } else {
            this.Seed = Random.seed;
        }

        List<Direction> directions = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        int count = Random.Range(this.MinDirections, this.MaxDirections + 1);
        float ratio = this.IncreasePathLength ? 4f / count : 1;
        directions = Utils.Sample(directions, count);

        this.RoomEntries = new() {
            [new(0, 0)] = new RoomEntry(new(0, 0))
        };
        this.RoomEntries[new(0, 0)].Start = true;

        directions.ForEach(direction => {
            CreatedRoom room = this.LinkRooms(new(0, 0), direction);
            float pathLength = Random.Range(this.MinLength, this.MaxLength + 1) * ratio;
            float alternatePathRate = 1f / Mathf.Pow(pathLength, this.AlternatePathDecreaseRatio);

            this.GeneratePath(room.Position, direction, direction, pathLength, alternatePathRate);
        });

        Dictionary<Direction, RoomMask> masks = new();
        this.Masks.ForEach(mask => masks[mask.Direction] = mask.RoomMask);

        this.RoomEntries.Values.ToList().ForEach(roomEntry => {
            List<Room> _;
            Room room = Utils.Sample(
                _ = this.Rooms.Where(room => room.Enabled && this.IsRoomValid(room, roomEntry.Doors.Keys.ToList())).ToList()
            );
            roomEntry.Room = this.SetRoom(room, roomEntry.Doors.Keys.Select(key => masks[key]).ToList(), roomEntry.Position);
            roomEntry.Room.Directions = roomEntry.Doors.Keys.ToArray();
        });

        this.CurrentRoom.gameObject.SetActive(true);
    }

    private void GenerateSpecialRooms() {
        List<Room> specialRooms = Utils.Shuffle(
            this.RoomEntries.Values
                .Where(roomEntry => roomEntry.End).ToList()
                .Select(roomEntry => roomEntry.Room).ToList()
        );
        specialRooms[0].GenerateBoss();
        float x = specialRooms.Count;
        float rate = x / (3 * (x - 1) + 1);
        specialRooms.Skip(1).ToList().ForEach(room => {
            if (Utils.Rate(rate)) { room.GenerateTreasure(); }
        });
    }

    private void GeneratePath(Vector2Int position, Direction previousDirection, Direction mainDirection, float pathLength, float alternatePathRate) {
        if (pathLength <= 0) {
            this.RoomEntries[position].End = true;
            return;
        }

        this.RoomEntries[position].End = false;

        List<Direction> directions = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        directions.RemoveAll(_direction => _direction == OppositeDirection(previousDirection) || _direction == OppositeDirection(mainDirection));
        Direction nextDirection = Utils.Sample(directions);

        CreatedRoom room = this.LinkRooms(position, nextDirection);
        if (!room.Created && pathLength <= 1) { pathLength++; }
        this.RoomEntries[room.Position].End = false;
        this.GeneratePath(room.Position, nextDirection, mainDirection, pathLength - 1, alternatePathRate);

        if (Utils.Rate(alternatePathRate)) {
            directions.Remove(nextDirection);
            nextDirection = Utils.Sample(directions);
            room = this.LinkRooms(position, nextDirection);
            if (!room.Created && pathLength <= 1) { pathLength++; }
            this.RoomEntries[room.Position].End = false;
            this.GeneratePath(room.Position, nextDirection, mainDirection, pathLength - 1, alternatePathRate * 0.75f);
        }
    }

    private static Direction OppositeDirection(Direction direction) {
        return direction switch {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new("[Map:OppositeDirection] Unexpection direction " + direction + "."),
        };
    }

    private CreatedRoom LinkRooms(Vector2Int position, Direction direction) {
        int x = position.x;
        int y = position.y;
        var adjacentRoom = direction switch {
            Direction.Up => new AdjacentRoom { Position = new(x, y - 1) },
            Direction.Down => new AdjacentRoom { Position = new(x, y + 1) },
            Direction.Left => new AdjacentRoom { Position = new(x - 1, y) },
            Direction.Right => new AdjacentRoom { Position = new(x + 1, y) },
            _ => throw new("[Map:LinkRooms] Unexpection direction " + direction + "."),
        };
        if (!this.RoomEntries.ContainsKey(adjacentRoom.Position)) {
            // Room empty
            // Setting new room
            this.RoomEntries[adjacentRoom.Position] = new RoomEntry(adjacentRoom.Position);

            // Setting bidirectional doorway
            this.RoomEntries[adjacentRoom.Position].Doors[OppositeDirection(direction)] = this.RoomEntries[position];
            this.RoomEntries[position].Doors[direction] = this.RoomEntries[adjacentRoom.Position];

            return new CreatedRoom { Position = adjacentRoom.Position, Created = true };
        } else {
            // Room already existing, setting doorway between the rooms
            this.RoomEntries[adjacentRoom.Position].Doors[OppositeDirection(direction)] = this.RoomEntries[position];
            this.RoomEntries[position].Doors[direction] = this.RoomEntries[adjacentRoom.Position];

            return new CreatedRoom { Position = adjacentRoom.Position, Created = false };
        }
    }

    private bool IsRoomValid(Room room, List<Direction> directions) {
        return directions.All(direction => {
            return direction switch {
                Direction.Up => room.UpDoorAllowed,
                Direction.Right => room.RightDoorAllowed,
                Direction.Down => room.DownDoorAllowed,
                Direction.Left => room.LeftDoorAllowed,
                _ => throw new("[Map:IsRoomValid] Unexpection direction " + direction + "."),
            };
        });
    }

    private Room SetRoom(Room roomPrefab, List<RoomMask> masks, Vector2Int position) {
        Room room = Instantiate(roomPrefab, this.transform);
        room.Position = position;
        room.gameObject.SetActive(false);
        room.name = "Room: " + position.x + "/" + position.y + " [" + roomPrefab.name + "]";

        Tilemap[] roomLayers = room.GetComponentsInChildren<Tilemap>();
        roomLayers[0].CompressBounds();
        BoundsInt roomBounds = roomLayers[0].cellBounds;

        masks.ForEach(mask => {
            Tilemap[] maskLayers = mask.GetComponentsInChildren<Tilemap>();
            maskLayers[0].CompressBounds();
            BoundsInt maskBounds = maskLayers[0].cellBounds;

            for (int layerIndex = 0; layerIndex < maskLayers.Length && layerIndex < roomLayers.Length; layerIndex++) {
                Tilemap maskLayer = maskLayers[layerIndex];
                Tilemap roomLayer = roomLayers[layerIndex];
                for (int x = maskBounds.min.x, localX = 0; x < maskBounds.max.x; x++, localX++) {
                    for (int y = maskBounds.min.y, localY = 0; y < maskBounds.max.y; y++, localY++) {
                        for (int z = maskBounds.min.z, localZ = 0; z < maskBounds.max.z; z++, localZ++) {
                            TileBase tile = maskLayer.GetTile(new(x, y, z));
                            if (tile == mask.Mask) { continue; }
                            roomLayer.SetTile(new(roomBounds.min.x + localX, roomBounds.min.y + localY, roomBounds.min.z + localZ), tile);
                        }
                    }
                }
                roomLayer.RefreshAllTiles();
            }
        });

        return room;
    }
}