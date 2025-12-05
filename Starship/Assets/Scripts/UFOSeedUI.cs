using UnityEngine;

public class UFOSeedUI : MonoBehaviour
{
    string seedInput = "";
    string countInput = "";
    Rect windowRect = new Rect(15, 15, 190, 120);

    void OnGUI()
    {
        windowRect = GUI.Window(0, windowRect, DrawWindow, "UFO Control");
    }

    void DrawWindow(int id)
    {
        var gen = GetComponent<UFOGenerator>();
        if (gen == null) return;

        GUI.Label(new Rect(10, 22, 170, 18), $"Seed: {gen.mainSeed}");
        GUI.Label(new Rect(10, 38, 170, 18), $"Count: {gen.ufoCount}");

        GUI.Label(new Rect(10, 58, 40, 18), "New:");
        seedInput = GUI.TextField(new Rect(45, 58, 55, 18), seedInput);
        if (GUI.Button(new Rect(105, 58, 70, 18), "Generate"))
        {
            if (int.TryParse(seedInput, out int s))
            {
                gen.mainSeed = s;
                gen.RefreshFleet();
            }
        }

        if (GUI.Button(new Rect(10, 80, 60, 18), "Random"))
        {
            gen.mainSeed = Random.Range(0, int.MaxValue);
            gen.RefreshFleet();
            seedInput = gen.mainSeed.ToString();
        }
        if (GUI.Button(new Rect(75, 80, 35, 18), "-"))
        {
            gen.mainSeed--;
            gen.RefreshFleet();
            seedInput = gen.mainSeed.ToString();
        }
        if (GUI.Button(new Rect(115, 80, 35, 18), "+"))
        {
            gen.mainSeed++;
            gen.RefreshFleet();
            seedInput = gen.mainSeed.ToString();
        }

        GUI.Label(new Rect(10, 100, 45, 18), "Count:");
        countInput = GUI.TextField(new Rect(55, 100, 35, 18), countInput);
        if (GUI.Button(new Rect(95, 100, 45, 18), "Apply"))
        {
            if (int.TryParse(countInput, out int c))
            {
                gen.ufoCount = Mathf.Max(1, c);
                gen.RefreshFleet();
            }
        }

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
}
