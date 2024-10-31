using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using Unity.Cinemachine;

public class StateController : Singleton<StateController>
{
    State currentState;

    // Start is called before the first frame update
    void Start()
    {
       ChangeState(new DefaultState());
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
