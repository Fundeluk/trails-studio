using Misc;
using States;
using UnityEngine;

namespace Managers
{
    public class StateController : Singleton<StateController>
    {
        public State CurrentState { get; protected set; }

        private void Awake()
        {
            Time.timeScale = 1;

        }

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
}
