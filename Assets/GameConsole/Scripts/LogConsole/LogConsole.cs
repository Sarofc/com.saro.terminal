using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saro.Console
{
    public class LogConsole : MonoBehaviour
    {
        public static LogConsole Instance => m_instance;

        private static LogConsole m_instance = null;

        #region Options

        private const string Key_LogWindowHeight = "GameConsoleHeight";
        private const string Key_LogWindowWidth = "GameConsoleWidth";

        private const string Key_IsInfoLogEnable = "InfoLogEnable";
        private const string Key_IsWarningLogEnable = "WarningLogEnable";
        private const string Key_IsErrorLogEnable = "ErrorLogEnable";
        private const string Key_IsCollapseEnable = "CollapseEnable";

        private const float MinHeightLogWindow = 120f;
        private const float MinWidthLogWindow = 250f;

        private float m_logWindowHeight;
        private float m_logWindowWidth;

        private bool m_isCollapsed;
        private bool m_isInfoEnabled;
        private bool m_isWarningEnabled;
        private bool m_isErrorEnabled;

        [Header("Properties")]
        [SerializeField] private float MaxHeightLogWindow;
        [SerializeField] private float MaxWidthLogWindow;

        [SerializeField] private KeyCode m_toggleKey = KeyCode.BackQuote;
        [SerializeField] private int m_commandHistorySize = 15;

        [Header("Views")]
        [SerializeField] private Sprite m_infoSprite;
        [SerializeField] private Sprite m_warningSprite;
        [SerializeField] private Sprite m_errorSprite;

        [SerializeField] private Color m_btnNormalColor;
        [SerializeField] private Color m_btnSelectedColor;

        [Header("Components")]
        [SerializeField] private LogWindow m_logWindow;
        [SerializeField] private MiniWindow m_miniWindow;
        [Space()]
        [SerializeField] private Button m_closeBtn;
        [SerializeField] private EventTrigger m_resizeBtn;
        [SerializeField] private Button m_clearBtn;
        [SerializeField] private Button m_collapseBtn;
        [Space()]
        [SerializeField] private Button m_filterInfoBtn;
        [SerializeField] private Button m_filterWarningBtn;
        [SerializeField] private Button m_filterErrorBtn;
        [Space()]
        [SerializeField] private InputField m_commandInput;


        private RectTransform m_selfRectTransform;
        #endregion

        #region Collections

        private Dictionary<LogType, Sprite> m_logSpriteLookup;              //存LogSprite

        private List<LogEntry> m_collapsedLogEntries;                       //存LogEntry，不允许重复，每个LogEntry唯一
        private Dictionary<LogEntry, int> m_collapsedLogEntriesLookup;      //根据唯一LogEntry，获取其所在的index

        private LogIndicesList m_uncollapsedLogEntryIndices;                //存所有LogEntry的index，相同的LogEntry的索引相等。
        private LogIndicesList m_logEntryIndicesToShow;                     //需要显示的LogEntry的Index

        private Queue<QueuedLogEntry> m_queueLogs;

        private LoopArray<string> m_commandHistory;                         // 历史命令
        private int m_commandIdx = -1;

        #endregion

        private bool m_screenDimensionsChanged;
        private bool m_isLogWindowVisible;

        private int m_errorCount;
        private int m_warningCount;
        private int m_infoCount;

        #region Unity Method

        private void Awake()
        {
            #region Instance
            if (m_instance == null)
            {
                m_instance = this;

                DontDestroyOnLoad(gameObject);
            }
            else if (this != m_instance)
            {
                Destroy(gameObject);
                return;
            }
            #endregion

            // init collections
            m_collapsedLogEntries = new List<LogEntry>(128);
            m_collapsedLogEntriesLookup = new Dictionary<LogEntry, int>(128);
            m_uncollapsedLogEntryIndices = new LogIndicesList(64);
            m_logEntryIndicesToShow = new LogIndicesList(64);

            m_queueLogs = new Queue<QueuedLogEntry>(64);
            m_commandHistory = new LoopArray<string>(m_commandHistorySize);

            m_logSpriteLookup = new Dictionary<LogType, Sprite>(5)
            {
                {LogType.Log, m_infoSprite },
                {LogType.Warning, m_warningSprite },
                {LogType.Error, m_errorSprite },
                {LogType.Assert, m_errorSprite },
                {LogType.Exception, m_errorSprite }
            };

            // read PlayerPrefs
            m_logWindowHeight = PlayerPrefs.GetFloat(Key_LogWindowHeight, MinHeightLogWindow);
            m_logWindowWidth = PlayerPrefs.GetFloat(Key_LogWindowWidth, MinWidthLogWindow);

            m_isCollapsed = PlayerPrefs.GetInt(Key_IsCollapseEnable, 1) == 1 ? true : false;
            m_isInfoEnabled = PlayerPrefs.GetInt(Key_IsInfoLogEnable, 1) == 1 ? true : false;
            m_isWarningEnabled = PlayerPrefs.GetInt(Key_IsWarningLogEnable, 1) == 1 ? true : false;
            m_isErrorEnabled = PlayerPrefs.GetInt(Key_IsErrorLogEnable, 1) == 1 ? true : false;

            // button state
            m_collapseBtn.image.color = m_isCollapsed ? m_btnSelectedColor : m_btnNormalColor;

            m_filterInfoBtn.image.color = m_isInfoEnabled ? m_btnSelectedColor : m_btnNormalColor;
            m_filterWarningBtn.image.color = m_isWarningEnabled ? m_btnSelectedColor : m_btnNormalColor;
            m_filterErrorBtn.image.color = m_isErrorEnabled ? m_btnSelectedColor : m_btnNormalColor;
        }

        private void Start()
        {
            // init log window
            m_selfRectTransform = transform as RectTransform;
            m_logWindow.Init(m_collapsedLogEntries, m_logEntryIndicesToShow);
            m_logWindow.SetCollapseMode(m_isCollapsed);
            m_logWindow.UpdateItemsInTheList(true);


            // new size
            MaxHeightLogWindow = m_selfRectTransform.sizeDelta.y;
            MaxWidthLogWindow = m_selfRectTransform.sizeDelta.x;

            if (m_logWindowHeight < MinHeightLogWindow) m_logWindowHeight = MinHeightLogWindow;
            if (m_logWindowWidth < MinWidthLogWindow) m_logWindowWidth = MinWidthLogWindow;

            Resize(m_logWindowHeight, m_logWindowWidth);

            // Test
            ShowLogWindow(false);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += ReceivedLog;

            if (m_miniWindow)
            {
                m_miniWindow.OnClick += MiniWindowClick;
            }

            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Drag;
            entry.callback.AddListener(OnDrag);
            m_resizeBtn.triggers.Add(entry);

            m_closeBtn.onClick.AddListener(OnCloseClick);
            m_clearBtn.onClick.AddListener(OnClearBtnClick);
            m_collapseBtn.onClick.AddListener(OnCollapseBtbClick);

            m_filterInfoBtn.onClick.AddListener(OnFilterInfoBtnClick);
            m_filterWarningBtn.onClick.AddListener(OnFilterWarningBtnClick);
            m_filterErrorBtn.onClick.AddListener(OnFilterErrorBtnClick);

            m_commandInput.onValidateInput += OnValidateCommand;
            m_commandInput.onValueChanged.AddListener(OnChangedCommand);

            // saving PlayerPrefs
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += (s) =>
            {
                if (s == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    print("exit playmode");

                    PlayerPrefs.SetFloat(Key_LogWindowHeight, m_logWindowHeight);
                    PlayerPrefs.SetFloat(Key_LogWindowWidth, m_logWindowWidth);

                    PlayerPrefs.SetInt(Key_IsCollapseEnable, m_isCollapsed ? 1 : 0);
                    PlayerPrefs.SetInt(Key_IsInfoLogEnable, m_isInfoEnabled ? 1 : 0);
                    PlayerPrefs.SetInt(Key_IsWarningLogEnable, m_isWarningEnabled ? 1 : 0);
                    PlayerPrefs.SetInt(Key_IsErrorLogEnable, m_isErrorEnabled ? 1 : 0);
                }
            };
#else
             Application.quitting += () =>
            {
                print("exit game");

                PlayerPrefs.SetFloat(Key_LogWindowHeight, m_logWindowHeight);
                PlayerPrefs.SetFloat(Key_LogWindowWidth, m_logWindowWidth);

                PlayerPrefs.SetInt(Key_IsCollapseEnable, m_isCollapsed ? 1 : 0);
                PlayerPrefs.SetInt(Key_IsInfoLogEnable, m_isInfoEnabled ? 1 : 0);
                PlayerPrefs.SetInt(Key_IsWarningLogEnable, m_isWarningEnabled ? 1 : 0);
                PlayerPrefs.SetInt(Key_IsErrorLogEnable, m_isErrorEnabled ? 1 : 0);
            };
#endif
            // TODO log andriod
        }

        private void OnDisable()
        {
            if (m_instance != this) return;

            Application.logMessageReceived -= ReceivedLog;

            if (m_miniWindow)
            {
                m_miniWindow.OnClick -= MiniWindowClick;
            }

            m_resizeBtn.triggers.Clear();

            m_closeBtn.onClick.RemoveListener(OnCloseClick);
            m_clearBtn.onClick.RemoveListener(OnClearBtnClick);
            m_collapseBtn.onClick.RemoveListener(OnCollapseBtbClick);

            m_filterInfoBtn.onClick.RemoveListener(OnFilterInfoBtnClick);
            m_filterWarningBtn.onClick.RemoveListener(OnFilterWarningBtnClick);
            m_filterErrorBtn.onClick.RemoveListener(OnFilterErrorBtnClick);

            m_commandInput.onValidateInput -= OnValidateCommand;
            m_commandInput.onValueChanged.RemoveListener(OnChangedCommand);
        }

        private void OnRectTransformDimensionsChange()
        {
            m_screenDimensionsChanged = true;
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
                m_logWindow.Show();
                m_miniWindow.Hide();

                // update text
                m_logWindow.UpdateInfoCountText(m_infoCount);
                m_logWindow.UpdateWarningCountText(m_warningCount);
                m_logWindow.UpdateErrorCountText(m_errorCount);
            }
            else
            {
                m_logWindow.Hide();
                m_miniWindow.Show();

                // reset inputfield
                m_commandInput.text = "";
                m_commandIdx = m_commandHistory.Length;
                EventSystem.current.SetSelectedGameObject(null);
            }

            m_isLogWindowVisible = show;
        }

        private void Resize(float newHeight, float newWidth)
        {
            var anchorMin = (m_logWindow.transform as RectTransform).anchorMin;
            anchorMin.y = Mathf.Max(0f, 1f - newHeight / m_selfRectTransform.sizeDelta.y);
            anchorMin.x = Mathf.Max(0f, 1f - newWidth / m_selfRectTransform.sizeDelta.x);
            (m_logWindow.transform as RectTransform).anchorMin = anchorMin;

            //var pos = (m_logWindow.transform as RectTransform).sizeDelta;
            //pos.y = Mathf.Max(0f, newHeight);
            //pos.x = Mathf.Max(0f, newWidth);
            //(m_logWindow.transform as RectTransform).sizeDelta = pos;

            m_logWindow.OnViewportDimensionsChanged();

            //m_logWindow.SnapToBottom();
        }

        #endregion

        #region Log

        private void ReceivedLog(string logString, string stackTrace, LogType type)
        {
            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                m_queueLogs.Enqueue(new QueuedLogEntry(logString, stackTrace, type));
                return;
            }

            var logEntry = new LogEntry(logString, stackTrace, null);

            bool isEntryInCollapsedEntryList = m_collapsedLogEntriesLookup.TryGetValue(logEntry, out int logEntryIdx);
            // already has entry in list
            if (isEntryInCollapsedEntryList)
            {
                logEntry = m_collapsedLogEntries[logEntryIdx];
                logEntry.count++;
            }
            // new one, store to map
            else
            {
                logEntry.typeSprite = m_logSpriteLookup[type];
                logEntryIdx = m_collapsedLogEntries.Count;
                m_collapsedLogEntries.Add(logEntry);
                m_collapsedLogEntriesLookup[logEntry] = logEntryIdx;
            }

            m_uncollapsedLogEntryIndices.Add(logEntryIdx);

            if (m_isCollapsed && isEntryInCollapsedEntryList)
            {
                if (m_isLogWindowVisible)
                {
                    m_logWindow.UpdateCollapsedLogEntryAtIdx(logEntryIdx);
                }
            }
            else if (m_isInfoEnabled || m_isWarningEnabled || m_isErrorEnabled)
            {
                m_logEntryIndicesToShow.Add(logEntryIdx);
                if (m_isLogWindowVisible)
                {
                    m_logWindow.UpdateLogEntries(false);
                }
            }

            if (type == LogType.Warning)
            {
                ++m_warningCount;
                if (m_isLogWindowVisible)
                    m_logWindow.UpdateWarningCountText(m_warningCount);

                //else
                //{
                //    //TODO update mini window count
                //}
            }
            else if (type == LogType.Log)
            {
                ++m_infoCount;
                if (m_isLogWindowVisible)
                    m_logWindow.UpdateInfoCountText(m_infoCount);

                //else
                //{
                //    //TODO update mini window
                //}
            }
            else
            {
                ++m_errorCount;
                if (m_isLogWindowVisible)
                    m_logWindow.UpdateErrorCountText(m_errorCount);

                //else
                //{
                //    //TODO update mini window
                //}
            }

            m_logWindow.SnapToBottom();
        }

        private void LogFilter()
        {
            m_logEntryIndicesToShow.Clear();

            if (m_isCollapsed)
            {
                for (int i = 0; i < m_collapsedLogEntries.Count; i++)
                {
                    var entry = m_collapsedLogEntries[i];

                    if ((m_isInfoEnabled && entry.typeSprite == m_infoSprite)
                        || (m_isWarningEnabled && entry.typeSprite == m_warningSprite)
                        || (m_isErrorEnabled && entry.typeSprite == m_errorSprite))
                    {
                        m_logEntryIndicesToShow.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_uncollapsedLogEntryIndices.Count; i++)
                {
                    var entry = m_collapsedLogEntries[m_uncollapsedLogEntryIndices[i]];

                    if ((m_isInfoEnabled && entry.typeSprite == m_infoSprite)
                        || (m_isWarningEnabled && entry.typeSprite == m_warningSprite)
                        || (m_isErrorEnabled && entry.typeSprite == m_errorSprite))
                    {
                        m_logEntryIndicesToShow.Add(m_uncollapsedLogEntryIndices[i]);
                    }
                }
            }

            m_logWindow.OnDeselectLogItem();
            m_logWindow.UpdateLogEntries(true);

            m_logWindow.SnapToBottom();
        }

        private string GetLog()
        {
            // calculate log string length
            int strLength = 0;
            int newLineLength = Environment.NewLine.Length;
            for (int i = 0; i < m_uncollapsedLogEntryIndices.Count; i++)
            {
                var entry = m_collapsedLogEntries[m_uncollapsedLogEntryIndices[i]];
                strLength += entry.logString.Length + entry.stackTrace.Length + newLineLength * 3;
            }

            strLength += 100; // just in case
            var sb = new StringBuilder(strLength);

            // append
            for (int i = 0; i < m_uncollapsedLogEntryIndices.Count; i++)
            {
                var entry = m_collapsedLogEntries[m_uncollapsedLogEntryIndices[i]];
                sb.AppendLine(entry.logString).AppendLine(entry.stackTrace).AppendLine();
            }

            return sb.ToString();
        }

        [Command("save_log", "Save the log file")]
        public static void SaveLogFile()
        {
            var path = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt");
            File.WriteAllText(path, m_instance.GetLog());

            Debug.Log("Save log file to : " + path);
        }

        #endregion

        #region Update

        private void ProcessLogQueue()
        {
            if (m_queueLogs.Count > 0)
            {
                var logItem = m_queueLogs.Dequeue();
                ReceivedLog(logItem.logString, logItem.stackTrace, logItem.logType);
            }
        }

        private void ProcessScreenDimensions()
        {
            if (m_screenDimensionsChanged)
            {
                if (m_isLogWindowVisible)
                {
                    m_logWindow.OnViewportDimensionsChanged();
                }

                m_screenDimensionsChanged = false;
            }
        }

        private void ProcessKey()
        {
#if UNITY_STANDALONE
            // toggle log window
            if (Input.GetKeyDown(m_toggleKey))
            {
                ShowLogWindow(m_isLogWindowVisible = !m_isLogWindowVisible);
            }
            else if (m_isLogWindowVisible && !m_commandInput.isFocused && Input.GetKeyDown(KeyCode.Tab))
            {
                EventSystem.current.SetSelectedGameObject(null);
                m_commandInput.Select();
            }

            // command history
            if (m_isLogWindowVisible && m_commandInput.isFocused)
            {
                if (m_commandHistory.Length == 0) return;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (--m_commandIdx < 0)
                    {
                        m_commandIdx = 0;
                    }

                    m_commandInput.text = m_commandHistory[m_commandIdx];
                    m_commandInput.caretPosition = m_commandInput.text.Length;
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (++m_commandIdx >= m_commandHistory.Length)
                    {
                        m_commandInput.text = "";
                        m_commandIdx = m_commandHistory.Length;
                    }
                    else
                    {
                        m_commandInput.text = m_commandHistory[m_commandIdx];
                        m_commandInput.caretPosition = m_commandInput.text.Length;
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
                    var command = ConsoleCommand.AutoComplete();
                    if (!string.IsNullOrEmpty(command))
                    {
                        m_commandInput.onValidateInput -= OnValidateCommand;
                        m_commandInput.onValueChanged.RemoveListener(OnChangedCommand);

                        m_commandInput.text = command;
                        m_commandInput.caretPosition = m_commandInput.text.Length;

                        m_commandInput.onValidateInput += OnValidateCommand;
                        m_commandInput.onValueChanged.AddListener(OnChangedCommand);
                    }
                }
                return '\0';
            }
            // submit
            // enter
            else if (addedChar == '\n')
            {
                m_commandInput.onValidateInput -= OnValidateCommand;
                m_commandInput.text = "";
                m_commandInput.onValidateInput += OnValidateCommand;

                if (text.Length > 0)
                {
                    m_commandHistory.AddTail(text);
                    m_commandIdx = m_commandHistory.Length;

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
            m_isErrorEnabled = !m_isErrorEnabled;
            m_filterErrorBtn.image.color = m_isErrorEnabled ? m_btnSelectedColor : m_btnNormalColor;

            LogFilter();
        }

        private void OnFilterWarningBtnClick()
        {
            m_isWarningEnabled = !m_isWarningEnabled;
            m_filterWarningBtn.image.color = m_isWarningEnabled ? m_btnSelectedColor : m_btnNormalColor;

            LogFilter();
        }

        private void OnFilterInfoBtnClick()
        {
            m_isInfoEnabled = !m_isInfoEnabled;
            m_filterInfoBtn.image.color = m_isInfoEnabled ? m_btnSelectedColor : m_btnNormalColor;

            LogFilter();
        }

        private void OnCollapseBtbClick()
        {
            m_isCollapsed = !m_isCollapsed;

            m_collapseBtn.image.color = m_isCollapsed ? m_btnSelectedColor : m_btnNormalColor;
            m_logWindow.SetCollapseMode(m_isCollapsed);

            LogFilter();
        }

        private void OnClearBtnClick()
        {
            m_infoCount = 0;
            m_warningCount = 0;
            m_errorCount = 0;

            m_logWindow.UpdateInfoCountText(m_infoCount);
            m_logWindow.UpdateWarningCountText(m_warningCount);
            m_logWindow.UpdateErrorCountText(m_errorCount);

            m_collapsedLogEntries.Clear();
            m_collapsedLogEntriesLookup.Clear();
            m_uncollapsedLogEntryIndices.Clear();
            m_logEntryIndicesToShow.Clear();

            m_logWindow.OnDeselectLogItem();
            m_logWindow.UpdateLogEntries(true);
        }

        // resize window
        private void OnDrag(BaseEventData eventData)
        {
            m_logWindowHeight = (Screen.height - ((PointerEventData)eventData).position.y) / m_selfRectTransform.localScale.y;
            m_logWindowWidth = (Screen.width - ((PointerEventData)eventData).position.x) / m_selfRectTransform.localScale.x;

            if (m_logWindowHeight < MinHeightLogWindow)
            {
                m_logWindowHeight = MinHeightLogWindow;
            }
            else if (m_logWindowHeight >= MaxHeightLogWindow)
            {
                m_logWindowHeight = MaxHeightLogWindow;
            }

            if (m_logWindowWidth < MinWidthLogWindow)
            {
                m_logWindowWidth = MinWidthLogWindow;
            }
            else if (m_logWindowWidth >= MaxWidthLogWindow)
            {
                m_logWindowWidth = MaxWidthLogWindow;
            }

            Resize(m_logWindowHeight, m_logWindowWidth);
        }

        #endregion
    }
}
