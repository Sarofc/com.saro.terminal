using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Saro.Console
{
    public class LogWindow : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float m_canvasGroupAlpha = .8f;
        [SerializeField] private ScrollRect m_scrollRect;
        private CanvasGroup m_canvasGroup;

        // log item prefab and color
        [SerializeField] private LogItem m_logItemPrefab;
        [SerializeField] private Color m_logItemSelectedColor;
        [SerializeField] private Color m_logItemNormalColor1;
        [SerializeField] private Color m_logItemNormalColor2;

        [Space()]
        // log count text
        [SerializeField] private TMP_Text m_infoEntryCountText;
        [SerializeField] private TMP_Text m_warningEntryCountText;
        [SerializeField] private TMP_Text m_errorEntryCountText;

        private float m_viewportHeight;
        private float m_itemHeight;
        private float m_itemHeightReciprocal;
        private float m_selectedItemHeight;

        private List<LogEntry> m_collapsedLogEntries = null;
        private LogIndicesList m_logEntryIndicesToShow = null;
        private Dictionary<int, LogItem> m_logItemsLookup = null;//根据index（LogEntryIndicesToShow），获取LogItem

        private Stack<LogItem> m_logItemPool;
        private int m_initPoolCount = 16;

        private bool m_isCollapsed = false;
        private int m_currentTopIdx = -1;
        private int m_currentBottomIdx = -1;

        private int m_idxOfSelectedLogEntry = -1;
        private float m_positionOfSelectedLogEntry = -1;
        private float m_deltaHeightOfSelectedLogEntry;

        public void Init(List<LogEntry> collapsedLogEntries, LogIndicesList logEntryIndicesToShow)
        {
            // get component and register event
            m_canvasGroup = GetComponent<CanvasGroup>();
            m_scrollRect.onValueChanged.AddListener(v => UpdateItemsInTheList(false));

            m_logItemsLookup = new Dictionary<int, LogItem>(56);
            m_logItemPool = new Stack<LogItem>(m_initPoolCount);
            
            for (int i = 0; i < m_initPoolCount; i++)
            {
                var go = GameObject.Instantiate(m_logItemPrefab, m_scrollRect.content, false);
                go.gameObject.SetActive(false);
                m_logItemPool.Push(go);
            }

            m_collapsedLogEntries = collapsedLogEntries;
            m_logEntryIndicesToShow = logEntryIndicesToShow;
            
            m_itemHeight = m_logItemPool.Peek().RectTransform.sizeDelta.y;
            m_itemHeightReciprocal = 1 / m_itemHeight;
            m_viewportHeight = m_scrollRect.viewport.rect.height;
        }

        #region public
        // --------------------------------------------------------
        // 
        // --------------------------------------------------------
        public void Show()
        {
            m_canvasGroup.alpha = m_canvasGroupAlpha;
            UpdateLogEntries(true);

            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
        }

        public void SetCollapseMode(bool collapse)
        {
            m_isCollapsed = collapse;
        }

        public void SnapToBottom()
        {
            m_scrollRect.verticalNormalizedPosition = 0;
        }

        public void OnSelectLogItem(LogItem item)
        {

            if (m_idxOfSelectedLogEntry != item.EntryIdx)
            {
                OnDeselectLogItem();

                m_idxOfSelectedLogEntry = item.EntryIdx;
                m_positionOfSelectedLogEntry = item.EntryIdx * m_itemHeight;
                m_selectedItemHeight = item.CalculateExpandedHeight(item.ToString(), m_itemHeight);
                m_deltaHeightOfSelectedLogEntry = m_selectedItemHeight - m_itemHeight;
            }
            else
            {
                OnDeselectLogItem();
            }

            if (m_idxOfSelectedLogEntry >= m_currentTopIdx && m_idxOfSelectedLogEntry <= m_currentBottomIdx)
            {
                ColorLogItem(m_logItemsLookup[m_idxOfSelectedLogEntry], m_idxOfSelectedLogEntry);
            }

            CalculateContentHeight();

            HardResetItems();
            UpdateItemsInTheList(true);
        }

        public void OnDeselectLogItem()
        {
            m_idxOfSelectedLogEntry = -1;
            m_positionOfSelectedLogEntry = -1;
            m_selectedItemHeight = m_deltaHeightOfSelectedLogEntry = 0;
        }

        // --------------------------------------------------------
        // update log entries
        // --------------------------------------------------------
        public void UpdateLogEntries(bool updateAllVisibleItemContents)
        {
            CalculateContentHeight();
            m_viewportHeight = m_scrollRect.viewport.rect.height;

            if (updateAllVisibleItemContents)
            {
                HardResetItems();
            }

            UpdateItemsInTheList(updateAllVisibleItemContents);
        }

        public void UpdateCollapsedLogEntryAtIdx(int idx)
        {
            if (m_logItemsLookup.TryGetValue(idx, out LogItem logItem))
            {
                logItem.ShowCount();
            }
        }

        public void UpdateItemsInTheList(bool updateAllVisibleItemContents)
        {
            if (m_logEntryIndicesToShow.Count > 0)
            {
                var contentPosTop = m_scrollRect.content.anchoredPosition.y - 1f;
                var contentPosBottom = contentPosTop + m_viewportHeight + 2f;

                if (m_positionOfSelectedLogEntry <= contentPosBottom)
                {
                    if (m_positionOfSelectedLogEntry <= contentPosTop)
                    {
                        contentPosTop -= m_deltaHeightOfSelectedLogEntry;
                        contentPosBottom -= m_deltaHeightOfSelectedLogEntry;

                        if (contentPosTop < m_positionOfSelectedLogEntry - 1f)
                        {
                            contentPosTop = m_positionOfSelectedLogEntry - 1f;
                        }

                        if (contentPosBottom < contentPosTop + 2f)
                        {
                            contentPosBottom = contentPosTop + 2f;
                        }
                    }
                    else
                    {
                        contentPosBottom -= m_deltaHeightOfSelectedLogEntry;
                        if (contentPosBottom < m_positionOfSelectedLogEntry + 1f)
                        {
                            contentPosBottom = m_positionOfSelectedLogEntry + 1f;
                        }
                    }
                }

                var newTopIdx = (int)(contentPosTop * m_itemHeightReciprocal);
                int newBottomIdx = (int)(contentPosBottom * m_itemHeightReciprocal);

                if (newTopIdx < 0) newTopIdx = 0;

                if (newBottomIdx > m_logEntryIndicesToShow.Count - 1)
                    newBottomIdx = m_logEntryIndicesToShow.Count - 1;

                if (m_currentTopIdx == -1)
                {
                    updateAllVisibleItemContents = true;
                    m_currentTopIdx = newTopIdx;
                    m_currentBottomIdx = newBottomIdx;

                    CreateLogItemsBetweenIndices(newTopIdx, newBottomIdx);
                }
                else
                {
                    // scroll a lot, there are no log items whithin
                    if (newBottomIdx < m_currentTopIdx || newTopIdx > m_currentBottomIdx)
                    {
                        updateAllVisibleItemContents = true;
                        DestroyLogItemsBetweenIndices(m_currentTopIdx, m_currentBottomIdx);
                        CreateLogItemsBetweenIndices(newTopIdx, newBottomIdx);
                    }
                    // after scrolled, there are still some log item within
                    else
                    {
                        if (newTopIdx > m_currentTopIdx)
                        {
                            DestroyLogItemsBetweenIndices(m_currentTopIdx, newTopIdx - 1);
                        }

                        if (newBottomIdx < m_currentBottomIdx)
                        {
                            DestroyLogItemsBetweenIndices(newBottomIdx + 1, m_currentBottomIdx);
                        }

                        if (newTopIdx < m_currentTopIdx)
                        {
                            CreateLogItemsBetweenIndices(newTopIdx, m_currentTopIdx - 1);

                            // if it's not necessary to update all the log items
                            if (!updateAllVisibleItemContents)
                            {
                                UpdateLogItemContentsBetweenIndices(newTopIdx, m_currentTopIdx - 1);
                            }
                        }

                        if (newBottomIdx > m_currentBottomIdx)
                        {
                            CreateLogItemsBetweenIndices(m_currentBottomIdx + 1, newBottomIdx);

                            // if it's not necessary to update all the log items
                            if (!updateAllVisibleItemContents)
                            {
                                UpdateLogItemContentsBetweenIndices(m_currentBottomIdx + 1, newBottomIdx);
                            }
                        }
                    }

                    m_currentTopIdx = newTopIdx;
                    m_currentBottomIdx = newBottomIdx;
                }

                // update all log items
                if (updateAllVisibleItemContents)
                {
                    UpdateLogItemContentsBetweenIndices(m_currentTopIdx, m_currentBottomIdx);
                }
            }
            else
            {
                HardResetItems();
            }
        }

        public void OnViewportDimensionsChanged()
        {
            m_viewportHeight = m_scrollRect.viewport.rect.height;

            if (m_idxOfSelectedLogEntry != -1)
            {
                var preIdx = m_idxOfSelectedLogEntry;
                OnDeselectLogItem();
                OnSelectLogItem(m_logItemsLookup[preIdx]);
            }
            else
            {
                UpdateItemsInTheList(false);
            }
        }

        // --------------------------------------------------------
        // update text
        // --------------------------------------------------------
        public void UpdateInfoCountText(int count)
        {
            m_infoEntryCountText.text = count.ToString();
        }

        public void UpdateErrorCountText(int count)
        {
            m_errorEntryCountText.text = count.ToString();
        }

        public void UpdateWarningCountText(int count)
        {
            m_warningEntryCountText.text = count.ToString();
        }

        #endregion

        #region private

        private void HardResetItems()
        {
            if (m_currentTopIdx != -1)
            {
                DestroyLogItemsBetweenIndices(m_currentTopIdx, m_currentBottomIdx);
                m_currentTopIdx = -1;
            }
        }

        private void UpdateLogItemContentsBetweenIndices(int start, int end)
        {
            for (int i = start; i < end + 1; i++)
            {
                LogItem logItem = m_logItemsLookup[i];
                logItem.SetContent(m_collapsedLogEntries[m_logEntryIndicesToShow[i]], i, i == m_idxOfSelectedLogEntry, m_selectedItemHeight, m_itemHeight);

                if (m_isCollapsed) logItem.ShowCount();
                else logItem.HideCount();
            }
        }

        private void CreateLogItemsBetweenIndices(int start, int end)
        {
            for (int i = start; i < end + 1; i++)
            {
                CreateLogItemAtIdx(i);
            }
        }

        private void DestroyLogItemsBetweenIndices(int start, int end)
        {
            for (int i = start; i < end + 1; i++)
            {
                m_logItemsLookup[i].OnClick -= OnSelectLogItem;
                PoolLogItem(m_logItemsLookup[i]);
            }
        }

        private void CreateLogItemAtIdx(int idx)
        {
            var logItem = PopLogItem(m_scrollRect.content);

            var anchor = new Vector2(1f, -idx * m_itemHeight);

            if (idx > m_idxOfSelectedLogEntry)
            {
                anchor.y -= m_deltaHeightOfSelectedLogEntry;
            }

            logItem.RectTransform.anchoredPosition = anchor;

            ColorLogItem(logItem, idx);
            logItem.OnClick += OnSelectLogItem;
            m_logItemsLookup[idx] = logItem;
        }

        private void CalculateContentHeight()
        {
            var newHeight = Mathf.Max(1f, m_logEntryIndicesToShow.Count * m_itemHeight + m_deltaHeightOfSelectedLogEntry);

            m_scrollRect.content.sizeDelta = new Vector2(0, newHeight);
        }

        private void ColorLogItem(LogItem logItem, int idx)
        {
            if (idx == m_idxOfSelectedLogEntry)
            {
                logItem.Image.color = m_logItemSelectedColor;
            }
            else if (idx % 2 == 0)
            {
                logItem.Image.color = m_logItemNormalColor1;
            }
            else
            {
                logItem.Image.color = m_logItemNormalColor2;
            }
        }

        private void PoolLogItem(LogItem logItem)
        {
            logItem.gameObject.SetActive(false);
            m_logItemPool.Push(logItem);
        }

        private LogItem PopLogItem(RectTransform parent)
        {
            LogItem newInstance;
            if (m_logItemPool.Count > 0)
            {
                newInstance = m_logItemPool.Pop();
                newInstance.gameObject.SetActive(true);
            }
            else
            {
                // create log item at scrollrect content
                newInstance = GameObject.Instantiate(m_logItemPrefab, parent, false);
            }
            return newInstance;
        }

        #endregion

    }
}
