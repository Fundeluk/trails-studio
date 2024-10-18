using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.States
{
    /// <summary>
    /// Base class of all states.
    /// </summary>
    public abstract class State
    {
        protected StateController stateController;

        public void OnStateEnter(StateController stateController)
        {
            this.stateController = stateController;
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
