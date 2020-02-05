using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAIMaster : MonoBehaviour
{
    [SerializeField] CarAI[] manuallyAddedAICars = null;
    Queue<CarAI> carAIqueue = new Queue<CarAI>();

    private void Start()
    {
        foreach (var item in manuallyAddedAICars)
        {
            carAIqueue.Enqueue(item);
        }
    }

    public void AddCarToQueue(CarAI newCar)
    {
        carAIqueue.Enqueue(newCar);
    }

    public float maxUpdateTime = 0.001f;
    private void Update()
    {
        float timeStart = Time.realtimeSinceStartup;
        int numOfCarsDone = 0;
        int carsInQueue = carAIqueue.Count;

        while (Time.realtimeSinceStartup < timeStart+maxUpdateTime && numOfCarsDone <= carsInQueue)
        {
            CarAI carToUpdate = carAIqueue.Dequeue();

            if (carToUpdate != null)
            {
                carToUpdate.UpdateStatus();
                carAIqueue.Enqueue(carToUpdate);
                numOfCarsDone++;
            }
            else
            {
                Debug.Log("CarRemovedFromUpdate");
            }
        }
        Debug.Log(numOfCarsDone + " cars done on update in " + (Time.realtimeSinceStartup - timeStart) * 1000 + " ms");
    }
}
