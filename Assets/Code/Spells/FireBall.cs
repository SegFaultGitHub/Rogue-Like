using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBall : Spell {
    [HideInInspector] public Vector2 Direction;
    private float SpawnDate;
    [SerializeField] private float MaxAge;

    public new void Start() {
        base.Start();
        this.SpawnDate = Time.time;
    }

    public void FixedUpdate() {
        this.Rigidbody.velocity = this.Direction;

        if (Time.time - this.SpawnDate > this.MaxAge) {
            this.Rigidbody.velocity = Vector2.zero;
            this.Rigidbody.simulated = false;
            this.Animator.SetTrigger("Die");
        }
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        this.Rigidbody.velocity = Vector2.zero;
        float angle = Vector2.SignedAngle(new(1, 0), -collision.contacts[0].normal);
        this.transform.eulerAngles = new(0, 0, angle);
        this.Rigidbody.simulated = false;
        this.Animator.SetTrigger("Die");
    }

    public void End() {
        Destroy(this.gameObject);
    }

    public override void CastTowards(Vector2 from, Vector2 to) {
        this.Direction = to - from;
        this.Direction.Normalize();
        this.Direction *= 20;
        this.transform.position = from;
        float angle = Vector2.SignedAngle(new(1, 0), this.Direction);
        this.transform.eulerAngles = new(0, 0, angle);
    }
}
