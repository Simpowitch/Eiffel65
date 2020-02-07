using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Mission debugTestMission;

    Mission mission = null;
    Transform playerCar;
    PoliceVehicle policeVehicle;

    CarAI missionCarAI = null;

    public bool missionTimerOn = true;
    float timer;

    private void Start()
    {
        playerCar = this.transform;
        policeVehicle = GetComponent<PoliceVehicle>();
    }

    private void Update()
    {
        if (mission)
        {
            CheckMissionComplete();
        }
        if (mission)
        { 
            CheckMissionFail();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            AddMission(debugTestMission);
        }
    }

    public void AddMission(Mission newMission)
    {
        mission = newMission;
        if (newMission.loseCondition == LoseCondition.TimeLimit)
        {
            timer = newMission.timeLimit;
        }

        missionCarAI = null;
        if (mission.winCondition == WinCondition.StopCar || mission.loseCondition == LoseCondition.LostSight)
        {
            foreach (var item in CarAIMaster.instance.GetCarAIs())
            {
                if (item.GetMissionCarID() == mission.missionCarID)
                {
                    missionCarAI = item;
                    break;
                }
            }
            if (missionCarAI == null)
            {
                Debug.LogError("Missing car needed but not found");
            }
        }

        MessageManager.instance.ReceiveMessage(mission.messageAtStart);

        Debug.Log("Mission added");
    }

    public void CheckMissionComplete()
    {
        switch (mission.winCondition)
        {
            case WinCondition.GetToPosition:
                if (Vector3.Distance(playerCar.position, mission.reachPosition) < mission.positionRadius)
                {
                    CompleteMission();
                    return;
                }
                break;
            case WinCondition.StopCar:
                List<CarAI> stoppedCars = policeVehicle.GetStoppedCars();

                if (stoppedCars.Contains(missionCarAI))
                {
                        CompleteMission();
                }
                break;
        }
    }

    public void CheckMissionFail()
    {
        switch (mission.loseCondition)
        {
            case LoseCondition.TimeLimit:
                if (missionTimerOn)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0)
                    {
                        FailMission();
                    }
                }
                break;
            case LoseCondition.LostSight:
                if (Vector3.Distance(playerCar.position, missionCarAI.transform.position) > mission.missionCriticalDistance)
                {
                    FailMission();
                }
                break;
        }
    }

    private void CompleteMission()
    {
        MessageManager.instance.ReceiveMessage(mission.messageAtWin);
        mission = null;
        Debug.Log("Mission complete");
    }

    private void FailMission()
    {
        MessageManager.instance.ReceiveMessage(mission.messageAtLose);
        mission = null;
        Debug.Log("Mission Failed");
    }
}
