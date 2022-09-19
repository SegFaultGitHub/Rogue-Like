using System;
using UnityEngine;

public class Player : MonoBehaviour {
    [Serializable]
    public struct _HP {
        public int Current;
        public int Max;
    }

    [SerializeField] public _HP HP;

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
            Enemy enemy = collider.gameObject.GetComponent<Enemy>();
            enemy.GainFocus();
        }
    }

    public void OnTriggerExit2D(Collider2D collider) {
        if (collider.CompareTag("Enemy/DetectionZone")) {
            Enemy enemy = collider.gameObject.GetComponent<Enemy>();
            enemy.LoseFocus();
        }
    }
}
