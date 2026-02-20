using UnityEngine;

public abstract class PoseRuleBase : MonoBehaviour
{
    // เรียกตอนเริ่ม session
    public virtual void OnSessionStart() {}

    // เรียกตอนจบ session
    public virtual void OnSessionEnd() {}

    // ให้ rule ประมวลผล frame นี้ แล้วส่งผลกลับว่าเฟรมนี้ “ถูกไหม”
    // ถ้าหาข้อมูล pose ไม่ได้ (หลุดเฟรม) ให้ return false และ valid=false
    public abstract bool EvaluateThisFrame(out bool valid);

    // ข้อมูล debug ให้ UI โชว์ได้ (optional)
    public virtual string GetDebugText() => "";
}