using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
public static class TextExtension
{
    /// <summary>
    /// Returns true when the Text object contains more lines of text than will fit in the text container vertically
    /// </summary>
    /// <returns></returns>
    public static bool IsOverflowingVerticle(this Text text)
    {
        return LayoutUtility.GetPreferredHeight(text.rectTransform) > GetCalculatedPermissibleHeight(text);
    }
 
    private static float GetCalculatedPermissibleHeight(Text text)
    {
        if (cachedCalculatedPermissibleHeight != -1) return cachedCalculatedPermissibleHeight;
 
        cachedCalculatedPermissibleHeight = text.gameObject.GetComponent<RectTransform>().rect.height;
        return cachedCalculatedPermissibleHeight;
    }
    private static float cachedCalculatedPermissibleHeight = -1;
 
    /// <summary>
    /// Returns true when the Text object contains more character than will fit in the text container horizontally
    /// </summary>
    /// <returns></returns>
    public static bool IsOverflowingHorizontal(this Text text)
    {
        return LayoutUtility.GetPreferredWidth(text.rectTransform) > GetCalculatedPermissibleHeight(text);
    }
 
    private static float GetCalculatedPermissibleWidth(Text text)
    {
        if (cachedCalculatedPermissiblWidth != -1) return cachedCalculatedPermissiblWidth;
 
        cachedCalculatedPermissiblWidth = text.gameObject.GetComponent<RectTransform>().rect.width;
        return cachedCalculatedPermissiblWidth;
    }
    private static float cachedCalculatedPermissiblWidth = -1;
 
}

public class UnityDebugLog : MonoBehaviour
{
    private Text text;

    void Awake()
    {
        text = gameObject.GetComponent<Text>();
    }

    void OnEnable()
    {
        Application.logMessageReceived += LogMessage;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogMessage;
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                text.text += "<color=red>";
                break;
            case LogType.Assert:
                text.text += "<color=red>";
                break;
            case LogType.Warning:
                text.text += "<color=yellow>";
                break;
            case LogType.Log:
                text.text += "<color=white>";
                break;
            case LogType.Exception:
                text.text += "<color=red>";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        text.text += message + "</color>\n";
        
        if (text.IsOverflowingVerticle())
        {
            // remove first element
            text.text = text.text.Substring(text.text.IndexOf('\n') + 1);
        }
    }
}
