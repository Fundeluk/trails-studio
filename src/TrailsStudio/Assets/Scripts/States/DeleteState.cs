using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.States
{
    public class DeleteState : State
    {
        protected override void OnEnter()
        {
            CameraManager.Instance.DetailedView(Line.Instance.line[^1]);
            UIManager.Instance.ShowUI(UIManager.Instance.deleteUI);
        }
        protected override void OnExit()
        {
        }

    }
}
