using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;

namespace Saro.Console
{
    public class MiniWindow : MonoBehaviour, /*IPointerEnterHandler, IPointerExitHandler,*/ IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public Action OnClick;

        [SerializeField] private AttachType m_attachType = AttachType.H;
        [SerializeField, Range(0, 1)] private float m_canvasGroupAlpha = .8f;
        //[SerializeField] private GameObject m_tips;

        private bool m_beginDrag;
        private Vector2 halfSize;

        private IEnumerator m_attachToEdgeCoroutine = null;

        private CanvasGroup m_canvasGroup;

        private enum AttachType
        {
            H,
            HV
        }

        private void Awake()
        {
            if(!m_canvasGroup) m_canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            halfSize = (transform as RectTransform).sizeDelta * .5f * transform.root.localScale.x;
        }

        public void Show()
        {
            m_canvasGroup.alpha = m_canvasGroupAlpha;

            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_beginDrag = true;
            if (m_attachToEdgeCoroutine != null)
            {
                StopCoroutine(m_attachToEdgeCoroutine);
                m_attachToEdgeCoroutine = null;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            var point = eventData.position;
            point.x = Mathf.Clamp(point.x, halfSize.x, Screen.width - halfSize.x);
            point.y = Mathf.Clamp(point.y, halfSize.y, Screen.height - halfSize.y);
            transform.position = point;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_attachToEdgeCoroutine != null)
            {
                StopCoroutine(m_attachToEdgeCoroutine);
            }

            m_attachToEdgeCoroutine = AttachToEdge(
                m_attachType == AttachType.H ?
                GetPointH() :
                GetPointHV()
            );
            StartCoroutine(m_attachToEdgeCoroutine);

            m_beginDrag = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!m_beginDrag)
            {
                OnClick?.Invoke();
            }
        }

        private Vector2 GetPointH()
        {
            var pos = transform.position;
            var dis2Left = pos.x;
            var dis2Right = Mathf.Abs(Screen.width - pos.x);

            // right
            if (dis2Right > dis2Left)
            {
                pos = new Vector3(halfSize.x, pos.y, 0);
            }
            // left
            else
            {
                pos = new Vector3(Screen.width - halfSize.x, pos.y, 0);
            }

            pos.y = Mathf.Clamp(pos.y, halfSize.y, Screen.width - halfSize.y);


            return pos;
        }

        private Vector2 GetPointHV()
        {
            var pos = transform.position;
            var dis2Left = pos.x;
            var dis2Right = Mathf.Abs(Screen.width - pos.x);
            var dis2Top = Mathf.Abs(Screen.height - pos.y);
            var dis2Bottom = pos.y;

            var minH = Mathf.Min(dis2Left, dis2Right);
            var minV = Mathf.Min(dis2Top, dis2Bottom);

            if (minH > minV)
            {
                // move to bottom
                if (dis2Top > dis2Bottom)
                {
                    pos = new Vector3(pos.x, halfSize.y, 0);
                }
                // move to top
                else
                {
                    pos = new Vector3(pos.x, Screen.height - halfSize.y, 0);
                }

                pos.x = Mathf.Clamp(pos.x, halfSize.x, Screen.width - halfSize.x);
            }
            else
            {
                // right
                if (dis2Right > dis2Left)
                {
                    pos = new Vector3(halfSize.x, pos.y, 0);
                }
                // left
                else
                {
                    pos = new Vector3(Screen.width - halfSize.x, pos.y, 0);
                }

                pos.y = Mathf.Clamp(pos.y, halfSize.y, Screen.width - halfSize.y);
            }

            return pos;
        }

        private IEnumerator AttachToEdge(Vector3 target)
        {
            float modifier = 0f;
            Vector3 pos = transform.position;
            while (modifier < 1f)
            {
                modifier += 4f * Time.unscaledDeltaTime;
                transform.position = Vector3.Lerp(pos, target, modifier);
                yield return null;
            }
        }

        //public void OnPointerEnter(PointerEventData eventData)
        //{
        //    m_tips.SetActive(true);
        //}

        //public void OnPointerExit(PointerEventData eventData)
        //{
        //    m_tips.SetActive(false);
        //}
    }
}
