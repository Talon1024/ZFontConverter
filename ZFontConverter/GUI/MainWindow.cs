using System;
using System.IO;
using System.Windows.Forms;

namespace ZFontConverter.GUI
{
    public class MainWindow
    {
        private FontFormat fontFile;
        // private Font codepageFont;
        // private PictureBox codepagePicture;
        private PictureBox fontPicture;
        // private ListBox codeList;
        private Form mainWindow;
        // private List<Encoding> encodings;
        // private static byte MinCharByte = 1;
        private Label outDirLabel;
        private Label fontNameLabel;
        private string outputDirectory;
        private string fontFileName;

        public MainWindow()
        {
            mainWindow = new Form();

            var allSidesAnchorStyle = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            TableLayoutPanel layoutPanel = new TableLayoutPanel();
            layoutPanel.Anchor = allSidesAnchorStyle;
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.ColumnCount = 2;
            layoutPanel.RowCount = 4;

            /*
            codeList = new ListBox();
            codeList.SelectionMode = SelectionMode.One;
            codeList.Anchor = allSidesAnchorStyle;
            encodings = new List<Encoding>(Encoding.GetEncodings().Select(
            (encodingInfo) => {
                return Encoding.GetEncoding(encodingInfo.CodePage);
            }).Where((encoding) => encoding.IsSingleByte));
            codeList.BeginUpdate();
            foreach (var codePage in encodings)
            {
                codeList.Items.Add(codePage.EncodingName);
            }
            codeList.EndUpdate();
            codeList.Dock = DockStyle.Fill;
            codeList.SelectedIndexChanged += CodeList_SelectedIndexChanged;
            layoutPanel.Controls.Add(codeList);
            layoutPanel.SetRow(codeList, 1);
            layoutPanel.SetColumn(codeList, 1);
            layoutPanel.SetColumnSpan(codeList, 2);
            */

            /*
            FontSelectButton fontButton = new FontSelectButton();
            fontButton.Text = "Codepage preview font";
            fontButton.Anchor = allSidesAnchorStyle;
            fontButton.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(fontButton);
            layoutPanel.SetRow(fontButton, 2);
            layoutPanel.SetColumn(fontButton, 1);
            */

            FileSelectButton fileButton = new FileSelectButton();
            fileButton.Text = "Font to convert";
            fileButton.Anchor = allSidesAnchorStyle;
            fileButton.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(fileButton);
            layoutPanel.SetRow(fileButton, 0);
            layoutPanel.SetColumn(fileButton, 0);

            fontNameLabel = new Label();
            fontNameLabel.Anchor = allSidesAnchorStyle;
            fontNameLabel.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(fontNameLabel);
            layoutPanel.SetRow(fontNameLabel, 0);
            layoutPanel.SetColumn(fontNameLabel, 1);

            OutDirSelectButton outDirSelectButton = new OutDirSelectButton();
            outDirSelectButton.Text = "Output directory";
            outDirSelectButton.Anchor = allSidesAnchorStyle;
            outDirSelectButton.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(outDirSelectButton);
            layoutPanel.SetRow(outDirSelectButton, 1);
            layoutPanel.SetColumn(outDirSelectButton, 0);

            outDirLabel = new Label();
            outDirLabel.Anchor = allSidesAnchorStyle;
            outDirLabel.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(outDirLabel);
            layoutPanel.SetRow(outDirLabel, 1);
            layoutPanel.SetColumn(outDirLabel, 1);

            Button convertButton = new Button();
            convertButton.Text = "Convert!";
            convertButton.Anchor = allSidesAnchorStyle;
            convertButton.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(convertButton);
            layoutPanel.SetRow(convertButton, 2);
            layoutPanel.SetColumn(convertButton, 0);

            /*
            codepagePicture = new PictureBox();
            codepagePicture.Anchor = allSidesAnchorStyle;
            codepagePicture.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(codepagePicture);
            layoutPanel.SetRow(codepagePicture, 3);
            layoutPanel.SetColumn(codepagePicture, 1);
            fontButton.FontSelected += FontButton_FontSelected;
            codepagePicture.Paint += CodepagePicture_Paint;
            */

            fontPicture = new PictureBox();
            fontPicture.Anchor = allSidesAnchorStyle;
            fontPicture.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(fontPicture);
            layoutPanel.SetRow(fontPicture, 3);
            layoutPanel.SetColumn(fontPicture, 0);
            layoutPanel.SetColumnSpan(fontPicture, 2);

            fileButton.FontFileSelected += (object sender, string fontFName) =>
            {
                Console.WriteLine($"Font: {fontFName}");
                fontFileName = fontFName;
                fontNameLabel.Text = fontFileName;
                fontNameLabel.Refresh();
                fontFile = FontProcessing.ReadFont(fontFName);
                fontPicture.Refresh();
                // Set output directory as well, if it is not already set
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.GetDirectoryName(fontFName);
                    Console.WriteLine($"Output directory: {outputDirectory}");
                    outDirLabel.Text = outputDirectory;
                }
            };
            outDirSelectButton.DirectorySelected += (object sender, string outDir) =>
            {
                Console.WriteLine($"Output directory: {outDir}");
                outDirLabel.Text = outDir;
                outDirLabel.Refresh();
                outputDirectory = outDir;
            };
            convertButton.Click += (object sender, EventArgs e) => {
                if (outputDirectory != null && fontFile != null && fontFileName != null)
                {
                    FontProcessing.ExportFont(fontFileName, fontFile, outputDirectory);
                }
            };

            fontPicture.Paint += FontPicture_Paint;

            mainWindow.Controls.Add(layoutPanel);
        }

        /*
        void CodeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            FontProcessing.codePage = encodings[codeList.SelectedIndex];
            codepagePicture.Refresh();
        }
        */

        void FontPicture_Paint(object sender, PaintEventArgs e)
        {
            if (fontFile == null) return;
            FontProcessing.DrawFontOn(fontFile, e.Graphics, e.ClipRectangle);
        }

        /*
        void CodepagePicture_Paint(object sender, PaintEventArgs e)
        {
            if (codepageFont == null) return;
            byte[] bytes = new byte[256 - MinCharByte];
            for (int i = MinCharByte; i < 256; i++)
            {
                bytes[i - MinCharByte] = (byte)i;
            }
            StringBuilder codePage = new StringBuilder(FontProcessing.codePage.GetString(bytes));
            Brush brush = new SolidBrush(Color.Black);
            PointF point = new PointF(0, 0);
            // 8 characters each line
            for (int i = 256 - 8; i >= 0; i -= 8)
            {
                codePage.Insert(i, "\r\n");
            }
            try
            {
                e.Graphics.DrawString(codePage.ToString(), codepageFont, brush, point);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not draw string: {ex}");
            }
        }
        */

        /*
        void FontButton_FontSelected(object sender, Font e)
        {
            if (e == null) return;
            codepageFont = e;
            codepagePicture.Refresh();
        }
        */

        public void ShowDialog()
        {
            mainWindow.ShowDialog();
        }
    }
}
