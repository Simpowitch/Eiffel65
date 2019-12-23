using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawnSystem : MonoBehaviour
{
    [SerializeField] GameObject[] vehicles = null;

    PathNode[] nodes;
    [SerializeField] Transform playerCar = null;

    [SerializeField] List<GameObject> spawnedCars = new List<GameObject>();
    [SerializeField] Transform carsParent = null;

    [SerializeField] int spawnChance = 25;
    [SerializeField] int maxCars = 50;

    [SerializeField] Collider carSpawnBlocker = null;

    // Start is called before the first frame update
    void Start()
    {
        nodes = GetComponentsInChildren<PathNode>();

        for (int i = 0; i < nodes.Length; i++)
        {
            int rng = Random.Range(0, 100);

            if (rng < spawnChance && Vector3.Distance(playerCar.transform.position, nodes[i].transform.position) < despawnRange)
            {
                if (TestSpawn(nodes[i]))
                {
                    SpawnVehicle(nodes[i]);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //spawn cars on spawnpoints that the player can't see, if below minimum-car and below maximum
        if (spawnedCars.Count < maxCars)
        {
            Collider[] collisions = Physics.OverlapSphere(playerCar.transform.position, despawnRange);
            List<PathNode> nodesAroundPlayer = new List<PathNode>();
            foreach (var item in collisions)
            {
                if (item.GetComponent<PathNode>())
                {
                    nodesAroundPlayer.Add(item.GetComponent<PathNode>());
                }
            }
            PathNode spawnNode = nodesAroundPlayer[Random.Range(0, nodesAroundPlayer.Count)];

            bool allowedSpawn = true;
            Collider[] objectsAtNode = Physics.OverlapSphere(spawnNode.transform.position, obstructionCheck);
            foreach (var item in objectsAtNode)
            {
                if (item.tag == "Vehicle")
                {
                    allowedSpawn = false;
                    break;
                }
            }

            int tries = 0;
            while (!allowedSpawn && tries < 10)
            {
                spawnNode = nodesAroundPlayer[Random.Range(0, nodesAroundPlayer.Count)];
                allowedSpawn = TestSpawn(spawnNode);
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
    float minimumSpawnRange = 100f;
    bool TestSpawn(PathNode nodeToSpawnAt)
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


        //If spawnpoint is within disallowed area for spawn (where the player can see it) dont allow spawn
        if (carSpawnBlocker.bounds.Contains(nodeToSpawnAt.transform.position))
        {
            return false;
        }

        float distance = Vector3.Distance(playerCar.transform.position, nodeToSpawnAt.transform.position);

        if (distance < despawnRange / 2)
        {
            return false;
        }




        //if player can see this position (no obstacles and infront of the player), dont spawn
        //float distance = Vector3.Distance(playerCar.transform.position, nodeToSpawnAt.transform.position);
        //if (Vector3.Distance(playerCar.transform.position + playerCar.transform.forward * 1, nodeToSpawnAt.transform.position) < distance)
        //{
        //    ////TODO: Need better Raycast, currently colliding on the way to the pathnode with other 
        //    //if (Physics.Raycast(playerCar.transform.position, nodeToSpawnAt.transform.position - playerCar.transform.position, out RaycastHit hit, Mathf.Infinity))
        //    //{
        //    //    if (hit.transform.GetComponent<PathNode>() == nodeToSpawnAt)
        //    //    {
        //    //        return false;
        //    //    }
        //    //}
        //}
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

            //check distance to player
            if (Vector3.Distance(playerCar.transform.position, spawnedCars[i].transform.position) > despawnRange)
            {
                Destroy(spawnedCars[i]);
                spawnedCars.RemoveAt(i);
                i--;
                Debug.Log("Car despawned");
                continue;
            }
        }
    }
}
