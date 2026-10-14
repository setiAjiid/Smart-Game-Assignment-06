using UnityEngine;
using UnityEngine.InputSystem;

// On-screen debugger untuk video demo:
// - Current Action Behavior Tree + tree dengan branch aktif ditandai
// - Health bar Enemy & Player
// - Perception (Can See Player, jarak)
// - Skor Utility AI
// Tombol demo:  K = Enemy kena 25 damage,  R = reset health Enemy & Player
public class EnemyDebugHUD : MonoBehaviour
{
    [SerializeField] private EnemyBTController enemy;
    [SerializeField] private UtilityAIEvaluator utility;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int damagePerKeyPress = 25;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private Texture2D whiteTexture;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || enemy == null)
            return;

        if (keyboard.kKey.wasPressedThisFrame)
            enemy.TakeDamage(damagePerKeyPress);

        if (keyboard.rKey.wasPressedThisFrame)
        {
            enemy.ResetHealth();

            if (playerHealth != null)
                playerHealth.ResetHealth();

            Debug.Log("Health Enemy & Player di-reset");
        }
    }

    private void OnGUI()
    {
        if (enemy == null)
            return;

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.7f));

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 14;
            labelStyle.richText = true;
            labelStyle.normal.textColor = Color.white;

            whiteTexture = Texture2D.whiteTexture;
        }

        GUILayout.BeginArea(new Rect(10, 10, 330, 470), boxStyle);

        string action = enemy.CurrentAction;

        Label($"<b>BEHAVIOR TREE</b>   Current Action: <b><color=yellow>{action}</color></b>");
        Label(Node("Flee Sequence  (Health Low?)", action == "FLEE"));
        Label(Node("Attack Sequence  (See? InRange? Cooldown)", action == "ATTACK"));
        Label(Node("Chase Sequence  (See?)", action == "CHASE"));
        Label(Node("Patrol", action == "PATROL"));

        GUILayout.Space(8);

        Label($"Enemy HP: {enemy.CurrentHealth}/{enemy.MaxHealth}  (low <= {enemy.LowHealthThreshold})");
        Bar((float)enemy.CurrentHealth / enemy.MaxHealth,
            enemy.CurrentHealth <= enemy.LowHealthThreshold ? Color.red : Color.green);

        if (playerHealth != null)
        {
            Label($"Player HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
            Bar((float)playerHealth.CurrentHealth / playerHealth.MaxHealth, Color.cyan);
        }

        GUILayout.Space(8);

        if (enemy.Player != null)
        {
            float distance = Vector3.Distance(enemy.transform.position, enemy.Player.position);
            string seen = enemy.PlayerVisible ? "<color=lime>YES</color>" : "<color=red>NO</color>";
            Label($"<b>PERCEPTION</b>  Can See Player: {seen}   Dist: {distance:0.0}");
        }

        if (utility != null)
        {
            GUILayout.Space(8);
            Label($"<b>UTILITY AI</b>   Selected: <b><color=orange>{utility.SelectedAction}</color></b>");
            Label($"Attack {utility.AttackScore:0.00}   Chase {utility.ChaseScore:0.00}");
            Label($"Flee   {utility.FleeScore:0.00}   Patrol {utility.PatrolScore:0.00}");

            string match = utility.SelectedAction == action
                ? "<color=lime>SAMA</color>"
                : "<color=orange>BERBEDA</color>";
            Label($"BT vs Utility: {match}");
        }

        GUILayout.Space(8);
        Label("<i>WASD: gerak   K: Enemy -25 HP   R: reset HP</i>");

        GUILayout.EndArea();
    }

    private string Node(string text, bool active)
    {
        return active
            ? $"  <color=lime>► {text}  [Running]</color>"
            : $"  <color=#888888>   {text}</color>";
    }

    private void Label(string text)
    {
        GUILayout.Label(text, labelStyle);
    }

    private void Bar(float value, Color color)
    {
        Rect rect = GUILayoutUtility.GetRect(300, 14);
        GUI.color = new Color(0.25f, 0.25f, 0.25f);
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), whiteTexture);
        GUI.color = Color.white;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
