using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    /// <summary>
    /// How many agent will be spawned per second?
    /// </summary>
    public float arrivalRate = 1f;

    /// <summary>
    /// Reference to the clerk prefab
    /// </summary>
    public GameObject clerkPrefab;

    /// <summary>
    /// Reference to the customer prefab
    /// </summary>
    public GameObject customerPrefab;

    /// <summary>
    /// Where the customer should enter
    /// </summary>
    public Transform entryPoint;

    /// <summary>
    /// Where the customer should exit
    /// </summary>
    public Transform exitPoint;

    /// <summary>
    /// The list of counters in the store
    /// </summary>
    public CounterLine[] counters;

    /// <summary>
    /// The minimum service time
    /// </summary>
    public float minServiceTime = 1;

    /// <summary>
    /// The maximum service time
    /// </summary>
    public float maxServiceTime = 3;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnAgents());
    }
    
    public IEnumerator SpawnAgents()
    {
        while (true)
        {
            if (arrivalRate == 0f)
                continue;

            yield return new WaitForSeconds((1f / arrivalRate) / Time.timeScale);

            var newCustomer = Instantiate(customerPrefab, entryPoint.position, Quaternion.identity).GetComponent<CustomerController>();
            newCustomer.serviceTime = Random.Range(minServiceTime, maxServiceTime);
            newCustomer.gameController = this;

            // Assign the line with the lowest amount of customers
            var assignedLine = Min(counters[0], counters[1]);
            assignedLine = Min(assignedLine, counters[2]);
            ++assignedLine.customerInLine;

            newCustomer.AssignLine(ref assignedLine);
            newCustomer.SetExitPosition(exitPoint.position);

            yield return null;
        }
    }

    public CounterLine Min(CounterLine lineA, CounterLine lineB)
    {
        return lineA.customerInLine < lineB.customerInLine ? lineA : lineB;
    }

    public void OnTimeScaleChanged(Slider slider)
    {
        Time.timeScale = slider.value;
    }

    public void OnMinServiceTimeChanged(Slider slider)
    {
        minServiceTime = slider.value;
    }

    public void OnMaxServiceTimeChanged(Slider slider)
    {
        maxServiceTime = slider.value;
    }

    public void OnCustomerPerMinuteChanged(Slider slider)
    {
        arrivalRate = slider.value / 60f;
    }

}
