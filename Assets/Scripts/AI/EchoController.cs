using System.Collections;
using System.Collections.Generic;
using SignalLost.Core;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.AI
{
    public enum EnemyState
    {
        Dormant,
        Idle,
        Patrol,
        Investigate,
        Chase,
        Search,
        Attack,
        Return
    }

    public class EchoController : MonoBehaviour, INoiseListener
    {
        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 1.0f;
        [SerializeField] private float investigateSpeed = 1.8f;
        [SerializeField] private float chaseSpeed = 2.4f;

        [Header("Perception tuning")]
        [SerializeField] private float loseSightTime = 5f;
        [SerializeField] private float searchDuration = 10f;
        [SerializeField] private float attackDamage = 34f;
        [SerializeField] private float attackInterval = 1.4f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float dormantTime = 35f;
        [SerializeField] private Transform[] patrolPoints;

        [Header("Refs")]
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private SignalLost.Player.PlayerVitals playerVitals;
        [SerializeField] private Transform player;

        public EnemyState State { get; private set; } = EnemyState.Dormant;

        private int _patrolIndex;
        private float _stateTimer;
        private float _lastSeenTimer;
        private float _attackTimer;
        private float _stuckTimer;
        private float _dormantTimer;
        private Vector3 _investigatePos;
        private Vector3 _lastPos;
        private Vector3 _spawnPos;
        private bool _playerVisible;
        private bool _awake;

        private void Awake()
        {
            _spawnPos = transform.position;
            _lastPos = transform.position;
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (player == null)
            {
                var pc = GameObject.FindWithTag("Player");
                if (pc != null) player = pc.transform;
            }
            if (playerVitals == null && player != null) playerVitals = player.GetComponent<SignalLost.Player.PlayerVitals>();
            if (agent != null) agent.enabled = false;
            NoiseSystem.Register(this);
        }

        private void OnDestroy() => NoiseSystem.Unregister(this);

        private void Start()
        {
            StartCoroutine(InitializeAgent());
        }

        private System.Collections.IEnumerator InitializeAgent()
        {
            for (int i = 0; i < 90; i++)
            {
                if (NavMesh.SamplePosition(_spawnPos, out var hit, 3f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    if (!agent.isOnNavMesh) agent.Warp(hit.position);
                    if (agent.isOnNavMesh)
                    {
                        agent.speed = patrolSpeed;
                        yield break;
                    }
                }
                yield return null;
            }
            Debug.LogWarning("[Echo] failed to place on NavMesh, agent disabled", this);
            agent.enabled = false;
        }

        private void Update()
        {
            if (!_awake)
            {
                _dormantTimer += Time.deltaTime;
                if (_dormantTimer >= dormantTime) Wake();
                return;
            }
            if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            _playerVisible = perception.CanSee(player);

            if (_playerVisible) { perception.ReportPlayerSighting(player.position); _lastSeenTimer = 0f; }
            else _lastSeenTimer += Time.deltaTime;

            var next = EvaluateState();
            if (next != State) TransitionTo(next);

            Act();
            TryForceOpenDoor();

            _attackTimer -= Time.deltaTime;
        }

        private void Wake()
        {
            _awake = true;
            State = EnemyState.Idle;
            EventBus.Publish(new SubtitleEvent("A.R.I.A.", "Power restoration has reactivated the north corridor. A former crew member remains there. I... cannot bring myself to classify it as human anymore. Avoid the communication deck.", 8f));
        }

        private EnemyState EvaluateState()
        {
            var dist = player != null ? Vector3.Distance(transform.position, player.position) : 999f;

            switch (State)
            {
                case EnemyState.Idle:
                    if (_playerVisible) return EnemyState.Chase;
                    if (patrolPoints != null && patrolPoints.Length > 0) return EnemyState.Patrol;
                    break;

                case EnemyState.Patrol:
                    if (_playerVisible) return EnemyState.Chase;
                    break;

                case EnemyState.Investigate:
                    if (_playerVisible) return EnemyState.Chase;
                    if (AgentDone()) return EnemyState.Search;
                    break;

                case EnemyState.Chase:
                    if (!_playerVisible && _lastSeenTimer > loseSightTime)
                        return EnemyState.Search;
                    break;

                case EnemyState.Search:
                    if (_playerVisible) return EnemyState.Chase;
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f) return EnemyState.Return;
                    break;

                case EnemyState.Return:
                    if (_playerVisible) return EnemyState.Chase;
                    if (AgentDone()) return EnemyState.Idle;
                    break;
            }
            return State;
        }

        private void TransitionTo(EnemyState next)
        {
            if (next == EnemyState.Dormant) return;
            State = next;
            _stateTimer = searchDuration;
            if (agent == null || !agent.isOnNavMesh) return;
            switch (next)
            {
                case EnemyState.Patrol:
                    agent.speed = patrolSpeed;
                    GoToNextPatrolPoint();
                    break;
                case EnemyState.Investigate:
                    agent.speed = investigateSpeed;
                    agent.SetDestination(_investigatePos);
                    break;
                case EnemyState.Search:
                    agent.speed = investigateSpeed;
                    agent.SetDestination(transform.position + Random.insideUnitSphere * 4f);
                    break;
                case EnemyState.Return:
                    agent.speed = patrolSpeed;
                    agent.SetDestination(_spawnPos);
                    break;
            }
        }

        private void Act()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            var dist = Vector3.Distance(transform.position, player.position);

            switch (State)
            {
                case EnemyState.Chase:
                    agent.speed = chaseSpeed;
                    if (perception.LastKnownPlayerPosition.HasValue)
                        agent.SetDestination(perception.LastKnownPlayerPosition.Value);
                    if (dist <= attackRange) TransitionTo(EnemyState.Attack);
                    break;

                case EnemyState.Attack:
                    if (dist > attackRange + 0.5f) { TransitionTo(EnemyState.Chase); break; }
                    if (_attackTimer <= 0f && playerVitals != null)
                    {
                        playerVitals.Damage(attackDamage);
                        _attackTimer = attackInterval;
                    }
                    break;

                case EnemyState.Patrol:
                    if (AgentDone()) GoToNextPatrolPoint();
                    break;

                case EnemyState.Search:
                    if (AgentDone() && _stateTimer > 0f)
                        agent.SetDestination(transform.position + Random.insideUnitSphere * 4f);
                    break;
            }
        }

        private void TryForceOpenDoor()
        {
            if (State != EnemyState.Chase && State != EnemyState.Investigate && State != EnemyState.Search) return;

            if ((transform.position - _lastPos).magnitude < 0.05f * Time.deltaTime * 60f) _stuckTimer += Time.deltaTime;
            else _stuckTimer = 0f;
            _lastPos = transform.position;

            if (_stuckTimer > 1.2f)
            {
                _stuckTimer = 0f;
                foreach (var d in SignalLost.Interaction.DoorRegistry.All)
                {
                    if (d.IsOpen) continue;
                    if (Vector3.Distance(transform.position, d.transform.position) < 4.5f)
                    {
                        d.SetOpen(true, broadcast: true);
                        SignalLost.Core.EventBus.Publish(new SubtitleEvent("SYSTEM", "*** BANG ***", 1.6f));
                        break;
                    }
                }
            }
        }

        private bool AgentDone() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;

        private void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            if (patrolPoints[_patrolIndex] != null)
                agent.SetDestination(patrolPoints[_patrolIndex].position);
        }

        public void OnNoiseHeard(Vector3 position, float radius, GameObject source)
        {
            if (!_awake) return;
            if (State == EnemyState.Chase || State == EnemyState.Attack) return;
            if (Vector3.Distance(transform.position, position) > radius) return;
            _investigatePos = position;
            TransitionTo(EnemyState.Investigate);
        }

        public void ForceWake()
        {
            if (_awake) return;
            _dormantTimer = dormantTime;
        }

        public void SetDormantTime(float t) => dormantTime = t;
    }
}
