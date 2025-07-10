using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.States;
using Assets.Scripts.Utilities;
using Unity.Cinemachine;

public class StateController : Singleton<StateController>
{
    public State CurrentState { get; protected set; }

    // Start is called before the first frame update
    void Start()
    {
       ChangeState(new DefaultState());
    }

    // Update is called once per frame
    void Update()
    {
        CurrentState?.OnStateUpdate();
    }

    public void ChangeState(State newState)
    {
        CurrentState?.OnStateExit();

        CurrentState = newState;
        CurrentState.OnStateEnter(this);
    }
}
