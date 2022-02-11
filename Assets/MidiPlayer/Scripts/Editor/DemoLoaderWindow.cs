using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MidiPlayerTK
{
    /// <summary>
    /// Window editor for selecting a demo in editor mode
    /// </summary>
    public class DemoLoaderWindow : EditorWindow
    {
        private static DemoLoaderWindow window;

        Vector2 scrollPosSoundFont = Vector2.zero;

        static float espace = 5;

        static GUIStyle styleBold;
        static GUIStyle styleButton;
        static GUIStyle styleLabelLeft;
        static GUIStyle styleListCenter;

        static bool activLog = false;

        List<ToolsEditor.DefineColumn> ListDemos;

        [MenuItem("MPTK/Load Demonstration &D", false, 50)] // The MenuItem's are sorted in increasing order and if you add more then 10 between two items (so, create at 10, 30, 50,...    ), an Separator-Line is drawn before the menuitem.
        public static void Init()
        {
            //if (Application.isPlaying)
            //    return;

            // Get existing open window or if none, make a new one:
            try
            {
                window = GetWindow<DemoLoaderWindow>(true, "Demonstration Loader - Version: " + ToolsEditor.version);
                window.minSize = new Vector2(1116, 300);

                styleBold = new GUIStyle(EditorStyles.boldLabel);
                styleBold.fontStyle = FontStyle.Bold;
                styleBold.alignment = TextAnchor.UpperLeft;
                styleBold.normal.textColor = Color.black;

                styleButton = new GUIStyle("button");

                styleListCenter = new GUIStyle(EditorStyles.textField);
                styleListCenter.alignment = TextAnchor.MiddleCenter;
                styleListCenter.wordWrap = true;

                styleLabelLeft = new GUIStyle(EditorStyles.textField);
                styleLabelLeft.alignment = TextAnchor.MiddleLeft;
                styleLabelLeft.wordWrap = true;
                styleLabelLeft.richText = true;

                Demonstrator.Load();
            }
            catch (System.Exception ex)
            {
                if (activLog)
                    Debug.Log(ex.ToString());
            }
        }

        private void OnEnable()
        {

        }
        /// <summary>
        /// Reload data
        /// </summary>
        private void OnFocus()
        {
            // Load description of available soundfont
            try
            {
                Init();
            }
            catch (Exception ex)
            {
                if (activLog)
                    Debug.Log(ex.ToString());
            }
        }

        void OnGUI()
        {
            try
            {
                //if (Application.isPlaying)
                //    window.Close();

                if (window == null)
                    Init();

                float startx = 0;
                float starty = 0;

                // Background dark gray
                float gray5 = 0.5f;
                GUI.color = new Color(gray5, gray5, gray5);
                GUI.Box(new Rect(0, 0, window.position.width, window.position.height), "", EditorStyles.helpBox);

                //Debug.Log(window.position.size);
                // Display list of soundfont already loaded 
                ShowListDemos(startx, starty, window.position.size.x - espace - startx, window.position.size.y - starty - espace);

            }
            catch (ExitGUIException) { }
            catch (Exception ex)
            {
                if (activLog)
                    Debug.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Display, add, remove Soundfont
        /// </summary>
        private void ShowListDemos(float startX, float startY, float width, float height)
        {
            try
            {
                if (ListDemos == null)
                {
                    ListDemos = new List<ToolsEditor.DefineColumn>();
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 20, Caption = "#" });
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 180, Caption = "Load" });
                    //ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 50, Caption = "Load" });
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 50, Caption = "Version" });
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 360, Caption = "Description" });
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 180, Caption = "Scene Name" });
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 180, Caption = "Main Scripts" });
                    ListDemos.Add(new ToolsEditor.DefineColumn() { Width = 130, Caption = "Class or Prefab" });
                }
                float titleHeight = 25;
                float itemContentHeight = 70;
                float gray = 0.65f;

                GUI.color = Color.white;
                float localstartX = 0;
                float localstartY = 0;

                // Title
                GUI.color = new Color(gray, gray, gray);
                float boxX = startX + localstartX;
                foreach (ToolsEditor.DefineColumn column in ListDemos)
                {
                    EditorGUI.LabelField(new Rect(boxX + column.PositionCaption, startY + localstartY, column.Width, titleHeight), column.Caption, styleListCenter);
                    boxX += column.Width - 1;
                }
                GUI.color = Color.white;

                localstartY += titleHeight + espace;

                // Content
                Rect listVisibleRect = new Rect(
                    startX + localstartX,
                    startY + localstartY - 6,
                    width - 10,
                    height - localstartY);
                Rect listContentRect = new Rect(
                    0, 0,
                    width - 25,
                    (Demonstrator.Demos.Count - 1) * itemContentHeight);

                scrollPosSoundFont = GUI.BeginScrollView(listVisibleRect, scrollPosSoundFont, listContentRect, false, true);
                float boxY = 0;

                // Loop on each demo (pass first, it's the title)
                for (int i = 1; i < Demonstrator.Demos.Count; i++)
                {
                    Demonstrator sf = Demonstrator.Demos[i];

                    // Start content position (from the visible rect)
                    boxX = 0;
                    float colw = 0;
                    int col = 0;

                    // Id
                    colw = ListDemos[col].Width;
                    EditorGUI.LabelField(new Rect(boxX, boxY, colw, itemContentHeight), i.ToString(), styleListCenter);
                    boxX += colw - 1; // -1 to avoid double border
                    col++;

                    // title + load
                    colw = ListDemos[col].Width;
                    if (GUI.Button(new Rect(boxX, boxY, colw, itemContentHeight), sf.Title, styleButton))
                    {
                        if (Application.isPlaying)
                            EditorUtility.DisplayDialog("Load a Scene", "Can't load a scnene when running", "ok");
                        else
                        {
                            string freepro = sf.Version == "Free" ? "FreeDemos" : "ProDemos";
                            string scenePath = $"Assets/MidiPlayer/Demo/{freepro}/{sf.SceneName}.unity";
                            try
                            {
                                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                                EditorSceneManager.OpenScene(scenePath);
                            }
                            catch (Exception ex)
                            {
                                Debug.Log(ex.ToString());
                                PopupWindow.Show(new Rect(boxX, boxY, colw, itemContentHeight), new GetFullVersion());
                            }
                        }
                    }
                    boxX += colw - 1; // -1 to avoid double border
                    col++;

                    // version
                    colw = ListDemos[col].Width;
                    EditorGUI.LabelField(new Rect(boxX, boxY, colw, itemContentHeight), sf.Version, styleListCenter);
                    boxX += colw - 1;
                    col++;

                    // Description
                    colw = ListDemos[col].Width;
                    EditorGUI.LabelField(new Rect(boxX, boxY, colw, itemContentHeight), sf.Description, styleLabelLeft);
                    boxX += colw - 1;
                    col++;

                    // SceneName
                    colw = ListDemos[col].Width;
                    EditorGUI.LabelField(new Rect(boxX, boxY, colw, itemContentHeight), sf.SceneName, styleLabelLeft);
                    boxX += colw - 1;
                    col++;

                    // ScripName
                    colw = ListDemos[col].Width;
                    EditorGUI.LabelField(new Rect(boxX, boxY, colw, itemContentHeight), sf.ScripName, styleLabelLeft);
                    boxX += colw - 1;
                    col++;

                    // Prefab or class
                    colw = ListDemos[col].Width;
                    EditorGUI.LabelField(new Rect(boxX, boxY, colw, itemContentHeight), sf.PrefabClass, styleLabelLeft);
                    boxX += colw - 1;
                    col++;

                    boxY += itemContentHeight - 1;

                }
                GUI.EndScrollView();
            }
            catch (ExitGUIException) { }
            catch (Exception ex)
            {
                if (activLog)
                    Debug.Log(ex.ToString());
            }
        }

        class Demonstrator
        {
            public string Title;
            public string Description;
            public string SceneName;
            public string ScripName;
            public string PrefabClass;
            public string Version;
            public bool Pro;
            public static List<Demonstrator> Demos;

            public static void Load()
            {
                try
                {
                    Demos = new List<Demonstrator>();
                    //TextAsset mytxtData = Resources.Load<TextAsset>("DemosList");
                    //string text = System.Text.Encoding.UTF8.GetString(mytxtData.bytes);
                    //text = text.Replace("\n", "");
                    //text = text.Replace("\\n", "\n");
                    string text = ToolsEditor.ReadTextFile(Application.dataPath + "/MidiPlayer/DemosList.csv");

                    string[] listDemos = text.Split('\n');
                    if (listDemos != null)
                    {
                        foreach (string demo in listDemos)
                        {
                            string[] colmuns = demo.Split(';');
                            if (colmuns.Length >= 5)
                                Demos.Add(new Demonstrator()
                                {
                                    Title = colmuns[0].Replace("\\n", "\n"),
                                    Description = colmuns[1].Replace("\\n", "\n"),
                                    SceneName = colmuns[2],
                                    ScripName = colmuns[3].Replace("\\n", "\n"),
                                    PrefabClass = colmuns[4].Replace("\\n", "\n"),
                                    Version = colmuns[5],
                                    Pro = colmuns[5] == "Pro" ? true : false
                                });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("Error loading demonstrator " + ex.Message);
                    throw;
                }
            }
        }
    }
}
