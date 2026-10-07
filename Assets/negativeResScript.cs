using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KeepCoding;
using UnityEngine;
using static negativeResSpace.NegativeResistanceExt;



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
    private bool TwitchPlaysActive;
    
    static int ModuleIdCounter;
    int ModuleId;
    private bool secondStage;
    private List<int> config;
    private List<int> shifts;
    private List<int> currentPosition;
    private Color[] colors;
    private Vector3[] positionVectors;
    private Vector3[] shiftedPositionVectors;
    private readonly string axisNames = "XYZWVURSTOPQLMNIJK"; //i'm actually making nres for higher dimensions ho lee sheet
    private List<List<int>> currentAnswer = new List<List<int>>{new List<int>()};
    private bool lockInput;

    private int amountOfDimensions;
    private int amountOfSubrotations = 1;

    private readonly Vector3[] axes = {
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

    private bool allSpheresLoaded;

    public AudioClip we_are_fucked;

    void initialize(){
        sphere.SetActive(false);
        secondStage = false;
        currentAnswer = new List<List<int>>();
        config = Enumerable.Range(1, amountOfDimensions).ToList();
        generateConfig();
        Center.text = config.Count.ToString();
        shifts = positionsFromConfig();
        currentPosition = Enumerable.Range(0, shifts.Count).ToList();
        shiftedPositionVectors = Enumerable.Range(0, 1<<amountOfDimensions).Select(i=>xyzFromNumber(shifts[i])).ToArray();
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            spheres[i].GetComponent<MeshRenderer>().material.color = colors[i];
        }
        StartCoroutine(move());
    }

    IEnumerator strike(){
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
        Center.text = config.Count.ToString();
        lockInput = false;
        
        
        secondStage = false;
        shifts = positionsFromConfig();
        currentPosition = Enumerable.Range(0, shifts.Count).ToList();
        shiftedPositionVectors = Enumerable.Range(0, 1<<amountOfDimensions).Select(i=>xyzFromNumber(shifts[i])).ToArray();
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            spheres[i].GetComponent<MeshRenderer>().material.color = colors[i];
        }
        StartCoroutine(move());
    }
    List<int> positionsFromConfig(){
        List<int> ans = new List<int>(1 << amountOfDimensions);
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            for (int j = 0; j < amountOfDimensions; j++)
            {
                if ((config[j] < 0) ^ (((1 << j) & i)!=0)) ans[i] += 1<<(config[j]*(config[j]<0?-1:1)-1); //AOOR exception
            }
        }
        return ans;
    }
    Vector3 xyzFromNumber(int num){
        return scalingFactor * Enumerable.Range(0, amountOfDimensions).Where(x => (num & (1 << x)) != 0)
            .Select(x => axes[x]).Concat(new Vector3(0, 0, 0)).Aggregate((a, b) => a + b);
    }
    IEnumerator move(){
        while (!secondStage)
        {
            float time = 0f;
            while (time < 1f)
            {
                for (int i = 0; i < 1 << amountOfDimensions; i++)
                    spheres[i].transform.localPosition = Vector3.Lerp(
                        positionVectors[currentPosition[i]],
                        shiftedPositionVectors[currentPosition[i]], time);
                yield return new WaitForEndOfFrame();
                time += Time.deltaTime;
            }
            List<int> tmp = currentPosition.ToList();
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

    void generateConfig(){
        List<List<int>> initialArray;
        List<int> ignored;
        do{
            initialArray = Enumerable.Range(0,amountOfSubrotations).Select(_=>new List<int>()).ToList();
            ignored = new List<int>();
            for (int i=0; i<amountOfDimensions; i++){
                if (UnityEngine.Random.value < 1f / amountOfDimensions) ignored.Add(i);
                else initialArray[UnityEngine.Random.Range(0,amountOfSubrotations)].Add(i);
            }
        } while (ignored.Count == amountOfDimensions);
        initialArray = initialArray.Select(x => x.Shuffle().ToList()).ToList();
        List<int> axis = Enumerable.Repeat(0, amountOfDimensions).ToList();
        List<string> readableSubrotations = new List<string>();
        foreach(var i in ignored) axis[i] = i + 1;
        foreach(var list in initialArray){
            if (list.Count == 1){
                axis[list[0]] = -(list[0]+1);
                readableSubrotations.Add($"-{axisNames[list[0]-1].ToString()}");
            }
            else{
                List<string> readableAxes = new List<string>();
                for (int i = 0; i<list.Count; i++){
                    axis[list[i]] = (list[(i+1)%list.Count] + 1) * UnityEngine.Random.value<.5f?-1:1;
                    readableAxes.Add((axis[list[i]] < 0 ? "-" : "+") + axisNames[list[i]]);
                }
                readableSubrotations.Add(readableAxes.AsEnumerable().Aggregate((a,b)=>b+a));
            }
        }
        Debug.Log($"[Negative-Resistance #{ModuleId}] Your answer is: {readableSubrotations.Aggregate((a,b)=> $"{a}, {b}")}.");
        config = axis.ToList();
    }

    string getStringFromButtonList(List<int> buttons){
        if (buttons.Count < 2) return "";
        if (buttons.First()== buttons.Last()) buttons = buttons.SkipLast(1).ToList();
        List<int> masks = Enumerable.Range(0,buttons.Count - 1).Select(i => buttons[i] ^ buttons[i+1]).Select(m => {
            if (m==0) return -1;
            int ans = 0;
            while ((m & 1) == 0) {ans++; m<<=1;}
            return m==1?ans:-1;
        }).ToList();
        if(masks.Any(x=>x==-1)) return "!";
        return masks.Select((m,i) => ((buttons[i + 1] & (1 << m)) == 1 ? "+" : "-")+axisNames[m]).Aggregate((a,b)=>a+b);
    }

    void updateText(){
        if (currentAnswer.Last().Count == 0)
        {
            if (currentAnswer.Count == 1){
                Center.text = "Ready.";
                for (int i = 0; i < 1 << amountOfDimensions; i++) spheres[i].GetComponent<MeshRenderer>().material.color = Color.gray;
            }
            else{
                Center.text = currentAnswer.SkipLast(1).Select(getStringFromButtonList).Aggregate((a,b)=>$"{a}, {b}").ToCharArray().ToList()
                .Chunk(10).Select(l => l.Select(c => c.ToString()).Aggregate((a,b)=> a+b)).Aggregate((a,b)=>$"{a}\n{b}");  // yeah...
                for (int i = 0; i < 1 << amountOfDimensions; i++) spheres[i].GetComponent<MeshRenderer>().material.color = Color.gray;
                foreach(var list in currentAnswer)
                {
                    foreach (var t in list)
                        spheres[t].GetComponent<MeshRenderer>().material.color = Color.blue;
                    spheres[list[0]].GetComponent<MeshRenderer>().material.color = Color.magenta;
                }
            }
            return;
        }
        for (int i = 0; i < 1 << amountOfDimensions; i++)
        {
            if (i == currentAnswer.Last().First()) spheres[i].GetComponent<MeshRenderer>().material.color = Color.green;
            else if (i == currentAnswer.Last().Last()) spheres[i].GetComponent<MeshRenderer>().material.color = Color.red;
            else if (currentAnswer.Last().Contains(i)) spheres[i].GetComponent<MeshRenderer>().material.color = Color.white;
            else if ((((i ^ currentAnswer.Last().Last()) - 1) & (i ^ currentAnswer.Last().Last())) == 0)
            {
                if (currentAnswer.SkipLast(1).SelectMany(x=>x).ToList().Contains(i))
                    spheres[i].GetComponent<MeshRenderer>().material.color = Color.gray / 4;
                else spheres[i].GetComponent<MeshRenderer>().material.color = Color.gray;
            }
            else spheres[i].GetComponent<MeshRenderer>().material.color = Color.black;
        }
        Center.text = currentAnswer.Select(getStringFromButtonList).Aggregate((a,b)=>$"{a}, {b}").ToCharArray().ToList()
                .Chunk(10).Select(l => l.Select(c => c.ToString()).Aggregate((a,b)=> a+b)).Aggregate((a,b)=>$"{a}\n{b}");
    }

    bool checkAnswer(){
        List<int> ans = Enumerable.Range(1, amountOfDimensions).ToList();
        foreach(var list in currentAnswer){
            if (list.Count == 0) continue;
            if (list.Count < 3 || list.First() != list.Last()) return false; 
            List<int> masks = Enumerable.Range(0,list.Count - 1).Select(i => list[i] ^ list[i+1]).Select(m => {
                if (m==0) return -1;
                int ansi = 0;
                while ((m & 1) == 0) {ansi++; m<<=1;}
                return m==1?ansi:-1;
            }).ToList();
            if (masks.Any(x=>x==-1)) return false;
            masks = masks.Select((m,i)=>(list[i+1] & (1 << m))==1?m+1:-m-1).ToList();
            for (int i=0; i<masks.Count; i++){
                ans[Math.Abs(masks[(i+1)%masks.Count])-1] = masks[i];
            }
        }
        return ans.All((x,i)=>x == config[i]);
    }

    IEnumerator solve(){
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

    void appendAnswer(int button){
        if (!currentAnswer.Last().Any()) {
            if (currentAnswer.Count() > 1){
                if (currentAnswer.SelectMany(x=>x).ToList().Contains(button)){
                    if(currentAnswer.Select(x=>x.First()).ToList().Contains(button)){
                        currentAnswer.RemoveAt(Enumerable.Range(0,currentAnswer.Count()).First(i => currentAnswer[i].First()==button));
                        Audio.PlaySoundAtTransform(sounds[0].name, transform);
                        updateText();
                    }else
                    {
                        StartCoroutine(checkAnswer() ? solve() : strike());
                        print("answer check");
                    }
                    return;
                }
            }
            currentAnswer.Last().Add(button);
            Audio.PlaySoundAtTransform(sounds[0].name, transform);
        }
        else{
            if (currentAnswer.SkipLast(1).SelectMany(x=>x).Contains(button)) return;
            else if (currentAnswer.Last().Last() == button) currentAnswer.Last().Remove(button);
            else if (currentAnswer.Last().First() == button) {
                currentAnswer.Last().Add(button);
                currentAnswer.Add(new List<int>());
            }
            else if (currentAnswer.Last().Contains(button) || currentAnswer.Last().Count>amountOfDimensions-1) return;
            else if ((((button ^ currentAnswer.Last().Last()) - 1) & (button ^ currentAnswer.Last().Last())) != 0)
            {
                return;
            }
            else currentAnswer.Last().Add(button);
            Audio.PlaySoundAtTransform(sounds[0].name, transform);
            updateText();
        }
    }

    void press(int i1){
        if (lockInput) return; 
        if (!secondStage)
        {
            lockInput = true;
            secondStage = true;
            Audio.PlaySoundAtTransform(sounds[1].name, transform);
        }
        else appendAnswer(i1);
    }
    
    void Awake(){
        ModuleId = ModuleIdCounter++;
        ModuleId++;
        
        ModConfig<NegativeResSettings> modConfig = new ModConfig<NegativeResSettings>("NegativeResSettings");
        Settings = modConfig.Settings;
        modConfig.Settings = Settings;
        TryOverrideMission();
        amountOfDimensions = Settings.amountOfDimensions < 3 || Settings.amountOfDimensions > axisNames.Length
            ? 6
            : Settings.amountOfDimensions;
        amountOfSubrotations = Settings.amountOfSubrotations < 1 || Settings.amountOfSubrotations > axisNames.Length
            ? 1
            : Settings.amountOfSubrotations;        
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
                StartCoroutine(generateSpheres());
                
                foreach (var s in spheres) s.transform.localScale = new Vector3(scalingFactor,scalingFactor,scalingFactor) * (.2f * 80f);
                sphere.SetActive(false);
                Center.text = amountOfDimensions.ToString();
                Audio.PlaySoundAtTransform(we_are_fucked.name, transform);
                StartCoroutine(getReadyForTorture());
                return true;
            };
        }
       
    }

    IEnumerator generateSpheres()
    {
        yield return null;
        spheres = Enumerable.Range(0, 1 << amountOfDimensions).Select(x => Instantiate(sphere,sphereParent)).ToArray();
        GetComponent<KMSelectable>().Children = spheres.Select(i=>i.GetComponent<KMSelectable>()).Concat(sphere.GetComponent<KMSelectable>()).ToArray();
        GetComponent<KMSelectable>().UpdateChildrenProperly();
        allSpheresLoaded = true;
    }

    IEnumerator getReadyForTorture(){
        float timer = 0f;
        float final = 4f;
        while (timer < final)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            for (int i=0; i<spheres.Length; i++)
            {
                spheres[i].transform.localPosition = Vector3.Lerp(new Vector3(0.5f,0f,0.5f), positionVectors[i], timer/final);
                spheres[i].GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(0.5f,0.5f,0.5f), colors[i], timer/final);
            }
        }
        yield return new WaitUntil(()=>allSpheresLoaded);
        for (int i = 0; i < spheres.Length; i++)
        {
            var i1 = i;
            spheres[i1].GetComponent<KMSelectable>().OnInteract += delegate
            {
                press(i1);
                return false;
            };
        }
        initialize();
    }
    
    private NegativeResSettings Settings = new NegativeResSettings();
    void TryOverrideMission()
    {
        var desc = Game.Mission.Description ?? "";
        Match regexMatchCountVariants = Regex.Match(desc, @"\[Negative-Resistance\]\s(\d+)\s(\d+)");
        if (!regexMatchCountVariants.Success) return;
        int? valueMatches1 = regexMatchCountVariants.Groups[1].Value.TryParseInt();
        if (valueMatches1 != null) Settings.amountOfDimensions = valueMatches1.Value;
        int? valueMatches2 = regexMatchCountVariants.Groups[2].Value.TryParseInt();
        if (valueMatches2 != null) Settings.amountOfSubrotations = valueMatches2.Value;
    }
    class NegativeResSettings
    {
        public int amountOfDimensions = 6;
        public int amountOfSubrotations = 1;
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
                new Dictionary<string, object>
                {
                    { "Key", "amountOfSubrotations" },
                    { "Text", "Subrotations" },
                    { "Description", "Max amount of subrotations within one rotation. Default is 1." }
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
                yield return "solve";
                yield return "strike";
                appendAnswer(x);
            }
            yield return new WaitForSeconds(.5f);
        }
    }

    List<int> getAnswer()
    {
        //input: List<int> config.
        List<List<int>> transformNotation = new List<List<int>>();
        List<int> configCopy = config.Select((x,i)=>x-1==i?0:x).ToList();
        while(configCopy.Any(x=>x!=0)){
            transformNotation.Add(new List<int>());
            int startIndex = Math.Abs(configCopy.First(x => x!=0))-1;
            int currentIndex = startIndex;
            do{
                transformNotation.Last().Add(configCopy[currentIndex]);
                int previousIndex = currentIndex;
                currentIndex = Math.Abs(configCopy[currentIndex])-1;
                configCopy[previousIndex] = 0;
            } while(currentIndex!=startIndex);
            transformNotation.Last().Reverse();
        }
        /*
        [-2,6,-4,7,-5,-1,-3,-8]; start = 1 -> tN = [[6]], curr = 5 ->
        [-2,0,-4,7,-5,-1,-3,-8]; tN = [[6, -1]], curr = 0 ->
        [-2,0,-4,7,-5,0,-3,-8]; tN = [[6, -1, -2]], curr = 1.
        [0,0,-4,7,-5,0,-3,-8]; start = 3 -> tN = [[-2,-1,6],[7]], curr = 6 ->
        [0,0,-4,0,-5,0,-3,-8]; tN = [[-2,-1,6],[7, -3]], curr = 2 ->
        [0,0,-4,0,-5,0,0,-8]; tN = [[-2,-1,6],[7, -3, -4]], curr = 3.
        [0,0,0,0,-5,0,0,-8]; start = 4 -> tN = [[-2,-1,6],[-4,-3,7],[-5]], curr = 4.
        [0,0,0,0,0,0,0,-8]; start = 7 -> tN = [[-2,-1,6],[-4,-3,7],[-5],[-8]], curr = 7.
        [0,0,0,0,0,0,0,0] -> tN = [[-2,-1,6],[-4,-3,7],[-5],[-8]] => -Y-X+U, -W-Z+R, -V, -S.
        */
        int definedMask = 0;
        List<int> answer = new List<int>();
        foreach(var subtransform in transformNotation.OrderByDescending(x=>x.Count()).Where(x=>x.Count()>1).ToList()){
            int startingPoint = definedMask + subtransform.Where(x=>x<0).Select(x => 1 << (-x-1)).Sum();
            definedMask +=  ((subtransform[0]<0)?1<<(Math.Abs(subtransform[0])-1):0) + 
                            ((subtransform[1]>0)?1<<(Math.Abs(subtransform[1])-1):0);
            answer.Add(startingPoint);
            foreach(var axis in subtransform){
                answer.Add(answer.Last() ^ (1 << Math.Abs(axis)-1));
            }
            answer.Add(startingPoint);
        }
        //configCopy = Enumerable.Range(0,amountOfDimensions).Select(i=> transformNotation.Where(x=>x.Count()==1).SelectMany(x=>x).Contains(i)?-i:0).ToList();
        configCopy = Enumerable.Range(0,amountOfDimensions).Select(i => transformNotation.Any(x => x.Count == 1 && x[0] == -(i+1)) ? -(i+1) : 0).ToList();
        answer = addOneAxisSubtransformation(answer, configCopy);
        if (answer == null) print("Negative-Resistance encountered a case where it didn't find a solution. Report this to rand06.");
        return answer;
    }

    List<int> addOneAxisSubtransformation(List<int> answerSoFar, List<int> config){
        if (config.All(x=>x==0)) return answerSoFar;
        int axisToAdd = -config.First(x => x!=0) - 1;
        List<int> bannedVertices = answerSoFar.Union(answerSoFar.Select(x=> x ^ (1 << axisToAdd))).ToList();
        if (bannedVertices.Count == (1 << amountOfDimensions)) return null;
        //List<int> possibleStartValues = Enumerable.Range(0,1<<amountOfDimensions).Where(x=> (((x&(1<<axisToAdd))==0) ^ (config[axisToAdd]<0)) && (!bannedVertices.Contains(x))).ToList();
        //foreach(var start in possibleStartValues){
        for(int start = 0; start < (1 << amountOfDimensions); start++){
            if ((((start&(1<<axisToAdd))==0) == (config[axisToAdd]<0)) || (bannedVertices.Contains(start))) continue;
            List<int> answer = addOneAxisSubtransformation(answerSoFar.Concat(new List<int>{start, start ^ (1 << axisToAdd), start}).ToList(), config.Select((x,i)=>i==axisToAdd?0:x).ToList());
            if (answer!=null) return answer;
        }
        return null;
    }
    
    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        if (!secondStage) spheres[0].GetComponent<KMSelectable>().OnInteract();
        yield return new WaitForSeconds(0.2f);
        while (lockInput) yield return new WaitForSeconds(0.3f);
        while (currentAnswer.Any())
        { 
            spheres[currentAnswer.Last(x => x.Count>0).Last()].GetComponent<KMSelectable>().OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        foreach (var point in getAnswer())
        {
            spheres[point].GetComponent<KMSelectable>().OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        yield return solve();
    }
}
