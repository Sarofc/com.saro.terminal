#if true

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Saro.Terminal.View.EditorStyle
{
    public class MiniWindow : MonoBehaviour, /*IPointerEnterHandler, IPointerExitHandler,*/ IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public Action OnClick;

        [SerializeField] private AttachType m_AttachType = AttachType.H;
        [SerializeField, Range(0, 1)] private float m_CanvasGroupAlpha = .8f;
        //[SerializeField] private GameObject m_tips;

        private bool m_BeginDrag;
        private Vector2 m_HalfSize;

        private IEnumerator m_AttachToEdgeCoroutine = null;

        private CanvasGroup m_CanvasGroup;

        private enum AttachType
        {
            H,
            HV
        }

        private void Awake()
        {
            if (!m_CanvasGroup) m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            m_HalfSize = (transform as RectTransform).sizeDelta * .5f * transform.root.localScale.x;
        }

        public void Show()
        {
            m_CanvasGroup.alpha = m_CanvasGroupAlpha;

            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_BeginDrag = true;
            if (m_AttachToEdgeCoroutine != null)
            {
                StopCoroutine(m_AttachToEdgeCoroutine);
                m_AttachToEdgeCoroutine = null;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 point = eventData.position;
            point.x = Mathf.Clamp(point.x, m_HalfSize.x, Screen.width - m_HalfSize.x);
            point.y = Mathf.Clamp(point.y, m_HalfSize.y, Screen.height - m_HalfSize.y);
            transform.position = point;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_AttachToEdgeCoroutine != null)
            {
                StopCoroutine(m_AttachToEdgeCoroutine);
            }

            m_AttachToEdgeCoroutine = AttachToEdge(
                m_AttachType == AttachType.H ?
                GetPointH() :
                GetPointHV()
            );
            StartCoroutine(m_AttachToEdgeCoroutine);

            m_BeginDrag = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!m_BeginDrag)
            {
                OnClick?.Invoke();
            }
        }

        private Vector2 GetPointH()
        {
            Vector3 pos = transform.position;
            float dis2Left = pos.x;
            float dis2Right = Mathf.Abs(Screen.width - pos.x);

            // right
            if (dis2Right > dis2Left)
            {
                pos = new Vector3(m_HalfSize.x, pos.y, 0);
            }
            // left
            else
            {
                pos = new Vector3(Screen.width - m_HalfSize.x, pos.y, 0);
            }

            pos.y = Mathf.Clamp(pos.y, m_HalfSize.y, Screen.width - m_HalfSize.y);


            return pos;
        }

        private Vector2 GetPointHV()
        {
            Vector3 pos = transform.position;
            float dis2Left = pos.x;
            float dis2Right = Mathf.Abs(Screen.width - pos.x);
            float dis2Top = Mathf.Abs(Screen.height - pos.y);
            float dis2Bottom = pos.y;

            float minH = Mathf.Min(dis2Left, dis2Right);
            float minV = Mathf.Min(dis2Top, dis2Bottom);

            if (minH > minV)
            {
                // move to bottom
                if (dis2Top > dis2Bottom)
                {
                    pos = new Vector3(pos.x, m_HalfSize.y, 0);
                }
                // move to top
                else
                {
                    pos = new Vector3(pos.x, Screen.height - m_HalfSize.y, 0);
                }

                pos.x = Mathf.Clamp(pos.x, m_HalfSize.x, Screen.width - m_HalfSize.x);
            }
            else
            {
                // right
                if (dis2Right > dis2Left)
                {
                    pos = new Vector3(m_HalfSize.x, pos.y, 0);
                }
                // left
                else
                {
                    pos = new Vector3(Screen.width - m_HalfSize.x, pos.y, 0);
                }

                pos.y = Mathf.Clamp(pos.y, m_HalfSize.y, Screen.width - m_HalfSize.y);
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

#endif