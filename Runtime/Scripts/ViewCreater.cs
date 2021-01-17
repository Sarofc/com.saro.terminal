//using UnityEngine;

//namespace Saro.Terminal
//{
//    class ViewCreater : MonoBehaviour
//    {
//        [SerializeField]
//        private GameObject m_ConsoleView;

//#if DEV_CONSOLE
//        private void Awake()
//        {
//            GameObject.Instantiate(m_ConsoleView);
//        }
//#endif

//#if UNITY_EDITOR
//        [UnityEditor.InitializeOnLoadMethod]
//        static void ChangeTag()
//        {
//#if !DEV_CONSOLE
//            FindObjectOfType<ViewCreater>().gameObject.tag = "EditorOnly";
//#else
//            FindObjectOfType<ViewCreater>().gameObject.tag = "Untagged";
//#endif
//        }
//#endif
//    }
//}