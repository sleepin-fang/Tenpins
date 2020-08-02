using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class tenpins : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Renderer[] pins;
    public Renderer bowlingBallRender;
    public KMSelectable bowlingBall;
    public Color[] colors;
    public Color[] bowlingBallColors;
    public Color gray;
    public GameObject[] bases;

    private int[] splits = new int[3];
    private int[] pivots = new int[3];
    private bool[] mirrored = new bool[3];
    private bool[] inverted = new bool[3];
    private int[] stageOrder = new int[3];
    private int stage;
    private int min;
    private int max;
    private int lastDigit;

    private static readonly int[][] pivotOrders = new int[6][]
    {
        new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        new int[10] { 6, 7, 3, 8, 4, 1, 9, 5, 2, 0 },
        new int[10] { 9, 5, 8, 2, 4, 7, 0, 1, 3, 6 },
        new int[10] { 0, 2, 1, 5, 4, 3, 9, 8, 7, 6 },
        new int[10] { 6, 3, 7, 1, 4, 8, 0, 2, 5, 9 },
        new int[10] { 9, 8, 5, 7, 4, 2, 6, 3, 1, 0 }
    };
    private static readonly bool[][] splitArrangements = new bool[12][]
    {
        new bool[10] { false, false, false, false, false, false, true, false, false, true },
        new bool[10] { false, false, false, false, false, false, true, false, true, false },
        new bool[10] { false, false, false, false, true, false, true, false, false, false },
        new bool[10] { false, false, false, false, true, false, true, false, false, true },
        new bool[10] { false, false, true, false, false, false, true, false, false, false },
        new bool[10] { false, true, false, false, false, false, true, false, false, true },
        new bool[10] { false, false, false, true, false, false, true, false, false, true },
        new bool[10] { false, false, false, true, false, true, true, false, false, true },
        new bool[10] { false, false, false, true, false, true, true, true, false, true },
        new bool[10] { false, false, true, true, false, true, true, false, false, true },
        new bool[10] { false, true, false, true, false, true, true, true, false, true },
        new bool[10] { true, false, false, false, false, false, true, false, false, true }
    };

    private static readonly string[] splitNames = new string[12] { "Goal Posts", "Cincinnati", "Woolworth Store", "Lily", "3-7 Split", "Cocked Hat", "4-7-10 Split", "Big Four", "Greek Church", "Big Five", "Big Six", "HOW" };
    private static readonly string[] pivotNames = new string[3] { "south", "northwest", "northeast" };
    private static readonly string[] rgbNames = new string[3] { "red", "green", "blue" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        bowlingBall.OnInteract += delegate () { PressBowlingBall(); return false; };
    }

    void Start()
    {
        stageOrder = Enumerable.Range(0, 3).ToList().Shuffle().ToArray();
        for (int i = 0; i < 3; i++)
        {
            splits[i] = rnd.Range(0, 12);
            pivots[i] = rnd.Range(0, 3);
            mirrored[i] = rnd.Range(0, 2) == 0;
            inverted[i] = rnd.Range(0, 2) == 0;
            if (splits[i] == 2 && mirrored[i])
                mirrored[i] = false;
        }
        var arrangementsPresent = new List<bool[]>();
        var orders = new List<int[]>();
        for (int i = 0; i < 3; i++)
        {
            orders.Add(pivotOrders[pivots[i] + (mirrored[i] ? 3 : 0)].ToArray());
            var arrangement = splitArrangements[splits[i]].ToArray();
            if (inverted[i])
                for (int j = 0; j < 10; j++)
                    arrangement[j] = !arrangement[j];
            arrangementsPresent.Add(arrangement);
        }
        var s = "";
        var svgColorNames = new string[] { "red", "green", "blue", "cyan", "magenta", "yellow", "white" };
        for (int pin = 0; pin < 10; pin++)
        {
            var color = 0;
            var r = Array.IndexOf(orders[0], pin);
            var g = Array.IndexOf(orders[1], pin);
            var b = Array.IndexOf(orders[2], pin);
            if (arrangementsPresent[0][r] && arrangementsPresent[1][g] && arrangementsPresent[2][b])
                color = 6;
            else if (arrangementsPresent[0][r] && arrangementsPresent[1][g])
                color = 5;
            else if (arrangementsPresent[0][r] && arrangementsPresent[2][b])
                color = 4;
            else if (arrangementsPresent[1][g] && arrangementsPresent[2][b])
                color = 3;
            else if (arrangementsPresent[0][r])
                color = 0;
            else if (arrangementsPresent[1][g])
                color = 1;
            else if (arrangementsPresent[2][b])
                color = 2;
            else
                color = 7;
            if (color == 7)
                pins[pin].gameObject.SetActive(false);
            else
            {
                pins[pin].material.color = colors[color];
                s += String.Format("<circle cx='{1}' cy='{2}' r='.007' fill='{0}'/>", svgColorNames[color], pins[pin].transform.localPosition.x, -pins[pin].transform.localPosition.z);
            }
        }
        foreach (GameObject b in bases)
            b.SetActive(true);
        for (int i = 0; i < 3; i++)
            Debug.LogFormat("[Tenpins #{0}] The {1} channel has a {2}, is {3}, and has the {4} pin as pin 1. It is{5} mirrored.", moduleId, rgbNames[i], splitNames[splits[i]], inverted[i] ? "inverted" : "normal", pivotNames[pivots[i]], mirrored[i] ? "" : "n't");
        Debug.LogFormat("[Tenpins #{0}]=svg[Pins:]<svg xmlns='http://www.w3.org/2000/svg' viewBox='-.05 -.065 .3 .085' stroke='black' stroke-width='.001'>{1}</svg>", moduleId, s);
        GenerateStage();
    }

    void GenerateStage()
    {
        if (stage < 3)
        {
            bowlingBallRender.material.color = bowlingBallColors[stageOrder[stage]];
            Debug.LogFormat("[Tenpins #{0}] Stage {1} is {2}.", moduleId, stage + 1, rgbNames[stageOrder[stage]]);
            min = mins[stageOrder[stage]][!inverted[stageOrder[stage]] ? 0 : 1];
            max = maxs[stageOrder[stage]][!inverted[stageOrder[stage]] ? 0 : 1];
            if (min == -1)
                Debug.LogFormat("[Tenpins #{0}] The bowling ball must be pressed when the seconds digits mod 20 are less than 10.", moduleId);
            else if (max == 20)
                Debug.LogFormat("[Tenpins #{0}] The bowling ball must be pressed when the seconds digits mod 20 are greater than 9.", moduleId);
            else
                Debug.LogFormat("[Tenpins #{0}] The bowling ball must be pressed when the seconds digits mod 20 are greater than {1} and less than {2}.", moduleId, min, max);
            if (splits[stageOrder[stage]] != 11)
                lastDigit = digits[pivots[stageOrder[stage]]][splits[stageOrder[stage]]];
            else if (splits.Count(x => x == 11) == 1)
            {
                var otherStages = Enumerable.Range(0, 3).Where(x => x != stage).ToArray();
                var otherPivots = otherStages.Select(x => pivots[x]).ToArray();
                if (pivots[otherStages[0]] == pivots[otherStages[1]])
                    lastDigit = digits[pivots[stageOrder[otherStages[0]]]][11];
                else
                    lastDigit = digits[Enumerable.Range(0, 3).First(x => !otherPivots.Contains(x))][11];
            }
            else
                lastDigit = digits[stageOrder[stage]][11];
            Debug.LogFormat("[Tenpins #{0}] The bowling ball must be pressed when the last digit of the timer is {1}.", moduleId, lastDigit);
        }
        else
        {
            module.HandlePass();
            Debug.LogFormat("[Tenpins #{0}] Module solved!", moduleId);
            moduleSolved = true;
            if (!splits.Any(x => x != 11))
                audio.PlaySoundAtTransform("HOW", transform);
            else
                audio.PlaySoundAtTransform("solve" + rnd.Range(1, 6), transform);
            foreach (Renderer r in pins)
                r.material.color = colors[6];
            bowlingBallRender.material.color = gray;
        }
    }

    void PressBowlingBall()
    {
        if (moduleSolved)
            return;
        var submmittedTime = ((int) bomb.GetTime()) % 60;
        Debug.LogFormat("[Tenpins #{0}] The bowling ball was pressed on {1}.", moduleId, submmittedTime);
        var req1 = submmittedTime % 20 > min && submmittedTime % 20 < max;
        var req2 = submmittedTime % 10 == lastDigit;
        if (req1 && req2)
        {
            Debug.LogFormat("[Tenpins #{0}] That time meets both requirements.", moduleId);
            stage++;
            if (stage != 3)
            {
                Debug.LogFormat("[Tenpins #{0}] Progressing to the next stage...", moduleId);
                audio.PlaySoundAtTransform("pins" + rnd.Range(1, 3), bowlingBall.transform);
            }
            GenerateStage();
        }
        else
        {
            if (req1)
                Debug.LogFormat("[Tenpins #{0}] The time was in range, but the last digit was not correct. Strike!", moduleId);
            else
                Debug.LogFormat("[Tenpins #{0}] The time was not in range, but the last digit was correct. Strike!", moduleId);
            module.HandleStrike();
        }
    }

    private static readonly int[][] mins = new int[3][]
    {
        new int[2] { -1, 9 },
        new int[2] { 4, 7 },
        new int[2] { 2, 6 }
    };

    private static readonly int[][] maxs = new int[3][]
    {
        new int[2] { 10, 20 },
        new int[2] { 15, 18 },
        new int[2] { 13, 17 }
    };

    private static readonly int[][] digits = new int[3][]
    {
        new int[12] { 7, 5, 9, 2, 8, 6, 3, 7, 5, 1, 0, 4 },
        new int[12] { 2, 1, 8, 9, 5, 0, 1, 4, 7, 3, 6, 2 },
        new int[12] { 1, 3, 4, 3, 8, 2, 4, 1, 6, 5, 9, 0 }
    };

    // Twitch Plays
    /*#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press 10 [Presses the bowling ball when the seconds digits are 10.]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        Match m;
        if ((m = Regex.Match(input, @"^\s*press\s+(\d)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success);
        {
            yield return null;
            var value = int.Parse(m.Groups[1].Value);
            while (((int) bomb.GetTime()) % 10 != value)
                yield return "trycancel";
            bowlingBall.OnInteract();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            while (!(((int) bomb.GetTime()) % 20 > min) || !(((int) bomb.GetTime()) % 20 < max) || (((int) bomb.GetTime()) % 10 == lastDigit))
                yield return null;
            bowlingBall.OnInteract();
        }
    }*/
}
