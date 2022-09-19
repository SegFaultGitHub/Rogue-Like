using UnityEngine;

public class BoneThrow : Spell {
    [HideInInspector] public Vector2 Direction;
    public override void CastTowards(Vector2 from, Vector2 to) {
        this.Direction = to - from;
        this.Direction.Normalize();
        this.Direction *= 10;
        this.transform.position = from;
        float angle = Vector2.SignedAngle(new(1, 0), this.Direction);
        this.transform.eulerAngles = new(0, 0, angle);
    }

    public void FixedUpdate() {
        this.Rigidbody.velocity = this.Direction;
    }

    public void End() {
        Destroy(this.gameObject);
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        this.Rigidbody.velocity = Vector2.zero;
        float angle = Vector2.SignedAngle(new(1, 0), -collision.contacts[0].normal);
        this.transform.eulerAngles = new(0, 0, angle);
        this.Rigidbody.simulated = false;
        this.End();
    }
}
