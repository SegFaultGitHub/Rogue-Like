using UnityEngine;

public class MagicAI : MonoBehaviour {
    public static void Compute(Enemy enemy, Player player, Enemy.MagicAIData data) {
        enemy.MovementDirection = player.transform.position - enemy.transform.position;
        if (enemy.MovementDirection.magnitude < data.Range && Time.time > data.LastCast + data.CastDelay) {
            enemy.MovementDirection /= 3f;
            if (enemy.MovementDirection.magnitude < 0.5f) {
                enemy.MovementDirection = Vector2.zero;
            }
            Spell spell = Utils.Sample(data.Spells);
            spell.Layer = "Enemy/Spell";
            spell.CastTowards(enemy.transform.position, player.transform.position);
            Instantiate(spell, enemy.transform.parent);
            data.LastCast = Time.time;
        }
        if (enemy.MovementDirection.magnitude > 1) {
            enemy.MovementDirection.Normalize();
        }
        enemy.MovementDirection = Vector2.zero;
    }
}
