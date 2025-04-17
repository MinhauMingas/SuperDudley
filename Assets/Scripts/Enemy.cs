using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public NavMeshAgent navAgent;
    private Transform player; // Player transform found by tag
    // public LayerMask playerLayer; // Removed playerLayer
    public float health;
    public float walkPointRange;
    public float timeBetweenAttacks;
    public float sightRange;
    public float attackRange;
    public int damage;
    public Animator animator;
    public ParticleSystem hitEffect;

    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool alreadyAttacked;
    private bool takeDamage;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Find the player by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Enemy could not find the Player. Make sure the Player has the 'Player' tag assigned.");
            enabled = false; // Disable the enemy if the player isn't found
            return;
        }
    }

    private void Update()
    {
        // Only proceed if the player reference is valid
        if (player == null)
        {
            return;
        }

        // Check for player within sight and attack range using distance instead of layers
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerInSightRange = distanceToPlayer <= sightRange;
        bool playerInAttackRange = distanceToPlayer <= attackRange;

        if (!playerInSightRange && !playerInAttackRange)
        {
            Patroling();
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer();
        }
        else if (playerInAttackRange && playerInSightRange)
        {
            AttackPlayer();
        }
        else if (!playerInSightRange && takeDamage)
        {
            ChasePlayer();
        }
    }

    private void Patroling()
    {
        if (!walkPointSet)
        {
            SearchWalkPoint();
        }

        if (walkPointSet)
        {
            navAgent.SetDestination(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        animator.SetFloat("Velocity", 0.2f);

        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        // Removed ground layer check
        walkPointSet = true; // Assume the random point is valid for now
        // You might want to implement more sophisticated patrol logic
        // that avoids falling off edges if that's a concern in your game.
    }

    private void ChasePlayer()
    {
        if (player != null)
        {
            navAgent.SetDestination(player.position);
            animator.SetFloat("Velocity", 0.6f);
            navAgent.isStopped = false;
        }
    }

    private void AttackPlayer()
    {
        navAgent.SetDestination(transform.position);

        if (!alreadyAttacked)
        {
            if (player != null)
            {
                transform.LookAt(player.position);
            }
            alreadyAttacked = true;
            animator.SetBool("Attack", true);
            Invoke(nameof(ResetAttack), timeBetweenAttacks);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange))
            {
                /*
                    YOU CAN USE THIS TO GET THE PLAYER HUD AND CALL THE TAKE DAMAGE FUNCTION

                PlayerHUD playerHUD = hit.transform.GetComponent<PlayerHUD>();
                if (playerHUD != null)
                {
                    playerHUD.takeDamage(damage);
                }
                 */
            }
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
        animator.SetBool("Attack", false);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        hitEffect.Play();
        StartCoroutine(TakeDamageCoroutine());

        if (health <= 0)
        {
            Invoke(nameof(DestroyEnemy), 0.5f);
        }
    }

    private IEnumerator TakeDamageCoroutine()
    {
        takeDamage = true;
        yield return new WaitForSeconds(2f);
        takeDamage = false;
    }

    private void DestroyEnemy()
    {
        StartCoroutine(DestroyEnemyCoroutine());
    }

    private IEnumerator DestroyEnemyCoroutine()
    {
        animator.SetBool("Dead", true);
        yield return new WaitForSeconds(1.8f);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}