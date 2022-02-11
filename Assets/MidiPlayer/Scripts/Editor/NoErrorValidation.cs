//#define MPTK_PRO
using MidiPlayerTK;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
class NoErrorValidator
{
    static public bool CantChangeAudioConfiguration;
    static NoErrorValidator()
    {
        //Debug.Log("NoErrorValidator");  
        CompilationPipeline.assemblyCompilationStarted += CompileStarted;
        CompilationPipeline.assemblyCompilationFinished += CompileFinish;
#if xxxxxUNITY_IOS
        // Now always enabled but without any garantee!
        Debug.Log("Platform iOS selected, change audio configuration is disabled.");
        CantChangeAudioConfiguration = true;
#else
        CantChangeAudioConfiguration = false;
#endif
    }

    private static void CompileStarted(string obj)
    {
        Debug.Log("Compilation Started...");
        // Better to let Unity doing what is set in Edit / Preferences / Script Changes when playing
        //if (EditorApplication.isPlaying)
        //{
        //    Debug.Log("Stop Playing...");
        //    EditorApplication.isPlaying = false;
        //}
        // in case of a call back has been set, it's mandatory to unset it to avoid crash
#if MPTK_PRO
        MidiKeyboard.MPTK_UnsetRealTimeRead();
#endif
        
//#if UNITY_IOS
//        Debug.Log("Platform iOS selected, change audio configuration is disabled.");
//#endif
#if NET_LEGACY
        Debug.LogWarning(".NET 2.0 is selected, .NET 4.x API compatibility level is recommended.");
#endif
    }

    static private void CompileFinish(string s, CompilerMessage[] compilerMessages)
    {
        Debug.Log("Compilation Finished, error: " + compilerMessages.Count(m => m.type == CompilerMessageType.Error));
        //if (compilerMessages.Count(m => m.type == CompilerMessageType.Error) > 0)
        //EditorApplication.Exit(-1);
        //Debug.Log("compilerMessages:" + compilerMessages.Count(m => m.type == CompilerMessageType.Error));
    }
}