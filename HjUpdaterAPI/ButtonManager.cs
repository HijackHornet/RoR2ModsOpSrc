using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HjUpdaterAPI
{
    class ButtonManager
    {
        public static Rect BtnRect(int y, bool isMultButton, string buttonType)
        {
            if (buttonType.Equals("DisplayUpdate"))
            {

                Styles.MainMulY = y;
                if (isMultButton)
                {
                    return new Rect(Styles.mainRect.x + 5, Styles.mainRect.y + 5 + 45 * y, Styles.widthSize - 90, 40);
                }
                else
                {
                    return new Rect(Styles.mainRect.x + 5, Styles.mainRect.y + 5 + 45 * y, Styles.widthSize, 40);
                }


            }
            /*else if (buttonType.Equals("DisplayYesNo"))
            {
                Main.StatMulY = y;
                if (isMultButton)
                {
                    return new Rect(Main.statRect.x + 5, Main.statRect.y + 5 + 45 * y, Main.widthSize - 90, 40);
                }
                else
                {
                    return new Rect(Main.statRect.x + 5, Main.statRect.y + 5 + 45 * y, Main.widthSize - 150, 40);
                }
            }
            */
            else
            {
                return new Rect(Styles.mainRect.x + 5, Styles.mainRect.y + 5 + 45 * y, Styles.widthSize - 90, 40);
            }
        }
    }
}

