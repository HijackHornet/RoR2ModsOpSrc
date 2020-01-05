using Hj;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HjUpdaterAPI
{
    class DisplayUpdateMessage
    {

        
        public static void DrawUI(float x, float y, float widthSize, float mulY, GUIStyle BGstyle, GUIStyle OnStyle, GUIStyle OffStyle, GUIStyle BtnStyle)
        {
            
            GUI.Box(new Rect(Styles.mainRect.x + 0f, Styles.mainRect.y + 0f, widthSize + 10, 50f + 45 * Styles.MainMulY), "", Styles.MainBgStyle);
            GUI.Button(ButtonManager.BtnRect(1, false, "DisplayUpdate"), "Updating", Styles.LabelStyle);
            GUI.Button(ButtonManager.BtnRect(1, false, "DisplayUpdate"), "Package Name here", Styles.LabelStyle);
        }
    }
}
