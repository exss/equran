using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;
using System.Reflection;



namespace eQuran {

            
    public class qViewer : Control {

        Font SptFont = new Font("Traditional Arabic", 16);
        SolidBrush dSolidBrush = new SolidBrush(Color.Green);
        SolidBrush fSolidBrush = new SolidBrush(Color.Black);
        public enum ItemState { Over, Selected };
        public enum ViewModeFlags { SingleLine, MultiLine };
     
        
        public delegate void ItemClickEvent(string ItemID);                
        public event ItemClickEvent ItemClick;

        public delegate void SouraClickEvent(Soura rSoura);
        public delegate void AyaClickEvent(Aya rAya);
        public event SouraClickEvent SouraSoundClick;
        public event AyaClickEvent AyaSoundClick;

        List<Aya> AyaList = new List<Aya>();
        List<string> vLines = new List<string>();
        VScrollBar fsbar;
       
        ViewModeFlags fViewMode = ViewModeFlags.SingleLine;

        int fHeaderTop;
        int fLineHeight = 33;
        int fBegin = 0;
        int fItemOver, fItemSelected = -1;
        int fItemOverOld = -1;
        int fHeaderHeight = 75;
        string fSelectedSoura;
        AyaID[] fQuranParts = new AyaID[30];
        int fTopAya;
        int fCurrentPart;
        Soura cnSoura = new Soura();
        Rectangle ContentRect;
        Font fontPart;

        XmlDocument fdata = new XmlDocument();
        StringFormat FlagRL = new StringFormat(StringFormatFlags.DirectionRightToLeft);
        Bitmap bborder = eQuran.Properties.Resources.border_soura;
        Bitmap bsound = eQuran.Properties.Resources.sound;

        /* control border fields */
        Bitmap bTopLeft; Bitmap bTopRight;
        Bitmap bBottomLeft; Bitmap bBottomRight;
        Bitmap bLeftTile; Bitmap bRightTile;
        Bitmap bTopTile; Bitmap bBottomTile;
        TextureBrush xTopTile; TextureBrush xLeftTile;
        TextureBrush xBottomTile; TextureBrush xRightTile;
        Rectangle[] sRects = new Rectangle[8];

        private static readonly string[] fNumbers =
        {
            "الأول", "الثاني", "الثالث" , "الرابع" , "الخامس" , "السادس" , 
            "السابع" , "الثامن" , "التاسع" , "العاشر" , "الحادي عشر" ,"الثاني عشر" ,
            "الثالث عشر" ,"الرابع عشر" , "الخامس عشر", "السادس عشر" ,"السابع عشر","الثامن عشر",
            "التاسع عشر","العشرون","الحادي والعشرون","الثاني والعشرون","الثالث والعشرون","الرابع والعشرون",
            "الخامس والعشرون","السادس والعشرون","السابع والعشرون","الثامن والعشرون","التاسع والعشرون","الثلاثون"
        };
        
        public qViewer() {
            
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            
            Font = new Font("Arial", 16);
            fsbar = new VScrollBar();

            BackColor = Color.White;
            
            /* Initalize data used for drawing the control border */
            Stream xBorder = Assembly.GetExecutingAssembly().GetManifestResourceStream("eQuran.Resources.border.xml");
            Bitmap b = eQuran.Properties.Resources.border;
            XmlTextReader fdata = new XmlTextReader(xBorder);
            
            int i = 0;
            fdata.ReadToFollowing("QuranBorder");
            do {
                if (fdata.Name == "part") {
                    sRects[i].X = int.Parse(fdata.GetAttribute(0));
                    sRects[i].Y = int.Parse(fdata.GetAttribute(1));
                    sRects[i].Width = int.Parse(fdata.GetAttribute(2));
                    sRects[i].Height = int.Parse(fdata.GetAttribute(3));
                    i++;
                    fdata.Read();
                }
                fdata.Read();
            } while (i < 8);

            PixelFormat p = b.PixelFormat;
            bTopLeft = b.Clone(sRects[0], p);
            bTopTile = b.Clone(sRects[1], p);
            bTopRight = b.Clone(sRects[2], p);
            bRightTile = b.Clone(sRects[3], p);
            bBottomRight = b.Clone(sRects[4], p);
            bBottomTile = b.Clone(sRects[5], p);
            bBottomLeft = b.Clone(sRects[6], p);
            bLeftTile = b.Clone(sRects[7], p);
            xTopTile = new TextureBrush(bTopTile);
            xBottomTile = new TextureBrush(bBottomTile);
            xLeftTile = new TextureBrush(bLeftTile);
            xRightTile = new TextureBrush(bRightTile);

            fdata.Close(); xBorder.Close();
            b.Dispose();

            /* ContentRect represents the control area minus the border */
            fHeaderTop = bTopTile.Height+ 5 ;
            ContentRect = new Rectangle();

            Controls.Add(fsbar);
            /* I used here the Scroll event instead of the on ValueChanged
             * because i want to respond only to the user input and not program-
                atically*/
            fsbar.Scroll += new ScrollEventHandler(VBar_ScrollEvent);                    

        }




