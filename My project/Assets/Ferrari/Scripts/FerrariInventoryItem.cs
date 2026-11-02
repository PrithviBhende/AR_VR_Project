using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FerrariLab
{
    public sealed class FerrariInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IScrollHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public FerrariPart part;
        public FerrariLoopingInventory inventory;
        public Text label;
        public RawImage thumbnail;
        public Image cardImage;
        public bool IsHovered { get; private set; }
        bool ownsDrag;
        float currentPop;
        RectTransform rt;

        public void OnPointerEnter(PointerEventData e) { IsHovered = true; }
        public void OnPointerExit(PointerEventData e) { IsHovered = false; }
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&part)inventory.assembly.Preview(part);}
        public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)ownsDrag=inventory.assembly.BeginDrag(part,e.position);}
        public void OnDrag(PointerEventData e){if(ownsDrag)inventory.assembly.DragPointer(e.position);}
        public void OnEndDrag(PointerEventData e){if(ownsDrag)inventory.assembly.Release(e.position);ownsDrag=false;}
        public void OnScroll(PointerEventData e){inventory.OnScroll(e);}
        void OnDisable(){if(ownsDrag&&inventory&&inventory.assembly)inventory.assembly.CancelDrag();ownsDrag=false;IsHovered=false;}

        void Update()
        {
            if (rt == null) rt = (RectTransform)transform;
            if (cardImage == null) cardImage = GetComponent<Image>();
            float target = (IsHovered && part) ? 1f : 0f;
            currentPop = Mathf.MoveTowards(currentPop, target, Time.unscaledDeltaTime * 9.0f);
            float ease = Mathf.SmoothStep(0f, 1f, currentPop);
            float scale = Mathf.Lerp(1.0f, 1.04f, ease);
            rt.localScale = new Vector3(scale, scale, 1f);

            if (cardImage && part)
            {
                bool isSel = inventory && inventory.assembly && part == inventory.assembly.PreviewPart;
                if (isSel)
                {
                    if (FerrariInspectionUI.glassCardSelectedSprite)
                        cardImage.sprite = FerrariInspectionUI.glassCardSelectedSprite;
                }
                else if (IsHovered || currentPop > 0.02f)
                {
                    if (FerrariInspectionUI.glassCardHoverSprite)
                        cardImage.sprite = FerrariInspectionUI.glassCardHoverSprite;
                }
                else
                {
                    if (FerrariInspectionUI.glassCardDefaultSprite)
                        cardImage.sprite = FerrariInspectionUI.glassCardDefaultSprite;
                }
            }
        }
    }
}
