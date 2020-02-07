using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoliceVehicle : MonoBehaviour
{
    private bool usingSirens = true; //Test true

    [SerializeField] GameObject policeAOEIndicator = null;

    float arrestDistance = 10f;
    float arrestMaxSpeed = 0.5f;
    float resistanceReductionDistanceMinimum = 10;
    float resistanceReductionDistance = 10f;
    float resistanceReductionValue = 1f;

    [SerializeField] List<CarAI> criminalWithinStopDistance = new List<CarAI>();
    [SerializeField] List<CarAI> criminalWithinAffectionDistance = new List<CarAI>();


    Rigidbody rb;
    public float carSpeed = 0f;
    [SerializeField] Text speedText = null;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        SetUsingSirens(false);
    }


    public void SetUsingSirens(bool onOff)
    {
        usingSirens = onOff;
        policeAOEIndicator.SetActive(onOff);
    }

    public void FlipUsingSirens()
    {
        SetUsingSirens(!usingSirens);
    }

    public bool GetUsingSirens()
    {
        return usingSirens;
    }

    private void Update()
    {
        carSpeed = rb.velocity.magnitude * 3.6f;
        if (speedText)
        {
            speedText.text = Mathf.RoundToInt(carSpeed).ToString();
        }

        criminalWithinStopDistance.Clear();
        criminalWithinAffectionDistance.Clear();

        if (!usingSirens)
        {
            return;
        }


        resistanceReductionDistance = Mathf.Max(resistanceReductionDistanceMinimum, carSpeed);

        //draw particle-effect//shader around car displaying the area of effect of the sirens
        policeAOEIndicator.transform.localScale = new Vector3(resistanceReductionDistance, policeAOEIndicator.transform.lossyScale.y, resistanceReductionDistance);

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

    public List<CarAI> GetStoppedCars()
    {
        List<CarAI> stoppedCars = new List<CarAI>();

        //Search for stopped cars (cars with low speed and criminals)
        Collider[] nearbyObjects = Physics.OverlapSphere(this.transform.position, arrestDistance);
        foreach (var item in nearbyObjects)
        {
            CarAI car = item.GetComponentInParent<CarAI>();
            if (car && !stoppedCars.Contains(car))
            {
                if (car.GetSpeed() < arrestMaxSpeed && carSpeed < arrestMaxSpeed)
                {
                    if (car.TryArrest())
                    {
                        stoppedCars.Add(car);
                    }
                }
            }
        }
        return stoppedCars;
    }
}
