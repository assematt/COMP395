using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// This is the main NPC controller that controls:
///     - The NPC behaviour
///     - NPC activation
///     - NPC reset
/// </summary>
public class EnemyController : MonoBehaviour
{
    private NavMeshAgent _agent;
    private GameObject _playerGameObject;

    private bool _shouldFollowPlayer = false;

    public UnityEvent onPlayerHit;

    
    /// <summary>
    /// Initialize the initial value for the enemy
    /// </summary>
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _playerGameObject = GameObject.FindWithTag("Player");
        _shouldFollowPlayer = false;
        gameObject.SetActive(false);

        switch (DifficultyController.GameDifficulty)
        {
            case DifficultyController.Difficulty.Easy:
                _agent.speed = 5f;
                break;
            case DifficultyController.Difficulty.Medium:
                _agent.speed = 7f;
                break;
            case DifficultyController.Difficulty.Hard:
                _agent.speed = 7f;
                FollowPlayer();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    /// <summary>
    /// Activate the NPC to follow the player
    /// </summary>
    public void FollowPlayer()
    {
        _shouldFollowPlayer = true;
        gameObject.SetActive(true);
        StartCoroutine(FollowPlayerEnumerator());
    }

    /// <summary>
    /// This coroutine updates the destination path of the NPC to match the current NPC position 
    /// </summary>
    /// <returns></returns>
    private IEnumerator FollowPlayerEnumerator()
    {
        while (_shouldFollowPlayer && _playerGameObject != null)
        {
            _agent.SetDestination(_playerGameObject.transform.position);

            yield return new WaitWhile(() => _agent.pathPending);
        }
        
    }

    /// <summary>
    /// When the NPC meets the player object:
    ///     - Self destroy and play a sound effect and particle system
    ///     - Notify the player controller of the hit
    ///     - Invoke a callback
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopCoroutine(FollowPlayerEnumerator());
            other.GetComponent<PlayerController>().RegisterHit();
            enabled = false;
            Destroy(gameObject, 1f);

            GetComponentInChildren<ParticleSystem>().Play();

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var capsuleCollider in colliders)
            {
                capsuleCollider.enabled = false;
            }
            var meshes = GetComponentsInChildren<MeshRenderer>();
            foreach (var mesh in meshes)
            {
                mesh.enabled = false;
            }

            onPlayerHit.Invoke();
        }
    }
}
