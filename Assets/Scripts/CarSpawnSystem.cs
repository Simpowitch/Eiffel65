using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawnSystem : MonoBehaviour
{
    [SerializeField] GameObject[] vehicles = null;

    PathNode[] allNodes;
    [SerializeField] Transform playerCar = null;

    public static List<GameObject> spawnedCars = new List<GameObject>();
    [SerializeField] Transform carsParent = null;

    [SerializeField] int spawnChance = 25;
    [SerializeField] int maxCars = 50;

    // Start is called before the first frame update
    void Start()
    {
        allNodes = GetComponentsInChildren<PathNode>();

        for (int i = 0; i < allNodes.Length; i++)
        {
            int rng = Random.Range(0, 100);

            if (rng < spawnChance && Vector3.Distance(playerCar.transform.position, allNodes[i].transform.position) < despawnRange)
            {
                if (CheckIfAllowedSpawn(allNodes[i]))
                {
                    SpawnVehicle(allNodes[i]);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if below the target car number
        if (spawnedCars.Count < maxCars)
        {
            List<PathNode> nodesAroundPlayer = new List<PathNode>();

            //Add all nodes within despawn range
            foreach (var item in allNodes)
            {
                if (Vector3.Distance(item.transform.position, playerCar.position) < despawnRange)
                {
                    nodesAroundPlayer.Add(item);
                }
            }


            //Remove nodes that can be seen from the car
            for (int i = 0; i < nodesAroundPlayer.Count; i++)
            {
                if (Physics.Raycast(nodesAroundPlayer[i].transform.position, playerCar.position - nodesAroundPlayer[i].transform.position, out RaycastHit hit))
                {
                    if (hit.transform == playerCar)
                    {
                        nodesAroundPlayer.RemoveAt(i);
                        i--;
                    }
                }
            }

            PathNode spawnNode = null;

            //Choose a random node that isn't obstructed
            bool allowedSpawn = false;
            int tries = 0;
            while (!allowedSpawn && tries < 10)
            {
                spawnNode = nodesAroundPlayer[Random.Range(0, nodesAroundPlayer.Count)];
                allowedSpawn = CheckIfAllowedSpawn(spawnNode);
                tries++;
            }

            if (allowedSpawn)
            {
                SpawnVehicle(spawnNode);
            }
        }

        CheckDespawn();
    }

    float obstructionCheck = 20f;
    bool CheckIfAllowedSpawn(PathNode nodeToSpawnAt)
    {
        //If obstructed
        Collider[] objectsAtNode = Physics.OverlapSphere(nodeToSpawnAt.transform.position, obstructionCheck);
        foreach (var item in objectsAtNode)
        {
            if (item.tag == "Vehicle")
            {
                return false;
            }
        }
        return true;
    }

    Vector3 spawnOffset = new Vector3(0, 1, 0);
    void SpawnVehicle(PathNode nodeSpawn)
    {
        GameObject spawn = Instantiate(vehicles[Random.Range(0, vehicles.Length)]);
        spawn.transform.position = nodeSpawn.transform.position + spawnOffset;
        spawn.transform.LookAt(nodeSpawn.outChoices[0].nextNode.transform.position);
        spawn.transform.SetParent(carsParent);

        spawn.GetComponent<CarAI>().pathParent = this.transform;

        spawnedCars.Add(spawn);
        Debug.Log("Car spawned");
    }

    [SerializeField] float despawnRange = 100f;
    void CheckDespawn()
    {
        for (int i = 0; i < spawnedCars.Count; i++)
        {
            if (spawnedCars[i] == null)
            {
                spawnedCars.RemoveAt(i);
                i--;
                Debug.Log("Null car removed");
                continue;
            }

            //If car is outside despawn range, respawn the car
            if (Vector3.Distance(playerCar.transform.position, spawnedCars[i].transform.position) > despawnRange)
            {
                Destroy(spawnedCars[i]);
                spawnedCars.RemoveAt(i);
                i--;
                continue;
            }
        }
    }
}