        public ViewModeFlags ViewMode {
            get {
                return fViewMode;	// return the value from privte field.
            }
            set {
                fViewMode = value;	// save value into private field.
                fsbar.Value = 0;
                UpdateView();
            }
        }

        public override Font Font {
            get {
                return base.Font;
            }
            set {
                base.Font = value;
                fLineHeight = (int)Font.GetHeight() + 14;
                fontPart = new Font(Font.FontFamily, Font.Size - 4);
                UpdateView();
            }
        }

        public string SelectedSoura {
            get {
                return fSelectedSoura;
            }
            set {
                fSelectedSoura = value;
                ShowSoura(value);
            }
        }

        public Aya SelectedAya {
            get {
                if (fItemSelected == -1) return null;
                else return AyaList[fItemSelected]; 
            }
            set {
                //fItemSelected = value;
                //if (value != -1) ItemClick(fItemSelected.ToString());
            }
        }
               
        public int SelectedIndex {
            get {
                return fItemSelected;
            }
            set {
                if (fItemSelected != -1) { //Invalidate Old Selection
                    InvalidateAya(fItemSelected);
                }
                
                fItemSelected = value;
                if (fItemSelected != -1) {
                    InvalidateAya(value);
                }
                
                if (value != -1) ItemClick(fItemSelected.ToString());
            }

        }

        public int ScrollPosition {
            get {
                return fsbar.Value;
            }
            set {
                try {
                    fsbar.Value = value;
                    VBar_ScrollEvent(this, null);
                }
                catch (System.ArgumentOutOfRangeException) {
                    fsbar.Value = 0;
                    VBar_ScrollEvent(this, null);
                }                
            }

        }

        protected override void OnSizeChanged(EventArgs e) {
            UpdateContentRect();
            UpdateView();
            fsbar.Left = ContentRect.Left - fsbar.Width;
            fsbar.Top = ContentRect.Top;
            fsbar.Height = ContentRect.Height;
            base.OnSizeChanged(e);
        }
       
        
        protected override void OnMouseUp(MouseEventArgs e) {

            if (fsbar.Visible) fsbar.Focus(); else Focus();

            if (fItemOver != -1 && ItemClick != null){

                if (GetSoundArea(e.Location, AyaList[fItemOver].DrawArea)) {

                    if (e.Location.Y <= fHeaderTop + fLineHeight) {
                        if (SouraSoundClick != null) SouraSoundClick(cnSoura);
                    }
                    else {
                        if (AyaSoundClick != null) AyaSoundClick(AyaList[fItemOver]);
                    }
                    return;
                }

                    if (fItemSelected != -1) { //Invalidate Old Selection
                        InvalidateAya(fItemSelected);
                    }
                    fItemSelected = fItemOver;
                    InvalidateAya(fItemOver);
                    
                    // don't raise events for soura header, for now
                    if (fItemSelected > 0) ItemClick(fItemOver.ToString());
            }
            
            base.OnMouseUp(e);
        }


