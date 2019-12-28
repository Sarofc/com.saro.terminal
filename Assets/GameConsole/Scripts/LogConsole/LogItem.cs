using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saro.Console
{
    public class LogItem : MonoBehaviour, IPointerClickHandler
    {
        public RectTransform RectTransform => m_selfRectTransform;
        private RectTransform m_selfRectTransform;

        public Image Image => m_image;
        private Image m_image;//self bg

        public int EntryIdx { get; private set; }

        public Action<LogItem> OnClick;

#pragma warning disable 649

        [SerializeField] private Text m_logText;
        [SerializeField] private Image m_logTypeImage; // error ? warning ? normal ?
        [SerializeField] private Text m_logCountText;

#pragma warning disable 649

        private GameObject m_logCountTextParent;

        private LogEntry m_logEntry;

        private void Awake()
        {
            if (!m_selfRectTransform) m_selfRectTransform = transform as RectTransform;
            if (!m_image) m_image = GetComponent<Image>();
            if (m_logCountText) m_logCountTextParent = m_logCountText.transform.parent.gameObject;
        }

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

            if (isExpanded)
            {
                //m_logText.overflowMode = TextOverflowModes.Overflow;
                m_logText.horizontalOverflow = HorizontalWrapMode.Wrap;
                Resize(selectedHeight);
            }
            else
            {
                //m_logText.overflowMode = TextOverflowModes.Ellipsis;
                m_logText.horizontalOverflow = HorizontalWrapMode.Overflow;
                Resize(normalHeight);
            }

            m_logText.text = logEntry.ToString(!isExpanded);//isExpanded ? logEntry.ToString(true) : logEntry.logString;
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
            m_logCountText.text = /*m_logEntry.dateTimes.Count.ToString();//*/m_logEntry.count.ToString();
            m_logCountTextParent.SetActive(true);
        }

        public void HideCount()
        {
            m_logCountTextParent.SetActive(false);
        }

        // Calculate selected logitem height
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
            return m_logEntry.ToString(true);
        }
    }
}
