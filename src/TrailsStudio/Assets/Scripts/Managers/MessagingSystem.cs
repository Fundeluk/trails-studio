using Assets.Scripts.Utilities;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public enum MessagePriority
{
    Low,
    Medium,
    High
}

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// A generic singleton manager for handling UI notifications and messages using Unity's UI Toolkit. 
    /// Provides support for prioritized messaging and automated cleanup through durations.
    /// </summary>
    /// <remarks>Type T is the inheriting class itself, as it needs to be passed to the <see cref="Singleton{T}"/> parent.</remarks>
    /// <typeparam name="T">The type of the inheriting class.</typeparam>
    public class MessagingSystem<T> : Singleton<T> where T : MonoBehaviour
    {
        public class ActiveMessage
        {
            public readonly UIDocument UI;
            public readonly MessagePriority priority;

            public ActiveMessage(UIDocument ui, MessagePriority priority)
            {
                UI = ui;
                this.priority = priority;
            }
        }

        public GameObject messagePrefab;

        private ActiveMessage activeMessage = null;

        public void ShowMessage(string message, float duration = 0, MessagePriority priority = MessagePriority.Low)
        {
            if (activeMessage != null && activeMessage.priority > priority)
            {
                // Do not show the message if there is a higher priority message already active
                return;
            }

            activeMessage ??= new(Instantiate(messagePrefab, transform).GetComponent<UIDocument>(), priority);

            activeMessage.UI.rootVisualElement.Q<Label>("Message").text = message;
            activeMessage.UI.enabled = true;

            if (duration > 0)
            {
                if (destroyMessageCoroutine != null)
                {
                    StopCoroutine(destroyMessageCoroutine);
                }

                destroyMessageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
            }
        }

        public void HideMessage()
        {
            if (activeMessage == null)
            {
                return;
            }

            if (destroyMessageCoroutine != null)
            {
                StopCoroutine(destroyMessageCoroutine);
                destroyMessageCoroutine = null;
            }

            activeMessage.UI.enabled = false;
            Destroy(activeMessage.UI.gameObject);
            activeMessage = null;
        }

        private Coroutine destroyMessageCoroutine;

        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideMessage();
        }

        public Coroutine StartCoroutineFromInstance(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public void StopCoroutineFromInstance(Coroutine routine)
        {
            StopCoroutine(routine);
        }

    }
}