        protected override void OnMouseMove(MouseEventArgs e) {
            bool rfound = false;
            bool rsameItem = false;
            
            for (int i = 0; i < AyaList.Count; i++) { // was 1 , 6 July 2007

                if (AyaList[i].DrawArea.IsVisible(e.X, e.Y)) {
                    if (i == fItemOverOld) {                        
                        rsameItem = true;
                        break;
                    }
                    if (fItemOver != -1) { //Invalidate Old Selection
                        InvalidateAya(fItemOver);
                    }
                    InvalidateAya(i);
                    fItemOver = i;
                    fItemOverOld = fItemOver;
                    rfound = true;
                    break; 
                }
            }

            if (rsameItem) return;

            if (rfound == false) { //No Selection was Found
                if (fItemOver != -1) { //Invalidate Old Selection
                    InvalidateAya(fItemOver);
                }
                fItemOver = -1;
                fItemOverOld = fItemOver;
            }

            base.OnMouseMove(e);
        }


        private void VBar_ScrollEvent(object sender, ScrollEventArgs e) {

            if (!fsbar.Focused) fsbar.Focus();
            fBegin = fBegin - fsbar.Value;

            Matrix mTranslate = new Matrix();
            PointF rpoint;
            mTranslate.Translate(0, fBegin * fLineHeight);

            for (int i = 0; i < AyaList.Count; i++) {
                AyaList[i].DrawArea.Transform(mTranslate);
                rpoint = AyaList[i].DrawArea.GetLastPoint();
                if ((rpoint.Y >= ContentRect.Top) && (rpoint.Y <= (ContentRect.Top + fLineHeight))) {
                    fTopAya = i - 1;
                }
            }

            Console.WriteLine(fTopAya);
            fBegin = fsbar.Value;
            Invalidate(ContentRect);        
        
        
        
        }                
        
        protected override void OnPaint(PaintEventArgs pe) {

            Graphics g = pe.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            
            /* Draw Borders */
            DrawBorder(pe.Graphics);            
            DrawSouraBorder(pe.Graphics);

            if ((fdata == null)||(AyaList.Count == 0)) { 
                base.OnPaint(pe); 
                return; 
            }
            
            /* Draw Soura Name */
            g.DrawString(AyaList[0].data, Font, fSolidBrush, 
                new Point((int)((ClientSize.Width / 2) + (g.MeasureString(AyaList[0].data,Font).Width / 2)),
                            fHeaderTop +  (fLineHeight/2)),FlagRL);            
            /* Draw Part Number */
            string rpart = "الجزء " + fNumbers[fCurrentPart];
            g.DrawString( rpart, fontPart, fSolidBrush,
                new Point((int)(ContentRect.Left + (g.MeasureString(rpart, fontPart).Width)),
                            fHeaderTop + (fLineHeight / 2)), FlagRL);            

            /* Draw Soura Ayas Number */
            //Font x = new Font(Font.FontFamily, Font.Size / 2);
            //g.DrawString(AyaList.Count.ToString(), x, fSolidBrush,
            //    new Point((int)(ContentRect.Left  + (g.MeasureString(AyaList.Count.ToString(), Font).Width / 2)),
            //               ContentRect.Top + 22), FlagRL);       
     

            /* Draw Ayas pregenerated in vLines */            
            int lcount = (ContentRect.Height) / fLineHeight;
            lcount = lcount + fsbar.Value ;
            for (int i = fsbar.Value; i < lcount; i++) {
                if (i >= vLines.Count) break;
                g.DrawString(vLines[i], Font, fSolidBrush,
                    new Point(ContentRect.Right ,
                              ContentRect.Top + (i * fLineHeight) - (fsbar.Value * fLineHeight)
                              ), 
                    FlagRL);
            }
            
            /* Draw Aya Selected Effect */
            if (fItemSelected != -1)
                DrawSelectedEffect(fItemSelected, g, ItemState.Selected);            
            
            /* Draw Aya Over Effect */
            if (fItemOver != -1)
                DrawSelectedEffect(fItemOver, g, ItemState.Over);
            
            
            base.OnPaint(pe);
        }
        
