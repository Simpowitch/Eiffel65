using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoliceVehicle : MonoBehaviour
{
    public bool usingSirens = true; //Test true


    float arrestDistance = 10f;
    float arrestMaxSpeed = 0.5f;
    float resistanceReductionDistanceMinimum = 10;
    float resistanceReductionDistance = 10f;
    float resistanceReductionValue = 1f;

    [SerializeField] List<CarAI> criminalWithinStopDistance = new List<CarAI>();
    [SerializeField] List<CarAI> criminalWithinAffectionDistance = new List<CarAI>();


    Rigidbody rb;
    public float carSpeed = 0f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        carSpeed = rb.velocity.magnitude * 3.6f;

        criminalWithinStopDistance.Clear();
        criminalWithinAffectionDistance.Clear();

        if (!usingSirens)
        {
            return;
        }

        resistanceReductionDistance = Mathf.Max(resistanceReductionDistanceMinimum, carSpeed);

        //Affect criminals around police (even speeding cars)
        Collider[] nearbyObjects = Physics.OverlapSphere(this.transform.position, resistanceReductionDistance);
        foreach (var item in nearbyObjects)
        {
            CarAI car = item.GetComponentInParent<CarAI>();
            if (car && car.breakingLaw)
            {
                if (criminalWithinAffectionDistance.Contains(car))
                {
                    continue;
                }

                criminalWithinAffectionDistance.Add(car);
                car.ReduceResistanceToArrest(resistanceReductionValue * Time.deltaTime);
            }
        }

        //Search for stopped cars (cars with low speed and criminals)
        nearbyObjects = Physics.OverlapSphere(this.transform.position, arrestDistance);
        foreach (var item in nearbyObjects)
        {
            CarAI car = item.GetComponentInParent<CarAI>();
            if (car && car.breakingLaw)
            {
                if (criminalWithinStopDistance.Contains(car))
                {
                    continue;
                }

                criminalWithinStopDistance.Add(car);
                if (car.GetSpeed() < arrestMaxSpeed && carSpeed < arrestMaxSpeed)
                {
                    if (car.TryArrest())
                    {
                        Debug.Log("Car stopped and criminal dealt with");
                    }
                }
            }
        }
    }
}
