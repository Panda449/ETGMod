﻿#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;
using System;
using Object = UnityEngine.Object;

public class ETGModConsole : IETGModMenu {

    /// <summary>
    /// All commands supported by the ETGModConsole. Add your own commands here!
    /// </summary>
    public static ConsoleCommandGroup Commands = new ConsoleCommandGroup();

    /// <summary>
    /// All items in the game, name sorted. Used for the give command.
    /// </summary>
    public static Dictionary<string, int> AllItems = new Dictionary<string, int>();

    /// <summary>
    /// All console logged text lines. Feel free to add your lines here!
    /// </summary>
    public static List<string> LoggedText = new List<string>();
    /// <summary>
    /// The currently typed in command in the text box.
    /// </summary>
    public static string CurrentTextFieldText = "";

    public static Vector2 MainScrollPos;
    public static Vector2 CorrectScrollPos;

    private Rect _MainBoxRect     = new Rect(16,                 16 , Screen.width - 32, Screen.height - 32 );
    private Rect _InputBox        = new Rect(16, Screen.height - 32 , Screen.width - 32,                 32 );
    private Rect _AutocompletionBox  = new Rect(16, Screen.height - 184, Screen.width - 32,                 120);

    private bool _CloseConsoleOnCommand = false;
    private bool _CutInputFocusOnCommand = false;

    private bool _AutocompleteOnNextFrame = false;

    private bool _NeedCorrectInput = false;

    private string[] _CurrentAutocompletionData = null;

    private static bool _FocusOnInputBox = true;

    public void Start() {


        // GLOBAL NAMESPACE
        Commands
                .AddUnit ("help", delegate(string[] args) {
                    List<List<String>> paths = Commands.ConstructPaths();
                    for (int i = 0; i < paths.Count; i++) {
                        LoggedText.Add(String.Join(" ", paths[i].ToArray()));
                    }
                })
                .AddUnit ("exit", (string[] args) => ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None)
                .AddUnit ("give", GiveItem, new AutocompletionSettings(delegate(string input) {
                    List<String> ret = new List<String>();
                    foreach (string key in AllItems.Keys) {
                        if (key.StartsWith(input)) {
                            ret.Add(key);
                        }
                    }
                    return ret.ToArray();
                }))
                .AddUnit ("screenshake", SetShake)
                .AddUnit ("echo", Echo)
                .AddUnit("tp", Teleport);

        // ROLL NAMESPACE
        Commands.AddUnit ("roll", new ConsoleCommandGroup());

        Commands.GetGroup ("roll")
                .AddUnit ("distance", DodgeRollDistance)
                .AddUnit ("speed", DodgeRollSpeed);

        // CONF NAMESPACE
        Commands.AddUnit ("conf", new ConsoleCommandGroup());

        Commands.GetGroup("conf")
                .AddUnit("close_console_on_command", delegate (string[] args) { _CloseConsoleOnCommand          = SetBool(args, _CloseConsoleOnCommand        );})
                .AddUnit("cut_input_focus_on_command", delegate (string[] args) { _CutInputFocusOnCommand         = SetBool(args, _CutInputFocusOnCommand       ); })
                .AddUnit("enable_damage_indicators", delegate (string[] args) { ETGModGUI.UseDamageIndicators  = SetBool(args, ETGModGUI.UseDamageIndicators); });
    }

    public void Update() {

    }

