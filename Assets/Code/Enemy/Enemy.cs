using UnityEngine;

public enum Behaviour {
    Idle, FocusingPlayer
}

public enum RandomWalkType {
    StraightLine, RandomDirections
}

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour {
    protected Animator Animator;
    private Rigidbody2D Rigidbody;
    protected GameObject Sprites;

    #region Movement
    [SerializeField] private float MovementSpeed;
    [SerializeField] protected float NoiseScale = 0.4f;
    public Vector2 MovementDirection { get; set; }
    protected int MovementSeed;
    protected float XOff, YOff;

    [SerializeField] protected RandomWalkType randomWalkType;
    #endregion

    protected Player Player;

    [Header("AI")]
    protected float StartMoving = 0;
    protected float MoveUntil = 0;
    protected float[] MoveDuration = new[] { 0.5f, 3.5f }; // in seconds
    protected float[] StayDuration = new[] { 1.5f, 5f };   // in seconds
    [SerializeField] protected Behaviour Behaviour;

    public void Start() {
        this.Animator = this.GetComponent<Animator>();
        this.Rigidbody = this.GetComponentInChildren<Rigidbody2D>();
        this.MovementSeed = (int) Random.Range((float) -1e3, (float) 1e3);
        this.XOff = (int) Random.Range((float) -1e3, (float) 1e3);
        this.YOff = (int) Random.Range((float) -1e3, (float) 1e3);
        this.Sprites = this.transform.Find("Sprites").gameObject;
        this.Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    protected void AdjustDirection() {
        if (this.MovementDirection.x > 0) {
            Vector3 scale = this.Sprites.transform.localScale;
            this.Sprites.transform.localScale = new(1, scale.y, scale.z);
        } else if (this.MovementDirection.x < 0) {
            Vector3 scale = this.Sprites.transform.localScale;
            this.Sprites.transform.localScale = new(-1, scale.y, scale.z);
        }
        this.Animator.SetBool("IsMoving", this.MovementDirection.sqrMagnitude != 0);
    }

    protected void RandomWalk() {
        float now = Time.time;
        if (this.StartMoving < now && now < this.MoveUntil) {
            // Currently moving
            switch (this.randomWalkType) {
                case RandomWalkType.RandomDirections:
                    this.MovementDirection = new(
                        Mathf.PerlinNoise(now * this.NoiseScale + this.XOff, this.MovementSeed) * 2 - 1,
                        Mathf.PerlinNoise(this.MovementSeed, now * this.NoiseScale + this.YOff) * 2 - 1
                    );
                    break;
                case RandomWalkType.StraightLine:
                    if (this.MovementDirection.sqrMagnitude == 0) {
                        this.MovementDirection = new(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    }
                    break;
            }
        } else if (now < this.StartMoving) {
            // Waiting to move
            // this.MovementDirection /= 5;
            //if (this.MovementDirection.magnitude < 1)
            this.MovementDirection *= 0;
        } else {
            // Just ended moving
            this.StartMoving = now + Random.Range(this.StayDuration[0], this.StayDuration[1]);
            this.MoveUntil = this.StartMoving + Random.Range(this.MoveDuration[0], this.MoveDuration[1]);
        }

        if (this.MovementDirection.sqrMagnitude != 0) {
            this.MovementDirection /= this.MovementDirection.magnitude;
        }
    }

    public void FixedUpdate() {
        this.Rigidbody.velocity = this.MovementDirection * this.MovementSpeed;
    }

    public void GainFocus() {
        this.Behaviour = Behaviour.FocusingPlayer;
    }

    public abstract void LoseFocus();

    public abstract void Attack();

    private void OnCollisionStay2D(Collision2D collision) {
        // Try to not stay stucked on a wall
        if (LayerMask.LayerToName(collision.collider.gameObject.layer).StartsWith("Map")) {
            switch (this.randomWalkType) {
                case RandomWalkType.StraightLine:
                    this.MovementDirection += collision.contacts[0].normal;
                    this.MovementDirection /= this.MovementDirection.magnitude;
                    // this.MoveUntil = Time.time;
                    break;
                case RandomWalkType.RandomDirections:
                    this.XOff += 10;
                    this.YOff += 10; 
                    break;
            }
        }
    }
}
