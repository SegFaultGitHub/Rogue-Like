using UnityEngine;

public class Goblin : Enemy {
    [SerializeField] private float ConcentratingDuration = 1f;
    [SerializeField] private float ConcentratingStartedAt;
    [SerializeField] private float ChargeDuration = 2f;
    [SerializeField] private float ChargeUntil;
    private bool Charging;

    public void Update() {
        float now = Time.time;

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

    private void AttackPlayer() {
        float now = Time.time;

        // concentrating started / concentrating started + concentration duration / charge until

        if (now < this.ConcentratingStartedAt) {
            // Should not happen
        } else if (this.ConcentratingStartedAt < now && now < this.ConcentratingStartedAt + this.ConcentratingDuration) {
            this.MovementDirection *= 0;
            if (this.Player.transform.position.x > this.transform.position.x) {
                Vector3 scale = this.Sprites.transform.localScale;
                this.Sprites.transform.localScale = new(1, scale.y, scale.z);
            } else {
                Vector3 scale = this.Sprites.transform.localScale;
                this.Sprites.transform.localScale = new(-1, scale.y, scale.z);
            }
            this.Animator.SetBool("Charging", true);
        } else if (now < this.ChargeUntil) {
            if (this.MovementDirection.sqrMagnitude == 0) {
                this.MovementDirection = this.Player.transform.position - this.transform.position;
                this.MovementDirection /= this.MovementDirection.magnitude;
            }
            this.Animator.SetBool("Charging", false);
        } else if (this.ChargeUntil < now) {
            this.ConcentratingStartedAt = now;
            this.ChargeUntil = this.ConcentratingStartedAt + this.ConcentratingDuration + this.ChargeDuration;
            this.MovementDirection *= 0;
            if (this.Player.transform.position.x > this.transform.position.x) {
                Vector3 scale = this.Sprites.transform.localScale;
                this.Sprites.transform.localScale = new(1, scale.y, scale.z);
            } else {
                Vector3 scale = this.Sprites.transform.localScale;
                this.Sprites.transform.localScale = new(-1, scale.y, scale.z);
            }
            this.Animator.SetBool("Charging", true);
        }
    }

    public override void LoseFocus() {
        float now = Time.time;

        if (now < this.ConcentratingStartedAt || this.ChargeUntil < now) {
            this.Behaviour = Behaviour.Idle;
        }
    }

    public override void Attack() {
        throw new System.NotImplementedException();
    }
}
