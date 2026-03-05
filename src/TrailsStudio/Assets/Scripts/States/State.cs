using Managers;

namespace States
{
    /// <summary>
    /// Base class of all states.
    /// </summary>
    public abstract class State
    {
        protected StateController StateController;

        public void OnStateEnter(StateController stateController)
        {
            this.StateController = stateController;
            OnEnter();
        }

        protected virtual void OnEnter()
        {
        }

        public void OnStateUpdate()
        {
            OnUpdate();
        }

        protected virtual void OnUpdate()
        {
        }

        public void OnStateExit()
        {
            OnExit();
        }

        protected virtual void OnExit()
        {
        }
    }
}
