using UnityEngine;

/// <summary>
/// Positions a tooltip RectTransform relative to an anchor RectTransform and keeps it inside bounds.
/// </summary>
public class TooltipPositioner : MonoBehaviour
{
    public enum Placement
    {
        Auto,
        Left,
        Right,
        Top,
        Bottom
    }

    [Header("References")]
    public RectTransform tooltipRect;
    public RectTransform anchorRect;
    public RectTransform boundsRect;
    public RectTransform positioningRoot;

    [Header("Placement")]
    public Placement preferredPlacement = Placement.Auto;
    public float gap = 12f;
    public Vector2 offset = Vector2.zero;
    public bool keepWithinBounds = true;
    public bool updateEveryFrame = true;

    private readonly Vector3[] anchorWorldCorners = new Vector3[4];
    private readonly Vector3[] boundsWorldCorners = new Vector3[4];

    private void Awake()
    {
        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

        if (positioningRoot != null && tooltipRect != null && tooltipRect.parent != positioningRoot)
            tooltipRect.SetParent(positioningRoot, false);
    }

    private void OnEnable()
    {
        Reposition();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame)
            Reposition();
    }

    public void Reposition()
    {
        if (tooltipRect == null || anchorRect == null)
            return;

        var parentRt = tooltipRect.parent as RectTransform;
        if (parentRt == null)
            return;

        Canvas.ForceUpdateCanvases();

        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0.5f, 0.5f);

        anchorRect.GetWorldCorners(anchorWorldCorners);
        Vector2 anchorCenter = WorldToLocalCenter(parentRt, anchorWorldCorners[0], anchorWorldCorners[2]);
        Vector2 anchorLeft = WorldToLocalCenter(parentRt, anchorWorldCorners[0], anchorWorldCorners[1]);
        Vector2 anchorRight = WorldToLocalCenter(parentRt, anchorWorldCorners[3], anchorWorldCorners[2]);
        Vector2 anchorTop = WorldToLocalCenter(parentRt, anchorWorldCorners[1], anchorWorldCorners[2]);
        Vector2 anchorBottom = WorldToLocalCenter(parentRt, anchorWorldCorners[0], anchorWorldCorners[3]);

        float halfW = tooltipRect.rect.width * 0.5f;
        float halfH = tooltipRect.rect.height * 0.5f;
        float safeGap = Mathf.Max(0f, gap);

        Vector2 leftCandidate = new Vector2(anchorLeft.x - halfW - safeGap, anchorCenter.y);
        Vector2 rightCandidate = new Vector2(anchorRight.x + halfW + safeGap, anchorCenter.y);
        Vector2 topCandidate = new Vector2(anchorCenter.x, anchorTop.y + halfH + safeGap);
        Vector2 bottomCandidate = new Vector2(anchorCenter.x, anchorBottom.y - halfH - safeGap);

        Vector2 chosen = ChooseCandidate(parentRt, halfW, halfH, leftCandidate, rightCandidate, topCandidate, bottomCandidate);
        chosen += offset;

        if (keepWithinBounds)
            chosen = ClampToBounds(parentRt, chosen, halfW, halfH);

        tooltipRect.anchoredPosition = chosen;
    }

    private Vector2 ChooseCandidate(
        RectTransform parentRt,
        float halfW,
        float halfH,
        Vector2 leftCandidate,
        Vector2 rightCandidate,
        Vector2 topCandidate,
        Vector2 bottomCandidate)
    {
        Vector2[] ordered;
        switch (preferredPlacement)
        {
            case Placement.Left:
                ordered = new[] { leftCandidate, topCandidate, bottomCandidate, rightCandidate };
                break;
            case Placement.Right:
                ordered = new[] { rightCandidate, topCandidate, bottomCandidate, leftCandidate };
                break;
            case Placement.Top:
                ordered = new[] { topCandidate, leftCandidate, rightCandidate, bottomCandidate };
                break;
            case Placement.Bottom:
                ordered = new[] { bottomCandidate, leftCandidate, rightCandidate, topCandidate };
                break;
            default:
                ordered = new[] { topCandidate, leftCandidate, rightCandidate, bottomCandidate };
                break;
        }

        if (!keepWithinBounds || boundsRect == null)
            return ordered[0];

        float bestScore = float.MaxValue;
        Vector2 best = ordered[0];
        for (int i = 0; i < ordered.Length; i++)
        {
            float score = ComputeOverflowScore(parentRt, ordered[i], halfW, halfH);
            if (score < bestScore)
            {
                bestScore = score;
                best = ordered[i];
            }
        }
        return best;
    }

    private float ComputeOverflowScore(RectTransform parentRt, Vector2 center, float halfW, float halfH)
    {
        GetBoundsLocal(parentRt, out float xMin, out float xMax, out float yMin, out float yMax);

        float leftOverflow = Mathf.Max(0f, xMin - (center.x - halfW));
        float rightOverflow = Mathf.Max(0f, (center.x + halfW) - xMax);
        float bottomOverflow = Mathf.Max(0f, yMin - (center.y - halfH));
        float topOverflow = Mathf.Max(0f, (center.y + halfH) - yMax);
        return leftOverflow + rightOverflow + bottomOverflow + topOverflow;
    }

    private Vector2 ClampToBounds(RectTransform parentRt, Vector2 center, float halfW, float halfH)
    {
        GetBoundsLocal(parentRt, out float xMin, out float xMax, out float yMin, out float yMax);

        float clampedX = Mathf.Clamp(center.x, xMin + halfW, xMax - halfW);
        float clampedY = Mathf.Clamp(center.y, yMin + halfH, yMax - halfH);
        return new Vector2(clampedX, clampedY);
    }

    private void GetBoundsLocal(RectTransform parentRt, out float xMin, out float xMax, out float yMin, out float yMax)
    {
        RectTransform effectiveBounds = boundsRect != null ? boundsRect : parentRt;
        effectiveBounds.GetWorldCorners(boundsWorldCorners);

        Vector2 a = parentRt.InverseTransformPoint(boundsWorldCorners[0]);
        Vector2 b = parentRt.InverseTransformPoint(boundsWorldCorners[1]);
        Vector2 c = parentRt.InverseTransformPoint(boundsWorldCorners[2]);
        Vector2 d = parentRt.InverseTransformPoint(boundsWorldCorners[3]);

        xMin = Mathf.Min(a.x, b.x, c.x, d.x);
        xMax = Mathf.Max(a.x, b.x, c.x, d.x);
        yMin = Mathf.Min(a.y, b.y, c.y, d.y);
        yMax = Mathf.Max(a.y, b.y, c.y, d.y);
    }

    private static Vector2 WorldToLocalCenter(RectTransform parentRt, Vector3 p1, Vector3 p2)
    {
        Vector3 center = (p1 + p2) * 0.5f;
        return parentRt.InverseTransformPoint(center);
    }
}