    public TextEditor GetTextEditor() {
        return (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
    }

    public string[] SplitArgs(string args) {
        return args.Split (new char[] { ' ' });
    }

    public void OnGUI() {

        //GUI.skin=skin;

        //THIS HAS TO BE CALLED TWICE, once on input, and once the frame after!
        //For some reason?....

        if (_NeedCorrectInput) {
            TextEditor texteditor = GetTextEditor ();
            if (texteditor != null) {
                texteditor.MoveTextEnd();
            }
            _NeedCorrectInput=false;
        }

        //Input
        GUI.SetNextControlName("CommandBox");
        string textfieldvalue = GUI.TextField(_InputBox, CurrentTextFieldText);
        if (textfieldvalue != CurrentTextFieldText) {
            _OnTextChanged (CurrentTextFieldText, textfieldvalue);
            CurrentTextFieldText = textfieldvalue;
        }

        if (_FocusOnInputBox) {
            GUI.FocusControl ("CommandBox");
            _FocusOnInputBox = false;
        }

        TextEditor te = GetTextEditor ();

        bool rancommand = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && CurrentTextFieldText.Length > 0;
        if (rancommand) {
            string[] input = SplitArgs(te.text.Trim());
            int commandindex = Commands.GetFirstNonUnitIndexInPath(input);

            List<String> path = new List<String>();
            for (int i = 0; i < input.Length - (input.Length - commandindex); i++) {
                if (!String.IsNullOrEmpty(input[i])) path.Add (input [i]);
            }
           
            List<String> args = new List<String>();
            for (int i = commandindex; i < input.Length; i++) {
                if (!String.IsNullOrEmpty(input[i])) args.Add (input [i]);
            }
            RunCommand (path.ToArray(), args.ToArray());
        }

        _MainBoxRect       = new Rect(16,                      16 , Screen.width - 32, Screen.height - 32 - 29 );
        _InputBox          = new Rect(16, Screen.height - 16 - 24 , Screen.width - 32,                      24 );
        _AutocompletionBox = new Rect(16, Screen.height - 16 - 144, Screen.width - 32,                     120 );

        GUI.Box(_MainBoxRect   , "Console");

        //Logged text
        GUILayout.BeginArea(_MainBoxRect);
        MainScrollPos = GUILayout.BeginScrollView(MainScrollPos);

        for (int i = 0; i < LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (_AutocompleteOnNextFrame) {
            // HACK
            // This is how you set the cursor position...
            te.cursorIndex = te.text.Length;
            te.selectIndex = te.text.Length;

            _AutocompleteOnNextFrame = false;
        }

        if (_CurrentAutocompletionData != null) {
            GUI.Box(_AutocompletionBox, "Autocompletion");

            GUILayout.BeginArea(_AutocompletionBox);
            CorrectScrollPos = GUILayout.BeginScrollView(CorrectScrollPos);

            for(int i = 0; i < _CurrentAutocompletionData.Length; i++) {
                GUILayout.Label(_CurrentAutocompletionData[i]);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // AUTO COMPLETIONATOR 3000
        // (by Zatherz)
        //
        // TODO: Make Tab autocomplete to the shared part of completions
        // TODO: AutocompletionRule interface and class per rule?
        var autocompletionrequested = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab;
        if(autocompletionrequested){
            // Create an input array by splitting it on spaces
            string inputtext = te.text.Substring(0, te.cursorIndex);
            string[] input = SplitArgs(inputtext);
            string otherinput = String.Empty;
            if (te.cursorIndex < te.text.Length) {
                otherinput = te.text.Substring (te.cursorIndex + 1);
            }
            // Get where the command appears in the path so that we know where the arguments start
            int commandindex = Commands.GetFirstNonUnitIndexInPath(input);
            List<String> pathcreation = new List<String>();
            for (int i = 0; i < input.Length - (input.Length - commandindex); i++) {
                pathcreation.Add (input [i]);
            }

            string[] path = pathcreation.ToArray();

            ConsoleCommandUnit unit = Commands.GetUnit (path);
            // Get an array of available completions
            string[] completions = unit.Autocompletion.Match (input [input.Length - 1]);

            if (completions.Length == 1) {
                if (path.Length > 0) {
                    CurrentTextFieldText = String.Join (" ", path) + " " + completions [0] + " " + otherinput;
                } else {
                    CurrentTextFieldText = completions [0] + " " + otherinput;
                }

                _AutocompleteOnNextFrame = true;
            } else if (completions.Length > 1) {
                _CurrentAutocompletionData = completions;
            }
        }

        //Command handling
        if (rancommand && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return) {

            //If this command is valid
            // No new line when we ran a command.
            CurrentTextFieldText="";
            if (_CutInputFocusOnCommand)
                GUI.FocusControl("");
            if (_CloseConsoleOnCommand) {
                ETGModGUI.CurrentMenu=ETGModGUI.MenuOpened.None;
                ETGModGUI.UpdatePlayerState();
            }
        }


    }

    public void OnOpen() { }

    public void OnClose() {
        _FocusOnInputBox = true;
    }

    public void OnDestroy() { }

    private void _OnTextChanged(string previous, string current) {
        _CurrentAutocompletionData = null;
    }

    // Use like this:
    //     if (!ArgCount(args, MIN_ARGS, OPTIONAL_MAX_ARGS)) return;
    // Automatically handles error reporting for you
    // ALL VALUES INCLUSIVE!
    public static bool ArgCount(string[] args, int min) {
        if (args.Length >= min) return true;
        LoggedText.Add ("Error: need at least " + min + " argument(s)");
        return false;
    }

    public static bool ArgCount(string[] args, int min, int max) {
        if (args.Length >= min && args.Length <= max) return true;
        if (min == max) {
            LoggedText.Add ("Error: need exactly " + min + " argument(s)");
        } else {
            LoggedText.Add ("Error: need between " + min + " and " + max + " argument(s)");
        }
        return false;
    }

    /// <summary>
    /// Runs the provided command with the provided args.
    /// </summary>
    public static void RunCommand(string[] unit, string[] args) {
        ConsoleCommand command = Commands.GetCommand (unit);
        if (command == null) {
            if (Commands.GetGroup (unit) == null) {
                LoggedText.Add ("Command doesn't exist");
            } else {
                LoggedText.Add ("Attempted to execute a command group");
            }
        } else {
            try {
                command.RunCommand (args);
            } catch (Exception e) {
                LoggedText.Add (e.ToString ());
            }
        }
        CurrentTextFieldText = string.Empty;
    }

    // Example commands

    void Echo(string[] args) {
        StringBuilder combined = new StringBuilder();
        for (int i = 0; i < args.Length; i++) {
            combined.Append(args[i]);
            if (i < args.Length - 1) {
                combined.Append(' ');
            }
        }
        string str = combined.ToString();
        Debug.Log(str);
        LoggedText.Add(str);
    }

    void DodgeRollDistance(string[] args) {
        if (!ArgCount (args, 1, 1)) return;
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.distance = float.Parse(args[0]);
        }
    }

    void DodgeRollSpeed(string[] args) {
        if (!ArgCount (args, 1, 1)) return;
        Debug.Log(args[0]);

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.rollStats.time = float.Parse(args[0]);
        }
    }

    void Teleport(string[] args) {
        if (!ArgCount (args, 3, 3)) return;

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null) {
            GameManager.Instance.PrimaryPlayer.transform.position = new Vector3(
                float.Parse(args[0]),
                float.Parse(args[1]),
                float.Parse(args[2])
            );
        }
    }

    public bool SetBool(string[] args, bool fallbackValue) {
        if (args.Length!=1)
            return fallbackValue;

        if (args[0].ToLower()=="true") 
            return true;
        else if (args[0].ToLower()=="false") 
            return false;
        else
            return fallbackValue;
    }

    void GiveItem(string[] args) {
        if (!ArgCount (args, 1, 2)) return;

        if (!GameManager.Instance.PrimaryPlayer) {
            LoggedText.Add ("Couldn't access Player Controller");
            return;
        }

        int id = -1;

        try {
            id = int.Parse(args[0]);
        }
        catch {
            //Arg isn't an id, so it's probably an item name.
            // Are you Brent?
            id = AllItems[args[0]];
        }

        if (id==-1) {
            LoggedText.Add("Invalid item ID/name!");
            return;
        }

        LoggedText.Add ("Attempting to spawn item ID " + args[0] + " (numeric " + id.ToString() + ")" + ", class " + PickupObjectDatabase.GetById (id).GetType());

        if (args.Length == 2) {
            int count = int.Parse(args[1]);

            for (int i = 0; i<count; i++)
                ETGMod.Player.GiveItemID(id);
        } else {
            ETGMod.Player.GiveItemID(id);
        }
    }

    void SetShake(string[] args) {
        if (!ArgCount (args, 1, 1)) return;
        LoggedText.Add ("Vlambeer set to " + args[0]);
        ScreenShakeSettings.GLOBAL_SHAKE_MULTIPLIER = float.Parse (args [0]);
    }

}

