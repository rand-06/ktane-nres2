using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KeepCoding;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class negativeResScript : MonoBehaviour
{
    public KMBombModule Module;
    //public KMSelectable Cover;
    //public KMSelectable Status;
    //public KMSelectable[] spheres;
    public GameObject sphere;
    public Transform sphereParent;
    private GameObject[] spheres;
    public TextMesh Center;
    public KMAudio Audio;
    public AudioClip[] sounds;
    //public AudioClip[] clips = new AudioClip[3];
    private bool TwitchPlaysActive, TwitchShouldSolve;
    
    static int ModuleIdCounter;
    int ModuleId;
    private bool secondStage;
    private int[] config;
    private int[] shifts;
    private int[] currentPosition;
    private Color[] colors;
    private Vector3[] positionVectors;
    private Vector3[] shiftedPositionVectors;
    private string axisNames = "XYZWVURSTOPQLMNIJK"; //i'm actually making nres for higher dimensions ho lee sheet
    private List<int> currentAnswer;
    private string readableConfig;
    private bool lockInput = false;

    private int amountOfDimensions;

    private Vector3[] axes = new[]
    {
        new Vector3(1f, 0f, 0f),            //X
        new Vector3(0f, 1f, 0f),            //Y
        new Vector3(0f, 0f, 1f),            //Z     3
        new Vector3(0.8f, 0.2f, 0.5f),      //W
        new Vector3(0.2f, 0.5f, 0.8f),      //V
        new Vector3(0.5f, 0.8f, 0.2f),      //U     6
        new Vector3(2f, 0.125f, 0.125f),    //R
        new Vector3(0.125f, 0.125f, 2f),    //S
        new Vector3(0.125f, 2f, 0.125f),    //T     9
        new Vector3(0.125f, 0.05f, 3.2f),   //O
        new Vector3(0.05f, 3.2f, 0.125f),   //P
        new Vector3(3.2f, 0.125f, 0.05f),   //Q     12
        new Vector3(4f, 1f, 2f),            //L
        new Vector3(2f, 4f, 1f),            //M
        new Vector3(1f, 2f, 4f),            //N     15
        new Vector3(5f, 0f, 0f),            //I
        new Vector3(0f, 5f, 0f),            //J
        new Vector3(0f, 0f, 5f),            //K     18
        new Vector3(4f, 1f, 2.5f),          //F
        new Vector3(1f, 2.5f, 4f),          //G
        new Vector3(2.5f, 4f, 1f),          //H     21
        new Vector3(10f, 0.6f, 0.6f),       //C
        new Vector3(0.6f, 0.6f, 10f),       //D
        new Vector3(0.6f, 10f, 0.6f),       //E     24
        new Vector3(0.6f, 0.25f, 16f),      //A
        new Vector3(0.25f, 16f, 0.6f),      //B
        new Vector3(16f, 0.6f, 0.25f),      //1     27
    };

    private float scalingFactor;
    private bool activatedOnce;

    public AudioClip we_are_fucked;

    void initialize()
    {
        sphere.SetActive(false);
        secondStage = false;
        currentAnswer = new List<int>();
        readableConfig = "";
        config = Enumerable.Range(1, amountOfDimensions).ToArray();
        generateConfig();
        Center.text = config.Length.ToString();
        shifts = positionsFromConfig();
        currentPosition = Enumerable.Range(0, shifts.Length).ToArray();
        shiftedPositionVectors = Enumerable.Range(0, 1<<amountOfDimensions).Select(i=>xyzFromNumber(shifts[i])).ToArray();
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            spheres[i].GetComponent<MeshRenderer>().material.color = colors[i];
        }
        StartCoroutine(move());
    }

    IEnumerator strike()
    {
        Audio.PlaySoundAtTransform(sounds[1].name, transform);
        lockInput = true;
        Debug.LogFormat("[Negative-Resistance #{0}] You've entered {1}, which is wrong. Going back to Read state.", ModuleId, Center.text);
        currentAnswer.Clear();
        //TwitchShouldSolve = false;
        float time2 = 0f;
        Color[] buttonCurrentColors = new Color[1 << amountOfDimensions];
        for  (int i = 0; i < 1 << amountOfDimensions; i++)
            buttonCurrentColors[i]=spheres[i].GetComponent<MeshRenderer>().material.color;
        while (time2 < 1f)
        {
            for (int i = 0; i < 1 << amountOfDimensions; i++)
            {
                spheres[i].GetComponent<MeshRenderer>().material.color = Color.Lerp(buttonCurrentColors[i], colors[i], time2);
            }
            yield return new WaitForSeconds(.1f);
            time2 += .1f;
        }
        yield return new  WaitForSeconds(2f);
        Module.HandleStrike();
        Center.text = config.Length.ToString();
        lockInput = false;
        
        
        secondStage = false;
        shifts = positionsFromConfig();
        currentPosition = Enumerable.Range(0, shifts.Length).ToArray();
        shiftedPositionVectors = Enumerable.Range(0, 1<<amountOfDimensions).Select(i=>xyzFromNumber(shifts[i])).ToArray();
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            spheres[i].GetComponent<MeshRenderer>().material.color = colors[i];
        }
        StartCoroutine(move());
    }
    int[] positionsFromConfig()
    {
        int[] ans = new int[1 << config.Length];
        for (int i = 0; i < 1 << config.Length; i++)
        {
            for (int j = 0; j < config.Length; j++)
            {
                if ((config[j] < 0) ^ (((1 << j) & i)!=0)) ans[i] += 1<<(config[j]*(config[j]<0?-1:1)-1);
            }
        }
        return ans;
    }
    Vector3 xyzFromNumber(int num)
    {
        return scalingFactor * Enumerable.Range(0, amountOfDimensions).Where(x => (num & (1 << x)) != 0)
            .Select(x => axes[x]).Concat(new Vector3(0, 0, 0)).Aggregate((a, b) => a + b);
    }
    IEnumerator move()
    {
        while (!secondStage)
        {
            float time = 0f;
            while (time < 1.03f)
            {
                for (int i = 0; i < 1 << amountOfDimensions; i++)
                    spheres[i].transform.localPosition = Vector3.Lerp(
                        positionVectors[currentPosition[i]],
                        shiftedPositionVectors[currentPosition[i]], time);
                yield return new WaitForSeconds(.05f);
                time += .05f;
            }
            int[] tmp = new int[1 << amountOfDimensions];
            Array.Copy(currentPosition, tmp, tmp.Length);
            for (int i = 0; i < 1 << amountOfDimensions; i++) currentPosition[i] = shifts[tmp[i]];
            yield return new WaitForSeconds(3f);
        }

        
        float time2 = 0f;
        while (time2 < 1f)
        {
            for (int i = 0; i < 1 << amountOfDimensions; i++)
            {
                spheres[i].GetComponent<MeshRenderer>().material.color = Color.Lerp(colors[i], Color.gray, time2);
            }
            yield return new WaitForSeconds(.1f);
            time2 += .1f;
        }
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            spheres[i].transform.localPosition = xyzFromNumber(i);
        }
        lockInput = false;
        Center.text = "Ready.";
        yield return null;
    }

    void generateConfig()
    {
        int[] axis = Enumerable.Range(1,amountOfDimensions).ToList().Shuffle().GetRange(0,amountOfDimensions-1).ToArray();
        bool[] signs = new bool[amountOfDimensions-1];
        for (int i = 0; i < amountOfDimensions-1; i++) signs[i] = Random.Range(0, 2) == 1;
        for (int i = amountOfDimensions-2; i >=0; i--) readableConfig += (signs[i]?"+":"-") + axisNames[axis[i]-1];
        Debug.LogFormat("[Negative-Resistance #{0}] Your answer is: {1}.",ModuleId, readableConfig);
        for (int i = 0; i < amountOfDimensions-1; i++) 
            config[axis[i]-1] = axis[(i + 1) % (amountOfDimensions-1)] * (signs[(i + 1) % (amountOfDimensions-1)] ? 1 : -1);
    }

    void updateText()
    {
        if (currentAnswer.Count == 0)
        {
            Center.text = "Ready.";
            for (int i = 0; i < 1 << amountOfDimensions; i++) spheres[i].GetComponent<MeshRenderer>().material.color = Color.gray;
            return;
        }
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            if (i == currentAnswer.First()) spheres[i].GetComponent<MeshRenderer>().material.color = Color.green;
            else if (i == currentAnswer.Last()) spheres[i].GetComponent<MeshRenderer>().material.color = Color.red;
            else if (currentAnswer.Contains(i)) spheres[i].GetComponent<MeshRenderer>().material.color = Color.white;
            else if ((((i ^ currentAnswer.Last()) - 1) & (i ^ currentAnswer.Last())) == 0)
                spheres[i].GetComponent<MeshRenderer>().material.color = Color.gray;
            else spheres[i].GetComponent<MeshRenderer>().material.color = Color.black;
        }
        string ans = "";
        for (int i = 0; i < currentAnswer.Count-1; i++)
        {
            if (i>0 && i % 5 == 0) ans += "\n";
            int diff = currentAnswer[i + 1] - currentAnswer[i];
            if (diff < 0)
            {
                ans += "-";
                diff *= -1;

            }
            else ans += "+";

            int index = 0;
            while (diff > 1)
            {
                diff >>= 1; index++;
            }
            ans+=axisNames[index];
        }
        Center.text = ans;
    }

    void checkAnswer()
    {
        string[] configs = new string[readableConfig.Length / 2];
        for (int i = 0; i < readableConfig.Length; i+=2) configs[i/2] = readableConfig.Substring(i) + readableConfig.Substring(0, i);
        if (configs.Contains(Center.text.Replace("\n",""))) StartCoroutine(solve());
        else StartCoroutine(strike());
    }

    IEnumerator solve()
    {
        lockInput = true;
        Audio.PlaySoundAtTransform(sounds[0].name, transform);
        Center.text = "";
        yield return new WaitForSeconds(3f);
        List<int> tmp = Enumerable.Range(0, 1<<amountOfDimensions).ToList().Shuffle();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 1 << (amountOfDimensions - 3); j++)
            {
                spheres[tmp[i*(1 << (amountOfDimensions - 3))+j]].SetActive(false);
            }
            Audio.PlaySoundAtTransform(sounds[1].name, transform);
            yield return new WaitForSeconds(.5f);
        }
        Audio.PlaySoundAtTransform(sounds[2].name, transform);
        Center.text = "Negative\nResistance";
        Module.HandlePass();
        yield return null;
    }

    void appendAnswer(int button)
    {
        if (!currentAnswer.Any()) currentAnswer.Add(button);
        else if (currentAnswer.Last() == button) currentAnswer.Remove(button);
        else if (currentAnswer.First() == button) {
            checkAnswer();
            print("answer check");
            return;
        }
        else if (currentAnswer.Contains(button) || currentAnswer.Count>amountOfDimensions-1) return;
        else if ((((button ^ currentAnswer.Last()) - 1) & (button ^ currentAnswer.Last())) != 0)
        {
            return;
        }
        else currentAnswer.Add(button);
        Audio.PlaySoundAtTransform(sounds[0].name, transform);
        updateText();
    }

    void press(int i1)
    {
        if (lockInput) return; 
        if (!secondStage)
        {
            lockInput = true;
            secondStage = true;
            Audio.PlaySoundAtTransform(sounds[1].name, transform);
        }
        else appendAnswer(i1);
    }
    
    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        ModuleId++;
        
        ModConfig<NegativeResSettings> modConfig = new ModConfig<NegativeResSettings>("NegativeResSettings");
        Settings = modConfig.Settings;
        modConfig.Settings = Settings;
        TryOverrideMission();
        amountOfDimensions = Settings.amountOfDimensions < 3 || Settings.amountOfDimensions > axisNames.Length
            ? 6
            : Settings.amountOfDimensions;
        
        config = new int[amountOfDimensions];
        
        scalingFactor = 1f / Enumerable.Range(0,amountOfDimensions).Select(x=>axes[x]).Aggregate((x,y) => x + y).Apply(a =>
        {
            if (a.x >= a.y && a.x >= a.z) return a.x;
            if (a.y >= a.z && a.y >= a.x) return a.y;
            return a.z;
        });
        
        positionVectors = Enumerable.Range(0,1<<amountOfDimensions).Select(xyzFromNumber).ToArray();
        colors = Enumerable.Range(0,1<<amountOfDimensions).Select(
            i => new Color( positionVectors[i].x,positionVectors[i].y,positionVectors[i].z)
            ).ToArray();
        
        
        if (amountOfDimensions<12)
        {
            spheres = Enumerable.Range(0, 1 << amountOfDimensions).Select(x => Instantiate(sphere,sphereParent)).ToArray();
            GetComponent<KMSelectable>().Children = spheres.Select(i=>i.GetComponent<KMSelectable>()).Concat(sphere.GetComponent<KMSelectable>()).ToArray();
            GetComponent<KMSelectable>().UpdateChildrenProperly();
            for (int i = 0; i < 1 << amountOfDimensions; i++)
            {
                spheres[i].transform.localPosition = positionVectors[i];
                spheres[i].transform.localScale = new Vector3(scalingFactor,scalingFactor,scalingFactor) * (.2f * 80f);
                colors[i] = new Color( positionVectors[i].x,positionVectors[i].y,positionVectors[i].z);
                var i1 = i;
                spheres[i1].GetComponent<KMSelectable>().OnInteract += delegate
                {
                    press(i1);
                    return false;
                };
            }
            initialize();
        }
        else
        {
            //foreach (var s in spheres)
            //{
            //    s.transform.localPosition = new Vector3(.5f,0f,.5f);
            //    s.GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
            //    s.transform.localScale = Vector3.zero;
            //}
            Center.text = "?";
            sphere.transform.localPosition = new Vector3(.5f,0f,.5f);
            GetComponent<KMSelectable>().OnInteract += delegate
            {
                if (activatedOnce) return true;
                activatedOnce = true;
                spheres = Enumerable.Range(0, 1 << amountOfDimensions).Select(x => Instantiate(sphere,sphereParent)).ToArray();
                GetComponent<KMSelectable>().Children = spheres.Select(i=>i.GetComponent<KMSelectable>()).Concat(sphere.GetComponent<KMSelectable>()).ToArray();
                GetComponent<KMSelectable>().UpdateChildrenProperly();
                for (int i = 0; i < 1 << amountOfDimensions; i++)
                {
                    //spheres[i].transform.localPosition = positionVectors[i];
                    spheres[i].transform.localScale = new Vector3(scalingFactor,scalingFactor,scalingFactor) * (.2f * 80f);
                    colors[i] = new Color( positionVectors[i].x,positionVectors[i].y,positionVectors[i].z);
                    var i1 = i;
                    spheres[i1].GetComponent<KMSelectable>().OnInteract += delegate
                    {
                        press(i1);
                        return false;
                    };
                }
                foreach (var s in spheres) s.transform.localScale = new Vector3(scalingFactor,scalingFactor,scalingFactor) * (.2f * 80f);
                sphere.SetActive(false);
                Center.text = amountOfDimensions.ToString();
                Audio.PlaySoundAtTransform(we_are_fucked.name, transform);
                StartCoroutine(getReadyForTorture());
                return true;
            };
        }
       
    }

    IEnumerator getReadyForTorture()
    {
        float timer = 0f;
        float final = 4f;
        while (timer < final)
        {
            timer += 0.05f;
            yield return new WaitForSeconds(0.05f);
            for (int i=0; i< 1<<amountOfDimensions; i++)
            {
                spheres[i].transform.localPosition = Vector3.Lerp(new Vector3(0.5f,0f,0.5f), positionVectors[i], timer/final);
                spheres[i].GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(0.5f,0.5f,0.5f), colors[i], timer/final);
            }
        }
        initialize();
    }
    
    private NegativeResSettings Settings = new NegativeResSettings();
    void TryOverrideMission()
    {
        var desc = Game.Mission.Description ?? "";
        Match regexMatchCountVariants = Regex.Match(desc, @"\[Negative-Resistance\]\s(\d+)");
        if (!regexMatchCountVariants.Success) return;
        int? valueMatches = regexMatchCountVariants.Groups[1].Value.TryParseInt();
        if (valueMatches != null) Settings.amountOfDimensions = valueMatches.Value;
    }
    class NegativeResSettings
    {
        public int amountOfDimensions = 6;
    }
    static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "NegativeResSettings.json" },
            { "Name", "Negative-Resistance Settings" },
            { "Listings", new List<Dictionary<string, object>>{
                new Dictionary<string, object>
                {
                    { "Key", "amountOfDimensions" },
                    { "Text", "Dimensions" },
                    { "Description", "Practice with different amount of dimensions. Default is 6." }
                },
            } }
        }
    };
    
    
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} -/+ to press orb on certain axis. For example !{0} ---+++ presses -X-Y-Z+W+V+U. You can chain coordinates with spaces.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
        if (!Command.RegexMatch("^(([-+]{"+amountOfDimensions+"})( |$))+"))
        {
            yield return "sendtochaterror Error";
        }
        else
        {
            foreach (var point in Command.Split(' '))
            {
                while (lockInput) yield return new WaitForSeconds(.3f);
                int x = 0;
                for (int i = 0; i < amountOfDimensions; i++) if (point[i] == '+') x += 1 << i;
                
                
                if (!lockInput)
                {
                    if (!secondStage)
                    {
                        lockInput = true;
                        secondStage = true;
                        Audio.PlaySoundAtTransform(sounds[1].name, transform);
                    }
                    else
                    {
                        if (!currentAnswer.Any()) currentAnswer.Add(x);
                        else if (currentAnswer.Last() == x) currentAnswer.Remove(x);
                        else if (currentAnswer.First() == x) {
                            string[] configs = new string[readableConfig.Length / 2];
                            for (int i = 0; i < readableConfig.Length; i+=2) configs[i/2] = readableConfig.Substring(i) + readableConfig.Substring(0, i);
                            if (configs.Contains(Center.text.Replace("\n","")))
                            {
                                
                                lockInput = true;
                                Audio.PlaySoundAtTransform(sounds[0].name, transform);
                                Center.text = "";
                                yield return new WaitForSeconds(3f);
                                List<int> tmp = Enumerable.Range(0, 1<<amountOfDimensions).ToList().Shuffle();
                                for (int i = 0; i < 8; i++)
                                {
                                    for (int j = 0; j < 1 << (amountOfDimensions - 3); j++)
                                    {
                                        spheres[tmp[i*(1 << (amountOfDimensions - 3))+j]].SetActive(false);
                                    }
                                    Audio.PlaySoundAtTransform(sounds[1].name, transform);
                                    yield return new WaitForSeconds(.5f);
                                }
                                Audio.PlaySoundAtTransform(sounds[2].name, transform);
                                Center.text = "Negative\nResistance";
                                yield return "solve";
                                Module.HandlePass();
                                yield return null;
                            }
                            else
                            {
                                yield return "strike";
                                yield return strike();
                            }
                            print("answer check");
                            continue;
                        }
                        else if (currentAnswer.Contains(x) || currentAnswer.Count>amountOfDimensions-1) continue;
                        else if ((((x ^ currentAnswer.Last()) - 1) & (x ^ currentAnswer.Last())) != 0)
                        {
                            continue;
                        }
                        else currentAnswer.Add(x);
                        Audio.PlaySoundAtTransform(sounds[0].name, transform);
                        updateText();
                    }
                }
                yield return new WaitForSeconds(.5f);
            }
        }
    }

    List<int> getAnswer()
    {
        int start = 0;
        for (int i = 0; i < amountOfDimensions; i++)
            if (readableConfig.Contains("-"+axisNames[i])) start += 1 << i;
        List<int> ans = new List<int>();
        ans.Add(start);
        for (int i = 0; i < readableConfig.Length / 2; i++)
            ans.Add(ans.Last()+
                    (readableConfig[i * 2]=='-'?-1:1)*( 1 << axisNames.IndexOf(readableConfig[i * 2+1]))
            );
        ans.Add(start);
        print(ans.Select(x=>x.ToString()).Aggregate((x,y) => x+" "+y));
        return ans;
    }
    
    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        if (!secondStage) spheres[0].GetComponent<KMSelectable>().OnInteract();
        yield return new WaitForSeconds(0.2f);
        while (lockInput) yield return new WaitForSeconds(0.3f);
        while (currentAnswer.Any())
        { 
            spheres[currentAnswer.Last()].GetComponent<KMSelectable>().OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        foreach (var point in getAnswer())
        {
            spheres[point].GetComponent<KMSelectable>().OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitUntil(() => TwitchShouldSolve);
        yield return solve();
    }
}