        public void UpdateView() {
            if (AyaList.Count == 0) return;

            //int valBar = fsbar.Value;
            Graphics g = CreateGraphics();
            vLines.Clear();
            
            /* Calculate header area */
            CalculateHeaderArea(g);
            
            /* Calculate each Aya location and area */
            for (int i = 1; i < AyaList.Count; i++) CalculateAyaArea(i, g);
                        
            /* Calculate scrollable area and adjust scrollbar*/
            int fVisibleCount = (ContentRect.Height - fHeaderHeight) / fLineHeight;
            if (vLines.Count <= fVisibleCount) {
                fsbar.Value = 0;
                fsbar.Visible = false;
            }
            else {
                fsbar.Visible = true;
                fsbar.Minimum = 0;
                fsbar.Maximum = vLines.Count; // workaround: +2
                fsbar.SmallChange = 1;
                fsbar.Value = 0;//(valBar < fsbar.Maximum) ? valBar : 0;
            }            

            g.Dispose();
            Invalidate();
        }

        private void UpdateContentRect() {
            int barWidth = fsbar.Width;

            ContentRect.X = bLeftTile.Width + barWidth;
            ContentRect.Y = bTopLeft.Height + fHeaderHeight;
            ContentRect.Width = Width - bLeftTile.Width - bRightTile.Width - barWidth - 5;
            ContentRect.Height = Height - ContentRect.Y - bBottomTile.Height;

        }

        /* Draw only the border around the soura name*/
        private void DrawSouraBorder(Graphics g) {

            int len = (int)((ContentRect.Width - 86) / 53) + 1;
            for (int i = 0; i < len; i++) {
                // Draw Top Tiles
                g.DrawImage(bborder,
                            new Rectangle(bLeftTile.Width + 43 + (i * 53), fHeaderTop, 53, 20),
                            new Rectangle(50, 0, 53, 20),
                            GraphicsUnit.Pixel);

                // Draw Bottom Tiles
                g.DrawImage(bborder,
                            new Rectangle(bLeftTile.Width + 43 + (i * 53), fHeaderTop + 49, 53, 20),
                            new Rectangle(50, 49, 53, 20),
                            GraphicsUnit.Pixel);
            }

            g.DrawImage(bborder,
                        new Rectangle(bLeftTile.Width + 4, fHeaderTop, 43, 69),
                        new Rectangle(0, 0, 43, 69),
                        GraphicsUnit.Pixel);
            g.DrawImage(bborder,
                        new Rectangle(ClientRectangle.Width - bRightTile.Width - 43, fHeaderTop, 43, 69),
                        new Rectangle(120, 0, 43, 69),
                        GraphicsUnit.Pixel);

        }

        /* Draw the border around the whole control */
        private void DrawBorder(Graphics g) {

            g.DrawImage(bTopLeft, sRects[0]);
            g.DrawImage(bTopRight, Width - sRects[2].Width, sRects[2].Top, sRects[2].Width, sRects[2].Height);
            g.DrawImage(bBottomRight, Width - sRects[4].Width, Height - sRects[4].Height, sRects[4].Width, sRects[4].Height);
            g.DrawImage(bBottomLeft, sRects[6].X, Height - sRects[6].Height, sRects[6].Width, sRects[6].Height);

            xRightTile = new TextureBrush(bRightTile);
            xRightTile.TranslateTransform(Width - sRects[3].Width, 0);

            xBottomTile = new TextureBrush(bBottomTile);
            xBottomTile.TranslateTransform(0, Height - sRects[5].Height);

            g.FillRectangle(xTopTile, sRects[0].Right, 0, Width - (sRects[0].Width * 2), sRects[1].Height);
            g.FillRectangle(xRightTile, Width - sRects[3].Width, sRects[2].Bottom, sRects[3].Width, Height - (sRects[2].Width * 2));
            g.FillRectangle(xBottomTile, sRects[7].Width, Height - sRects[5].Height, Width - (sRects[5].Width * 2), sRects[6].Height);
            g.FillRectangle(xLeftTile, sRects[7].Left, sRects[0].Bottom, sRects[7].Width, Height - (sRects[0].Height * 2));

            g.DrawRectangle(new Pen(Color.FromKnownColor(KnownColor.ActiveCaption)), 0, 0, Width - 1, Height - 1);


        }

