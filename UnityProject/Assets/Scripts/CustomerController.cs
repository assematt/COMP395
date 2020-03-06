using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerController : MonoBehaviour
{
    public enum State
    {
        Spawned,
        InLine,
        Ordering,
        Leaving
    }

    private Animator _animator;
    private NavMeshAgent _navMeshAgent;
    private CounterLine _assignedLine;
    private Vector3 _exitPos;

    [SerializeField]
    private State _currentState;

    public LayerMask agentMask;
    public float _lineDistance = 2f;
    public float serviceTime;
    public GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        SwitchState(State.Spawned);
    }

    // Update is called once per frame
    public void Update()
    {
        switch (_currentState)
        {
            case State.Spawned:
                OnSpawned();
                break;
            case State.InLine:
                OnInLine();
                break;
            case State.Ordering:
                OnOrdering();
                break;
            case State.Leaving:
                OnLeaving();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_currentState), _currentState, null);
        }
    }

    public void SwitchState(State newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case State.Spawned:
                _navMeshAgent.SetDestination(_assignedLine.counterTransform.position);
                break;
            case State.InLine:
                break;
            case State.Ordering:
                break;
            case State.Leaving:
                _assignedLine.customerInLine--;
                _navMeshAgent.SetDestination(_exitPos);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_currentState), _currentState, null);
        }
    }

    public void AssignLine(ref CounterLine line)
    {
        _assignedLine = line;
    }

    public void SetExitPosition(Vector3 destination)
    {
        _exitPos = destination;
    }

    private void OnSpawned()
    {
        if (_navMeshAgent.pathPending)
            return;

        SwitchState(State.InLine);
    }

    private void OnInLine()
    {
        _navMeshAgent.isStopped = Physics.BoxCast(transform.position, Vector3.one, transform.forward, Quaternion.identity, _lineDistance,
            agentMask);

        if (_navMeshAgent.remainingDistance < 0.2f)
        {
            SwitchState(State.Ordering);
        }
    }

    private void OnOrdering()
    {
        serviceTime -= Time.deltaTime;

        Vector3 direction = (_assignedLine.assignedClerk.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 1f);

        if (serviceTime <= 0f)
            SwitchState(State.Leaving);
    }

    private void OnLeaving()
    {
        if (_navMeshAgent.pathPending == false && _navMeshAgent.remainingDistance < 0.2f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawRay(transform.position, transform.forward * _lineDistance);
        Gizmos.color = _navMeshAgent.isStopped ? Color.red : Color.green;
    }
}
