using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {
    internal enum AIType {
        Melee, Distance, Magic
    }
    internal enum EnemyBehaviour {
        FocusPlayer, Idle
    }

    [Serializable]
    internal class _AIData {
        public AIType Type;
        public float DetectionRadius; // in units

        public MeleeAIData MeleeAIData;
        public DistanceAIData DistanceAIData;
        public MagicAIData MagicAIData;
    }

    [Serializable]
    public class MeleeAIData {
    }

    [Serializable]
    public class DistanceAIData {
        public float Range;
    }

    [Serializable]
    public class MagicAIData {
        public float Range;
        public List<Spell> Spells;
        public float LastCast;
        public float CastDelay;
    }

    private Animator Animator;
    private Rigidbody2D Rigidbody;
    private GameObject Sprites;

    [SerializeField] private float MovementSpeed;
    public Vector2 MovementDirection { get; set; }
    private int MovementSeed;
    public float NoiseScale;
    private float XOff, YOff;
    private Player Player;

    [Header("AI")]
    private float StartMoving = 0;
    private float MoveUntil = 0;
    private float[] MoveDuration = new[] { 0.5f, 3.5f }; // in seconds
    private float[] StayDuration = new[] { 1.5f, 5f };   // in seconds
    private EnemyBehaviour Behaviour;
    [SerializeField] private _AIData AIData;

    public void Start() {
        this.Animator = this.GetComponent<Animator>();
        this.Rigidbody = this.GetComponentInChildren<Rigidbody2D>();
        this.MovementSeed = (int) UnityEngine.Random.Range((float) -1e3, (float) 1e3);
        this.Sprites = this.transform.Find("Sprites").gameObject;
        CircleCollider2D detectionZone = this.gameObject.AddComponent<CircleCollider2D>();
        detectionZone.tag = "Enemy/DetectionZone";
        detectionZone.isTrigger = true;
        detectionZone.radius = this.AIData.DetectionRadius;
        this.Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    public void Update() {
        float now = Time.time;

        switch (this.Behaviour) {
            case EnemyBehaviour.FocusPlayer:
                switch (this.AIData.Type) {
                    case AIType.Melee:
                        MeleeAI.Compute(this, this.Player, this.AIData.MeleeAIData);
                        break;
                    case AIType.Magic:
                        MagicAI.Compute(this, this.Player, this.AIData.MagicAIData);
                        break;
                }
                break;
            case EnemyBehaviour.Idle:
                if (this.StartMoving < now && now < this.MoveUntil) {
                    // Currently moving
                    this.MovementDirection = new(
                        Mathf.PerlinNoise(now * this.NoiseScale + this.XOff, this.MovementSeed) * 2 - 1,
                        Mathf.PerlinNoise(this.MovementSeed, now * this.NoiseScale + this.YOff) * 2 - 1
                    );
                    this.MovementDirection.Normalize();
                    // Slow movement at the end
                    this.MovementDirection *= Mathf.Clamp((1 - (now - this.StartMoving) / (this.MoveUntil - this.StartMoving)), 0.5f, 1f) / 2;
                } else if (now < this.StartMoving) {
                    // Waiting to move
                    this.MovementDirection = Vector2.zero;
                } else {
                    // Just ended moving
                    this.StartMoving = now + UnityEngine.Random.Range(this.StayDuration[0], this.StayDuration[1]);
                    this.MoveUntil = this.StartMoving + UnityEngine.Random.Range(this.MoveDuration[0], this.MoveDuration[1]);
                }
                break;
        }

        if (this.MovementDirection.x > 0) {
            Vector3 scale = this.Sprites.transform.localScale;
            this.Sprites.transform.localScale = new(1, scale.y, scale.z);
        } else if (this.MovementDirection.x < 0) {
            Vector3 scale = this.Sprites.transform.localScale;
            this.Sprites.transform.localScale = new(-1, scale.y, scale.z);
        }
        this.Animator.SetBool("IsMoving", this.MovementDirection != new Vector2());
    }

    public void FixedUpdate() {
        this.Rigidbody.velocity = this.MovementDirection * this.MovementSpeed;
    }

    public void GainFocus() {
        this.Behaviour = EnemyBehaviour.FocusPlayer;
    }

    public void LoseFocus() {
        this.Behaviour = EnemyBehaviour.Idle;
    }

    private void OnCollisionStay2D(Collision2D collision) {
        if (LayerMask.LayerToName(collision.collider.gameObject.layer) == "Map") {
            this.XOff += 10;
            this.YOff += 10;
        }
    }
}