        /* Draw the effect around a selected aya*/
        private void DrawSelectedEffect(int AyaNumber, Graphics g, ItemState EffectType) {

            PointF re;
            re = AyaList[AyaNumber].DrawArea.PathPoints[AyaList[AyaNumber].DrawArea.PathPoints.Length - 4];

            Pen sPen = new Pen(Color.FromArgb(26, 54, 26));
            Rectangle sRect = Rectangle.Round(AyaList[AyaNumber].DrawArea.GetBounds());
            LinearGradientBrush sBrush = (EffectType == ItemState.Over) ?
                new LinearGradientBrush(sRect, Color.FromArgb(60, 34, 85, 51), Color.FromArgb(75, 48, 85, 51), LinearGradientMode.Vertical)
              : new LinearGradientBrush(sRect, Color.FromArgb(90, 34, 85, 51), Color.FromArgb(90, 48, 85, 51), LinearGradientMode.Vertical);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (AyaNumber == 0) g.SetClip(ClientRectangle);
            else g.SetClip(ContentRect);
            g.DrawImage(bsound, re.X + 5, re.Y, bsound.Width, bsound.Height);
            g.DrawPath(sPen, AyaList[AyaNumber].DrawArea);
            g.FillPath(sBrush, AyaList[AyaNumber].DrawArea);

            g.SmoothingMode = SmoothingMode.Default;

        }

        private void CalculateHeaderArea(Graphics g) {

            const int radius = 5;
            float textLen, fromX, toX, fromY;

            textLen = g.MeasureString(AyaList[0].data, Font).Width;
            fromX = (ClientSize.Width / 2) + (textLen / 2);
            toX = fromX - textLen - bsound.Width - 10;
            fromY = fHeaderTop + (fLineHeight/2);

            AyaList[0].DrawArea.Reset();
            AyaList[0].DrawArea.StartFigure();
            AyaList[0].DrawArea.AddArc(fromX - (radius * 2), fromY, radius * 2, radius * 2, 270, 90);
            AyaList[0].DrawArea.AddArc(fromX - (radius * 2), fromY + fLineHeight - (radius * 2), radius * 2, radius * 2, 0, 90);
            AyaList[0].DrawArea.AddArc(toX, fromY + fLineHeight - (radius * 2), radius * 2, radius * 2, 90, 90);
            AyaList[0].DrawArea.AddArc(toX, fromY, radius * 2, radius * 2, 180, 90);
            AyaList[0].DrawArea.CloseFigure();

        }
        
