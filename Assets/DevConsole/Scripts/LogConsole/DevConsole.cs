using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saro.Console
{
    public class DevConsole : MonoBehaviour
    {
        public static DevConsole Get() => s_Instance;

        private static DevConsole s_Instance = null;

        public const float k_Version = 0.01f;

        #region Options

        private const string k_Key_LogWindowHeight = "GameConsoleHeight";
        private const string k_Key_LogWindowWidth = "GameConsoleWidth";

        private const string k_Key_IsInfoLogEnable = "InfoLogEnable";
        private const string k_Key_IsWarningLogEnable = "WarningLogEnable";
        private const string k_Key_IsErrorLogEnable = "ErrorLogEnable";
        private const string k_Key_IsCollapseEnable = "CollapseEnable";

        private const float k_MinHeightLogWindow = 120f;
        private const float k_MinWidthLogWindow = 250f;
        private float m_MaxHeightLogWindow;
        private float m_MaxWidthLogWindow;

        private float m_LogWindowHeight;
        private float m_LogWindowWidth;

        private bool m_IsCollapsed;
        private bool m_IsInfoEnabled;
        private bool m_IsWarningEnabled;
        private bool m_IsErrorEnabled;

        [Header("Properties")]

        [SerializeField] private KeyCode m_ToggleKey = KeyCode.BackQuote;
        [SerializeField] private int m_CommandHistorySize = 15;

#pragma warning disable 649

        [Header("Views")]
        [SerializeField] private Sprite m_InfoSprite;
        [SerializeField] private Sprite m_WarningSprite;
        [SerializeField] private Sprite m_ErrorSprite;

        [SerializeField] private Color m_BtnNormalColor;
        [SerializeField] private Color m_BtnSelectedColor;

        [Header("Components")]
        [SerializeField] private LogWindow m_LogWindow;
        [SerializeField] private MiniWindow m_MiniWindow;
        [Space()]
        [SerializeField] private Button m_CloseBtn;
        [SerializeField] private EventTrigger m_ResizeBtn;
        [SerializeField] private Button m_ClearBtn;
        [SerializeField] private Button m_CollapseBtn;
        [Space()]
        [SerializeField] private Button m_FilterInfoBtn;
        [SerializeField] private Button m_FilterWarningBtn;
        [SerializeField] private Button m_FilterErrorBtn;
        [Space()]
        [SerializeField] private InputField m_CommandInput;


        private RectTransform m_SelfRectTransform;
        #endregion

#pragma warning disable 649

        #region Collections

        private Dictionary<LogType, Sprite> m_LogSpriteLookup;              //存LogSprite

        private List<LogEntry> m_CollapsedLogEntries;                       //存LogEntry，不允许重复，每个LogEntry唯一
        private Dictionary<LogEntry, int> m_CollapsedLogEntriesLookup;      //根据唯一LogEntry，获取其所在的index
        private List<int> m_UncollapsedLogEntryIndices;                     //存所有LogEntry的index，相同的LogEntry的索引相等。
        private List<int> m_LogEntryIndicesToShow;                          //需要显示的LogEntry的Index

        private Queue<QueuedLogEntry> m_QueueLogs;

        private LoopArray<string> m_CommandHistory;                         // 历史命令
        private int m_CommandIdx = -1;

        #endregion

        private bool m_ScreenDimensionsChanged;
        private bool m_IsLogWindowVisible;

        private int m_ErrorCount;
        private int m_WarningCount;
        private int m_InfoCount;

        #region Unity Method

        private void Awake()
        {
            #region Instance
            if (s_Instance == null)
            {
                s_Instance = this;

                DontDestroyOnLoad(gameObject);
            }
            else if (this != s_Instance)
            {
                Destroy(gameObject);
                return;
            }
            #endregion

            // init collections
            m_CollapsedLogEntries = new List<LogEntry>(128);
            m_CollapsedLogEntriesLookup = new Dictionary<LogEntry, int>(128);
            m_UncollapsedLogEntryIndices = new List<int>(64);
            m_LogEntryIndicesToShow = new List<int>(64);

            m_QueueLogs = new Queue<QueuedLogEntry>(64);
            m_CommandHistory = new LoopArray<string>(m_CommandHistorySize);

            m_LogSpriteLookup = new Dictionary<LogType, Sprite>(5)
            {
                {LogType.Log, m_InfoSprite },
                {LogType.Warning, m_WarningSprite },
                {LogType.Error, m_ErrorSprite },
                {LogType.Assert, m_ErrorSprite },
                {LogType.Exception, m_ErrorSprite }
            };

            // read PlayerPrefs
            m_LogWindowHeight = PlayerPrefs.GetFloat(k_Key_LogWindowHeight, k_MinHeightLogWindow);
            m_LogWindowWidth = PlayerPrefs.GetFloat(k_Key_LogWindowWidth, k_MinWidthLogWindow);

            m_IsCollapsed = PlayerPrefs.GetInt(k_Key_IsCollapseEnable, 1) == 1 ? true : false;
            m_IsInfoEnabled = PlayerPrefs.GetInt(k_Key_IsInfoLogEnable, 1) == 1 ? true : false;
            m_IsWarningEnabled = PlayerPrefs.GetInt(k_Key_IsWarningLogEnable, 1) == 1 ? true : false;
            m_IsErrorEnabled = PlayerPrefs.GetInt(k_Key_IsErrorLogEnable, 1) == 1 ? true : false;

            // button state
            m_ResizeBtn.GetComponent<Image>().color = m_BtnNormalColor;
            m_CloseBtn.image.color = m_BtnNormalColor;
            m_ClearBtn.image.color = m_BtnNormalColor;

            m_CollapseBtn.image.color = m_IsCollapsed ? m_BtnSelectedColor : m_BtnNormalColor;

            m_FilterInfoBtn.image.color = m_IsInfoEnabled ? m_BtnSelectedColor : m_BtnNormalColor;
            m_FilterWarningBtn.image.color = m_IsWarningEnabled ? m_BtnSelectedColor : m_BtnNormalColor;
            m_FilterErrorBtn.image.color = m_IsErrorEnabled ? m_BtnSelectedColor : m_BtnNormalColor;
        }

        private void Start()
        {
            // init log window
            m_SelfRectTransform = transform as RectTransform;
            m_LogWindow.Init(m_CollapsedLogEntries, m_LogEntryIndicesToShow);
            m_LogWindow.SetCollapseMode(m_IsCollapsed);
            m_LogWindow.UpdateItemsInTheList(true);


            // new size
            m_MaxHeightLogWindow = m_SelfRectTransform.sizeDelta.y;
            m_MaxWidthLogWindow = m_SelfRectTransform.sizeDelta.x;

            if (m_LogWindowHeight < k_MinHeightLogWindow) m_LogWindowHeight = k_MinHeightLogWindow;
            if (m_LogWindowWidth < k_MinWidthLogWindow) m_LogWindowWidth = k_MinWidthLogWindow;

            Resize(m_LogWindowHeight, m_LogWindowWidth);

            // Test
            ShowLogWindow(false);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += ReceivedLog;

            if (m_MiniWindow)
            {
                m_MiniWindow.OnClick += MiniWindowClick;
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Drag;
            entry.callback.AddListener(OnDrag);
            m_ResizeBtn.triggers.Add(entry);

            m_CloseBtn.onClick.AddListener(OnCloseClick);
            m_ClearBtn.onClick.AddListener(OnClearBtnClick);
            m_CollapseBtn.onClick.AddListener(OnCollapseBtbClick);

            m_FilterInfoBtn.onClick.AddListener(OnFilterInfoBtnClick);
            m_FilterWarningBtn.onClick.AddListener(OnFilterWarningBtnClick);
            m_FilterErrorBtn.onClick.AddListener(OnFilterErrorBtnClick);

            m_CommandInput.onValidateInput += OnValidateCommand;
            m_CommandInput.onValueChanged.AddListener(OnChangedCommand);

            // saving PlayerPrefs
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += (s) =>
            {
                if (s == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    // print("exit playmode");

                    PlayerPrefs.SetFloat(k_Key_LogWindowHeight, m_LogWindowHeight);
                    PlayerPrefs.SetFloat(k_Key_LogWindowWidth, m_LogWindowWidth);

                    PlayerPrefs.SetInt(k_Key_IsCollapseEnable, m_IsCollapsed ? 1 : 0);
                    PlayerPrefs.SetInt(k_Key_IsInfoLogEnable, m_IsInfoEnabled ? 1 : 0);
                    PlayerPrefs.SetInt(k_Key_IsWarningLogEnable, m_IsWarningEnabled ? 1 : 0);
                    PlayerPrefs.SetInt(k_Key_IsErrorLogEnable, m_IsErrorEnabled ? 1 : 0);
                }
            };
#else
             Application.quitting += () =>
            {
                // print("exit game");

                PlayerPrefs.SetFloat(k_Key_LogWindowHeight, m_LogWindowHeight);
                PlayerPrefs.SetFloat(k_Key_LogWindowWidth, m_LogWindowWidth);

                PlayerPrefs.SetInt(k_Key_IsCollapseEnable, m_IsCollapsed ? 1 : 0);
                PlayerPrefs.SetInt(k_Key_IsInfoLogEnable, m_IsInfoEnabled ? 1 : 0);
                PlayerPrefs.SetInt(k_Key_IsWarningLogEnable, m_IsWarningEnabled ? 1 : 0);
                PlayerPrefs.SetInt(k_Key_IsErrorLogEnable, m_IsErrorEnabled ? 1 : 0);
            };
#endif
            // TODO log andriod
        }

        private void OnDisable()
        {
            if (s_Instance != this) return;

            Application.logMessageReceived -= ReceivedLog;

            if (m_MiniWindow)
            {
                m_MiniWindow.OnClick -= MiniWindowClick;
            }

            m_ResizeBtn.triggers.Clear();

            m_CloseBtn.onClick.RemoveListener(OnCloseClick);
            m_ClearBtn.onClick.RemoveListener(OnClearBtnClick);
            m_CollapseBtn.onClick.RemoveListener(OnCollapseBtbClick);

            m_FilterInfoBtn.onClick.RemoveListener(OnFilterInfoBtnClick);
            m_FilterWarningBtn.onClick.RemoveListener(OnFilterWarningBtnClick);
            m_FilterErrorBtn.onClick.RemoveListener(OnFilterErrorBtnClick);

            m_CommandInput.onValidateInput -= OnValidateCommand;
            m_CommandInput.onValueChanged.RemoveListener(OnChangedCommand);
        }

        private void OnRectTransformDimensionsChange()
        {
            m_ScreenDimensionsChanged = true;
        }

        private void LateUpdate()
        {
            ProcessLogQueue();

            ProcessScreenDimensions();

            ProcessKey();
        }

        #endregion

        #region Window
        public void ShowLogWindow(bool show)
        {
            if (show)
            {
                m_LogWindow.Show();
                m_MiniWindow.Hide();

                // update text
                m_LogWindow.UpdateInfoCountText(m_InfoCount);
                m_LogWindow.UpdateWarningCountText(m_WarningCount);
                m_LogWindow.UpdateErrorCountText(m_ErrorCount);
            }
            else
            {
                m_LogWindow.Hide();
                m_MiniWindow.Show();

                // reset inputfield
                m_CommandInput.text = "";
                m_CommandIdx = m_CommandHistory.Length;
                EventSystem.current.SetSelectedGameObject(null);
            }

            m_IsLogWindowVisible = show;
        }

        private void Resize(float newHeight, float newWidth)
        {
            Vector2 anchorMin = (m_LogWindow.transform as RectTransform).anchorMin;
            anchorMin.y = Mathf.Max(0f, 1f - newHeight / m_SelfRectTransform.sizeDelta.y);
            anchorMin.x = Mathf.Max(0f, 1f - newWidth / m_SelfRectTransform.sizeDelta.x);
            (m_LogWindow.transform as RectTransform).anchorMin = anchorMin;

            //var pos = (m_logWindow.transform as RectTransform).sizeDelta;
            //pos.y = Mathf.Max(0f, newHeight);
            //pos.x = Mathf.Max(0f, newWidth);
            //(m_logWindow.transform as RectTransform).sizeDelta = pos;

            m_LogWindow.OnViewportDimensionsChanged();

            //m_logWindow.SnapToBottom();
        }

        #endregion

        #region Log

        private void ReceivedLog(string logString, string stackTrace, LogType type)
        {
            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                m_QueueLogs.Enqueue(new QueuedLogEntry(logString, stackTrace, type));
                return;
            }

            LogEntry logEntry = new LogEntry(logString, stackTrace, null);

            bool isEntryInCollapsedEntryList = m_CollapsedLogEntriesLookup.TryGetValue(logEntry, out int logEntryIdx);
            // already has entry in list
            if (isEntryInCollapsedEntryList)
            {
                logEntry = m_CollapsedLogEntries[logEntryIdx];
                logEntry.count++;
            }
            // new one, store to map
            else
            {
                logEntry.typeSprite = m_LogSpriteLookup[type];
                logEntryIdx = m_CollapsedLogEntries.Count;
                m_CollapsedLogEntries.Add(logEntry);
                m_CollapsedLogEntriesLookup[logEntry] = logEntryIdx;
            }

            m_UncollapsedLogEntryIndices.Add(logEntryIdx);

            if (m_IsCollapsed && isEntryInCollapsedEntryList)
            {
                if (m_IsLogWindowVisible)
                {
                    m_LogWindow.UpdateCollapsedLogEntryAtIdx(logEntryIdx);
                }
            }
            else if (m_IsInfoEnabled || m_IsWarningEnabled || m_IsErrorEnabled)
            {
                m_LogEntryIndicesToShow.Add(logEntryIdx);
                if (m_IsLogWindowVisible)
                {
                    m_LogWindow.UpdateLogEntries(false);
                }
            }

            if (type == LogType.Warning)
            {
                ++m_WarningCount;
                if (m_IsLogWindowVisible)
                    m_LogWindow.UpdateWarningCountText(m_WarningCount);

                //else
                //{
                //    //TODO update mini window count
                //}
            }
            else if (type == LogType.Log)
            {
                ++m_InfoCount;
                if (m_IsLogWindowVisible)
                    m_LogWindow.UpdateInfoCountText(m_InfoCount);

                //else
                //{
                //    //TODO update mini window
                //}
            }
            else
            {
                ++m_ErrorCount;
                if (m_IsLogWindowVisible)
                    m_LogWindow.UpdateErrorCountText(m_ErrorCount);

                //else
                //{
                //    //TODO update mini window
                //}
            }

            m_LogWindow.SnapToBottom();
        }

        private void LogFilter()
        {
            m_LogEntryIndicesToShow.Clear();

            if (m_IsCollapsed)
            {
                for (int i = 0; i < m_CollapsedLogEntries.Count; i++)
                {
                    LogEntry entry = m_CollapsedLogEntries[i];

                    if ((m_IsInfoEnabled && entry.typeSprite == m_InfoSprite)
                        || (m_IsWarningEnabled && entry.typeSprite == m_WarningSprite)
                        || (m_IsErrorEnabled && entry.typeSprite == m_ErrorSprite))
                    {
                        m_LogEntryIndicesToShow.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_UncollapsedLogEntryIndices.Count; i++)
                {
                    LogEntry entry = m_CollapsedLogEntries[m_UncollapsedLogEntryIndices[i]];

                    if ((m_IsInfoEnabled && entry.typeSprite == m_InfoSprite)
                        || (m_IsWarningEnabled && entry.typeSprite == m_WarningSprite)
                        || (m_IsErrorEnabled && entry.typeSprite == m_ErrorSprite))
                    {
                        m_LogEntryIndicesToShow.Add(m_UncollapsedLogEntryIndices[i]);
                    }
                }
            }

            m_LogWindow.OnDeselectLogItem();
            m_LogWindow.UpdateLogEntries(true);

            m_LogWindow.SnapToBottom();
        }

        internal string GetLog()
        {
            // calculate log string length
            int strLength = 0;
            int newLineLength = Environment.NewLine.Length;
            for (int i = 0; i < m_UncollapsedLogEntryIndices.Count; i++)
            {
                LogEntry entry = m_CollapsedLogEntries[m_UncollapsedLogEntryIndices[i]];
                strLength += entry.logString.Length + entry.stackTrace.Length + newLineLength * 3;
            }

            strLength += 100; // just in case
            StringBuilder sb = new StringBuilder(strLength);

            // append
            for (int i = 0; i < m_UncollapsedLogEntryIndices.Count; i++)
            {
                LogEntry entry = m_CollapsedLogEntries[m_UncollapsedLogEntryIndices[i]];
                sb.AppendLine(entry.logString).AppendLine(entry.stackTrace).AppendLine();
            }

            return sb.ToString();
        }



        #endregion

        #region Update

        private void ProcessLogQueue()
        {
            if (m_QueueLogs.Count > 0)
            {
                QueuedLogEntry logItem = m_QueueLogs.Dequeue();
                ReceivedLog(logItem.logString, logItem.stackTrace, logItem.logType);
            }
        }

        private void ProcessScreenDimensions()
        {
            if (m_ScreenDimensionsChanged)
            {
                if (m_IsLogWindowVisible)
                {
                    m_LogWindow.OnViewportDimensionsChanged();
                }

                m_ScreenDimensionsChanged = false;
            }
        }

        private void ProcessKey()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            // toggle log window
            if (Input.GetKeyDown(m_ToggleKey))
            {
                ShowLogWindow(m_IsLogWindowVisible = !m_IsLogWindowVisible);
            }
            else if (m_IsLogWindowVisible && !m_CommandInput.isFocused && Input.GetKeyDown(KeyCode.Tab))
            {
                EventSystem.current.SetSelectedGameObject(null);
                m_CommandInput.Select();
            }

            // command history
            if (m_IsLogWindowVisible && m_CommandInput.isFocused)
            {
                if (m_CommandHistory.Length == 0) return;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (--m_CommandIdx < 0)
                    {
                        m_CommandIdx = 0;
                    }

                    m_CommandInput.text = m_CommandHistory[m_CommandIdx];
                    m_CommandInput.caretPosition = m_CommandInput.text.Length;
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (++m_CommandIdx >= m_CommandHistory.Length)
                    {
                        m_CommandInput.text = "";
                        m_CommandIdx = m_CommandHistory.Length;
                    }
                    else
                    {
                        m_CommandInput.text = m_CommandHistory[m_CommandIdx];
                        m_CommandInput.caretPosition = m_CommandInput.text.Length;
                    }
                }
            }
#endif
        }

        #endregion

        #region Callback



        //--------------------------------------------
        // mini window
        //--------------------------------------------
        private void MiniWindowClick()
        {
            ShowLogWindow(true);
        }


        //--------------------------------------------
        // ui component
        //--------------------------------------------
        private char OnValidateCommand(string text, int charIndex, char addedChar)
        {
            // autocomplete
            // tab
            if (addedChar == '\t')
            {
                if (!string.IsNullOrEmpty(text))
                {
                    string command = ConsoleCommand.AutoComplete();
                    if (!string.IsNullOrEmpty(command))
                    {
                        m_CommandInput.onValidateInput -= OnValidateCommand;
                        m_CommandInput.onValueChanged.RemoveListener(OnChangedCommand);

                        m_CommandInput.text = command;
                        m_CommandInput.caretPosition = m_CommandInput.text.Length;

                        m_CommandInput.onValidateInput += OnValidateCommand;
                        m_CommandInput.onValueChanged.AddListener(OnChangedCommand);
                    }
                }
                return '\0';
            }
            // submit
            // enter
            else if (addedChar == '\n')
            {
                m_CommandInput.onValidateInput -= OnValidateCommand;
                m_CommandInput.text = "";
                m_CommandInput.onValidateInput += OnValidateCommand;

                if (text.Length > 0)
                {
                    m_CommandHistory.AddTail(text);
                    m_CommandIdx = m_CommandHistory.Length;

                    // Execute command
                    ConsoleCommand.ExecuteCommand(text);
                }

                return '\0';
            }
            /// inputfield can't recieve backspace event
            /// use OnValueChanged instead

            //else if(addedChar == '\b')
            //{
            //    UnityEngine.Debug.Log(text + " " + charIndex + " " + addedChar);
            //    ConsoleCommand.GetPossibleCommand(text);
            //    return addedChar;
            //}

            return addedChar;
        }

        private void OnChangedCommand(string newString)
        {
            if (string.IsNullOrEmpty(newString)) return;
            ConsoleCommand.GetPossibleCommand(newString);
        }

        private void OnCloseClick()
        {
            ShowLogWindow(false);
        }

        private void OnFilterErrorBtnClick()
        {
            m_IsErrorEnabled = !m_IsErrorEnabled;
            m_FilterErrorBtn.image.color = m_IsErrorEnabled ? m_BtnSelectedColor : m_BtnNormalColor;

            LogFilter();
        }

        private void OnFilterWarningBtnClick()
        {
            m_IsWarningEnabled = !m_IsWarningEnabled;
            m_FilterWarningBtn.image.color = m_IsWarningEnabled ? m_BtnSelectedColor : m_BtnNormalColor;

            LogFilter();
        }

        private void OnFilterInfoBtnClick()
        {
            m_IsInfoEnabled = !m_IsInfoEnabled;
            m_FilterInfoBtn.image.color = m_IsInfoEnabled ? m_BtnSelectedColor : m_BtnNormalColor;

            LogFilter();
        }

        private void OnCollapseBtbClick()
        {
            m_IsCollapsed = !m_IsCollapsed;

            m_CollapseBtn.image.color = m_IsCollapsed ? m_BtnSelectedColor : m_BtnNormalColor;
            m_LogWindow.SetCollapseMode(m_IsCollapsed);

            LogFilter();
        }

        private void OnClearBtnClick()
        {
            m_InfoCount = 0;
            m_WarningCount = 0;
            m_ErrorCount = 0;

            m_LogWindow.UpdateInfoCountText(m_InfoCount);
            m_LogWindow.UpdateWarningCountText(m_WarningCount);
            m_LogWindow.UpdateErrorCountText(m_ErrorCount);

            m_CollapsedLogEntries.Clear();
            m_CollapsedLogEntriesLookup.Clear();
            m_UncollapsedLogEntryIndices.Clear();
            m_LogEntryIndicesToShow.Clear();

            m_LogWindow.OnDeselectLogItem();
            m_LogWindow.UpdateLogEntries(true);
        }

        // resize window
        private void OnDrag(BaseEventData eventData)
        {
            m_LogWindowHeight = (Screen.height - ((PointerEventData)eventData).position.y) / m_SelfRectTransform.localScale.y;
            m_LogWindowWidth = (Screen.width - ((PointerEventData)eventData).position.x) / m_SelfRectTransform.localScale.x;

            if (m_LogWindowHeight < k_MinHeightLogWindow)
            {
                m_LogWindowHeight = k_MinHeightLogWindow;
            }
            else if (m_LogWindowHeight >= m_MaxHeightLogWindow)
            {
                m_LogWindowHeight = m_MaxHeightLogWindow;
            }

            if (m_LogWindowWidth < k_MinWidthLogWindow)
            {
                m_LogWindowWidth = k_MinWidthLogWindow;
            }
            else if (m_LogWindowWidth >= m_MaxWidthLogWindow)
            {
                m_LogWindowWidth = m_MaxWidthLogWindow;
            }

            Resize(m_LogWindowHeight, m_LogWindowWidth);
        }

        #endregion
    }
}
