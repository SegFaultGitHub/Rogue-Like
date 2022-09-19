using UnityEngine;

public class MeleeAI : MonoBehaviour {
    public static void Compute(Enemy enemy, Player player, Enemy.MeleeAIData data) {
        enemy.MovementDirection = player.transform.position - enemy.transform.position;
        if (enemy.MovementDirection.magnitude < 1.2f) {
            enemy.MovementDirection = Vector2.zero;
        }
        enemy.MovementDirection.Normalize();
    }
}