        private void CalculateAyaArea(int c, Graphics g) {

            /* Fields responsible for the Aya Area */
            const int radius = 5;
            int fromX = 0, fromY = 0, toX = 0;

            int textLen = 0, rspace = 0;
            String[] rdata;
            StringBuilder rtext = new StringBuilder("");
            bool addedNewLine = false;
            bool OpenedBracket = false;
            string rstr;

            AyaList[c].LineCount = 1;
            AyaList[c].DrawArea.Reset();           
            textLen = (int)(g.MeasureString(AyaList[c].data, Font).Width +
                         g.MeasureString(ConvertSep(c), Font).Width);

            if (c == 1) { // if this is the first aya
                fromX = ContentRect.Right;
                fromY = ContentRect.Top ;
                rspace = ContentRect.Width;
                vLines.Add(""); // Add a new line
            }
            else {
                fromX = AyaList[c - 1].Column ;
                fromY = AyaList[c - 1].Line;
                
                if (fViewMode == ViewModeFlags.SingleLine) {
                    fromX = ContentRect.Right;
                    fromY += fLineHeight; // We always start from in a new line
                    rspace = ContentRect.Width;
                    vLines.Add("");
                }
                else if (fViewMode == ViewModeFlags.MultiLine) rspace = AyaList[c - 1].Column - ContentRect.Left;
            }
            
            /* Check if the Aya can fit on a single line or not */
            rstr = AyaList[c].data + " " + ConvertSep(c);
            if (textLen < rspace) {
                rdata = new string[1];
                rdata[0] = rstr ;
            }
            else rdata = rstr.Split(' ');


            for (int i = 0; i < rdata.Length; i++) {
                
                textLen = (int)g.MeasureString(rtext.ToString() + rdata[i], Font).Width;                
                if (textLen > rspace) {
                    // we are out of space, start a new line
                    if (vLines.Count == 0) vLines.Add("");                  
                    vLines[vLines.Count- 1] = vLines[vLines.Count-1]  + rtext.ToString();
                    vLines.Add("");
                    rtext.Remove(0, rtext.Length); //clear text
                    rtext.Append(rdata[i] + " ");
                    rspace = ContentRect.Width ;
                    addedNewLine = true;

                    if ((AyaList[c].LineCount == 1) && (!OpenedBracket)) {
                        /*  Workaround to handle when view is multiline, when the difference between 2 verses 
                         * is 2 lines although it's only really one line, as the new line start from the beginning */
                        addedNewLine = false;
                        fromY += fLineHeight;
                        fromX = ContentRect.Right;
                    }
                    else if (AyaList[c].LineCount == 1){ // Case A: We are still on the first line, close it and continue
                        AyaList[c].DrawArea.AddLine(ContentRect.Left, fromY + fLineHeight, ContentRect.Left, fromY);
                        AyaList[c].DrawArea.CloseFigure();
                    }
                    else  if (AyaList[c].LineCount > 1){ // Case B: We are on a middle line, surround it with a rectangle and continue
                        AyaList[c].DrawArea.StartFigure();
                        AyaList[c].DrawArea.AddRectangle(new RectangleF(ContentRect.Left, fromY + (fLineHeight * AyaList[c].LineCount) - fLineHeight, ContentRect.Width, fLineHeight));
                        AyaList[c].DrawArea.CloseFigure();
                    }
                    if (i > 0) AyaList[c].LineCount++;

                }
                else {
                    // There is still space in the same line, add another word
                    rtext.Append(rdata[i] + " ");
                }
                // if this is the first word, open the bracket
                if (!OpenedBracket) {
                    /* Draw the opening of the effect */
                    AyaList[c].DrawArea.StartFigure();
                    AyaList[c].DrawArea.AddArc(fromX - (radius * 2) - 2, fromY, radius * 2, radius * 2, 270, 90);
                    AyaList[c].DrawArea.AddArc(fromX - (radius * 2) - 2, fromY + fLineHeight - (radius * 2), radius * 2, radius * 2, 0, 90);
                    OpenedBracket = true;
                }
            }


            
            /* Deal with any text left at the end */
            if (addedNewLine) {
                // Some text left at the end, draw on a new line                
                vLines[vLines.Count -1] = rtext.ToString();
            }
            else {
                // The Aya fitted on only one line, draw on the same line of the previous verse
                vLines[vLines.Count - 1] += rtext.ToString();
                //vLines.Add("");
            }


            /* Draw the closure of the effect */
            textLen = (int)g.MeasureString(vLines[vLines.Count - 1], Font).Width;
            fromY = fromY + (fLineHeight * AyaList[c].LineCount) - fLineHeight;
            toX = ContentRect.Right - textLen;
            if (addedNewLine) {
                AyaList[c].DrawArea.StartFigure();
                AyaList[c].DrawArea.AddLine(ContentRect.Right - 2, fromY , ContentRect.Right - 2, fromY + fLineHeight);
            }
            AyaList[c].DrawArea.AddArc(toX , fromY + fLineHeight - (radius * 2), radius * 2, radius * 2, 90, 90);
            AyaList[c].DrawArea.AddArc(toX , fromY, radius * 2, radius * 2, 180, 90);
            AyaList[c].DrawArea.CloseFigure();

            /* Update all Aya's fields*/
            AyaList[c].Line = fromY;
            AyaList[c].Column = toX;
            
        }

