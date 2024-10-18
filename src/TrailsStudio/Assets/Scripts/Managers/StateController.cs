using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;

public class StateController : Singleton<StateController>
{
    State currentState;

    public GameObject sidebarMenuUI;
    public GameObject buildPhaseUI;

    public static DefaultState defaultState = new DefaultState();
    public static TakeoffState takeoffState = new TakeoffState();
    public static LandingState landingState = new LandingState();
    public static NewObstacleState newObstacleState = new NewObstacleState();
    public static MeasureState measureState = new MeasureState();
    

    // Start is called before the first frame update
    void Start()
    {
       ChangeState(defaultState);
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.OnStateUpdate();
    }

    public void ChangeState(State newState)
    {
        currentState?.OnStateExit();

        currentState = newState;
        currentState.OnStateEnter(this);
    }
}
