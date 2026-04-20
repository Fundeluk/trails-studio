using System;
using UnityEngine;

namespace Obstacles.Landing
{
    public class PositionHolder : MonoBehaviour
    {
        private Action onSelected;

        public void Init(Action onSelected)
        {
            this.onSelected = onSelected;
        }

        public void Select()
        {
            onSelected?.Invoke();
        }
    }
}