using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BeadGameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 16;
    public int gridHeight = 16;

    [Header("Palette")]
    public Color[] paletteColors;
    public int paletteColumns = 4;

    [Header("Pattern")]
    [TextArea(5, 30)]
    public string patternCsv;

    private int selectedIndex;
    private int[,] cellStates;
    private int[,] patternData;
    private BeadCell[,] cells;
    private Sprite circleSprite;

    private static readonly Color EmptyColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
    private static readonly Color GridBg = new Color(0.13f, 0.13f, 0.14f);
    private const float Spacing = 2f;
    private const float Padding = 12f;
    private const float RefW = 1024f;
    private const float RefH = 768f;

    void Start()
    {
        var existing = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in existing)
            c.gameObject.SetActive(false);

        if (paletteColors == null || paletteColors.Length == 0)
            paletteColors = DefaultColors();

        circleSprite = GenerateCircleSprite();
        ParsePattern();
        BuildUI();
        SelectColor(0);
    }

    #region Colors & Pattern

    Color[] DefaultColors()
    {
        return new Color[]
        {
            new Color(0.90f, 0.10f, 0.10f),
            new Color(0.95f, 0.50f, 0.00f),
            new Color(1.00f, 0.90f, 0.00f),
            new Color(0.00f, 0.70f, 0.10f),
            new Color(0.00f, 0.40f, 0.90f),
            new Color(0.50f, 0.10f, 0.80f),
            new Color(1.00f, 0.40f, 0.70f),
            new Color(0.55f, 0.27f, 0.07f),
            new Color(0.15f, 0.15f, 0.15f),
            new Color(1.00f, 1.00f, 1.00f),
            new Color(0.55f, 0.55f, 0.55f),
            new Color(0.40f, 0.75f, 0.95f),
        };
    }

    void ParsePattern()
    {
        if (!string.IsNullOrEmpty(patternCsv))
        {
            var lines = patternCsv.Trim().Split('\n');
            int h = lines.Length;
            int w = 0;
            foreach (var line in lines)
            {
                var p = line.Trim().Split(',');
                w = Mathf.Max(w, p.Length);
            }
            if (w == 0 || h == 0) { GenerateHeartPattern(); return; }
            patternData = new int[w, h];
            for (int y = 0; y < h; y++)
            {
                var p = lines[y].Trim().Split(',');
                for (int x = 0; x < p.Length; x++)
                {
                    int v;
                    patternData[x, y] = int.TryParse(p[x].Trim(), out v) ? v : -1;
                }
                for (int x = p.Length; x < w; x++)
                    patternData[x, y] = -1;
            }
        }
        else
        {
            GenerateHeartPattern();
        }
    }

    void GenerateHeartPattern()
    {
        int s = Mathf.Min(gridWidth, gridHeight, 14);
        patternData = new int[s, s];
        float cx = (s - 1) / 2f;
        float cy = (s - 1) / 2f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float nx = (x - cx) / s * 3.6f;
                float ny = (y - cy) / s * 3.6f;
                float h = nx * nx + ny * ny - 0.55f;
                patternData[x, y] = (h * h * h - nx * nx * ny * ny * ny <= 0) ? 0 : -1;
            }
        }
    }

    #endregion

    #region Circle Sprite

    Sprite GenerateCircleSprite()
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float radius = size / 2f;
        float aaWidth = 1.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - dist) / aaWidth + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    #endregion

    #region UI Building

    void BuildUI()
    {
        EnsureEventSystem();

        var canvas = CreateCanvas();
        var root = canvas.GetComponent<RectTransform>();

        var bg = CreateImage("Bg", root, Vector2.zero, Vector2.one);
        bg.color = new Color(0.22f, 0.22f, 0.24f);
        bg.raycastTarget = false;

        var pegPanel = CreatePanel("PegboardPanel", root, new Vector2(0, 0), new Vector2(0.65f, 1));
        var pegBg = pegPanel.gameObject.AddComponent<Image>();
        pegBg.color = GridBg;
        pegBg.raycastTarget = false;
        CreatePegboard(pegPanel);

        var sidePanel = CreatePanel("SidePanel", root, new Vector2(0.65f, 0), new Vector2(1, 1));
        var sideBg = sidePanel.gameObject.AddComponent<Image>();
        sideBg.color = new Color(0.17f, 0.17f, 0.19f);
        sideBg.raycastTarget = false;

        var patternPanel = CreatePanel("PatternPanel", sidePanel, new Vector2(0, 0.5f), new Vector2(1, 1));
        CreatePatternPreview(patternPanel);

        var palettePanel = CreatePanel("PalettePanel", sidePanel, new Vector2(0, 0), new Vector2(1, 0.5f));
        CreatePalette(palettePanel);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
        }
    }

    Canvas CreateCanvas()
    {
        var go = new GameObject("BeadCanvas");
        go.layer = 5;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    Image CreateImage(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(Image));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go.GetComponent<Image>();
    }

    #endregion

    #region Pegboard

    void CreatePegboard(RectTransform parent)
    {
        float pw = RefW * 0.65f;
        float ph = RefH;

        float cellSize = Mathf.Min(
            (pw - Padding * 2 - (gridWidth - 1) * Spacing) / gridWidth,
            (ph - Padding * 2 - (gridHeight - 1) * Spacing) / gridHeight
        );
        cellSize = Mathf.Floor(cellSize);

        float totalW = gridWidth * cellSize + (gridWidth - 1) * Spacing;
        float totalH = gridHeight * cellSize + (gridHeight - 1) * Spacing;

        cells = new BeadCell[gridWidth, gridHeight];
        cellStates = new int[gridWidth, gridHeight];

        for (int y = 0; y < gridHeight; y++)
            for (int x = 0; x < gridWidth; x++)
                cellStates[x, y] = -1;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var go = new GameObject("Cell_" + x + "_" + y, typeof(Image));
                go.layer = 5;
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.sizeDelta = new Vector2(cellSize, cellSize);

                float px = x * (cellSize + Spacing) + cellSize / 2f - totalW / 2f;
                float py = y * (cellSize + Spacing) + cellSize / 2f - totalH / 2f;
                rt.anchoredPosition = new Vector2(px, py);

                var img = go.GetComponent<Image>();
                img.color = EmptyColor;

                var cell = go.AddComponent<BeadCell>();
                cell.Initialize(this, x, y, circleSprite);
                cells[x, y] = cell;
            }
        }

        var bgImg = parent.GetComponent<Image>();
        if (bgImg != null)
        {
            bgImg.sprite = null;
        }
    }

    #endregion

    #region Pattern Preview

    void CreatePatternPreview(RectTransform parent)
    {
        int pw = patternData.GetLength(0);
        int ph = patternData.GetLength(1);

        float panelW = RefW * 0.35f;
        float panelH = RefH * 0.5f;

        float cellSize = Mathf.Min(
            (panelW - Padding * 2 - (pw - 1) * (Spacing * 0.5f)) / pw,
            (panelH - Padding * 2 - (ph - 1) * (Spacing * 0.5f)) / ph
        );
        cellSize = Mathf.Floor(cellSize);
        if (cellSize < 4) cellSize = 4;

        float totalW = pw * cellSize + (pw - 1) * (Spacing * 0.5f);
        float totalH = ph * cellSize + (ph - 1) * (Spacing * 0.5f);

        var titleBg = CreateImage("PatternTitleBg", parent, new Vector2(0, 0.85f), new Vector2(1, 1));
        titleBg.color = new Color(0.12f, 0.12f, 0.13f);
        titleBg.raycastTarget = false;

        for (int y = 0; y < ph; y++)
        {
            for (int x = 0; x < pw; x++)
            {
                var go = new GameObject("P_" + x + "_" + y, typeof(Image));
                go.layer = 5;
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.sizeDelta = new Vector2(cellSize, cellSize);

                float px = x * (cellSize + Spacing * 0.5f) + cellSize / 2f - totalW / 2f;
                float py = y * (cellSize + Spacing * 0.5f) + cellSize / 2f - totalH / 2f + panelH * 0.12f;
                rt.anchoredPosition = new Vector2(px, py);

                var img = go.GetComponent<Image>();
                int idx = patternData[x, y];
                if (idx >= 0 && idx < paletteColors.Length)
                    img.color = paletteColors[idx];
                else
                    img.color = EmptyColor;
                img.raycastTarget = false;
            }
        }
    }

    #endregion

    #region Palette

    void CreatePalette(RectTransform parent)
    {
        float panelW = RefW * 0.35f;
        float panelH = RefH * 0.5f;

        int cols = Mathf.Min(paletteColumns, paletteColors.Length);
        int rows = Mathf.CeilToInt((float)paletteColors.Length / cols);
        float spacing = PaletteSpacing();

        float cellSize = Mathf.Min(
            (panelW - Padding * 2 - (cols - 1) * spacing) / cols,
            (panelH - Padding * 2 - (rows - 1) * spacing) / rows
        );
        cellSize = Mathf.Floor(cellSize);
        if (cellSize < 10) cellSize = 10;

        float totalW = cols * cellSize + (cols - 1) * spacing;
        float totalH = rows * cellSize + (rows - 1) * spacing;

        for (int i = 0; i < paletteColors.Length; i++)
        {
            int ix = i % cols;
            int iy = i / cols;

            var go = new GameObject("Palette_" + i, typeof(Image));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(cellSize, cellSize);

            float px = ix * (cellSize + spacing) + cellSize / 2f - totalW / 2f;
            float py = iy * (cellSize + spacing) + cellSize / 2f - totalH / 2f;
            rt.anchoredPosition = new Vector2(px, py);

            var img = go.GetComponent<Image>();
            img.color = paletteColors[i];

            var hlGo = new GameObject("Highlight", typeof(Image));
            hlGo.layer = 5;
            var hlRt = hlGo.GetComponent<RectTransform>();
            hlRt.SetParent(rt, false);
            hlRt.sizeDelta = new Vector2(cellSize + 6, cellSize + 6);
            hlRt.anchoredPosition = Vector2.zero;
            var hlImg = hlGo.GetComponent<Image>();
            hlImg.color = new Color(1, 1, 1, 0.6f);
            hlImg.raycastTarget = false;
            hlGo.SetActive(false);

            var btn = go.AddComponent<Button>();
            int captured = i;
            btn.onClick.AddListener(() => SelectColor(captured));

            var tracker = go.AddComponent<PaletteTracker>();
            tracker.highlight = hlGo;
        }
    }

    float PaletteSpacing()
    {
        return Mathf.Min(6f, (RefW * 0.35f) / paletteColumns * 0.15f);
    }

    #endregion

    #region Public API

    public void SelectColor(int index)
    {
        if (index < 0 || index >= paletteColors.Length) return;
        selectedIndex = index;

        var trackers = FindObjectsByType<PaletteTracker>(FindObjectsSortMode.None);
        foreach (var t in trackers)
        {
            t.highlight.SetActive(t.transform.GetSiblingIndex() == index);
        }
    }

    public void PlaceBead(int x, int y)
    {
        if (selectedIndex < 0) return;
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;

        cellStates[x, y] = selectedIndex;
        cells[x, y].SetColor(paletteColors[selectedIndex]);
    }

    public void RemoveBead(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
        if (cellStates[x, y] < 0) return;

        cellStates[x, y] = -1;
        cells[x, y].SetColor(EmptyColor);
    }

    #endregion
}

public class PaletteTracker : MonoBehaviour
{
    public GameObject highlight;
}
