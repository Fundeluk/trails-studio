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
            // fixes bug where event system makes scene unresponsive when loaded from another scene
            Time.timeScale = 1;
        }

        // Start is called before the first frame update
        void Start()
        {
            ChangeState(new IdleState());
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
