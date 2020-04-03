using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saro.Console
{
    public class LogItem : MonoBehaviour, IPointerClickHandler
    {
        public RectTransform RectTransform => m_SelfRectTransform;
        private RectTransform m_SelfRectTransform;

        public Image Image => m_Image;
        private Image m_Image;//self bg

        public int EntryIdx { get; private set; }

        public Action<LogItem> OnClick;

#pragma warning disable 649

        [SerializeField] private Text m_LogText;
        [SerializeField] private Image m_LogTypeImage; // error ? warning ? normal ?
        [SerializeField] private Text m_LogCountText;

#pragma warning disable 649

        private GameObject m_LogCountTextParent;

        private LogEntry m_LogEntry;

        private void Awake()
        {
            if (!m_SelfRectTransform) m_SelfRectTransform = transform as RectTransform;
            if (!m_Image) m_Image = GetComponent<Image>();
            if (m_LogCountText) m_LogCountTextParent = m_LogCountText.transform.parent.gameObject;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
#if UNITY_EDITOR
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                m_LogEntry.TraceScript();
            }
            else
#endif
                OnClick?.Invoke(this);
        }

        public void SetContent(LogEntry logEntry, int entryIdx, bool isExpanded, float selectedHeight, float normalHeight)
        {
            m_LogEntry = logEntry;
            EntryIdx = entryIdx;

            if (isExpanded)
            {
                //m_logText.overflowMode = TextOverflowModes.Overflow;
                m_LogText.horizontalOverflow = HorizontalWrapMode.Wrap;
                Resize(selectedHeight);
            }
            else
            {
                //m_logText.overflowMode = TextOverflowModes.Ellipsis;
                m_LogText.horizontalOverflow = HorizontalWrapMode.Overflow;
                Resize(normalHeight);
            }

            m_LogText.text = logEntry.ToString(!isExpanded);//isExpanded ? logEntry.ToString(true) : logEntry.logString;
            m_LogTypeImage.sprite = logEntry.typeSprite;
        }

        public void Resize(float newHeight)
        {
            Vector2 size = RectTransform.sizeDelta;
            size.y = newHeight;
            RectTransform.sizeDelta = size;
        }

        public void ShowCount()
        {
            m_LogCountText.text = /*m_logEntry.dateTimes.Count.ToString();//*/m_LogEntry.count.ToString();
            m_LogCountTextParent.SetActive(true);
        }

        public void HideCount()
        {
            m_LogCountTextParent.SetActive(false);
        }

        // Calculate selected logitem height
        public float CalculateExpandedHeight(string content, float itemHeight)
        {
            string text = m_LogText.text;
            //var mode = m_logText.overflowMode;
            HorizontalWrapMode wapMode = m_LogText.horizontalOverflow;

            m_LogText.text = content;
            //m_logText.overflowMode = TextOverflowModes.Overflow;
            m_LogText.horizontalOverflow = HorizontalWrapMode.Wrap;

            float result = m_LogText.preferredHeight;

            m_LogText.text = text;
            //m_logText.overflowMode = mode;
            m_LogText.horizontalOverflow = wapMode;

            return Mathf.Max(itemHeight, result);
        }

        public override string ToString()
        {
            return m_LogEntry.ToString(true);
        }
    }
}
