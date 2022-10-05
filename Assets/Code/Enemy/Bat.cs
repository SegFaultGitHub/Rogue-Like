using System.Collections;
using UnityEngine;

public class Bat : Enemy {
    private float FleeUntil;
    [SerializeField] private float FleeDuration = 0.5f;
    [SerializeField] private int Damage = 1;

    public new void Start() {
        base.Start();
        this.MoveDuration = new[] { 100f, 100f }; // in seconds
        this.StayDuration = new[] { 0f, 0f };     // in seconds
    }

    public void Update() {
        float now = Time.time;

        if (this.FleeUntil < now) {
            switch (this.Behaviour) {
                case Behaviour.Idle:
                    this.RandomWalk();
                    break;
                case Behaviour.FocusingPlayer:
                    this.FocusPlayer();
                    break;
            }
        } else {
            // Override all behaviours to flee player
            Vector2 direction = this.transform.position - this.Player.transform.position;
            Vector2 offset = new(
                Mathf.PerlinNoise(now * this.NoiseScale + this.XOff, this.MovementSeed) * 2 - 1,
                Mathf.PerlinNoise(this.MovementSeed, now * this.NoiseScale + this.YOff) * 2 - 1
            );
            this.MovementDirection = (2 * direction / direction.magnitude) + (offset / offset.magnitude);

            if (this.MovementDirection.sqrMagnitude != 0) {
                this.MovementDirection /= this.MovementDirection.magnitude;
            }
        }

        this.AdjustDirection();
    }

    public override void Attack() {
        bool damaged = this.Player.TakeDamage(this.Damage);
        if (damaged) {
            this.FleeUntil = Time.time + this.FleeDuration;
        }
    }

    public override void LoseFocus() {
        this.Behaviour = Behaviour.Idle;
    }

    private void FocusPlayer() {
        float now = Time.time;
        Vector2 offset = new(
            Mathf.PerlinNoise(now * this.NoiseScale + this.XOff, this.MovementSeed) * 2 - 1,
            Mathf.PerlinNoise(this.MovementSeed, now * this.NoiseScale + this.YOff) * 2 - 1
        );
        Vector2 direction = this.Player.transform.position - this.transform.position;
        this.MovementDirection = (3 * direction / direction.magnitude) + (offset / offset.magnitude);

        if (this.MovementDirection.sqrMagnitude != 0) {
            this.MovementDirection /= this.MovementDirection.magnitude;
        }
    }
}
