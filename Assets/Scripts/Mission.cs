using UnityEngine;

public enum WinCondition { GetToPosition, StopCar}
public enum LoseCondition { TimeLimit, LostSight}
[CreateAssetMenu(menuName = "Scenario/Mission")]
public class Mission : ScriptableObject
{
    public WinCondition winCondition;
    public LoseCondition loseCondition;

    //Car to follow/stop
    public int missionCarID;
    public float missionCriticalDistance;

    //Get To Pos
    public Vector3 reachPosition;
    public float positionRadius;

    public float timeLimit;

    public Message messageAtStart;
    public Message messageAtWin;
    public Message messageAtLose;
}
