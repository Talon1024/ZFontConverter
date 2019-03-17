using System;
using System.Windows.Forms;
namespace ZFontConverter.GUI
{
    // Button used to select font used for the codepage preview
    public class FontSelectButton : Button
    {
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            FontDialog fontDialog = new FontDialog();
            fontDialog.ShowDialog();
        }
    }
}
