using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saro.Console
{
    public class LogItem : MonoBehaviour, IPointerClickHandler
    {
        public RectTransform RectTransform { get => m_rectTransform; }
        public Image Image { get => m_image; }

        public Action<LogItem> OnClick;

        [SerializeField] private RectTransform m_rectTransform;
        [SerializeField] private Image m_image;

        //[SerializeField] private TMP_Text m_logText;
        [SerializeField] private Text m_logText;
        [SerializeField] private Image m_logTypeImage; // error ? warning ? normal ?

        [SerializeField] private GameObject m_logCountParrent;
        [SerializeField] private TMP_Text m_logCountText;
        //[SerializeField] private Text m_logCountText;

        public int EntryIdx { get; private set; }
        private LogEntry m_logEntry;


        public void OnPointerClick(PointerEventData eventData)
        {
#if UNITY_EDITOR
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                m_logEntry.TraceScript();
            }
            else
#endif
                OnClick?.Invoke(this);
        }

        public void SetContent(LogEntry logEntry, int entryIdx, bool isExpanded, float selectedHeight, float normalHeight)
        {
            m_logEntry = logEntry;
            EntryIdx = entryIdx;

            //var size = RectTransform.sizeDelta;
            if (isExpanded)
            {
                //m_logText.overflowMode = TextOverflowModes.Overflow;
                m_logText.horizontalOverflow = HorizontalWrapMode.Wrap;
                //size.y = selectedHeight;
                Resize(selectedHeight);
            }
            else
            {
                //m_logText.overflowMode = TextOverflowModes.Ellipsis;
                m_logText.horizontalOverflow = HorizontalWrapMode.Overflow;
                //size.y = normalHeight;
                Resize(normalHeight);
            }
            //RectTransform.sizeDelta = size;

            m_logText.text = isExpanded ? logEntry.ToString() : logEntry.logString;
            m_logTypeImage.sprite = logEntry.typeSprite;
        }

        public void Resize(float newHeight)
        {
            var size = RectTransform.sizeDelta;
            size.y = newHeight;
            RectTransform.sizeDelta = size;
        }

        public void ShowCount()
        {
            m_logCountText.text = m_logEntry.count.ToString();
            m_logCountParrent.SetActive(true);
        }

        public void HideCount()
        {
            m_logCountParrent.SetActive(false);
        }

        public float CalculateExpandedHeight(string content, float itemHeight)
        {
            string text = m_logText.text;
            //var mode = m_logText.overflowMode;
            var wapMode = m_logText.horizontalOverflow;

            m_logText.text = content;
            //m_logText.overflowMode = TextOverflowModes.Overflow;
            m_logText.horizontalOverflow = HorizontalWrapMode.Wrap;

            float result = m_logText.preferredHeight;

            m_logText.text = text;
            //m_logText.overflowMode = mode;
            m_logText.horizontalOverflow = wapMode;

            return Mathf.Max(itemHeight, result);
        }

        public override string ToString()
        {
            return m_logEntry.ToString();
        }
    }
}
