using UnityEngine;

// Eksperimen Utility-Based AI.
// Script ini TIDAK menggerakkan Enemy. Ia hanya menghitung skor Attack, Chase, Flee,
// dan Patrol setiap frame dari data yang sama dengan Behavior Tree, lalu memilih skor
// tertinggi, sehingga hasilnya dapat dibandingkan dengan pilihan Behavior Tree.
[RequireComponent(typeof(EnemyBTController))]
public class UtilityAIEvaluator : MonoBehaviour
{
    [Header("Utility Scores (read-only)")]
    [SerializeField] private float attackScore;
    [SerializeField] private float chaseScore;
    [SerializeField] private float fleeScore;
    [SerializeField] private float patrolScore;

    [Header("Result")]
    [SerializeField] private string selectedAction = "None";
    [SerializeField] private string behaviorTreeAction = "None";
    [SerializeField] private bool sameAsBehaviorTree;

    [Header("Tuning")]
    [SerializeField] private float fleeWeight = 1.4f;
    [SerializeField] private float patrolBaseline = 0.1f;
    [SerializeField] private bool logWhenSelectionChanges = true;

    private EnemyBTController enemy;
    private string lastLoggedAction = "";

    public float AttackScore => attackScore;
    public float ChaseScore => chaseScore;
    public float FleeScore => fleeScore;
    public float PatrolScore => patrolScore;
    public string SelectedAction => selectedAction;

    private void Awake()
    {
        enemy = GetComponent<EnemyBTController>();
    }

    private void LateUpdate()
    {
        if (enemy.Player == null)
            return;

        bool visible = enemy.PlayerVisible;

        float distance =
            Vector3.Distance(
                transform.position,
                enemy.Player.position
            );

        float healthRatio =
            (float)enemy.CurrentHealth / enemy.MaxHealth;

        // Jarak 0..1 relatif terhadap vision range (0 = menempel, 1 = batas penglihatan).
        float normalizedDistance =
            Mathf.Clamp01(distance / enemy.VisionRange);

        bool inAttackRange = distance <= enemy.AttackRange;

        // FLEE: makin rendah health, makin tinggi keinginan kabur.
        fleeScore = Mathf.Clamp01((1f - healthRatio) * fleeWeight);

        // ATTACK: hanya jika player terlihat. Tinggi sekali bila dalam attack range.
        if (!visible)
            attackScore = 0f;
        else if (inAttackRange)
            attackScore = 0.9f;
        else
            attackScore = (1f - normalizedDistance) * 0.5f;

        // CHASE: hanya jika player terlihat. Makin jauh (tapi masih terlihat), makin perlu dikejar.
        if (!visible)
            chaseScore = 0f;
        else if (inAttackRange)
            chaseScore = 0.3f;
        else
            chaseScore = 0.4f + 0.4f * normalizedDistance;

        // PATROL: nilai dasar kecil, menang hanya jika tidak ada pilihan lain.
        patrolScore = patrolBaseline;

        selectedAction = PickHighest();
        behaviorTreeAction = enemy.CurrentAction;
        sameAsBehaviorTree = selectedAction == behaviorTreeAction;

        if (logWhenSelectionChanges &&
            selectedAction != lastLoggedAction)
        {
            lastLoggedAction = selectedAction;

            Debug.Log(
                $"[Utility] Attack={attackScore:0.00} Chase={chaseScore:0.00} " +
                $"Flee={fleeScore:0.00} Patrol={patrolScore:0.00} " +
                $"-> Selected = {selectedAction} | BT = {behaviorTreeAction}"
            );
        }
    }

    private string PickHighest()
    {
        string best = "PATROL";
        float bestScore = patrolScore;

        if (chaseScore > bestScore) { best = "CHASE"; bestScore = chaseScore; }
        if (attackScore > bestScore) { best = "ATTACK"; bestScore = attackScore; }
        if (fleeScore > bestScore) { best = "FLEE"; bestScore = fleeScore; }

        return best;
    }
}