        /* Outputs the number surrounded by two brackets(fassla) */
        private string ConvertSep(int rnumber) {            
           StringBuilder r = new StringBuilder(rnumber.ToString());

           for (int i = 0; i < r.Length; i++) {
               r[i] = (char)(r[i] + 0x630);
           }
            
           return  ("\ufd3f" + r.ToString() + "\ufd3e");
        }

        private void ShowSoura(string Name) {
            string aID;
            Aya rAya;

            AyaList.Clear();
            fItemOver = -1; fItemSelected = -1;
            
            XmlNode nodeSoura = fdata.SelectSingleNode("//SOURA[@name='" + Name + "']");
            if (nodeSoura == null) return;

            cnSoura.Clear();
            cnSoura.Name = Name;
            cnSoura.AyasCount = nodeSoura.ChildNodes.Count;
            cnSoura.Number = int.Parse(nodeSoura.Attributes[0].Value);

            AyaList.Add(new Aya(nodeSoura.Attributes[0].Value, Name)); // soura header has an id of -1
            for (int i = 0; i < nodeSoura.ChildNodes.Count; i++) {
                aID = nodeSoura.ChildNodes[i].Attributes[0].Value;
                rAya = new Aya(aID, nodeSoura.ChildNodes[i].InnerText);
                rAya.ParentSoura = cnSoura;

                cnSoura.Ayas.Add(rAya);
                AyaList.Add(rAya);
            }

            
            for (int i = 0; i < fQuranParts.Length; i++) {
                if (fQuranParts[i].Soura <= cnSoura.Number) fCurrentPart = i;
            }

            //fsbar.Value = 0;
            UpdateView();

        }

        /* Load the whole quran */
        public void LoadXmlFile(string re) {

            fdata.Load(re);

        }

        /* Loaf the parts of the quran */
        public void LoadQuranParts(string Path) {

            XmlDocument partsFile;
            XmlNodeList partsList;
            string r;

            partsFile = new XmlDocument();
            partsFile.Load(Path);
            partsList = partsFile.SelectNodes("//part");

            for (int i = 0; i < partsList.Count; i++) {
                r = partsList[i].Attributes["startFrom"].Value;
                fQuranParts[i].Soura = byte.Parse(r.Substring(0, 3));
                fQuranParts[i].Aya = byte.Parse(r.Substring(4, 2));
            }

        }

        private bool GetSoundArea(Point m, GraphicsPath gp) {
            PointF re;
            re = gp.PathPoints[gp.PathPoints.Length - 4];


            if (m.X > re.X && m.X < re.X + bsound.Width &&
                 m.Y > re.Y && m.Y < re.Y + fLineHeight)
                return true;
            else return false;
        }

        private void InvalidateAya(int re) {

            Rectangle r;
           
            r = Rectangle.Round(AyaList[re].DrawArea.GetBounds()); 
            r.Inflate(2, 2);
            Invalidate(r);
        }


 }

    public class Soura {
        public int AyasCount = 0;
        public int Part = 0; // for el 2agza2, to be used in future
        public int Number;
        public string Name = "";
        public List<Aya> Ayas = new List<Aya>();

        public Soura() {
        }

        public Soura(string rName) {
            Name = rName;
        }

        public Soura(string rName, int rAyasCount, int rNumber,int rPart ) {
            Name = rName;
            AyasCount = rAyasCount;
            Number = rNumber;
            Part = rPart;
        }

        public void Clear() {
            Ayas.Clear();
            Name = "";
            AyasCount = 0; Part = 0; Number = 0;            
        }
    }
    
    public class Aya {
        public int LineCount = 1;
        public int Line = 0; // Line represents when last line in the ends(TopBound)
        public int Column = 0; //Column represent where the aya ENDS
        public string ID;
        public string data;
        public Soura ParentSoura;
        public GraphicsPath DrawArea;

        public Aya(string rID, string rdata) {
            ID = rID;
            data = rdata;
            DrawArea = new GraphicsPath();
            
        }

    }

    struct AyaID {
        public byte Soura;
        public byte Aya;
        
    }

}
