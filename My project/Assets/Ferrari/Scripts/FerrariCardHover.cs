using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FerrariLab
{
    public sealed class FerrariCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public FerrariPart part;
        public FerrariInspectionController controller;

        RectTransform rectTransform;
        Image cardImage;
        Text cardLabel;
        Vector2 baseCenterPos;
        float currentPop;

        public bool IsHovered { get; private set; }

        void Awake()
        {
            rectTransform = (RectTransform)transform;
            cardImage = GetComponent<Image>();
            cardLabel = GetComponentInChildren<Text>();
        }

        public void Setup(FerrariPart p, FerrariInspectionController ctrl, Vector2 topLeftPos, Vector2 size)
        {
            part = p;
            controller = ctrl;
            if (rectTransform == null) rectTransform = (RectTransform)transform;

            // Use center pivot for smooth, symmetric scaling without clipping
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            baseCenterPos = new Vector2(topLeftPos.x + size.x * 0.5f, topLeftPos.y - size.y * 0.5f);
            rectTransform.anchoredPosition = baseCenterPos;
            rectTransform.sizeDelta = size;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
        }

        void Update()
        {
            if (!cardImage) return;

            float target = IsHovered ? 1f : 0f;
            currentPop = Mathf.MoveTowards(currentPop, target, Time.unscaledDeltaTime * 9.0f);
            float ease = Mathf.SmoothStep(0f, 1f, currentPop);

            // Pop up scale: 1.0 -> 1.04
            float scale = Mathf.Lerp(1.0f, 1.04f, ease);
            rectTransform.localScale = new Vector3(scale, scale, 1f);

            // Pop slightly to the left: base.x -> base.x - 4px (stepping outward towards the viewer)
            rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(baseCenterPos.x, baseCenterPos.x - 4f, ease), baseCenterPos.y);

            // Highlight state
            bool isSel = controller && controller.Selected == part;
            if (isSel)
            {
                if (FerrariInspectionUI.glassCardSelectedSprite)
                    cardImage.sprite = FerrariInspectionUI.glassCardSelectedSprite;
                cardImage.color = Color.white;
                if (cardLabel) cardLabel.color = new Color(.25f, .92f, 1f);
            }
            else if (IsHovered || currentPop > 0.02f)
            {
                if (FerrariInspectionUI.glassCardHoverSprite)
                    cardImage.sprite = FerrariInspectionUI.glassCardHoverSprite;
                cardImage.color = Color.white;
                if (cardLabel) cardLabel.color = Color.Lerp(new Color(.90f, .94f, .98f), Color.white, ease);
            }
            else
            {
                if (FerrariInspectionUI.glassCardDefaultSprite)
                    cardImage.sprite = FerrariInspectionUI.glassCardDefaultSprite;
                cardImage.color = Color.white;
                if (cardLabel) cardLabel.color = new Color(.90f, .94f, .98f);
            }
        }
    }
}
