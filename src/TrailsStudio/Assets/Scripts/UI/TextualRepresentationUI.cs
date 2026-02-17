using Assets.Scripts.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Managers;

namespace Assets.Scripts.UI
{
    public class TextualRepresentationUI : MonoBehaviour
    {
        VisualElement root;
        ScrollView scrollView;
        Label textLabel;
        Button closeButton;

        readonly StringBuilder textContent = new StringBuilder();

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            scrollView = root.Q<ScrollView>("TextScrollView");
            textLabel = root.Q<Label>("TextLabel");
            closeButton = root.Q<Button>("CloseButton");
            closeButton.RegisterCallback<ClickEvent>(CloseClicked);

            // get information about the line to be shown
            UpdateLineInformation();
        }

        void UpdateLineInformation()
        {
            textContent.Clear();

            Stack<SlopeChange> printedSlopes = new Stack<SlopeChange>();

            SlopeChange currentSlopeChange = null;

            foreach (ILineElement lineElem in Line.Instance)
            {                
                if (currentSlopeChange != null && lineElem.GetSlopeChange() != currentSlopeChange)
                {
                    textContent.AppendLine("=======End of slope change=========");
                    currentSlopeChange = null;
                }

                SlopeChange lastPrintedSlope = printedSlopes.Count > 0 ? printedSlopes.Peek() : null;

                if (lineElem.GetSlopeChange() != null && lineElem.GetSlopeChange() != lastPrintedSlope)
                {
                    currentSlopeChange = lineElem.GetSlopeChange();

                    var slopeChangeInfo = currentSlopeChange.GetInfoText();

                    textContent.AppendLine("=== Slope Change ===");

                    foreach (var (name, value) in slopeChangeInfo)
                    {
                        textContent.AppendLine($"{name}: {value}");
                    }                
                    
                    printedSlopes.Push(currentSlopeChange);
                }

                var infoList = lineElem.GetLineElementInfo();
                textContent.AppendLine("-----");
                foreach (var (name, value) in infoList)
                {
                    textContent.AppendLine($"{name}: {value}");
                }

                textContent.AppendLine();
            }

            textLabel.text = textContent.ToString();
        }

        void CloseClicked(ClickEvent evt)
        {
            Destroy(gameObject);
        }


    }
}
