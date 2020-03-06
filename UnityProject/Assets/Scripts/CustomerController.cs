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
        Moving,
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
    public float _lineDistance = 0f;
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
            case State.Moving:
                break;
            case State.InLine:
                OnInLine();
                break;
            case State.Ordering:
                OnOrdering();
                break;
            case State.Leaving:
                gameObject.layer = 1 << LayerMask.NameToLayer("Default");
                OnLeaving();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_currentState), _currentState, null);
        }

        //_navMeshAgent.speed = 3.5f * Time.timeScale;
        //_navMeshAgent.angularSpeed = 120f * Time.timeScale;
        //_navMeshAgent.acceleration = 8f * Time.timeScale;
        //Debug.Log(_navMeshAgent.speed);
    }

    public void SwitchState(State newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case State.Spawned:
                _navMeshAgent.SetDestination(_assignedLine.counterTransform.position);
                break;
            case State.Moving:
                break;
            case State.InLine:
                break;
            case State.Ordering:
                _navMeshAgent.isStopped = false;
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

        SwitchState(State.Moving);
    }

    private void OnMoving()
    {
        
    }

    private void OnInLine()
    {
        _navMeshAgent.isStopped = Physics.BoxCast(transform.position + (transform.forward * 1.5f), Vector3.one / 2, transform.forward, Quaternion.identity, _lineDistance,
            agentMask);

        ExtDebug.DrawBoxCastBox(transform.position + (transform.forward * 1.5f), Vector3.one / 2, Quaternion.identity, transform.forward, _lineDistance, _navMeshAgent.isStopped ? Color.red : Color.green);

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
        if (_navMeshAgent.pathPending == false && _navMeshAgent.remainingDistance < 0.2f * Time.timeScale)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (_currentState != State.InLine)
            return;

        //Gizmos.color = _navMeshAgent.isStopped ? Color.red : Color.green;
        //Gizmos.DrawRay(transform.position, transform.forward * _lineDistance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Doors") && _currentState == State.Moving)
        {
            SwitchState(State.InLine);
            Debug.Log("Agent onLine now");
        }
    }
}
