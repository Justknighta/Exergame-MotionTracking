using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InfiniteScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public ScrollRect scrollRect;
    public RectTransform viewPortTransform;
    public RectTransform contentPanelTransform;
    public HorizontalLayoutGroup HLG;

    public RectTransform[] ItemList;

    [Header("Snapping Settings")]
    public float snapSpeed = 10f; 
    public float snapVelocityThreshold = 50f; 
    
    private bool isDragging = false;
    private float itemStep; 

    // --- ส่วนที่เพิ่มเข้ามาจัดการเป้าหมายการ Snap ---
    private bool isSnapping = false;
    private float targetPosition;
    // ----------------------------------------

    Vector2 OldVelocity;
    bool isUpdated;

    void Start()
    {
        isUpdated = false;
        OldVelocity = Vector2.zero;
        
        itemStep = ItemList[0].rect.width + HLG.spacing;

        int ItemsToAdd = Mathf.CeilToInt(viewPortTransform.rect.width / itemStep);

        for (int i = 0; i < ItemsToAdd; i++)
        {
            RectTransform RT = Instantiate(ItemList[i % ItemList.Length], contentPanelTransform);
            RT.SetAsLastSibling();
        }

        for (int i = 0; i < ItemsToAdd; i++)
        {
            int num = ItemList.Length - i - 1;
            while (num < 0)
            {
                num += ItemList.Length;
            }
            RectTransform RT = Instantiate(ItemList[num], contentPanelTransform);
            RT.SetAsFirstSibling();
        }

        contentPanelTransform.localPosition = new Vector3((0 - itemStep * ItemsToAdd),
            contentPanelTransform.localPosition.y,
            contentPanelTransform.localPosition.z);

        // ตั้งค่าเป้าหมายเริ่มต้นให้อยู่ที่ตำแหน่งปัจจุบัน
        targetPosition = contentPanelTransform.localPosition.x;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapping = false; // ยกเลิกการ Snap ถ้ายูสเซอร์เอามือแตะหน้าจอ
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    void Update()
    {
        if(isUpdated)
        {
            isUpdated = false;
            scrollRect.velocity = OldVelocity;
        }

        // --- ระบบ Teleport วนลูป (อัปเดต Target ให้ตามไปตอนโดนวาร์ปด้วย) ---
        if (contentPanelTransform.localPosition.x > 0)
        {
            Canvas.ForceUpdateCanvases();
            OldVelocity = scrollRect.velocity;
            float offset = ItemList.Length * itemStep;
            contentPanelTransform.localPosition -= new Vector3(offset, 0, 0);
            targetPosition -= offset; // ขยับเป้าหมายตามเพื่อไม่ให้ดึงกลับ
            isUpdated = true;
        }

        if (contentPanelTransform.localPosition.x < 0 - (ItemList.Length * itemStep))
        {
            Canvas.ForceUpdateCanvases();
            OldVelocity = scrollRect.velocity;
            float offset = ItemList.Length * itemStep;
            contentPanelTransform.localPosition += new Vector3(offset, 0, 0);
            targetPosition += offset; // ขยับเป้าหมายตามเพื่อไม่ให้ดึงกลับ
            isUpdated = true;
        }

        // --- ระบบ Snapping ---
        if (!isDragging)
        {
            // ถ้าไม่ได้ลาก และความเร็วลดลงจนเกือบหยุด ให้เริ่ม Snap
            if (!isSnapping && Mathf.Abs(scrollRect.velocity.x) < snapVelocityThreshold)
            {
                scrollRect.velocity = Vector2.zero; 
                targetPosition = Mathf.Round(contentPanelTransform.localPosition.x / itemStep) * itemStep;
                isSnapping = true;
            }
        }

        // เลื่อน UI ไปหาตำแหน่งเป้าหมาย (ทำงานทั้งตอนปล่อยมือ และตอนกดปุ่ม)
        if (isSnapping && !isDragging)
        {
            contentPanelTransform.localPosition = new Vector3(
                Mathf.Lerp(contentPanelTransform.localPosition.x, targetPosition, Time.deltaTime * snapSpeed),
                contentPanelTransform.localPosition.y,
                contentPanelTransform.localPosition.z
            );
        }
    }

    // ==========================================
    // ฟังก์ชันสำหรับผูกกับปุ่ม UI Button (On Click)
    // ==========================================

    // ใช้กับ "ปุ่มลูกศรขวา" (เลื่อนดูไอเทมถัดไป -> ตัว Content จะขยับไปทางซ้าย)
    public void GoToNextItem()
    {
        if (isDragging) return; // ถ้าผู้เล่นลากหน้าจออยู่ ไม่ต้องทำงาน
        
        scrollRect.velocity = Vector2.zero; // หยุดแรงเหวี่ยงเดิม
        
        // ถ้ายังไม่ได้ Snap ให้อ้างอิงเป้าหมายจากตำแหน่งปัจจุบันก่อน
        if (!isSnapping) {
            targetPosition = Mathf.Round(contentPanelTransform.localPosition.x / itemStep) * itemStep;
        }
        
        // เลื่อนเป้าหมายไป 1 ล็อก (ติดลบคือขยับ Content ไปทางซ้าย)
        targetPosition -= itemStep;
        isSnapping = true;
    }

    // ใช้กับ "ปุ่มลูกศรซ้าย" (เลื่อนดูไอเทมก่อนหน้า -> ตัว Content จะขยับไปทางขวา)
    public void GoToPreviousItem()
    {
        if (isDragging) return; // ถ้าผู้เล่นลากหน้าจออยู่ ไม่ต้องทำงาน

        scrollRect.velocity = Vector2.zero; // หยุดแรงเหวี่ยงเดิม

        // ถ้ายังไม่ได้ Snap ให้อ้างอิงเป้าหมายจากตำแหน่งปัจจุบันก่อน
        if (!isSnapping) {
            targetPosition = Mathf.Round(contentPanelTransform.localPosition.x / itemStep) * itemStep;
        }
        
        // เลื่อนเป้าหมายไป 1 ล็อก (บวกคือขยับ Content ไปทางขวา)
        targetPosition += itemStep;
        isSnapping = true;
    }
}