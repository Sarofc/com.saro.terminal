using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Saro.Console
{
    public class LogWindow : MonoBehaviour
    {
#pragma warning disable 649

        [SerializeField, Range(0, 1)] private float m_CanvasGroupAlpha = .8f;
        [SerializeField] private ScrollRect m_ScrollRect;
        private CanvasGroup m_CanvasGroup;

        // log item prefab and color
        [SerializeField] private LogItem m_LogItemPrefab;
        [SerializeField] private Color m_LogItemSelectedColor;
        [SerializeField] private Color m_LogItemNormalColor1;
        [SerializeField] private Color m_LogItemNormalColor2;

        [Space()]
        // log count text
        [SerializeField] private Text m_InfoEntryCountText;
        [SerializeField] private Text m_WarningEntryCountText;
        [SerializeField] private Text m_ErrorEntryCountText;

#pragma warning disable 649

        private float m_ViewportHeight;
        private float m_ItemHeight;
        private float m_ItemHeightReciprocal;
        private float m_SelectedItemHeight;

        private List<LogEntry> m_CollapsedLogEntries = null;
        private List<int> m_LogEntryIndicesToShow = null;
        private Dictionary<int, LogItem> m_LogItemsLookup = null;//根据index（LogEntryIndicesToShow），获取LogItem

        private Stack<LogItem> m_LogItemPool;
        private int m_InitPoolCount = 16;

        private bool m_IsCollapsed = false;
        private int m_CurrentTopIdx = -1;
        private int m_CurrentBottomIdx = -1;

        private int m_IdxOfSelectedLogEntry = -1;
        private float m_PositionOfSelectedLogEntry = -1;
        private float m_DeltaHeightOfSelectedLogEntry;

        public void Init(List<LogEntry> collapsedLogEntries, List<int> logEntryIndicesToShow)
        {
            // get component and register event
            m_CanvasGroup = GetComponent<CanvasGroup>();
            m_ScrollRect.onValueChanged.AddListener(v => UpdateItemsInTheList(false));

            m_LogItemsLookup = new Dictionary<int, LogItem>(56);
            m_LogItemPool = new Stack<LogItem>(m_InitPoolCount);

            for (int i = 0; i < m_InitPoolCount; i++)
            {
                LogItem go = GameObject.Instantiate(m_LogItemPrefab, m_ScrollRect.content, false);
                go.gameObject.SetActive(false);
                m_LogItemPool.Push(go);
            }

            m_CollapsedLogEntries = collapsedLogEntries;
            m_LogEntryIndicesToShow = logEntryIndicesToShow;

            m_ItemHeight = m_LogItemPool.Peek().RectTransform.sizeDelta.y;
            m_ItemHeightReciprocal = 1 / m_ItemHeight;
            m_ViewportHeight = m_ScrollRect.viewport.rect.height;
        }

        #region public
        // --------------------------------------------------------
        // 
        // --------------------------------------------------------
        public void Show()
        {
            m_CanvasGroup.alpha = m_CanvasGroupAlpha;
            UpdateLogEntries(true);

            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void SetCollapseMode(bool collapse)
        {
            m_IsCollapsed = collapse;
        }

        public void SnapToBottom()
        {
            m_ScrollRect.verticalNormalizedPosition = 0;
        }

        public void OnSelectLogItem(LogItem item)
        {

            if (m_IdxOfSelectedLogEntry != item.EntryIdx)
            {
                OnDeselectLogItem();

                m_IdxOfSelectedLogEntry = item.EntryIdx;
                m_PositionOfSelectedLogEntry = item.EntryIdx * m_ItemHeight;
                m_SelectedItemHeight = item.CalculateExpandedHeight(item.ToString(), m_ItemHeight);
                m_DeltaHeightOfSelectedLogEntry = m_SelectedItemHeight - m_ItemHeight;
            }
            else
            {
                OnDeselectLogItem();
            }

            if (m_IdxOfSelectedLogEntry >= m_CurrentTopIdx && m_IdxOfSelectedLogEntry <= m_CurrentBottomIdx)
            {
                ColorLogItem(m_LogItemsLookup[m_IdxOfSelectedLogEntry], m_IdxOfSelectedLogEntry);
            }

            CalculateContentHeight();

            HardResetItems();
            UpdateItemsInTheList(true);
        }

        public void OnDeselectLogItem()
        {
            m_IdxOfSelectedLogEntry = -1;
            m_PositionOfSelectedLogEntry = -1;
            m_SelectedItemHeight = m_DeltaHeightOfSelectedLogEntry = 0;
        }

        // --------------------------------------------------------
        // update log entries
        // --------------------------------------------------------
        public void UpdateLogEntries(bool updateAllVisibleItemContents)
        {
            CalculateContentHeight();
            m_ViewportHeight = m_ScrollRect.viewport.rect.height;

            if (updateAllVisibleItemContents)
            {
                HardResetItems();
            }

            UpdateItemsInTheList(updateAllVisibleItemContents);
        }

        public void UpdateCollapsedLogEntryAtIdx(int idx)
        {
            if (m_LogItemsLookup.TryGetValue(idx, out LogItem logItem))
            {
                logItem.ShowCount();
            }
        }

        public void UpdateItemsInTheList(bool updateAllVisibleItemContents)
        {
            if (m_LogEntryIndicesToShow.Count > 0)
            {
                float contentPosTop = m_ScrollRect.content.anchoredPosition.y - 1f;
                float contentPosBottom = contentPosTop + m_ViewportHeight + 2f;

                if (m_PositionOfSelectedLogEntry <= contentPosBottom)
                {
                    if (m_PositionOfSelectedLogEntry <= contentPosTop)
                    {
                        contentPosTop -= m_DeltaHeightOfSelectedLogEntry;
                        contentPosBottom -= m_DeltaHeightOfSelectedLogEntry;

                        if (contentPosTop < m_PositionOfSelectedLogEntry - 1f)
                        {
                            contentPosTop = m_PositionOfSelectedLogEntry - 1f;
                        }

                        if (contentPosBottom < contentPosTop + 2f)
                        {
                            contentPosBottom = contentPosTop + 2f;
                        }
                    }
                    else
                    {
                        contentPosBottom -= m_DeltaHeightOfSelectedLogEntry;
                        if (contentPosBottom < m_PositionOfSelectedLogEntry + 1f)
                        {
                            contentPosBottom = m_PositionOfSelectedLogEntry + 1f;
                        }
                    }
                }

                int newTopIdx = (int)(contentPosTop * m_ItemHeightReciprocal);
                int newBottomIdx = (int)(contentPosBottom * m_ItemHeightReciprocal);

                if (newTopIdx < 0) newTopIdx = 0;

                if (newBottomIdx > m_LogEntryIndicesToShow.Count - 1)
                    newBottomIdx = m_LogEntryIndicesToShow.Count - 1;

                if (m_CurrentTopIdx == -1)
                {
                    updateAllVisibleItemContents = true;
                    m_CurrentTopIdx = newTopIdx;
                    m_CurrentBottomIdx = newBottomIdx;

                    CreateLogItemsBetweenIndices(newTopIdx, newBottomIdx);
                }
                else
                {
                    // scroll a lot, there are no log items whithin
                    if (newBottomIdx < m_CurrentTopIdx || newTopIdx > m_CurrentBottomIdx)
                    {
                        updateAllVisibleItemContents = true;
                        DestroyLogItemsBetweenIndices(m_CurrentTopIdx, m_CurrentBottomIdx);
                        CreateLogItemsBetweenIndices(newTopIdx, newBottomIdx);
                    }
                    // after scrolled, there are still some log item within
                    else
                    {
                        if (newTopIdx > m_CurrentTopIdx)
                        {
                            DestroyLogItemsBetweenIndices(m_CurrentTopIdx, newTopIdx - 1);
                        }

                        if (newBottomIdx < m_CurrentBottomIdx)
                        {
                            DestroyLogItemsBetweenIndices(newBottomIdx + 1, m_CurrentBottomIdx);
                        }

                        if (newTopIdx < m_CurrentTopIdx)
                        {
                            CreateLogItemsBetweenIndices(newTopIdx, m_CurrentTopIdx - 1);

                            // if it's not necessary to update all the log items
                            if (!updateAllVisibleItemContents)
                            {
                                UpdateLogItemContentsBetweenIndices(newTopIdx, m_CurrentTopIdx - 1);
                            }
                        }

                        if (newBottomIdx > m_CurrentBottomIdx)
                        {
                            CreateLogItemsBetweenIndices(m_CurrentBottomIdx + 1, newBottomIdx);

                            // if it's not necessary to update all the log items
                            if (!updateAllVisibleItemContents)
                            {
                                UpdateLogItemContentsBetweenIndices(m_CurrentBottomIdx + 1, newBottomIdx);
                            }
                        }
                    }

                    m_CurrentTopIdx = newTopIdx;
                    m_CurrentBottomIdx = newBottomIdx;
                }

                // update all log items
                if (updateAllVisibleItemContents)
                {
                    UpdateLogItemContentsBetweenIndices(m_CurrentTopIdx, m_CurrentBottomIdx);
                }
            }
            else
            {
                HardResetItems();
            }
        }

        public void OnViewportDimensionsChanged()
        {
            m_ViewportHeight = m_ScrollRect.viewport.rect.height;

            if (m_IdxOfSelectedLogEntry != -1)
            {
                int preIdx = m_IdxOfSelectedLogEntry;
                OnDeselectLogItem();
                OnSelectLogItem(m_LogItemsLookup[preIdx]);
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
            m_InfoEntryCountText.text = count.ToString();
        }

        public void UpdateErrorCountText(int count)
        {
            m_ErrorEntryCountText.text = count.ToString();
        }

        public void UpdateWarningCountText(int count)
        {
            m_WarningEntryCountText.text = count.ToString();
        }

        #endregion

        #region private

        private void HardResetItems()
        {
            if (m_CurrentTopIdx != -1)
            {
                DestroyLogItemsBetweenIndices(m_CurrentTopIdx, m_CurrentBottomIdx);
                m_CurrentTopIdx = -1;
            }
        }

        private void UpdateLogItemContentsBetweenIndices(int start, int end)
        {
            for (int i = start; i < end + 1; i++)
            {
                LogItem logItem = m_LogItemsLookup[i];
                logItem.SetContent(m_CollapsedLogEntries[m_LogEntryIndicesToShow[i]], i, i == m_IdxOfSelectedLogEntry, m_SelectedItemHeight, m_ItemHeight);

                if (m_IsCollapsed) logItem.ShowCount();
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
                m_LogItemsLookup[i].OnClick -= OnSelectLogItem;
                PoolLogItem(m_LogItemsLookup[i]);
            }
        }

        private void CreateLogItemAtIdx(int idx)
        {
            LogItem logItem = PopLogItem(m_ScrollRect.content);

            Vector2 anchor = new Vector2(1f, -idx * m_ItemHeight);

            if (idx > m_IdxOfSelectedLogEntry)
            {
                anchor.y -= m_DeltaHeightOfSelectedLogEntry;
            }

            logItem.RectTransform.anchoredPosition = anchor;

            ColorLogItem(logItem, idx);
            logItem.OnClick += OnSelectLogItem;
            m_LogItemsLookup[idx] = logItem;
        }

        private void CalculateContentHeight()
        {
            float newHeight = Mathf.Max(1f, m_LogEntryIndicesToShow.Count * m_ItemHeight + m_DeltaHeightOfSelectedLogEntry);

            m_ScrollRect.content.sizeDelta = new Vector2(0, newHeight);
        }

        private void ColorLogItem(LogItem logItem, int idx)
        {
            if (idx == m_IdxOfSelectedLogEntry)
            {
                logItem.Image.color = m_LogItemSelectedColor;
            }
            else if (idx % 2 == 0)
            {
                logItem.Image.color = m_LogItemNormalColor1;
            }
            else
            {
                logItem.Image.color = m_LogItemNormalColor2;
            }
        }

        private void PoolLogItem(LogItem logItem)
        {
            logItem.gameObject.SetActive(false);
            m_LogItemPool.Push(logItem);
        }

        private LogItem PopLogItem(RectTransform parent)
        {
            LogItem newInstance;
            if (m_LogItemPool.Count > 0)
            {
                newInstance = m_LogItemPool.Pop();
                newInstance.gameObject.SetActive(true);
            }
            else
            {
                // create log item at scrollrect content
                newInstance = GameObject.Instantiate(m_LogItemPrefab, parent, false);
            }
            return newInstance;
        }

        #endregion

    }
}
