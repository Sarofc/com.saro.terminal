using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Saro.Terminal.View.EditorStyle
{
    public class DevConsole : MonoBehaviour
    {
        internal static DevConsole Get() => s_Instance;

        private static DevConsole s_Instance = null;

        #region Options

        private const string k_Key_LogWindowHeight = "GameConsoleHeight";
        private const string k_Key_LogWindowWidth = "GameConsoleWidth";


        private float m_MaxHeightLogWindow;
        private float m_MaxWidthLogWindow;

        private float m_LogWindowHeight;
        private float m_LogWindowWidth;

        [Header("Properties")]

        [SerializeField] private KeyCode m_ToggleKey = KeyCode.BackQuote;

        [SerializeField] private float m_MinHeightLogWindow = 120f;
        [SerializeField] private float m_MinWidthLogWindow = 120f;

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

        /// <summary>
        /// //存LogSprite
        /// </summary>
        public Dictionary<LogType, Sprite> logSpriteLookup;

        #endregion

        private bool m_ScreenDimensionsChanged;
        private bool m_IsLogWindowVisible;

        private int m_ErrorCount;
        private int m_WarningCount;
        private int m_InfoCount;

        #region Unity Method

        private void Awake()
        {
            Terminal.Console.LogMessageReceived += UpdateWindow;

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

            logSpriteLookup = new Dictionary<LogType, Sprite>(5)
            {
                {LogType.Log, m_InfoSprite },
                {LogType.Warning, m_WarningSprite },
                {LogType.Error, m_ErrorSprite },
                {LogType.Assert, m_ErrorSprite },
                {LogType.Exception, m_ErrorSprite }
            };

            // read PlayerPrefs
            m_LogWindowHeight = PlayerPrefs.GetFloat(k_Key_LogWindowHeight, m_MinHeightLogWindow);
            m_LogWindowWidth = PlayerPrefs.GetFloat(k_Key_LogWindowWidth, m_MinWidthLogWindow);

            // button state
            m_ResizeBtn.GetComponent<Image>().color = m_BtnNormalColor;
            m_CloseBtn.image.color = m_BtnNormalColor;
            m_ClearBtn.image.color = m_BtnNormalColor;

            m_CollapseBtn.image.color = Terminal.Console.IsCollapsedEnable ? m_BtnSelectedColor : m_BtnNormalColor;

            m_FilterInfoBtn.image.color = Terminal.Console.IsInfoEnable ? m_BtnSelectedColor : m_BtnNormalColor;
            m_FilterWarningBtn.image.color = Terminal.Console.IsWarningEnable ? m_BtnSelectedColor : m_BtnNormalColor;
            m_FilterErrorBtn.image.color = Terminal.Console.IsErrorEnable ? m_BtnSelectedColor : m_BtnNormalColor;
        }

        IEnumerator Start()
        {
            // init log window
            m_LogWindow.Init(Terminal.Console.CollapsedLogEntries, Terminal.Console.LogEntryIndicesToShow);
            m_LogWindow.SetCollapseMode(Terminal.Console.IsCollapsedEnable);
            m_LogWindow.UpdateItemsInTheList(true);

            // new size
            m_SelfRectTransform = transform as RectTransform;
            m_MaxHeightLogWindow = m_SelfRectTransform.sizeDelta.y;
            m_MaxWidthLogWindow = m_SelfRectTransform.sizeDelta.x;

            if (m_LogWindowHeight < m_MinHeightLogWindow) m_LogWindowHeight = m_MinHeightLogWindow;
            if (m_LogWindowWidth < m_MinWidthLogWindow) m_LogWindowWidth = m_MinWidthLogWindow;
            Resize(m_LogWindowHeight, m_LogWindowWidth);

            ShowLogWindow(false);

            yield return null;

            FilterLog();
        }

        private void OnEnable()
        {
            //Application.logMessageReceived += ReceivedLog;

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

            // TODO log andriod
        }

        private void OnDisable()
        {
            if (s_Instance != this) return;

            //Application.logMessageReceived -= ReceivedLog;

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

        private void OnDestroy()
        {
            Terminal.Console.LogMessageReceived -= UpdateWindow;
        }

        private void OnRectTransformDimensionsChange()
        {
            m_ScreenDimensionsChanged = true;
        }

        private void LateUpdate()
        {
            //ProcessLogQueue();
            Terminal.Console.ProcessLogQueue();

            ProcessScreenDimensions();

            ProcessKey();
        }

        private void OnApplicationQuit()
        {
            Terminal.Log("----------------quit");

#if UNITY_EDITOR || UNITY_STANDALONE
            SaveSettings();
#endif
        }

        private void OnApplicationPause(bool pause)
        {
            Terminal.Log("----------------pause");

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            SaveSettings();
#endif
        }

        private void SaveSettings()
        {
            Terminal.Log("----------------save settings");

            PlayerPrefs.SetFloat(k_Key_LogWindowHeight, m_LogWindowHeight);
            PlayerPrefs.SetFloat(k_Key_LogWindowWidth, m_LogWindowWidth);

            Terminal.SaveSettings();
        }

        #endregion

        #region Window
        public void ShowLogWindow(bool show)
        {
            if (show)
            {
                m_LogWindow.Show();
                if (m_MiniWindow) m_MiniWindow.Hide();

                // update text
                m_LogWindow.UpdateInfoCountText(m_InfoCount);
                m_LogWindow.UpdateWarningCountText(m_WarningCount);
                m_LogWindow.UpdateErrorCountText(m_ErrorCount);
            }
            else
            {
                m_LogWindow.Hide();
                if (m_MiniWindow) m_MiniWindow.Show();

                // reset inputfield
                m_CommandInput.text = "";
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

            m_LogWindow.OnViewportDimensionsChanged();
        }

        #endregion

        #region Log

        private void UpdateWindow(bool has, int index, int infoCount, int warningCount, int errorCount)
        {
            if (m_WarningCount != warningCount)
            {
                m_WarningCount = warningCount;
                if (m_IsLogWindowVisible)
                {
                    m_LogWindow.UpdateWarningCountText(m_WarningCount);
                }

            }

            if (m_InfoCount != infoCount)
            {
                m_InfoCount = infoCount;
                if (m_IsLogWindowVisible)
                    m_LogWindow.UpdateInfoCountText(m_InfoCount);

            }

            if (m_ErrorCount != errorCount)
            {
                m_ErrorCount = errorCount;
                if (m_IsLogWindowVisible)
                    m_LogWindow.UpdateErrorCountText(m_ErrorCount);

            }

            // update force
            if (index == -1)
            {
                m_LogWindow.UpdateLogEntries(true);
                return;
            }

            var entryIdx = index;
            if (m_IsLogWindowVisible)
            {
                if (Terminal.Console.IsCollapsedEnable && has)
                {
                    if (!Terminal.Console.IsDebugAll)
                    {
                        entryIdx = Terminal.Console.GetEntryIndexAtIndicesToShow(index);
                    }

                    m_LogWindow.UpdateCollapsedLogEntryAtIdx(entryIdx);
                }
                else if (Terminal.Console.IsInfoEnable || Terminal.Console.IsWarningEnable || Terminal.Console.IsErrorEnable)
                {
                    m_LogWindow.UpdateLogEntries(false);
                }
            }

            m_LogWindow.SnapToBottom();
        }

        private void FilterLog()
        {
            Terminal.Console.FilterLog();

            m_LogWindow.OnDeselectLogItem();
            m_LogWindow.UpdateLogEntries(true);

            m_LogWindow.SnapToBottom();
        }

        #endregion

        #region Update

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
            PcInput();
#elif UNITY_ANDROID || UNITY_IOS
            MobileInput();
#endif
        }

        private void MobileInput()
        {
            // toggle log window
            if (Input.touchCount == 4 && !m_IsLogWindowVisible)
            {
                ShowLogWindow(m_IsLogWindowVisible = !m_IsLogWindowVisible);
            }

            // command history
            if (m_IsLogWindowVisible && m_CommandInput.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    m_CommandInput.text = Terminal.Shell.GetPrevCommand();

                    m_CommandInput.caretPosition = m_CommandInput.text.Length;
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    m_CommandInput.text = Terminal.Shell.GetNextCommand();
                    m_CommandInput.caretPosition = m_CommandInput.text.Length;
                }
            }
        }

        private void PcInput()
        {
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
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    m_CommandInput.text = Terminal.Shell.GetPrevCommand();

                    m_CommandInput.caretPosition = m_CommandInput.text.Length;
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    m_CommandInput.text = Terminal.Shell.GetNextCommand();
                    m_CommandInput.caretPosition = m_CommandInput.text.Length;
                }
            }
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
                    string command = Terminal.Shell.AutoComplete();
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
                    //m_CommandHistory.AddTail(text);
                    //m_CommandIdx = m_CommandHistory.Length;

                    //Terminal.Shell.AddCommandHistory(text);

                    // Execute command
                    Terminal.Shell.ExecuteCommand(text);
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
            Terminal.Shell.GetPossibleCommand(newString);
        }

        private void OnCloseClick()
        {
            ShowLogWindow(false);
        }

        private void OnFilterErrorBtnClick()
        {
            //m_IsErrorEnabled = !m_IsErrorEnabled;
            //m_FilterErrorBtn.image.color = m_IsErrorEnabled ? m_BtnSelectedColor : m_BtnNormalColor;
            Terminal.Console.IsErrorEnable = !Terminal.Console.IsErrorEnable;
            m_FilterErrorBtn.image.color = Terminal.Console.IsErrorEnable ? m_BtnSelectedColor : m_BtnNormalColor;

            FilterLog();
        }

        private void OnFilterWarningBtnClick()
        {
            Terminal.Console.IsWarningEnable = !Terminal.Console.IsWarningEnable;
            m_FilterWarningBtn.image.color = Terminal.Console.IsWarningEnable ? m_BtnSelectedColor : m_BtnNormalColor;

            FilterLog();
        }

        private void OnFilterInfoBtnClick()
        {
            Terminal.Console.IsInfoEnable = !Terminal.Console.IsInfoEnable;
            m_FilterInfoBtn.image.color = Terminal.Console.IsInfoEnable ? m_BtnSelectedColor : m_BtnNormalColor;

            FilterLog();
        }

        private void OnCollapseBtbClick()
        {
            Terminal.Console.IsCollapsedEnable = !Terminal.Console.IsCollapsedEnable;

            m_CollapseBtn.image.color = Terminal.Console.IsCollapsedEnable ? m_BtnSelectedColor : m_BtnNormalColor;
            m_LogWindow.SetCollapseMode(Terminal.Console.IsCollapsedEnable);

            FilterLog();
        }

        private void OnClearBtnClick()
        {
            m_InfoCount = 0;
            m_WarningCount = 0;
            m_ErrorCount = 0;

            m_LogWindow.UpdateInfoCountText(m_InfoCount);
            m_LogWindow.UpdateWarningCountText(m_WarningCount);
            m_LogWindow.UpdateErrorCountText(m_ErrorCount);

            Terminal.Console.ClearLog();

            m_LogWindow.OnDeselectLogItem();
            m_LogWindow.UpdateLogEntries(true);
        }

        // resize window
        private void OnDrag(BaseEventData eventData)
        {
            m_LogWindowHeight = (Screen.height - ((PointerEventData)eventData).position.y) / m_SelfRectTransform.localScale.y;
            m_LogWindowWidth = (Screen.width - ((PointerEventData)eventData).position.x) / m_SelfRectTransform.localScale.x;

            if (m_LogWindowHeight < m_MinHeightLogWindow)
            {
                m_LogWindowHeight = m_MinHeightLogWindow;
            }
            else if (m_LogWindowHeight >= m_MaxHeightLogWindow)
            {
                m_LogWindowHeight = m_MaxHeightLogWindow;
            }

            if (m_LogWindowWidth < m_MinWidthLogWindow)
            {
                m_LogWindowWidth = m_MinWidthLogWindow;
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
