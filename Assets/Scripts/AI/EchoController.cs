using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.AI
{
    public enum EnemyState
    {
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
        [SerializeField] private float patrolSpeed = 1.4f;
        [SerializeField] private float investigateSpeed = 2.6f;
        [SerializeField] private float chaseSpeed = 3.6f;
        [SerializeField] private float stoppingDistance = 1.1f;

        [Header("Perception tuning")]
        [SerializeField] private float loseSightTime = 4.5f;
        [SerializeField] private float searchDuration = 9f;
        [SerializeField] private float attackDamage = 22f;
        [SerializeField] private float attackInterval = 1.2f;
        [SerializeField] private Transform[] patrolPoints;

        [Header("Refs")]
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private SignalLost.Player.PlayerVitals playerVitals;
        [SerializeField] private Transform player;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        private readonly List<int> _patrolIndices = new();
        private int _patrolIndex;
        private float _stateTimer;
        private float _lastSeenTimer;
        private float _attackTimer;
        private Vector3 _investigatePos;
        private Vector3 _spawnPos;
        private bool _playerVisible;

        private void Awake()
        {
            _spawnPos = transform.position;
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            if (perception == null) perception = GetComponent<EnemyPerception>();
            if (player == null)
            {
                var pc = GameObject.FindWithTag("Player");
                if (pc != null) player = pc.transform;
            }
            if (playerVitals == null && player != null) playerVitals = player.GetComponent<SignalLost.Player.PlayerVitals>();
            NoiseSystem.Register(this);
        }

        private void OnDestroy() => NoiseSystem.Unregister(this);

        private void Start()
        {
            StartCoroutine(InitializeAgent());
        }

        private System.Collections.IEnumerator InitializeAgent()
        {
            // NavMesh data may load after scene start in builds; retry placement for a few frames.
            for (int i = 0; i < 60; i++)
            {
                if (NavMesh.SamplePosition(_spawnPos, out var hit, 3f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    if (!agent.isOnNavMesh) agent.Warp(hit.position);
                    if (agent.isOnNavMesh) yield break;
                }
                yield return null;
            }
            Debug.LogWarning("[Echo] failed to place on NavMesh, agent disabled", this);
            agent.enabled = false;
        }

        private void Update()
        {
            if (player == null || agent == null || !agent.isOnNavMesh) return;
            _playerVisible = perception.CanSee(player);

            if (_playerVisible) { perception.ReportPlayerSighting(player.position); _lastSeenTimer = 0f; }
            else _lastSeenTimer += Time.deltaTime;

            var next = EvaluateState();
            if (next != State) TransitionTo(next);

            Act();

            _attackTimer -= Time.deltaTime;
        }

        private EnemyState EvaluateState()
        {
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

                case EnemyState.Attack:
                    if (!_playerVisible) return EnemyState.Chase;
                    var dist = Vector3.Distance(transform.position, player.position);
                    if (dist > stoppingDistance + 0.4f) return EnemyState.Chase;
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
            switch (State)
            {
                case EnemyState.Chase:
                    agent.speed = chaseSpeed;
                    if (perception.LastKnownPlayerPosition.HasValue)
                        agent.SetDestination(perception.LastKnownPlayerPosition.Value);
                    var dist = Vector3.Distance(transform.position, player.position);
                    if (dist <= stoppingDistance) TransitionTo(EnemyState.Attack);
                    break;

                case EnemyState.Patrol:
                    if (AgentDone()) GoToNextPatrolPoint();
                    break;

                case EnemyState.Search:
                    if (AgentDone() && _stateTimer > 0f)
                        agent.SetDestination(transform.position + Random.insideUnitSphere * 4f);
                    break;

                case EnemyState.Attack:
                    if (_attackTimer <= 0f && playerVitals != null)
                    {
                        playerVitals.Damage(attackDamage);
                        _attackTimer = attackInterval;
                    }
                    break;
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
            if (State == EnemyState.Chase || State == EnemyState.Attack) return;
            if (Vector3.Distance(transform.position, position) > radius) return;
            _investigatePos = position;
            TransitionTo(EnemyState.Investigate);
        }

        public void TeleportTo(Vector3 pos) => agent.Warp(pos);
    }
}
