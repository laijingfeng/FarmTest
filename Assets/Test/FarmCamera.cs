using UnityEngine;
using System.Collections;

public class FarmCamera : MonoBehaviour
{
    void Update()
    {
        TryZoom();
        TryDrag();
    }

    #region Zoom

    [Header("Zoom Settings")]

    public float zoomMin = -1;
    public float zoomMax = 1;
    public float zoomFactor = 0.1f;
    public Transform cam;
    public float zoomLerpFactor = 1f;
    public float zoomAdd = 0f;

    private Vector2 zoomLastPos0 = Vector3.zero;
    private Vector2 zoomLastPos1 = Vector3.zero;
    private float zoomOffset = 0;
    private bool zooming = false;
    private Vector3 zoomingPos = Vector3.zero;

    private void TryZoom()
    {
        if (IsTouchTwo())
        {
            if (!zooming)
            {
                MarkZoomLastPos();
                zooming = true;
            }
        }
        else
        {
            zooming = false;
            DoZoom(0, 0);
        }

        if (IsTouchTwo() && zooming)
        {
            CalZoomOffset();
            if (Mathf.Abs(zoomOffset) < 3f)
            {
                return;
            }
            MarkZoomLastPos();
            DoZoom(zoomOffset * zoomFactor, zoomAdd);
        }
    }

    private void CalZoomOffset()
    {
        if (!IsTouchTwo())
        {
            return;
        }
#if UNITY_EDITOR
        zoomOffset = Input.mousePosition.magnitude - (zoomLastPos0 - zoomLastPos1).magnitude;
#else
        zoomOffset = (Input.touches[0].position - Input.touches[1].position).magnitude - (zoomLastPos0 - zoomLastPos1).magnitude;
#endif
    }

    private void MarkZoomLastPos()
    {
        if (!IsTouchTwo())
        {
            return;
        }
#if UNITY_EDITOR
        zoomLastPos0 = Input.mousePosition;
        zoomLastPos1 = Vector2.zero;
#else
        zoomLastPos0 = Input.touches[0].position;
        zoomLastPos1 = Input.touches[1].position;
#endif
    }

    private void DoZoom(float val, float add)
    {
        val = cam.localPosition.z + val;
        val = Mathf.Clamp(val, zoomMin - add, zoomMax + add);
        zoomingPos = cam.localPosition;
        zoomingPos.z = val;
        if (add > 0)
        {
            cam.localPosition = zoomingPos;
        }
        else
        {
            this.StopCoroutine("IE_DoZoom");
            this.StartCoroutine("IE_DoZoom", zoomingPos);
        }
    }

    private IEnumerator IE_DoZoom(Vector3 pos)
    {
        while (cam != null
            && !Vector3Equal(pos, cam.localPosition, 0.005f))
        {
            cam.localPosition = Vector3.Lerp(cam.localPosition, pos, Time.deltaTime * zoomLerpFactor);
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion Zoom

    # region Drag

    [Header("Drag Settings")]

    public Vector3 posMin = new Vector3(-18.28f, 19f, 11.66f);
    public Vector3 posMax = new Vector3(-10.67f, 19f, 16.97f);

    public float dragFactor = 0.005f;
    public float lerpFactor = 4f;
    public float lerpMul = 10f;

    private Vector3 tmp1;

    private bool m_DragUsefull = false;

    private Vector3 m_LastPos;
    private Vector3 m_Offset;

    private Vector3 m_LastOffset = Vector3.zero;
    private float m_LastOffsetTime = 0;

    private void TryDrag()
    {
        if (IsTouchTwo())
        {
            m_DragUsefull = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            this.StopCoroutine("IE_AdjustCamera");
            //Debug.LogWarning("xxx");
            m_LastPos = GetClickPos();
            m_DragUsefull = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (m_DragUsefull)
            {
                //Debug.LogWarning("ddx " + Time.realtimeSinceStartup + " " + m_LastOffsetTime);
                if (Time.realtimeSinceStartup - m_LastOffsetTime < 0.2f)
                {
                    //Debug.LogWarning("dd v=" + m_LastOffset);
                    DoDrag(-m_LastOffset * lerpMul, true);
                }
                //else
                //{
                //    Debug.LogWarning("ddx " + Time.realtimeSinceStartup + " " + m_LastOffset);
                //}
            }
            m_DragUsefull = false;
        }

        if (m_DragUsefull)
        {
            JudgeDrag();
        }
    }

    private void JudgeDrag()
    {
        m_Offset = GetClickPos() - m_LastPos;
        if (Mathf.Abs(m_Offset.x) < 3f
            && Mathf.Abs(m_Offset.y) < 3f)
        {
            return;
        }
        m_LastPos = GetClickPos();
        m_LastOffset = m_Offset;
        m_LastOffsetTime = Time.realtimeSinceStartup;
        //Debug.LogWarning(m_Offset + " off " + m_LastOffset);
        DoDrag(-m_Offset);
    }

    public void DoDrag(Vector3 val, bool smooth = false)
    {
        Vector3 v2 = Vector3.zero;
        v2.y = 0;
        v2.x = (-val.x + val.y) * 1.414f;
        v2.z = (-val.x - val.y) * 1.414f;

        //Debug.LogWarning("val=" + val + " v2=" + v2);

        tmp1 = this.transform.position;
        tmp1 += v2 * dragFactor;

        tmp1 = Vector3Clamp(tmp1, posMin, posMax);

        if (smooth)
        {
            this.StopCoroutine("IE_AdjustCamera");
            this.StartCoroutine("IE_AdjustCamera", tmp1);
        }
        else
        {
            this.transform.position = tmp1;
        }
    }

    private IEnumerator IE_AdjustCamera(Vector3 pos)
    {
        //Debug.LogWarning(MapUtil.Vector3String(Camera.main.transform.position) + " ||| " + MapUtil.Vector3String(tmp1));
        while (!Vector3Equal(pos, this.transform.position, 0.005f))
        {
            this.transform.position = Vector3.Lerp(this.transform.position, pos, Time.deltaTime * lerpFactor);
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion Drag

    #region 辅助

    private bool IsTouchTwo()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.Z)
            && Input.GetMouseButton(0))
#else
        if (Input.touchCount > 1)
#endif
        {
            return true;
        }
        return false;
    }

    private Vector3 Vector3Clamp(Vector3 val, Vector3 min, Vector3 max)
    {
        val.x = Mathf.Clamp(val.x, min.x, max.x);
        val.y = Mathf.Clamp(val.y, min.y, max.y);
        val.z = Mathf.Clamp(val.z, min.z, max.z);
        return val;
    }

    private bool Vector3Equal(Vector3 a, Vector3 b, float val = 0.1f)
    {
        if (Mathf.Abs(a.x - b.x) > val
            || Mathf.Abs(a.y - b.y) > val
            || Mathf.Abs(a.z - b.z) > val)
        {
            return false;
        }
        return true;
    }

    private Vector3 GetClickPos()
    {
        Vector3 pos = Input.mousePosition;
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
        if(Input.touchCount > 0)
        {
            pos = Input.touches[0].position;
        }
#endif
        return pos;
    }

    #endregion 辅助
}