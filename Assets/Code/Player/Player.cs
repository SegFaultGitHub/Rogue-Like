using System;
using UnityEngine;

public class Player : MonoBehaviour {
    [Serializable]
    public struct _HP {
        public int Current;
        public int Max;
    }

    [SerializeField] public _HP HP;
    [SerializeField] private float InvulnerabilityDuration;
    private float InvulnerableUntil;
    private bool Invulnerable { get => this.InvulnerableUntil > Time.time; }

    public bool TakeDamage(int damage) {
        float now = Time.time;
        if (this.Invulnerable)
            return false;

        this.HP.Current -= damage;
        this.InvulnerableUntil = now + this.InvulnerabilityDuration;
        return true;
    }

    public void OnTriggerEnter2D(Collider2D collider) {
        if (collider.CompareTag("Door/Left")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Left);
            return;
        } else if (collider.CompareTag("Door/Right")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Right);
            return;
        } else if (collider.CompareTag("Door/Up")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Up);
            return;
        } else if (collider.CompareTag("Door/Down")) {
            Map map = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>();
            map.ChangeRoom(Direction.Down);
            return;
        }

        if (collider.CompareTag("Enemy/DetectionZone")) {
            Enemy enemy = collider.transform.parent.GetComponent<Enemy>();
            enemy.GainFocus();
        }
    }

    public void OnTriggerStay2D(Collider2D collider) {
        if (collider.CompareTag("Enemy/AttackZone")) {
            Enemy enemy = collider.transform.parent.GetComponent<Enemy>();
            enemy.Attack();
        }
    }

    public void OnTriggerExit2D(Collider2D collider) {
        if (collider.CompareTag("Enemy/DetectionZone")) {
            Enemy enemy = collider.transform.parent.GetComponent<Enemy>();
            enemy.LoseFocus();
        }
    }
}
