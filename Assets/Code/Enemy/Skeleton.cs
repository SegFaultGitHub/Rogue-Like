using System.Collections;
using UnityEngine;

public class Skeleton : Enemy {
    [SerializeField] private float Range = 4;
    [SerializeField] private float CastDelay = 2;
    private float LastCast;

    [SerializeField] private Spell BoneThrow;

    public void Update() {
        switch (this.Behaviour) {
            case Behaviour.Idle:
                this.RandomWalk();
                break;
            case Behaviour.FocusingPlayer:
                this.AttackPlayer();
                break;
        }

        this.AdjustDirection();
    }

    public override void LoseFocus() {
        this.Behaviour = Behaviour.Idle;
    }

    private void AttackPlayer() {
        float now = Time.time;
        this.MovementDirection = this.Player.transform.position - this.transform.position;
        float distance = this.MovementDirection.magnitude;

        if (distance < this.Range * 0.75f) {
            this.MovementDirection = Vector2.zero;
        }
        if (distance < this.Range && Time.time > this.LastCast + this.CastDelay) {
            this.LastCast = Time.time;
            this.StartCoroutine(this.Cast1(0f));
            this.StartCoroutine(this.Cast2(0.5f));
        }

        if (distance > 1) {
            Vector2 offset = new Vector2(
                Mathf.PerlinNoise(now * this.NoiseScale + this.XOff, this.MovementSeed) - 0.5f,
                Mathf.PerlinNoise(this.MovementSeed, now * this.NoiseScale + this.YOff) - 0.5f
            );
            offset *= distance / offset.magnitude;
            this.MovementDirection += offset;
            this.MovementDirection /= distance;
        }
    }

    private IEnumerator Cast1(float delay) {
        yield return new WaitForSeconds(delay);
        this.BoneThrow.Layer = "Enemy/Spell";
        this.BoneThrow.CastTowards(this.transform.position, this.Player.transform.position);
        Spell castedSpell = Instantiate(this.BoneThrow, this.transform.parent);
        castedSpell.transform.position = this.transform.position;
    }

    private IEnumerator Cast2(float delay) {
        yield return new WaitForSeconds(delay);
        this.BoneThrow.Layer = "Enemy/Spell";
        Spell castedSpell;

        this.BoneThrow.CastTowards(this.transform.position, this.Player.transform.position, -10);
        castedSpell = Instantiate(this.BoneThrow, this.transform.parent);
        castedSpell.transform.position = this.transform.position;

        this.BoneThrow.CastTowards(this.transform.position, this.Player.transform.position, 10);
        castedSpell = Instantiate(this.BoneThrow, this.transform.parent);
        castedSpell.transform.position = this.transform.position;
    }

    public override void Attack() {}
}
