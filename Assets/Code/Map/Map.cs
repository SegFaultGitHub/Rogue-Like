using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using System.Collections;

public enum Direction {
    None = 0,
    Up, Down, Left, Right
}

public class Map : MonoBehaviour {
    private class RoomEntry {
        public Direction OriginalDirection;
        public Vector2Int Position;
        public Dictionary<Direction, RoomEntry> Doors;
        public List<DirectionGroup> DirectionGroups;
        public bool Start, End;
        public bool Hub;
        public Room Room;
        public bool Drawn;

        public RoomEntry(Vector2Int position) {
            this.Position = position;
            this.Doors = new();
        }

        public void UpdateDirectionGroups() {
            this.DirectionGroups = new() { new() { Directions = this.Doors.Keys.ToList() } };
        }

        public Dictionary<Direction, RoomEntry> AccessibleDoors(Direction from) {
            List<Direction> directions = this.DirectionGroups.Find(directionGroup => directionGroup.Directions.Contains(OppositeDirection(from))).Directions;
            if (directions == null) {
                return new();
            }
            Dictionary<Direction, RoomEntry> roomEntries = new();
            directions.ForEach(direction => {
                roomEntries[direction] = this.Doors[direction];
            });
            return roomEntries;
        }
    }

    [Serializable]
    public struct DirectionGroup {
        public List<Direction> Directions;
    }

    private struct MapGraphEntry {
        public Direction From;
        public Vector2Int Position;

        public new string ToString() {
            return this.Position + "/" + this.From;
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
    [Serializable]
    public struct _MapLayoutData {
        public int MinI, MinJ, MaxI, MaxJ;
        public Vector2Int RoomSize;
        public Vector2Int DoorOffset;
        public Vector2Int RoomOffset;
        public Vector2Int TextureSize;
        public Texture2D Texture;
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

    [SerializeField] private List<Room> RoomTemplates;
    [SerializeField] private List<Mask> Masks;

    private Dictionary<Vector2Int, RoomEntry> RoomEntries;
    private Vector2Int CurrentRoomPosition = Vector2Int.zero;


    private Room CurrentRoom { get => this.RoomEntries[this.CurrentRoomPosition].Room; }
    public _MapLayoutData MapLayoutData;

    private PlayerHUD PlayerHUD;
    private Player Player;

    public void Start() {
        this.RoomTemplates = Resources.LoadAll<Room>("Rooms").Where(room => room.Enabled).ToList();

        this.GenerateMap();
        this.GenerateHubs();
        this.SetRooms();
        this.GenerateSpecialRooms();
        this.CreateMapLayoutSprite();
        this.PlayerHUD = GameObject.FindGameObjectWithTag("Player/PlayerHUD").GetComponent<PlayerHUD>();
        this.Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        this.ChangeRoom(Vector2Int.zero);
        // this.PlayerHUD.SetMapSprite(this.DrawFullMap());
    }

    public void ChangeRoom(Vector2Int position) {
        this.CurrentRoom.gameObject.SetActive(false);
        this.CurrentRoomPosition = position;
        Vector2 startingPosition = this.CurrentRoom.StartingPosition;
        this.Player.transform.position = new(startingPosition.x, startingPosition.y);
        if (!this.RoomEntries[this.CurrentRoomPosition].Drawn) {
            Sprite sprite = this.DrawRoomOnMapLayout(this.CurrentRoom);
            this.PlayerHUD.SetMapSprite(sprite);
        }
        this.PlayerHUD.MoveMapLayout(this.CurrentRoomPosition, this.MapLayoutData);

        this.CurrentRoom.gameObject.SetActive(true);
        FindObjectOfType<DebugUI>().SetMapPosition(this.CurrentRoomPosition);
    }

    public void ChangeRoom(Direction direction) {
        TransitionMask transitionInPrefab = Resources.Load<TransitionMask>("Transitions/TransitionIn");
        TransitionMask transitionOutPrefab = Resources.Load<TransitionMask>("Transitions/TransitionOut");

        TransitionMask transitionOut = Instantiate(transitionOutPrefab);
        transitionOut.Activate(direction)
            .setOnComplete(() => {
                Vector2 position;
                switch (direction) {
                    case Direction.Left:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x - 1, this.CurrentRoomPosition.y));
                        position = this.CurrentRoom.FromRightPosition;
                        this.Player.transform.position = new(position.x, position.y);
                        break;
                    case Direction.Right:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x + 1, this.CurrentRoomPosition.y));
                        position = this.CurrentRoom.FromLeftPosition;
                        this.Player.transform.position = new(position.x, position.y);
                        break;
                    case Direction.Up:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x, this.CurrentRoomPosition.y - 1));
                        position = this.CurrentRoom.FromBelowPosition;
                        this.Player.transform.position = new(position.x, position.y);
                        break;
                    case Direction.Down:
                        this.ChangeRoom(new Vector2Int(this.CurrentRoomPosition.x, this.CurrentRoomPosition.y + 1));
                        position = this.CurrentRoom.FromAbovePosition;
                        this.Player.transform.position = new(position.x, position.y);
                        break;
                };

                TransitionMask transitionIn = Instantiate(transitionInPrefab);
                Destroy(transitionOut.gameObject);
                transitionIn.Activate(direction)
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
        FindObjectOfType<DebugUI>().SetSeed(this.Seed);

        List<Direction> directions = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        int count = Random.Range(this.MinDirections, this.MaxDirections + 1);
        float ratio = this.IncreasePathLength ? 4f / count : 1;
        directions = Utils.Sample(directions, count);

        this.RoomEntries = new() {
            [new(0, 0)] = new RoomEntry(new(0, 0)) { Hub = false, OriginalDirection = Direction.None }
        };
        this.RoomEntries[new(0, 0)].Start = true;

        directions.ForEach(direction => {
            CreatedRoom room = this.LinkRooms(direction, new(0, 0), direction);
            float pathLength = Random.Range(this.MinLength, this.MaxLength + 1) * ratio * 2;
            float alternatePathRate = 1f / Mathf.Pow(pathLength, this.AlternatePathDecreaseRatio);

            this.GeneratePath(direction, room.Position, direction, direction, pathLength, alternatePathRate);
        });
    }

    private void SetRooms() {
        Dictionary<Direction, RoomMask> masks = new();
        this.Masks.ForEach(mask => masks[mask.Direction] = mask.RoomMask);

        this.RoomEntries.Values.ToList().ForEach(roomEntry => {
            List<Room> rooms = this.RoomTemplates.Where(room => this.IsRoomValid(room, roomEntry.Doors.Keys.ToList())).ToList();
            Room room = null;
            if (roomEntry.Hub) {
                List<Room> hubs = rooms.Where(room => {
                    return room.Hub && this.DirectionGroupsMatch(room, roomEntry);
                }).ToList();
                if (hubs.Count > 0) {
                    room = Utils.Sample(hubs);
                }
            } else {
                room = Utils.Sample(rooms.Where(room => !room.Hub).ToList());
            }
            if (room == null) {
                room = Utils.Sample(rooms.Where(room => !room.Hub).ToList());
            }
            roomEntry.Room = this.SetRoom(room, roomEntry.Doors.Keys.Select(key => masks[key]).ToList(), roomEntry.Position);
            roomEntry.Room.Hub = roomEntry.Hub;
            roomEntry.Room.OriginalDirection = roomEntry.OriginalDirection;
            roomEntry.Room.Directions = roomEntry.Doors.Keys.ToArray();
            if (this.MapLayoutData.MinI > roomEntry.Room.Position.x) { this.MapLayoutData.MinI = roomEntry.Room.Position.x; }
            if (this.MapLayoutData.MaxI < roomEntry.Room.Position.x) { this.MapLayoutData.MaxI = roomEntry.Room.Position.x; }
            if (this.MapLayoutData.MinJ > roomEntry.Room.Position.y) { this.MapLayoutData.MinJ = roomEntry.Room.Position.y; }
            if (this.MapLayoutData.MaxJ < roomEntry.Room.Position.y) { this.MapLayoutData.MaxJ = roomEntry.Room.Position.y; }
        });
    }

    private bool DirectionGroupsMatch(Room room, RoomEntry roomEntry) {
        return roomEntry.DirectionGroups.All(roomEntryDirectionGroup => {
            if (roomEntryDirectionGroup.Directions.Count == 0) { return false; }
            DirectionGroup roomDirectionGroup = room.DirectionGroups.Find(roomDirectionGroup => roomDirectionGroup.Directions.Contains(roomEntryDirectionGroup.Directions[0]));

            return roomEntryDirectionGroup.Directions.All(direction => roomDirectionGroup.Directions.Contains(direction));
        });
    }

    private void GenerateHubs() {
        this.RoomEntries.Values.ToList().ForEach(roomEntry => roomEntry.UpdateDirectionGroups());

        List<List<DirectionGroup>> allPossibleHubDirectionGroups = this.RoomTemplates.Where(room => room.Hub).Select(room => room.DirectionGroups).ToList();

        this.RoomEntries.Values.ToList().Where(roomEntry => roomEntry.Hub).ToList().ForEach(roomEntry => {
            List<List<DirectionGroup>> currentPossibleDirectionGroups = Utils
                .Shuffle(allPossibleHubDirectionGroups)
                .Select(directionGroups => {
                    return directionGroups.Select(directionGroup => {
                        return new DirectionGroup() { Directions = directionGroup.Directions.Where(direction => roomEntry.Doors.Keys.Contains(direction)).ToList() };
                    }).ToList();
                }).ToList();

            foreach (List<DirectionGroup> directionGroups in currentPossibleDirectionGroups) {
                roomEntry.DirectionGroups = directionGroups;
                if (this.CheckPaths()) {
                    return;
                }
            }
            roomEntry.DirectionGroups = new() { new() { Directions = roomEntry.Doors.Keys.ToList() } };
            roomEntry.Hub = false;
        });
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

    private void GeneratePath(Direction originalDirection, Vector2Int position, Direction previousDirection, Direction mainDirection, float pathLength, float alternatePathRate) {
        if (pathLength <= 0) {
            this.RoomEntries[position].End = true;
            return;
        }

        this.RoomEntries[position].End = false;

        List<Direction> directions = new() { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        directions.RemoveAll(_direction => _direction == OppositeDirection(previousDirection) || _direction == OppositeDirection(mainDirection));
        Direction nextDirection = Utils.Sample(directions);

        CreatedRoom room = this.LinkRooms(originalDirection, position, nextDirection);
        if (!room.Created && pathLength <= 1) { pathLength++; }
        this.RoomEntries[room.Position].End = false;
        this.GeneratePath(originalDirection, room.Position, nextDirection, mainDirection, pathLength - 1, alternatePathRate);

        if (Utils.Rate(alternatePathRate)) {
            directions.Remove(nextDirection);
            nextDirection = Utils.Sample(directions);
            room = this.LinkRooms(originalDirection, position, nextDirection);
            if (!room.Created && pathLength <= 1) { pathLength++; }
            this.RoomEntries[room.Position].End = false;
            this.GeneratePath(originalDirection, room.Position, nextDirection, mainDirection, pathLength - 1, alternatePathRate * 0.75f);
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

    private CreatedRoom LinkRooms(Direction originalDirection, Vector2Int position, Direction direction) {
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
            this.RoomEntries[adjacentRoom.Position] = new RoomEntry(adjacentRoom.Position) { OriginalDirection = originalDirection, Hub = false };

            // Setting bidirectional doorway
            this.RoomEntries[adjacentRoom.Position].Doors[OppositeDirection(direction)] = this.RoomEntries[position];
            this.RoomEntries[position].Doors[direction] = this.RoomEntries[adjacentRoom.Position];

            return new CreatedRoom { Position = adjacentRoom.Position, Created = true };
        } else {
            // Room already existing, setting doorway between the rooms
            this.RoomEntries[adjacentRoom.Position].Doors[OppositeDirection(direction)] = this.RoomEntries[position];
            this.RoomEntries[adjacentRoom.Position].Hub = true;
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

        masks.ForEach(mask => {
            Tilemap[] maskLayers = mask.GetComponentsInChildren<Tilemap>();

            for (int layerIndex = 0; layerIndex < maskLayers.Length && layerIndex < roomLayers.Length; layerIndex++) {
                Tilemap maskLayer = maskLayers[layerIndex];
                Tilemap roomLayer = roomLayers[layerIndex];
                maskLayer.CompressBounds();
                BoundsInt maskBounds = maskLayer.cellBounds;
                roomLayer.CompressBounds();
                BoundsInt roomBounds = roomLayer.cellBounds;
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

    private Sprite DrawFullMap() {
        this.RoomEntries.Values.ToList().ForEach(roomEntry => {
            this.DrawRoomOnMapLayout(roomEntry.Room);
        });
        return Sprite.Create(
            this.MapLayoutData.Texture,
            new(0, 0, this.MapLayoutData.TextureSize.x, this.MapLayoutData.TextureSize.y),
            Vector2.zero,
            16
        );
    }

    private Sprite DrawRoomOnMapLayout(Room room) {
        if (this.RoomEntries[room.Position].Drawn) {
            return Sprite.Create(
                this.MapLayoutData.Texture,
                new(0, 0, this.MapLayoutData.TextureSize.x, this.MapLayoutData.TextureSize.y),
                Vector2.zero,
                16
            );
        }
        Vector2Int upDownDoorSize = new(
            this.MapLayoutData.RoomSize.x - 2 * this.MapLayoutData.DoorOffset.x,
            this.MapLayoutData.RoomOffset.y
        );
        Vector2Int leftRightDoorSize = new(
            this.MapLayoutData.RoomOffset.x,
            this.MapLayoutData.RoomSize.y - 2 * this.MapLayoutData.DoorOffset.y
        );
        Color32 roomColor = new(255, 255, 255, 192);
        Color32 doorColor = new(255, 255, 255, 128);
        int localX = (room.Position.x - this.MapLayoutData.MinI) * this.MapLayoutData.RoomSize.x;
        int localY = (room.Position.y - this.MapLayoutData.MinJ) * this.MapLayoutData.RoomSize.y;
        this.SetPixels(
            this.MapLayoutData.Texture,
            new(localX + this.MapLayoutData.RoomOffset.x, localY + this.MapLayoutData.RoomOffset.y),
            new(this.MapLayoutData.RoomSize.x - 2 * this.MapLayoutData.RoomOffset.x, this.MapLayoutData.RoomSize.y - 2 * this.MapLayoutData.RoomOffset.y),
            roomColor
        );

        foreach (Direction direction in room.Directions) {
            switch (direction) {
                case Direction.Up:
                    this.SetPixels(
                        this.MapLayoutData.Texture,
                        new(localX + this.MapLayoutData.DoorOffset.x, localY),
                        upDownDoorSize,
                        doorColor
                    );
                    break;
                case Direction.Left:
                    this.SetPixels(
                        this.MapLayoutData.Texture,
                        new(localX, localY + this.MapLayoutData.DoorOffset.y),
                        leftRightDoorSize,
                        doorColor
                    );
                    break;
                case Direction.Down:
                    this.SetPixels(
                        this.MapLayoutData.Texture,
                        new(localX + this.MapLayoutData.DoorOffset.x, localY + this.MapLayoutData.RoomSize.y - this.MapLayoutData.RoomOffset.y),
                        upDownDoorSize,
                        doorColor
                    );
                    break;
                case Direction.Right:
                    this.SetPixels(
                        this.MapLayoutData.Texture,
                        new(localX + this.MapLayoutData.RoomSize.x - this.MapLayoutData.RoomOffset.x, localY + this.MapLayoutData.DoorOffset.y),
                        leftRightDoorSize,
                        doorColor
                    );
                    break;
            }
        }

        this.MapLayoutData.Texture.Apply();
        this.RoomEntries[room.Position].Drawn = true;
        return Sprite.Create(
            this.MapLayoutData.Texture,
            new(0, 0, this.MapLayoutData.TextureSize.x, this.MapLayoutData.TextureSize.y),
            Vector2.zero,
            16
        );
    }

    private void CreateMapLayoutSprite() {
        Vector2Int mapSize = new(this.MapLayoutData.MaxI - this.MapLayoutData.MinI + 1, this.MapLayoutData.MaxJ - this.MapLayoutData.MinJ + 1);
        this.MapLayoutData.TextureSize = new(mapSize.x * this.MapLayoutData.RoomSize.x, mapSize.y * this.MapLayoutData.RoomSize.y);
        this.MapLayoutData.Texture = new(this.MapLayoutData.TextureSize.x, this.MapLayoutData.TextureSize.y);
        this.SetPixels(this.MapLayoutData.Texture, Vector2Int.zero, this.MapLayoutData.TextureSize, new Color32(255, 255, 255, 0));
        this.MapLayoutData.Texture.filterMode = FilterMode.Point;
    }

    private void SetPixels(Texture2D texture, Vector2Int position, Vector2Int size, Color32 color) {
        texture.SetPixels32(
            position.x, position.y, size.x, size.y,
            Enumerable.Repeat(color, size.x * size.y).ToArray()
        );
    }

    private bool CheckPaths() {
        Dictionary<Vector2Int, bool> visitedRooms = new() {
            [Vector2Int.zero] = true,
        };
        Dictionary<MapGraphEntry, bool> foo = new() {
            [new() { From = Direction.None, Position = Vector2Int.zero }] = true,
        };

        Dictionary<Direction, RoomEntry> doors = this.RoomEntries[new()].Doors;
        foreach (KeyValuePair<Direction, RoomEntry> keyValue in doors) {
            this._CheckPath(keyValue.Value.Position, keyValue.Key, visitedRooms, foo);
        }

        return visitedRooms.Count == this.RoomEntries.Count;
    }

    private void _CheckPath(Vector2Int position, Direction from, Dictionary<Vector2Int, bool> visitedRooms, Dictionary<MapGraphEntry, bool> foo) {
        Dictionary<Direction, RoomEntry> keyValues = this.RoomEntries[position].AccessibleDoors(from);
        foreach (KeyValuePair<Direction, RoomEntry> keyValue in keyValues) {
            MapGraphEntry mapGraphEntry = new() {
                Position = keyValue.Value.Position,
                From = from,
            };

            if (foo.ContainsKey(mapGraphEntry)) { continue; }

            foo[mapGraphEntry] = true;
            visitedRooms[keyValue.Value.Position] = true;

            this._CheckPath(keyValue.Value.Position, keyValue.Key, visitedRooms, foo);
        }
    }
}
